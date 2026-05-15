---
title: Herb Farming System
doc_type: techdesign-spec
status: draft
owner: techdesign
created_at: 2026-05-14
updated_at: 2026-05-14
source_design_docs:
  - docs/game-design-wp/requirements/herb-farming-system.md
  - docs/game-design-wp/features/herb-farming-system.md
related_shared_rules:
  - docs/game-design-wp/shared-rules.md
code_grounding:
  - GameServer/Services/HerbService.cs
  - GameServer/Services/ItemService.cs
  - GameServer/Services/AlchemyService.cs
  - GameServer/Entities/PlayerHerbEntity.cs
  - GameServer/Entities/PlayerGardenPlotEntity.cs
  - GameServer/Entities/PlayerSoilEntity.cs
  - GameServer/Entities/PlayerItemEntity.cs
  - GameServer/Runtime/AlchemySystemTypes.cs
  - GameServer/Runtime/NotificationSystemTypes.cs
  - GameShared/Packets/Packets/CharacterPackets.cs
  - GameShared/Messages/MessageCode.cs
  - database/initDatabase.sql
---

# Herb Farming System — Tech Design Spec

## Goal

Implement the two-step herb lifecycle (harvest → living herb entity → extract → linh dược items), wire client-facing network handlers for all garden actions, add background offline expiry sweep for living herbs, and migrate alchemy recipe validation away from the legacy `required_herb_maturity` guard.

## Source Design Summary

- Player inserts linh thổ into plot → plants seed or mầm non → herb grows in real time offline → harvest at `mature` or `thousand_year` → herb entity moves to inventory with expiry timestamp → player extracts → consumes herb entity → receives linh dược items + optional mầm non return.
- `thousand_year` is terminal while planted; does not auto-expire.
- Linh thổ has a finite active lifetime; expiry pauses herb growth and starts herb survival countdown.
- Each phẩm cấp of linh dược is a distinct item template.
- Harvest and extract are rejected entirely if inventory is full (no inbox fallback for these two actions).
- Herb drop overflow from quái uses shared inbox rule.
- Living herb expiry while offline: server deletes herb entity silently on next access or background sweep; no spoiled-item state; no notification.
- `required_herb_maturity` guard in `AlchemyService` must be removed; recipe validation uses item template identity going forward.

## Scope

### In Scope
- Add `young` growth stage and rename/keep `perfect` → `thousand_year` enum value.
- Add `expire_at` to `player_herbs` for inventory living herb expiry.
- Refactor `HarvestHerbAsync` to two-step: `HarvestAsync` (plot → inventory entity) and `ExtractHerbAsync` (entity → item outputs).
- Background sweep service to delete expired living herb entities.
- Inventory capacity pre-check before harvest and extract (reject if would exceed cap; no cap enforced yet — see note).
- Six new network handlers for garden interactions.
- Remove `required_herb_maturity` guard from `AlchemyService`.
- New packet types and MessageCode entries.
- DB migrations: `player_herbs.expire_at`, updated enum constants, growth stage config rows.

### Out Of Scope
- Inventory slot cap system — deferred to future design. Harvest/extract inventory-full check is a placeholder `false` (never full) until cap system exists.
- Blueprint-grade-driven plot count — current fixed config is kept as phase placeholder.
- Wild herb nodes.
- Balance values (timers, drop rates, yields).
- Client UI layout.
- Herb drop from quái (EnemyRewardRuntimeService wiring) — separate slice.

## Code Grounding Summary

