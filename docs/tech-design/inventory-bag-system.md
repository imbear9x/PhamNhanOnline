---
title: Inventory Bag System
doc_type: techdesign-spec
status: draft
owner: techdesign
created_at: 2026-05-15
updated_at: 2026-05-15
spec_iteration: 2
spec_change_note: Bổ sung contract random output handling cho active actions (herb harvest fix per QA fail #13)
source_design_docs:
  - docs/game-design-wp/requirements/inventory-bag-system.md
  - docs/game-design-wp/features/inventory-bag-system.md
  - docs/game-design-wp/features/npc-system.md
  - docs/game-design-wp/features/inbox-mail-system.md
related_shared_rules:
  - docs/game-design-wp/shared-rules.md
code_grounding:
  - GameServer/Services/CharacterService.cs
  - GameServer/Services/ItemService.cs
  - GameServer/Repositories/PlayerItemRepository.cs
  - GameServer/Runtime/ItemSystemTypes.cs
  - database/initDatabase.sql
---

# Inventory Bag System — Tech Design Spec

## Goal

Implement per-character bag capacity as a first-class server concept so inventory can have grade, slot cap, used/total display, server-side full checks, and bag upgrade flow without item loss.

## Source Design Summary

- Each character always has exactly 1 bag.
- Bag is not an inventory item and cannot be traded/dropped/mailed.
- Bag has grade 1–4; slot count comes from config.
- New character starts with bag grade 1.
- Upgrade is one-way only, bought from NPC using linh thạch.
- Upgrade must never lose items.
- Full inventory behavior:
  - passive reward → inbox overflow
  - active action → reject entirely

## Scope

### In Scope
- New bag schema and config schema.
- Character creation bag initialization.
- Server-side slot counting and capacity enforcement.
- Atomic bag upgrade transaction.
- Dedicated bag upgrade service + packet/handler path.
- Inventory summary model changes for used/total slot info.
- Reusable inventory-full policy helpers for passive vs active grants.
- Hook points for herb harvest/extract rejection.

### Out Of Scope
- Full NPC shop framework.
- Client UI layout.
- Shared storage/account storage.
- Bag downgrade.
- Balance values.

## Code Grounding Summary

| Area | Findings |
|---|---|
| Bag schema | No bag entity/table/config exists in DB or code. |
| Inventory data model | `player_items` stores rows by `player_id` + `location_type`; no slot index, no bag_id, no capacity metadata. |
| Capacity | `ItemService.AddItemAsync` has no slot-cap guard; inventory currently unlimited. |
| Character init | `CharacterService.CreateCharacterAsync` initializes cave + starter skill only; no bag assignment exists. |
| NPC shop | No NPC shop service/table/handler exists in current codebase. |
| Currency cost | Currency cost metadata exists in some recipes, but currency spending flow is not implemented yet. |
| Inventory counting | `PlayerItemRepository` can list inventory items, but has no count helpers or used-slot abstraction. |

## Current System Fit

The smallest safe integration is:
- keep `player_items` ownership model unchanged (`player_id` on item row)
- add a new `player_bags` row as capacity metadata for each character
- define "used slot" as **number of occupied inventory stacks/instances**, i.e. row count of `player_items` in `location_type = Inventory` after expiry filtering
- do not add `bag_id` to `player_items`

This avoids rewriting item ownership and lets bag upgrade be a metadata change instead of physical item migration.

### Important interpretation of requirement

Requirement text says "replace bag, transfer items from old bag to new bag". In current architecture, items are already attached directly to character inventory, not to a bag container row. Therefore TD interprets this as:

- upgrade transaction changes **capacity metadata** (`player_bags.grade`) atomically
- existing `player_items` remain attached to the character without row migration
- since item rows never move, item loss risk is lower than literal copy-transfer design

This preserves required gameplay behavior while minimizing architecture churn.

---

## DB / Schema Plan

### Canonical Data Model

#### Tables And Field Semantics

| Table | Field | Type | Meaning | Required | Source of truth | Notes |
|---|---|---|---|---|---|---|
| `bag_grade_configs` | `grade` | int PK | Bag grade identifier | yes | DB/init data | Canonical tier key, expected range 1–4 |
| `bag_grade_configs` | `slot_count` | int | Total inventory slots granted by this grade | yes | DB/init data | Must be derived from config, not duplicated on player row |
| `bag_grade_configs` | `upgrade_cost_linh_thach` | bigint | Currency cost to upgrade into this grade | yes | DB/init data | Interpreted as linh thạch quantity |
| `bag_grade_configs` | `display_name` | varchar | Human-readable bag grade label | yes | DB/init data | Used by UI/model mapping |
| `player_bags` | `player_id` | uuid PK/FK | Character who owns this bag state | yes | DB/runtime | Exactly one row per character |
| `player_bags` | `grade` | int FK | Current bag grade | yes | DB/runtime | Resolves capacity via `bag_grade_configs` |
| `player_bags` | `updated_at` | timestamp | Last bag state update time | yes | DB/runtime | Changes on create/upgrade |
| `player_items` | `id` | bigint PK | Inventory row / item instance / stack row | yes | Existing DB/runtime | Each occupied inventory row counts as 1 used slot |
| `player_items` | `player_id` | uuid FK | Character owner of inventory row | no | Existing DB/runtime | Null for ground items |
| `player_items` | `item_template_id` | int FK | Template identity of stored item | yes | Existing DB/runtime | Used for stack merge/capacity simulation |
| `player_items` | `location_type` | int | Item location bucket | yes | Existing DB/runtime | Only `Inventory` rows count for bag usage |
| `player_items` | `quantity` | int | Stack quantity for stackable rows | yes | Existing DB/runtime | Quantity alone does not equal slot usage |
| `player_items` | `is_bound` | bool | Bind state of the row | yes | Existing DB/runtime | Affects compatible stack merging |
| `player_items` | `expire_at` | timestamp? | Optional expiry cutoff | no | Existing DB/runtime | Expired rows do not count toward used slots |
| `characters` | `id` | uuid PK | Character identity | yes | Existing DB/runtime | Parent for `player_bags` |

#### Enums / Codes / State Values

| Name | Value | Meaning | Used by | Notes |
|---|---|---|---|---|
| `BagGrade` | `1..4` | Config-defined bag progression tier | `player_bags.grade`, bag config lookup | No downgrade path |
| `ItemLocationType.Inventory` | `1` | Row is in character inventory | `player_items.location_type`, bag capacity count | Only this location counts toward used slots |
| `InventoryOverflowPolicy.Reject` | `1` | Active grant must fail if full | active actions such as harvest/extract | No inbox fallback |
| `InventoryOverflowPolicy.InboxOverflow` | `2` | Passive grant routes to inbox sink if batch does not fit | passive rewards | Whole-batch overflow policy for this slice |

#### Relations And Ownership

| From | To | Relation | Ownership / authority | Notes |
|---|---|---|---|---|
| `characters.id` | `player_bags.player_id` | 1 → 1 | Character owns exactly one bag row | Must exist for all characters |
| `bag_grade_configs.grade` | `player_bags.grade` | 1 → many | Config defines capacity/cost for all player bag rows | Runtime lookup only; no duplicated slot_count on player row |
| `characters.id` | `player_items.player_id` | 1 → many | Character owns inventory item rows | Inventory storage model remains unchanged |
| `player_bags` | `player_items` | logical 1 → many | Bag capacity envelopes character inventory | No physical `bag_id` FK on item rows |

#### State Transitions By Field

| Entity / table | Field(s) | Transition | Trigger | Owner service |
|---|---|---|---|---|
| `player_bags` | `player_id`, `grade`, `updated_at` | no row → default grade 1 row | Character creation / backfill | `BagService.EnsureDefaultBagAsync`, `CharacterService.CreateCharacterAsync` |
| `player_bags` | `grade`, `updated_at` | `grade_n → grade_n+1` | Successful upgrade request | `BagService.UpgradeBagAsync` |
| `player_items` + bag capacity runtime | logical used slot count | recalculate from active inventory rows | Bag state query / capacity pre-check | `BagService`, capacity helper |
| passive grant path | overflow policy outcome | inventory add → inbox overflow sink | Capacity insufficient on passive reward | shared grant helper + inbox sink |
| active grant path | action result | allowed → reject with `InventoryFull` | Capacity insufficient on active action | action owner service + capacity helper |

### New Tables

| Table | Purpose | Key columns | Notes |
|---|---|---|---|
| `bag_grade_configs` | Fixed config per bag grade | `grade`, `slot_count`, `upgrade_cost_linh_thach`, `display_name` | grades 1–4 |
| `player_bags` | Bag state per character | `player_id`, `grade`, `updated_at` | exactly one row per character |

### Table Definitions

#### `bag_grade_configs`
```sql
CREATE TABLE IF NOT EXISTS public.bag_grade_configs (
    grade integer PRIMARY KEY,
    slot_count integer NOT NULL,
    upgrade_cost_linh_thach bigint NOT NULL DEFAULT 0,
    display_name varchar(100) NOT NULL
);
```

Constraints:
- `grade BETWEEN 1 AND 4`
- `slot_count > 0`
- `upgrade_cost_linh_thach >= 0`

#### `player_bags`
```sql
CREATE TABLE IF NOT EXISTS public.player_bags (
    player_id uuid PRIMARY KEY,
    grade integer NOT NULL,
    updated_at timestamp without time zone NOT NULL DEFAULT now(),
    CONSTRAINT fk_player_bags_player
        FOREIGN KEY (player_id) REFERENCES public.characters(id) ON DELETE CASCADE,
    CONSTRAINT fk_player_bags_grade
        FOREIGN KEY (grade) REFERENCES public.bag_grade_configs(grade)
);
```

Invariant:
- exactly one row per character

### Why no `slot_count` column on `player_bags`

`slot_count` must be derived from config by grade per requirement (`REQ-004`). Storing both `grade` and `slot_count` on player row would create drift risk. Use config join/runtime lookup instead.

### Why no `bag_id` on `player_items`

Current inventory model already scopes items to character. Adding `bag_id` would force broad refactor with no gameplay benefit for this slice. Capacity can be enforced from `player_bags` + current inventory row count.

### Repository / Entity Plan

New entities:
- `BagGradeConfigEntity`
- `PlayerBagEntity`

New repositories:
- `BagGradeConfigRepository`
- `PlayerBagRepository`

New repo methods:

`PlayerItemRepository`
```csharp
Task<int> CountInventorySlotsUsedAsync(Guid playerId, CancellationToken ct = default);
Task<List<PlayerItemEntity>> ListInventoryActiveAsync(Guid playerId, CancellationToken ct = default);
```

Slot counting rule:
- each inventory row = 1 used slot
- expired items do not count

---

## Runtime Model / Service Plan

### New Services

#### `BagService`
Primary authority for bag state.

Methods:
```csharp
Task<BagStateDto> GetBagStateAsync(Guid playerId, CancellationToken ct = default);
Task EnsureDefaultBagAsync(Guid playerId, CancellationToken ct = default);
Task<bool> HasCapacityForAsync(Guid playerId, IReadOnlyList<ItemGrantRequest> grants, CancellationToken ct = default);
Task<InventoryCapacityCheckResult> CheckCapacityForAsync(Guid playerId, IReadOnlyList<ItemGrantRequest> grants, CancellationToken ct = default);
Task<BagUpgradeResult> UpgradeBagAsync(Guid playerId, int targetGrade, CancellationToken ct = default);
```

Responsibilities:
- load player bag row + grade config
- compute used slots / total slots
- estimate additional slots needed for incoming grants
- enforce no downgrade / no same-grade upgrade
- execute atomic upgrade

#### `InventoryCapacityService` (optional helper)
Can be folded into `BagService`. If kept separate, it only calculates slot deltas.

### New DTOs

```csharp
public sealed record BagStateDto(
    int Grade,
    int UsedSlots,
    int TotalSlots,
    string DisplayName);

public sealed record ItemGrantRequest(
    int ItemTemplateId,
    int Quantity,
    bool IsBound,
    DateTime? ExpireAtUtc = null);

public sealed record InventoryCapacityCheckResult(
    bool CanFit,
    int UsedSlots,
    int TotalSlots,
    int AdditionalSlotsNeeded);
```

### Slot counting rule

Used slots are based on resulting inventory rows, not raw item quantity.

Examples:
- 1 equipment item row = 1 slot
- 1 soil row = 1 slot
- stackable material quantity 200 in one row = 1 slot
- adding 50 more to same stack where `max_stack` allows → may consume 0 new slots
- adding quantity that spills into new stack(s) consumes additional slots accordingly

Therefore capacity check must simulate `ItemService.AddItemAsync` stacking behavior before granting.

### Capacity estimation helper

Add helper in `ItemService` or `BagService`:
```csharp
Task<int> EstimateAdditionalInventorySlotsNeededAsync(
    Guid playerId,
    IReadOnlyList<ItemGrantRequest> grants,
    CancellationToken ct = default);
```

Algorithm:
1. Load active inventory rows.
2. Group incoming grants by `(itemTemplateId, isBound, expireAtUtc)`.
3. For each definition:
   - if non-stackable: additional slots += quantity
   - if stackable:
     - fill existing compatible stacks first
     - remaining quantity creates `ceil(remaining / maxStack)` new rows
4. Sum additional slots.
5. Compare `usedSlots + additionalSlots` vs bag capacity.

---

## Character Creation Plan

### Required change

`CharacterService.CreateCharacterAsync` must initialize default bag grade 1 inside the same transaction as character creation.

Add call:
```csharp
await EnsureDefaultBagAsync(character.Id, cancellationToken);
```

inside the existing transaction, after character row creation.

### Guarantee

A character creation transaction is not committed unless:
- character row exists
- base stats row exists
- current state row exists
- home cave exists
- player bag row exists

This satisfies `REQ-001` and `REQ-003`.

---

## Bag Upgrade Plan

## Decision: do not wait for full NPC shop framework

Current codebase has no NPC shop implementation. Waiting for a general shop system would block bag capacity work and herb dependency. Smallest implementation-ready path is:

- implement a dedicated **bag upgrade action** now
- client may open it from NPC UI later
- when full NPC shop system arrives, it can call the same `BagService.UpgradeBagAsync`

This keeps runtime logic reusable and avoids speculative general shop architecture.

### New packet flow

Add new packet file or extend character packet set:
- `GetBagStatePacket`
- `GetBagStateResultPacket`
- `UpgradeBagPacket`
- `UpgradeBagResultPacket`

Suggested packet IDs: `220–223`

### Validation for upgrade

Server validates in order:
1. player has a bag row
2. target grade exists in config
3. target grade > current grade
4. target grade is not above max config grade
5. player has enough linh thạch
6. commit grade update + currency deduction atomically

### Currency representation decision

Because no dedicated wallet/currency subsystem exists, this slice should treat linh thạch as an **inventory item template** of `ItemType.Currency` and use `ItemService.RemoveItemAsync` for deduction.

This is the smallest fit with current architecture.

### Atomicity

Upgrade must run inside one DB transaction and one player inventory lock.

Pseudo flow:
```csharp
_inventoryTransactions.ExecuteAsync(playerId, async ct =>
{
    begin db tx
    load current bag
    load target config
    validate target > current
    validate player owns enough linh thạch item quantity
    remove linh thạch via ItemService internal/core path
    update player_bags.grade = targetGrade
    update updated_at
    commit tx
});
```

### Why no item transfer step in DB

There is no separate bag container holding items. Items remain on `player_items.player_id = characterId` before and after upgrade. Therefore the atomic safety requirement is satisfied by not moving any item rows at all.

Requirement phrase "transfer all items from old bag to new bag" is fulfilled behaviorally because after upgrade the same full inventory remains accessible under the higher capacity bag.

### Downgrade rejection

Need explicit result code and server log when target grade `<= current grade`.

---

## Passive vs Active Item Grant Policy

### Passive reward path

Examples: quái drop reward claim, quest reward, mail attachment, event reward.

Desired behavior:
- if item fits → add to inventory
- if not fit → redirect to inbox/mail overflow

### Active action path

Examples: herb harvest, herb extract, future manual crafting outputs where design says reject.

Desired behavior:
- pre-check capacity before mutation
- if cannot fit → reject action entirely, do not mutate state, do not partial grant, do not inbox fallback

### Random output handling in active actions

Khi active action có output ngẫu nhiên (OutputChance < 1.0), capacity check **phải dựa trên tập output đã được roll (materialize) trước**, không dựa trên guaranteed-only subset.

Contract bắt buộc cho `HarvestHerbAsync` (và mọi active action có random output):

1. **Roll all outputs first** — trong cùng inventory lock/transaction, thực hiện random roll cho toàn bộ `lockedOutputs`. Kết quả là `procOutputs`: tập output thực tế sẽ được grant nếu inventory cho phép.
2. **Check capacity on full proc set** — gọi `CheckCapacityForAsync(playerId, procOutputs)` trên toàn bộ `procOutputs`, không chỉ guaranteed subset.
3. **If not fit → reject entirely** — throw `GameException(MessageCode.InventoryFull)`. Không grant bất kỳ item nào, không xóa herb, không xóa plot link. Transaction rollback toàn bộ.
4. **If fit → grant all proc outputs** — gọi `AddItemAsync` cho từng item trong `procOutputs`, sau đó mới clear plot link và delete herb.

**Không được:**
- Pre-check guaranteed-only rồi roll-and-grant random outputs riêng: tạo rào cản TOCTOU logic giữa check và grant.
- Bỏ qua capacity check nếu `procOutputs` rỗng (0 proc): grant vãn an toàn nhưng cần được xử lý explicit (không phải bỏ qua hoàn toàn — vẫn xóa herb/plot được vì không có item nào cần grant).

Lý do thiết kế: active action là hành động chủ động của player, kết quả phải rõ ràng và predictable. Overflow sau khi đã commit herb destroy là data loss không thể phục hồi — phải ngăn trước, không xử lý sau.

### New reusable API

Add a grant policy enum:
```csharp
public enum InventoryOverflowPolicy
{
    Reject = 1,
    InboxOverflow = 2
}
```

Add helper service method:
```csharp
Task<ItemGrantExecutionResult> GrantItemsAsync(
    Guid playerId,
    IReadOnlyList<ItemGrantRequest> grants,
    InventoryOverflowPolicy overflowPolicy,
    CancellationToken ct = default);
```

Behavior:
- `Reject`: if cannot fit, return failure without granting anything
- `InboxOverflow`: grant what fits to inventory if design allows partial? **No** — to avoid ambiguity, for this slice use all-or-overflow by grant batch:
  - if whole grant batch fits → add to inventory
  - else redirect whole batch to inbox/notification mail system

This keeps behavior deterministic and simpler than partial split.

### Note on inbox implementation dependency

Inbox overflow final wiring depends on inbox/mail implementation. If inbox runtime is not yet complete, Dev may stage this as:
- abstraction/interface now: `IPassiveRewardOverflowSink`
- concrete inbox integration in follow-up

But bag capacity checks must still be implemented now.

---

## Packet / Model Plan

### Request Packets

| Packet | Purpose |
|---|---|
| `GetBagStatePacket` | fetch current bag grade and used/total slots |
| `UpgradeBagPacket` | request upgrade to target grade |

### Response Packets

| Packet | Payload |
|---|---|
| `GetBagStateResultPacket` | `Success`, `Code`, `BagStateModel` |
| `UpgradeBagResultPacket` | `Success`, `Code`, `BagStateModel`, `RemainingLinhThach` |

### Model additions

Add shared model:
```csharp
public sealed class BagStateModel
{
    public int Grade { get; set; }
    public string? DisplayName { get; set; }
    public int UsedSlots { get; set; }
    public int TotalSlots { get; set; }
}
```

Inventory UI can use this alongside existing `InventoryItemModel` list.

### MessageCode additions

Use new range `6021–6035`:
- `BagNotFound`
- `BagGradeInvalid`
- `BagUpgradeInvalidTarget`
- `BagUpgradeMaxGradeReached`
- `BagUpgradeInsufficientCurrency`
- `InventoryFull`

`InventoryFull` should be reusable by harvest/extract and future active actions.

---

## Validation And Authority

- server is authoritative for grade, slot count, used count, and upgrade eligibility
- client only sends target grade intent
- downgrade and same-grade requests are always rejected server-side
- slot count always resolved from `bag_grade_configs`, never client-provided
- capacity must be checked before active actions mutate state
- all bag upgrade writes run under player-scoped inventory transaction lock

---

## Persistence Flow

### Character creation
- create character-related rows
- create `player_bags(player_id, grade=1)`
- commit once

### Bag upgrade
- validate current grade and target config
- deduct linh thạch item quantity
- update `player_bags.grade`
- commit once

### Passive reward grant
- estimate fit
- if fit: create/update `player_items`
- if not fit: redirect full batch to overflow sink/inbox

### Active action grant
- estimate fit before mutation
- if not fit: abort action with `InventoryFull`

---

## Implementation Slices

### Slice 1 — schema + entities + repos
- add `bag_grade_configs`
- add `player_bags`
- add entities/repos
- seed grades 1–4

### Slice 2 — bag runtime + character init
- add `BagService`
- add counting helpers
- initialize bag grade 1 in `CharacterService.CreateCharacterAsync`
- add bag state DTO/model

### Slice 3 — capacity estimation + grant policy
- add additional-slot estimation helper
- add reusable `InventoryFull` checks
- integrate with herb harvest/extract path and any immediate active-action callers

### Slice 4 — bag upgrade action
- add `GetBagState` / `UpgradeBag` packets + handlers
- add `BagService.UpgradeBagAsync`
- deduct linh thạch as inventory currency item
- add downgrade rejection log

### Slice 5 — passive overflow integration
- add abstraction for inbox overflow sink
- wire passive reward path(s) progressively, starting with highest-priority runtime grant path

---

## Dev Acceptance Criteria

- [ ] New character creation always creates `player_bags` row with grade 1 in same transaction.
- [ ] Every existing character can be backfilled with a default bag row via migration/backfill script.
- [ ] Bag state API returns correct `used/total` slot counts.
- [ ] Used slots count inventory rows, not raw quantity.
- [ ] Stackable item grant that merges into existing stack consumes 0 extra slots when applicable.
- [ ] Stackable item grant that overflows existing stack computes correct new-slot count.
- [ ] Non-stackable item grant consumes one slot per instance.
- [ ] `UpgradeBag` rejects target grade <= current grade.
- [ ] `UpgradeBag` rejects when target grade config missing.
- [ ] `UpgradeBag` rejects when player lacks enough linh thạch.
- [ ] Successful `UpgradeBag` deducts linh thạch and updates bag grade atomically.
- [ ] Successful `UpgradeBag` does not delete or alter unrelated inventory rows.
- [ ] If upgrade transaction fails mid-flight, neither grade nor linh thạch deduction is partially committed.
- [ ] Active action with full inventory returns `InventoryFull` and performs no grant/mutation.
- [ ] Passive reward path can route full batch to overflow sink when capacity insufficient.

## Automated Test Cases

- `CharacterServiceTests.CreateCharacter_CreatesDefaultBag`
- `BagServiceTests.GetBagState_ReturnsUsedAndTotal`
- `BagServiceTests.CheckCapacity_StackMerge_NoExtraSlot`
- `BagServiceTests.CheckCapacity_StackOverflow_CreatesExtraSlots`
- `BagServiceTests.CheckCapacity_NonStackable_CountsPerInstance`
- `BagServiceTests.UpgradeBag_SameGrade_Rejected`
- `BagServiceTests.UpgradeBag_Downgrade_Rejected`
- `BagServiceTests.UpgradeBag_InsufficientCurrency_Rejected`
- `BagServiceTests.UpgradeBag_Success_DeductsCurrencyAndUpdatesGrade`
- `BagServiceTests.UpgradeBag_Failure_RollsBackAtomically`
- `ItemGrantPolicyTests.ActiveReject_NoMutation`
- `ItemGrantPolicyTests.PassiveOverflow_RoutesWholeBatch`

## Manual E2E Test Script

1. Create new character.
2. Query bag state → expect grade 1 and configured slot count.
3. Fill inventory close to limit with test items.
4. Grant stackable item that merges into existing stack → expect used slot unchanged.
5. Grant stackable item that spills into new row → expect used slot +1.
6. Try active herb action with full inventory → expect reject `InventoryFull`.
7. Give player enough linh thạch currency items.
8. Upgrade bag from grade 1 to 2 → expect cost deducted, grade updated, used/total refreshed.
9. Try upgrade to grade 1 again → expect reject.
10. Try upgrade at max grade → expect no higher options / server reject.

## Migration / Backfill Plan

### Schema migration
- create `bag_grade_configs`
- create `player_bags`
- insert grade 1–4 seed rows

### Existing character backfill
```sql
INSERT INTO public.player_bags (player_id, grade, updated_at)
SELECT c.id, 1, NOW()
FROM public.characters c
LEFT JOIN public.player_bags b ON b.player_id = c.id
WHERE b.player_id IS NULL;
```

This is required because `REQ-001` says every character always has exactly one bag.

---

## Open Questions

- [ ] Exact linh thạch item template id/code to use as upgrade currency must be confirmed from seed data/code ownership. TD assumes a currency item template exists or will be added. _(User/Dev seed alignment)_
- [ ] Inbox overflow concrete implementation may require coordination with inbox/mail runtime if not already complete. TD only defines the capacity contract here. _(Dev/runtime integration)_
- [ ] Requirement says NPC shop shows only higher grades, but no NPC shop framework exists. This spec uses a dedicated bag upgrade action as minimal runtime slice; later NPC shop should call the same service. Confirm acceptable for implementation sequencing. _(User approval implicit unless redirected)_

## Risks

- If future design wants multiple bag types or removable bags, this simplified `player_bags` model will need extension.
- Passive overflow semantics can become inconsistent if some grant paths bypass the new capacity helper; all item-granting services must gradually adopt the shared helper.
- Currency-as-item deduction is pragmatic for current architecture but may need migration if a future wallet subsystem is introduced.
- Backfill migration must run before any code path assumes `player_bags` exists for all characters.
