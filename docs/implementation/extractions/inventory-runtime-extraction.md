# title
Inventory runtime extraction

# scope
Inventory listing, stack/item storage behavior, inventory-bound transactions, ground transfer, and inventory view building.

# source files
- `GameServer/Services/ItemService.cs`
- `GameServer/Services/PlayerInventoryTransactionService.cs`
- `GameServer/Network/Handlers/GetInventoryHandler.cs`
- `GameServer/Network/Handlers/DropInventoryItemHandler.cs`
- `GameServer/Descriptions/GameplayDescriptionService.cs`
- `GameServer/DTO/NetworkModelMapper.cs`
- `GameServer/Config/GameConfigValues.cs`

# current runtime behavior
- `ItemService` owns add/split/consume/move operations and wraps mutating inventory work in `PlayerInventoryTransactionService.ExecuteAsync(...)` (`GameServer/Services/ItemService.cs`).
- Stackable items try to merge into existing non-expired stacks with matching bind/expiry metadata before creating new rows; non-stackable items create one row per instance (`GameServer/Services/ItemService.cs`).
- `GetInventoryAsync` builds read-model views by joining item definitions with player items, equipment rows, equipment bonuses, and soil rows, then compiles descriptions through `GameplayDescriptionService` (`GameServer/Services/ItemService.cs`, `GameServer/Descriptions/GameplayDescriptionService.cs`).
- `GetInventoryHandler` returns the current inventory list plus configured equipment slot count for UI layout (`GameServer/Network/Handlers/GetInventoryHandler.cs`).
- Dropping inventory items moves rows from inventory location to ground location, then wraps them into a runtime `GroundRewardEntity` in the active map instance (`GameServer/Network/Handlers/DropInventoryItemHandler.cs`, `GameServer/Services/ItemService.cs`).

# validations / guards
- Inventory mutations reject non-positive quantities, wrong owner, wrong location type, expired items, and invalid stack split quantities (`GameServer/Services/ItemService.cs`).
- Non-droppable definitions cannot leave inventory through drop flow (`GameServer/Services/ItemService.cs`).
- Non-stackable items can only move one instance at a time to ground (`GameServer/Services/ItemService.cs`).
- `GetInventoryHandler` requires the character to have entered world before returning inventory data (`GameServer/Network/Handlers/GetInventoryHandler.cs`).

# config/data dependencies
- Config: `CharacterEquipmentSlotCount` used by inventory response (`GameServer/Config/GameConfigValues.cs`).
- DB repositories for player items, equipment, equipment bonuses, and soils (`GameServer/Services/ItemService.cs`).
- Item definitions and description-template data for item read-model shaping.

# client/server touch points
- `GetInventoryPacket` / `GetInventoryResultPacket` expose inventory state.
- Drop flow returns `DropInventoryItemResultPacket` and causes runtime ground reward publication via map-instance state.
- Inventory view models include compiled descriptions, item icons, stack info, equipment info, and soil metadata.

# edge cases
- Partial stack drop splits the stack first, then relocates the split row to ground.
- Exact-quantity drop on a stack flips the same row to ground rather than cloning it.
- Residual ground items are cleaned on startup and expired-despawn cleanup through `GroundItemRuntimeService` calling back into `ItemService`.

# unclear or suspicious behavior
- Inventory read model mixes core item state with equipment/soil overlays in one view path, so canonical docs need to separate storage truth from presentation truth.
- Drop handler maps many `InvalidOperationException` branches to generic `InventoryItemInvalid`, which hides the concrete failure reason.

# suggested canonical target docs
- `docs/inventory/inventory-runtime.md`
- `docs/inventory/inventory-item-read-model.md`