| Area | Findings |
|---|---|
| `HerbService` | Full service exists: `InsertSoilAsync`, `PlantSeedAsync`, `PlantExistingHerbAsync`, `MoveHerbToInventoryAsync`, `HarvestHerbAsync`, `GetHerbRuntimeStateAsync`. `HarvestHerbAsync` currently resolves output items directly (harvest + extract combined) — must be refactored. |
| `PlayerHerbEntity` | Has `CurrentStage`, `AccumulatedGrowthSeconds`, `State` (Planting / InInventory), `PlantedAt`, `CurrentPlotId`. Missing `expire_at` for inventory decay. |
| `HerbGrowthStage` enum | `Seedling=1, Mature=2, Perfect=3` — only 3 stages; `Young` and `ThousandYear` missing. |
| `PlayerItemEntity` | Has `expire_at` field, used for consumables. |
| `ItemService.AddItemAsync` | No inventory capacity check — adds freely. |
| `AlchemyService` | Hard guard: recipes with `required_herb_maturity != None` → fail immediately. No data currently uses this field with non-zero value — safe to remove guard. |
| `PlayerNotificationService` | Push notification infrastructure exists. Used for practice results and lifespan expiry. Herb expiry does not send notification (silent delete per design). |
| Packet pattern | `[Packet(N)]` attribute, `partial class XxxPacket : IPacket`, `IPacketHandler<TPacket>`. Highest existing ID is ~102. New herb packets will use IDs 200–220. |
| MessageCode | Highest code block is `5011`. New herb codes will use range `6000–6020`. |
| Background sweep | No hosted service or background sweep exists currently. Must add. |
| Plot count | Single global config `character.home_garden_plot_count = 8`. No blueprint-grade table. Kept as-is for this phase. |

## Current System Fit

- `HerbService` is the correct service layer. All new operations go here.
- New handlers follow the `IPacketHandler<TPacket>` pattern in `GameServer/Network/Handlers/`.
- New packets go in a new file `GameShared/Packets/Packets/HerbPackets.cs`.
- Background sweep is a new `IHostedService` in `GameServer/Runtime/`.
- Migration SQL goes in `database/migrations/`.

---

## DB / Schema Plan

### Canonical Data Model

#### Tables And Field Semantics

| Table | Field | Type | Meaning | Required | Source of truth | Notes |
|---|---|---|---|---|---|---|
| `herb_templates` | `item_template_id` | int PK/FK | Base item template identity for the herb family | yes | DB/init data | One herb family per template |
| `herb_templates` | `seed_item_template_id` | int FK | Inventory item used to plant this herb from seed | yes | DB/init data | Must be `ItemType.HerbSeed` |
| `herb_templates` | `survival_seconds_without_soil` | long | Time herb can survive after soil expires before terminal loss | yes | DB/init data | Used while planted only |
| `herb_templates` | `inventory_expiry_seconds` | long | Time harvested living herb can survive in inventory before deletion | yes | DB/init data | Used to compute `player_herbs.expire_at` |
| `herb_templates` | `replant_item_template_id` | int? FK | Mầm non / replant item returned on successful extract chance | no | DB/init data | Can be null if no replant output |
| `herb_growth_stage_configs` | `herb_template_id` | int FK | Herb family this stage config belongs to | yes | DB/init data | Composite identity with stage |
| `herb_growth_stage_configs` | `stage` | int | Growth stage code for this threshold row | yes | DB/init data | See enum contract below |
| `herb_growth_stage_configs` | `required_growth_seconds` | long | Total accumulated growth seconds needed to reach this stage | yes | DB/init data | Progression order comes from threshold values |
| `player_garden_plots` | `id` | long PK | Unique plot row | yes | DB/runtime | One row per player-owned plot |
| `player_garden_plots` | `player_cave_id` | long FK | Cave that owns the plot | yes | DB/runtime | Plot ownership comes from cave ownership |
| `player_garden_plots` | `plot_index` | int | Stable slot index inside the cave garden | yes | DB/runtime | Used for UI ordering |
| `player_garden_plots` | `current_soil_player_item_id` | long? FK | Soil item currently inserted into this plot | no | DB/runtime | Null = no active soil |
| `player_garden_plots` | `current_player_herb_id` | long? FK | Herb entity currently planted in this plot | no | DB/runtime | Null = empty plot |
| `player_soils` | `player_item_id` | long PK/FK | Soil inventory item instance backing soil state | yes | DB/runtime | Soil remains an item with extra state row |
| `player_soils` | `total_used_seconds` | long | Total active soil usage consumed so far | yes | DB/runtime | Determines remaining soil lifetime |
| `player_soils` | `inserted_plot_id` | long? FK | Plot currently using this soil item | no | DB/runtime | Null = soil still in inventory |
| `player_herbs` | `id` | long PK | Unique living herb entity | yes | DB/runtime | Herb in world or inventory, not regular item |
| `player_herbs` | `player_id` | uuid FK | Owning character | yes | DB/runtime | Character authority |
| `player_herbs` | `herb_template_id` | int FK | Herb family/type | yes | DB/runtime | Links to growth/output config |
| `player_herbs` | `state` | int | Herb lifecycle state | yes | DB/runtime | See state enum contract below |
| `player_herbs` | `current_stage` | int | Current growth maturity of this herb | yes | DB/runtime | See growth stage enum |
| `player_herbs` | `accumulated_growth_seconds` | long | Total effective growth accumulated while planted | yes | DB/runtime | Used with stage thresholds |
| `player_herbs` | `current_plot_id` | long? FK | Plot currently holding the herb | no | DB/runtime | Null once harvested to inventory |
| `player_herbs` | `planted_at` | timestamp? | Last planted timestamp used for offline growth settlement | no | DB/runtime | Null in inventory state |
| `player_herbs` | `expire_at` | timestamp? | Inventory expiry cutoff for harvested living herb | no | DB/runtime | New field in this slice |
| `herb_harvest_outputs` | `herb_template_id` | int FK | Herb family that can yield this output | yes | DB/init data | |
| `herb_harvest_outputs` | `required_stage` | int | Minimum/exact maturity stage used to select output set | yes | DB/init data | Mature vs ThousandYear split |
| `herb_harvest_outputs` | `output_type` | int | Output semantic type | yes | DB/init data | Usually material/replant |
| `herb_harvest_outputs` | `result_item_template_id` | int FK | Item template granted on extract roll success | yes | DB/init data | Distinct phẩm cấp = distinct item template |
| `herb_harvest_outputs` | `quantity` | int | Quantity granted | yes | DB/init data | |
| `herb_harvest_outputs` | `chance_rate` | double | Roll chance for this output | yes | DB/init data | |

