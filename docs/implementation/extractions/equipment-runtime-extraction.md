# title
Equipment runtime extraction

# scope
Server-side equipment slot validation, equip/unequip persistence, stat modifier aggregation from equipped items, and immediate downstream sync into inventory/stats/skills. Focused on current runtime truth in `GameServer`.

# source files
- `GameServer/Services/EquipmentService.cs`
- `GameServer/Services/EquipmentActionService.cs`
- `GameServer/Services/EquipmentStatService.cs`
- `GameServer/Services/CharacterFinalStatService.cs`
- `GameServer/Services/SkillService.cs`
- `GameServer/Network/Handlers/GetInventoryHandler.cs`
- `GameServer/Network/Handlers/EquipInventoryItemHandler.cs`
- `GameServer/Network/Handlers/UnequipInventoryItemHandler.cs`
- `GameServer/Runtime/SkillRuntimeNotifier.cs`
- `GameServer/Config/GameConfigKeys.cs`
- `GameServer/Config/GameConfigValues.cs`

# current runtime behavior
- Equipment slot count is config-driven via `character.equipment_slot_count` and defaults to `4` in `GameConfigValues` (`GameServer/Config/GameConfigKeys.cs`, `GameServer/Config/GameConfigValues.cs`).
- `EquipmentService.ValidateEquipAsync(...)` checks slot range, verifies the `playerItemId` belongs to the player, verifies the item definition exists and has `Equipment` metadata, then ensures there is a `PlayerEquipmentEntity` row for that item, creating one on demand if missing (`GameServer/Services/EquipmentService.cs`).
- Equip is slot-agnostic with respect to equipment type: the current runtime only checks “is this item equipment” and target slot range; there is no slot-type compatibility check in `EquipmentService` (`GameServer/Services/EquipmentService.cs`).
- If another item already occupies the requested slot, equiping a new item silently unequips the previous occupant by setting its `EquippedSlot = null` before assigning the new item to that slot (`GameServer/Services/EquipmentService.cs`).
- `EquipmentActionService.EquipAsync(...)` and `UnequipAsync(...)` wrap the operation in `PlayerInventoryTransactionService.ExecuteAsync(...)`, then immediately run `SkillService.SyncEquipmentGrantedSkillsAsync(...)`, `CharacterFinalStatService.ApplyAuthoritativeFinalStatsAsync(...)`, and `ItemService.GetInventoryAsync(...)` before returning (`GameServer/Services/EquipmentActionService.cs`).
- Final equipment-derived stats come from two sources per equipped item: base equipment stat modifiers from item definition and persisted per-item bonus rows from `PlayerEquipmentStatBonusRepository` (`GameServer/Services/EquipmentStatService.cs`).
- Equipped rows with missing inventory items or missing equipment definitions are skipped by `EquipmentStatService.BuildEquipmentStatModifiersAsync(...)` instead of failing the whole recompute (`GameServer/Services/EquipmentStatService.cs`).
- Inventory fetch replies (`GetInventoryResultPacket`, equip result, unequip result) always include `EquipmentSlotCount` and a full refreshed item list, not just the delta item (`GameServer/Network/Handlers/GetInventoryHandler.cs`, `GameServer/Network/Handlers/EquipInventoryItemHandler.cs`, `GameServer/Network/Handlers/UnequipInventoryItemHandler.cs`).
- If equipment-granted skills changed during equip/unequip, the server emits an additional `OwnedSkillsChangedPacket` after the main equip/unequip result packet (`GameServer/Network/Handlers/EquipInventoryItemHandler.cs`, `GameServer/Network/Handlers/UnequipInventoryItemHandler.cs`, `GameServer/Runtime/SkillRuntimeNotifier.cs`).

# validations / guards
- Invalid target slots outside `1..CharacterEquipmentSlotCount` fail validation or return `false` depending on call path (`GameServer/Services/EquipmentService.cs`).
- `EquipmentActionService.EquipAsync(...)` and `UnequipAsync(...)` reject non-positive slot indexes up front with `MessageCode.EquipmentSlotInvalid` (`GameServer/Services/EquipmentActionService.cs`).
- Equipping fails if the inventory item does not exist, belongs to another player, or is not an equipment item definition (`GameServer/Services/EquipmentService.cs`).
- Unequip fails with `MessageCode.EquipmentSlotEmpty` when no equipment row is currently assigned to the requested slot (`GameServer/Services/EquipmentActionService.cs`).
- Network handlers reject equip/unequip/get-inventory requests when the session has not entered world yet (`GameServer/Network/Handlers/GetInventoryHandler.cs`, `GameServer/Network/Handlers/EquipInventoryItemHandler.cs`, `GameServer/Network/Handlers/UnequipInventoryItemHandler.cs`).
- `GetEquippedItemsAsync(...)` throws if a row is marked equipped but its item definition no longer resolves as equipment (`GameServer/Services/EquipmentService.cs`).