#### Enums / Codes / State Values

| Name | Value | Meaning | Used by | Notes |
|---|---|---|---|---|
| `HerbGrowthStage.Seedling` | `1` | Newly planted herb | `player_herbs.current_stage`, growth config | Earliest planted state |
| `HerbGrowthStage.Mature` | `2` | Harvestable normal maturity | `player_herbs.current_stage`, outputs, harvest validation | Produces base linh dược outputs |
| `HerbGrowthStage.ThousandYear` | `3` | Terminal high maturity | `player_herbs.current_stage`, outputs, harvest validation | Rename of old `Perfect` code; DB value unchanged |
| `HerbGrowthStage.Young` | `4` | Intermediate growth stage between Seedling and Mature | `player_herbs.current_stage`, growth config | New in this slice |
| `PlayerHerbState.Planting` | `0` | Herb is planted on a plot | `player_herbs.state` | `current_plot_id` and `planted_at` expected non-null |
| `PlayerHerbState.InInventory` | `1` | Herb is harvested and stored as living herb in inventory | `player_herbs.state` | `expire_at` may be non-null |
| `HerbHarvestOutputType.Material` | runtime enum | Extract grants material item | output resolution | Existing enum in code |
| `HerbHarvestOutputType.Replant` | runtime enum | Extract grants mầm non / replant item | output resolution | Existing enum in code |

#### Relations And Ownership

| From | To | Relation | Ownership / authority | Notes |
|---|---|---|---|---|
| `player_caves.id` | `player_garden_plots.player_cave_id` | 1 → many | Cave owns plots | Garden size currently derived from config |
| `player_garden_plots.current_soil_player_item_id` | `player_items.id` | 0/1 → 1 | Plot references inserted soil item | Soil item remains regular inventory item instance |
| `player_soils.player_item_id` | `player_items.id` | 1 → 1 extension row | Soil runtime metadata owned by Item/Herb services | Extra state for soil depletion |
| `player_garden_plots.current_player_herb_id` | `player_herbs.id` | 0/1 → 1 | Plot references planted herb | Null when plot empty |
| `player_herbs.player_id` | `characters.id` | many → 1 | Character owns living herb entity | Applies in both planted and inventory states |
| `player_herbs.herb_template_id` | `herb_templates.item_template_id` | many → 1 | Herb instance type | Drives config lookup |
| `herb_growth_stage_configs.herb_template_id` | `herb_templates.item_template_id` | many → 1 | Stage thresholds per herb family | |
| `herb_harvest_outputs.herb_template_id` | `herb_templates.item_template_id` | many → 1 | Extract output config per herb family | |

#### State Transitions By Field

| Entity / table | Field(s) | Transition | Trigger | Owner service |
|---|---|---|---|---|
| `player_garden_plots` + `player_soils` | `current_soil_player_item_id`, `inserted_plot_id` | `null → soil item id` | Insert soil into plot | `HerbService.InsertSoilAsync` |
| `player_herbs` + `player_garden_plots` | `state`, `current_stage`, `current_plot_id`, `planted_at`, `current_player_herb_id` | inventory/seed intent → planted herb | Plant seed / plant existing herb | `HerbService.PlantSeedAsync`, `HerbService.PlantExistingHerbAsync` |
| `player_herbs` | `accumulated_growth_seconds`, `current_stage` | growth progress settlement | Read or mutate herb while planted | `HerbService.MaterializeHerbProgressAsync` |
| `player_herbs` + `player_garden_plots` | `state: Planting → InInventory`, `current_plot_id: plot id → null`, `planted_at: ts → null`, `expire_at: null → timestamp`, `current_player_herb_id: herb id → null` | Harvest to inventory | `HarvestHerbPacket` accepted | `HerbService.HarvestAsync` |
| `player_herbs` + `player_items` | delete herb row + add output items | Extract harvested herb | `ExtractHerbPacket` accepted | `HerbService.ExtractHerbAsync` |
| `player_herbs` | delete herb row | Silent expiry cleanup | Background sweep or expired extract access | `HerbExpiryBackgroundService`, `HerbService.ExtractHerbAsync` |
| `player_soils` | `total_used_seconds` | increase while active | Growth settlement on planted herb | `HerbService.MaterializeHerbProgressAsync` |

### Changed Tables

| Table | Change | Reason |
|---|---|---|
| `player_herbs` | Add `expire_at timestamp NULL` | Living herb in inventory needs expiry timestamp |

### New Enum Values

`HerbGrowthStage` — add `Young = 4` (new stage between Seedling and Mature). Rename `Perfect` to `ThousandYear` at code level; DB integer value `3` can stay as-is to avoid migration, just update the C# enum name.

Updated enum:
```
Seedling      = 1
Mature        = 2
ThousandYear  = 3   (was Perfect — DB value unchanged)
Young         = 4   (new)
```

Stage order for progression: `Seedling(1) → Young(4) → Mature(2) → ThousandYear(3)`.
`RequiredGrowthSeconds` in `herb_growth_stage_configs` determines transition order — enum integer value does not need to be sequential.

### New Growth Stage Config Rows

For each existing herb template, add a row for `stage = 4` (Young) in `herb_growth_stage_configs` with a placeholder `required_growth_seconds`. This is seed/init data.

### Migration SQL Files

1. `database/migrations/YYYYMMDD_add_player_herbs_expire_at.sql`
   - `ALTER TABLE player_herbs ADD COLUMN IF NOT EXISTS expire_at timestamp without time zone NULL;`

2. `database/migrations/YYYYMMDD_add_herb_growth_stage_young.sql`
   - Insert `young` (stage = 4) rows into `herb_growth_stage_configs` for all existing herb templates.

### Entity / DAO / Repository Plan

**`PlayerHerbEntity`** — add property:
```csharp
[Column("expire_at")] public DateTime? ExpireAt { get; set; }
```