# config/data dependencies
- Config key `character.equipment_slot_count` controls legal slot index range and the inventory payload metadata (`GameServer/Config/GameConfigKeys.cs`, `GameServer/Config/GameConfigValues.cs`).
- Runtime depends on player inventory rows, player-equipment rows, optional player-equipment bonus rows, and item definitions with `Equipment` metadata (`GameServer/Services/EquipmentService.cs`, `GameServer/Services/EquipmentStatService.cs`).
- Equipment-granted skills depend on `itemDefinition.Equipment.SkillGrants`, so equipment and skill domains are coupled at runtime (`GameServer/Services/SkillService.cs`).

# client/server touch points
- Client fetches inventory through `GetInventoryPacket` / `GetInventoryResultPacket`, which includes `EquipmentSlotCount` and item models (`GameServer/Network/Handlers/GetInventoryHandler.cs`).
- Equip/unequip mutations return `EquipInventoryItemResultPacket` / `UnequipInventoryItemResultPacket` with refreshed items plus updated `BaseStats` and `CurrentState` (`GameServer/Network/Handlers/EquipInventoryItemHandler.cs`, `GameServer/Network/Handlers/UnequipInventoryItemHandler.cs`).
- Skill-side follow-up after equipment changes is pushed through `OwnedSkillsChangedPacket` (`GameServer/Runtime/SkillRuntimeNotifier.cs`).
- Final stat changes caused by equipment also propagate through normal character stat/state sync packets because `CharacterFinalStatService` uses `CharacterRuntimeService.ApplyBaseStatsMutation(...)` (`GameServer/Services/CharacterFinalStatService.cs`, `GameServer/Runtime/CharacterRuntimeNotifier.cs`).

# edge cases
- `ValidateEquipAsync(...)` auto-creates a missing `PlayerEquipmentEntity` row even before actual equip succeeds; this means merely validating/equipping a never-before-seen equipment item can leave a persisted equipment row with `EquippedSlot = null` (`GameServer/Services/EquipmentService.cs`).
- `EquipFirstAvailableAsync(...)` fails with `EquipmentSlotInvalid` when all configured slots are occupied; there is no overflow or replacement fallback in that helper path (`GameServer/Services/EquipmentActionService.cs`, `GameServer/Services/EquipmentService.cs`).
- Multiple equipped rows are ordered by slot when read back, but runtime does not appear to enforce one row per slot except by overwriting the previous occupant during equip (`GameServer/Services/EquipmentService.cs`).
- Stat aggregation ignores stale/missing item rows instead of throwing, so some equipment stat drift could remain silent if persistence becomes inconsistent (`GameServer/Services/EquipmentStatService.cs`).

# unclear or suspicious behavior
- There is no visible runtime rule mapping specific equipment categories to specific slot indexes; all slots are generic integer slots in the inspected code (`GameServer/Services/EquipmentService.cs`).
- `ValidateEquipAsync(...)` writes a new `PlayerEquipmentEntity` during validation/setup, which is more side-effectful than a pure validation method name suggests (`GameServer/Services/EquipmentService.cs`).
- Equip conflict resolution is unconditional replacement: the previous occupant is simply unequipped, with no separate failure/confirmation path (`GameServer/Services/EquipmentService.cs`).
- Equipment stat recompute and equipment-granted skill sync happen in the action service, not inside `EquipmentService` itself, so callers bypassing `EquipmentActionService` would need to remember these downstream steps manually (`GameServer/Services/EquipmentActionService.cs`).

# suggested canonical target docs
- `docs/inventory/equipment-runtime.md`
- `docs/systems/equipment-to-stats-and-skills-flow.md`
- `docs/data-design/config-contracts/equipment-and-inventory-runtime-config.md`