**`PlayerHerbRepository`** — add method:
```csharp
Task<List<PlayerHerbEntity>> ListExpiredInventoryHerbsAsync(DateTime now, CancellationToken ct);
// WHERE state = InInventory AND expire_at IS NOT NULL AND expire_at <= now
```

**`HerbService`** — changes:
- Rename `HarvestHerbAsync` → `HarvestAsync`: moves herb entity from plot to inventory, sets `expire_at`, does NOT produce item outputs.
- Add `ExtractHerbAsync`: validates herb is InInventory + not expired, resolves output items, optionally returns mầm non item, deletes herb entity.
- Add `IsHerbExpired(PlayerHerbEntity)` helper.
- Add `CheckInventoryHasSpace(Guid playerId)` stub — always returns `true` for now; placeholder for future inventory cap system.

**`HerbExpiryBackgroundService`** (new `IHostedService`) — periodic sweep:
- Interval: configurable, suggest 60 seconds.
- Queries `PlayerHerbRepository.ListExpiredInventoryHerbsAsync`.
- Deletes expired herb entities in batch. No notification sent.

---

## Config / Seed Data Plan

### Config Contracts

| Config key | Purpose | Prototype value |
|---|---|---|
| `character.home_garden_plot_count` | Plot count per player (fixed, placeholder for blueprint-grade system) | `8` |
| `herb.expiry_sweep_interval_seconds` | Background sweep interval | `60` |

### Seed / Init Data

- `herb_growth_stage_configs`: add `Young` stage row per herb template with placeholder `required_growth_seconds`.
- `herb_harvest_outputs`: for each herb template, add rows for `ThousandYear` stage (stage = 3) pointing to the high-tier linh dược item templates. Low-tier outputs remain on `Mature` stage rows.
- Item templates: add high-tier linh dược item templates (ItemType = `HerbMaterial`) for each herb type. Example: `linh_chi_hoa_common` (existing) and `linh_chi_hoa_perfect` (new).

### Local DB Test Setup

```sql
-- Reset a specific herb to InInventory with near-expiry for extract/expiry testing
UPDATE player_herbs SET state = 1, current_plot_id = NULL, expire_at = NOW() + INTERVAL '60 seconds' WHERE id = <herb_id>;

-- Expire a herb immediately for expiry sweep testing
UPDATE player_herbs SET expire_at = NOW() - INTERVAL '1 second' WHERE id = <herb_id>;
```

---

## Packet And Broadcast Flow

### New Packet File
`GameShared/Packets/Packets/HerbPackets.cs`

### Request Packets

| Packet | ID | Sender | Handler | Key validation |
|---|---|---|---|---|
| `GetGardenPlotsPacket` | 200 | Client | `GetGardenPlotsHandler` | Cave owned by player |
| `InsertSoilPacket` | 201 | Client | `InsertSoilHandler` | Item exists + owned + type=Soil; plot owned; no active soil |
| `PlantHerbSeedPacket` | 202 | Client | `PlantHerbSeedHandler` | Item exists + owned + type=HerbSeed; plot has soil; plot empty |
| `PlantExistingHerbPacket` | 203 | Client | `PlantExistingHerbHandler` | Herb exists + owned + state=InInventory; plot has soil; plot empty |
| `HarvestHerbPacket` | 204 | Client | `HarvestHerbHandler` | Herb owned + state=Planting; stage=Mature or ThousandYear; inventory not full |
| `ExtractHerbPacket` | 205 | Client | `ExtractHerbHandler` | Herb owned + state=InInventory + not expired; inventory not full |

### Response Packets

| Packet | ID | When sent | Payload |
|---|---|---|---|
| `GetGardenPlotsResultPacket` | 210 | Reply to GetGardenPlots | `List<GardenPlotStateModel>` — each plot: plot index, soil state, herb state, herb stage, next-stage remaining seconds, soil remaining seconds |
| `InsertSoilResultPacket` | 211 | Reply to InsertSoil | `Success`, `Code`, updated `GardenPlotStateModel` |
| `PlantHerbSeedResultPacket` | 212 | Reply to PlantHerbSeed | `Success`, `Code`, updated `GardenPlotStateModel` |
| `PlantExistingHerbResultPacket` | 213 | Reply to PlantExistingHerb | `Success`, `Code`, updated `GardenPlotStateModel` |
| `HarvestHerbResultPacket` | 214 | Reply to HarvestHerb | `Success`, `Code`, `PlayerHerbId` of new inventory herb, `ExpireAtUnixMs` |
| `ExtractHerbResultPacket` | 215 | Reply to ExtractHerb | `Success`, `Code`, `List<InventoryItemModel>` outputs, `bool MamNonReturned` |

### Broadcast Packets
None required for this phase. Garden is single-player owned; no observers need updates.

### New MessageCode Entries (range 6000–6020)

```csharp
GardenCaveNotFound          = 6000
GardenPlotNotFound          = 6001
GardenPlotNotOwned          = 6002
GardenPlotAlreadyHasSoil    = 6003
GardenPlotNoSoil            = 6004
GardenPlotAlreadyHasHerb    = 6005
GardenPlotNoHerb            = 6006
GardenHerbNotHarvestable    = 6007   // stage below mature
GardenHerbNotInInventory    = 6008
GardenHerbExpired           = 6009
GardenHerbNotOwned          = 6010
GardenInventoryFull         = 6011   // placeholder — currently never triggered
```

---

## Runtime Flow

### Harvest Flow (`HarvestHerbPacket`)
1. Handler receives `HarvestHerbPacket(playerHerbId)`.
2. `HerbService.HarvestAsync`:
   a. Load herb, verify owned, state=Planting.
   b. `MaterializeHerbProgressAsync` — settle growth seconds, update stage.
   c. Verify stage ∈ {Mature, ThousandYear} — else return `GardenHerbNotHarvestable`.
   d. `CheckInventoryHasSpace` — currently always true.
   e. Detach herb from plot (`plot.CurrentPlayerHerbId = null`).
   f. Set `herb.State = InInventory`, `herb.CurrentPlotId = null`, `herb.PlantedAt = null`.
   g. Set `herb.ExpireAt = UtcNow + herb_template.inventory_expiry_seconds`.
   h. Persist herb + plot in transaction.
3. Return `HarvestHerbResultPacket(Success, playerHerbId, expireAtUnixMs)`.

### Extract Flow (`ExtractHerbPacket`)
1. Handler receives `ExtractHerbPacket(playerHerbId)`.
2. `HerbService.ExtractHerbAsync`:
   a. Load herb, verify owned, state=InInventory.
   b. Check `herb.ExpireAt` — if expired, delete herb, return `GardenHerbExpired`.
   c. `CheckInventoryHasSpace` — currently always true.
   d. Resolve output definitions from `herb_harvest_outputs` by `herb_template_id` + `required_stage`.
   e. For each output: roll chance, if pass call `ItemService.AddItemAsync`.
   f. Roll mầm non return chance — if pass, `AddItemAsync` for replant item template.
   g. Delete herb entity.
   h. Commit transaction.
3. Return `ExtractHerbResultPacket(Success, createdItems, mamNonReturned)`.

### Background Expiry Sweep
1. `HerbExpiryBackgroundService` runs every `herb.expiry_sweep_interval_seconds`.
2. `PlayerHerbRepository.ListExpiredInventoryHerbsAsync(DateTime.UtcNow)`.
3. For each expired herb: delete row. No notification sent.
4. Log count of deleted herbs per sweep cycle.

### InsertSoil / PlantSeed / PlantExistingHerb / GetGardenPlots
These map directly to existing `HerbService` methods. Handlers are thin wrappers adding:
- ownership validation
- MessageCode-based error response
- model mapping

---

## Validation And Authority

- **Server authority**: all herb/garden state mutations are server-authoritative. Client sends intent; server validates and persists.
- **Client trust**: zero — no client values are trusted for stage, expiry, or soil state.
- **Ownership**: `RequireOwnedCaveAsync`, `RequireOwnedPlotAsync`, `RequireOwnedHerbAsync` already enforce this.
- **Expiry validation at action time**: expiry is checked lazily when player attempts extract. Background sweep handles offline cleanup.
- **Transaction boundary**: harvest and extract each run in a single DB transaction. Partial failure rolls back.
- **Race condition**: `PlayerInventoryTransactionService` (per-player lock) already used in `ItemService.AddItemAsync`. Extract must wrap item grant + herb delete inside the same transaction.

---

## Persistence Flow

- `HarvestAsync`: writes `player_herbs` (state, expire_at, clear plot fields) + `player_garden_plots` (clear herb ref) in one transaction.
- `ExtractHerbAsync`: writes `player_items` (new items) + deletes `player_herbs` in one transaction.
- `HerbExpiryBackgroundService`: batch delete of expired `player_herbs` rows outside any player transaction.
- Soil state is persisted inside `MaterializeHerbProgressAsync` (already existing behavior — no change needed).

---

## Alchemy Migration

**Remove** the following guard from `AlchemyService.ValidateCraftPillAsync`:
```csharp
// REMOVE THIS BLOCK:
if (recipe.Inputs.Any(static x => x.RequiredHerbMaturity != HerbMaturityRequirement.None))
    return Failed("Recipe co required_herb_maturity, tinh nang nay de phase sau.", recipe, normalizedRequestedCraftCount);
```

No data migration needed — all current `pill_recipe_inputs` rows have `required_herb_maturity = 0` (None).

The `required_herb_maturity` column in DB and `PillRecipeInputDefinition` field may be kept as dormant/deprecated metadata for now; no functional path uses it after guard removal.

---

## Implementation Slices

### Slice 1 — DB migration + entity update
- Migration: add `player_herbs.expire_at`.
- Migration: add Young stage rows to `herb_growth_stage_configs`.
- Update `PlayerHerbEntity` to add `ExpireAt` property.
- Update `HerbGrowthStage` enum: add `Young = 4`, rename `Perfect → ThousandYear`.
- Add ThousandYear harvest output rows + high-tier linh dược item templates to seed data.

### Slice 2 — HerbService refactor
- Rename/refactor `HarvestHerbAsync` → `HarvestAsync` (move to inventory, set expire_at, no item output).
- Add `ExtractHerbAsync` (item output, mầm non chance, delete herb).
- Add `CheckInventoryHasSpace` stub.
- Add `IsHerbExpired` helper.
- Update `PlayerHerbRepository` with `ListExpiredInventoryHerbsAsync`.

### Slice 3 — Background expiry sweep
- Add `HerbExpiryBackgroundService` (`IHostedService`).
- Register in DI.
- Add config key `herb.expiry_sweep_interval_seconds`.

### Slice 4 — Network handlers + packets
- Add `HerbPackets.cs` with all request/result packets (IDs 200–215).
- Add MessageCode entries 6000–6011.
- Implement all 6 handlers.
- Register handlers in DI.

### Slice 5 — Alchemy migration
- Remove `required_herb_maturity` guard from `AlchemyService`.
- Update related tests if any.

---

## Dev Acceptance Criteria

- [ ] `HarvestAsync` on a `mature` planted herb: herb moves to InInventory, `expire_at` set, plot cleared, no items created.
- [ ] `HarvestAsync` on a herb below `mature`: rejected with `GardenHerbNotHarvestable`.
- [ ] `HarvestAsync` on a `thousand_year` herb: succeeds, herb stage preserved in inventory.
- [ ] `ExtractHerbAsync` on a valid InInventory herb: herb deleted, linh dược items created per config.
- [ ] `ExtractHerbAsync` on an expired herb: rejected with `GardenHerbExpired`, herb deleted.
- [ ] `ExtractHerbAsync` — `thousand_year` herb produces different item template than `mature` herb (per config).
- [ ] `ExtractHerbAsync` — mầm non return chance is applied correctly per config.
- [ ] `InsertSoilPacket` rejected when plot already has active soil.
- [ ] `PlantHerbSeedPacket` rejected when plot has no soil.
- [ ] `PlantHerbSeedPacket` rejected when plot already has herb.
- [ ] `GetGardenPlotsHandler` returns herb stage, next-stage remaining time, soil remaining time per plot.
- [ ] Background sweep: after `expire_at` passes, herb entity is deleted on next sweep cycle.
- [ ] `AlchemyService`: crafting a recipe with `required_herb_maturity != None` (if data row exists) no longer fails with the old guard message.
- [ ] `Young` stage exists: herb planted with enough growth seconds advances through Young before reaching Mature.
- [ ] All new handlers return correct MessageCode on failure and do not mutate state on rejection.

## Automated Test Cases

- `HerbServiceTests`:
  - HarvestAsync_Mature_MovesToInventoryWithExpiry
  - HarvestAsync_BelowMature_Rejected
  - HarvestAsync_ThousandYear_Succeeds
  - ExtractHerbAsync_Valid_ProducesItems
  - ExtractHerbAsync_Expired_RejectsAndDeletes
  - ExtractHerbAsync_ThousandYear_ProducesHighTierItem
  - ExtractHerbAsync_MamNonChance_Respected
  - InsertSoilAsync_PlotAlreadyHasSoil_Rejected
  - PlantSeedAsync_NoSoil_Rejected
  - PlantSeedAsync_PlotOccupied_Rejected
  - MaterializeHerbProgressAsync_YoungStage_AdvancesCorrectly

- `AlchemyServiceTests`:
  - ValidateCraftPill_RequiredHerbMaturityNonZero_NoLongerBlockedByGuard

## Manual E2E Test Script

1. Login as test character. Ensure home cave exists.
2. GET garden plots → expect N empty plots.
3. Insert linh thổ item into plot 1 → expect success.
4. Plant herb seed into plot 1 → expect seedling state.
5. Use DB script to fast-forward `accumulated_growth_seconds` past Young + Mature thresholds.
6. Harvest herb from plot 1 → expect InInventory herb with expire_at set, no items yet.
7. Extract herb from inventory → expect linh dược item(s) in inventory.
8. Use DB script to expire a second InInventory herb. Attempt extract → expect `GardenHerbExpired`.
9. Wait for background sweep cycle. Confirm expired herb row deleted.
10. Insert soil into plot 1 again. Plant mầm non (if received from extract). Expect seedling state.
11. Craft alchemy recipe that previously had `required_herb_maturity = 0` → confirm still works.

## Debug / Dev Tools Needed

- Seed script: insert test soil items, herb seed items, high-tier linh dược item templates.
- DB reset script: clear `player_herbs`, `player_soils`, reset `player_garden_plots`.
- Fast-forward script: update `accumulated_growth_seconds` to target stage for a given herb.
- Log: background sweep should log `[HerbExpirySweep] Deleted {n} expired herb(s)` each cycle.

---

## Open Questions

- [ ] **Inventory cap system**: `CheckInventoryHasSpace` is a stub returning `true`. GD needs to design inventory slot progression (starting size, upgrade path, per-character or per-account). TD will wire this when GD delivers requirement. _(Routed to GD/User)_
- [ ] **Herb drop from quái**: EnemyRewardRuntimeService wiring for herb drops is deferred. Separate slice needed when GD finalizes drop config schema. _(Routed to GD/User)_

## Risks

- `HarvestHerbAsync` rename/refactor is a breaking change — any existing internal caller (e.g. tests, other services) must be updated in same slice.
- `HerbGrowthStage.Perfect → ThousandYear` rename: DB integer value 3 unchanged so no data migration, but any client-side hardcoded int comparisons must be updated.
- Background `IHostedService` must be cancellation-token safe to avoid blocking server shutdown.
- `required_herb_maturity` guard removal is safe today (all DB rows = 0), but if new recipe data is seeded with non-zero value before full linh dược item template setup is complete, recipes may become craftable with missing inputs. Seed data must be added atomically with guard removal.
