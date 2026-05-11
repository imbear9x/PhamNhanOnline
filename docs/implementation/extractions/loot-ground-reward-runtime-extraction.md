# title
Loot and ground reward runtime extraction

# scope
Enemy reward rolls, direct grant vs ground drop delivery, ownership windows, pickup flow, and ground-item cleanup.

# source files
- `GameServer/Runtime/EnemyRewardRuntimeService.cs`
- `GameServer/Runtime/GroundItemRuntimeService.cs`
- `GameServer/Network/Handlers/PickupGroundRewardHandler.cs`
- `GameServer/Network/Handlers/DropInventoryItemHandler.cs`
- `GameServer/Services/ItemService.cs`
- `GameServer/Randomness/GameRandomService.cs`
- `GameServer/Config/GameConfigValues.cs`

# current runtime behavior
- `EnemyRewardRuntimeService` dequeues enemy death events from map instances, grants progression rewards first, then evaluates each reward rule against eligible targets (`GameServer/Runtime/EnemyRewardRuntimeService.cs`).
- Reward tables are rolled through `IGameRandomService.Roll(...)`, with player luck passed as `GameRandomOptions(luck)` for each roll (`GameServer/Runtime/EnemyRewardRuntimeService.cs`).
- Direct-grant rules add items straight to player inventory via `ItemService`; ground-drop rules materialize item rows as ground items and wrap them in `GroundRewardEntity` runtime objects (`GameServer/Runtime/EnemyRewardRuntimeService.cs`).
- Ground rewards can have temporary owner-only access; `freeAtUtc` and `destroyAtUtc` are derived from rule values or fallback config defaults (`GameServer/Runtime/EnemyRewardRuntimeService.cs`).
- `PickupGroundRewardHandler` validates world presence, action gating, map-instance existence, reward id, interaction range, and runtime claim ownership before moving underlying ground items into inventory inside a transaction (`GameServer/Network/Handlers/PickupGroundRewardHandler.cs`).
- `GroundItemRuntimeService` cleans residual ground items on startup and destroys persisted item rows for despawned rewards that should delete items (`GameServer/Runtime/GroundItemRuntimeService.cs`).

# validations / guards
- Reward roll failures from invalid random tables are logged and stop that rule loop rather than crashing the whole tick (`GameServer/Runtime/EnemyRewardRuntimeService.cs`).
- Pickup rejects missing world player, invalid reward id, out-of-range claims, failed ownership checks, and inventory mutation failures (`GameServer/Network/Handlers/PickupGroundRewardHandler.cs`).
- Claim cancel path runs if inventory grant fails after runtime claim start (`GameServer/Network/Handlers/PickupGroundRewardHandler.cs`).
- Drop-from-inventory and enemy ground-drop flows both clamp reward spawn positions against map bounds (`GameServer/Network/Handlers/DropInventoryItemHandler.cs`, `GameServer/Runtime/EnemyRewardRuntimeService.cs`).

# config/data dependencies
- Game config: enemy item drop ownership/free-for-all durations, player drop ownership/free-for-all durations, pickup radius, ground spawn offset.
- Random table config and reward rule definitions.
- Item definitions and map-instance runtime state.

# client/server touch points
- Ground reward pickup uses `PickupGroundRewardPacket` / `PickupGroundRewardResultPacket`.
- Runtime world snapshots include ground reward snapshot state for clients to render.
- Drop inventory flow produces ground rewards using the same runtime object family.

# edge cases
- Reward entries resolving to `none` silently skip item creation.
- Unsupported reward entry ids are logged and ignored.
- Runtime claim can fail to complete after DB grant commits; handler logs this inconsistency but still returns success once grant succeeded.

# unclear or suspicious behavior
- Luck affects reward rolling, but the exact per-table modifier rules live in random-table config rather than in reward service itself.
- Canonical docs should distinguish inventory-drop ground rewards from enemy-drop ground rewards because ownership defaults differ.

# suggested canonical target docs
- `docs/loot/ground-reward-runtime.md`
- `docs/loot/enemy-reward-roll-and-delivery-runtime.md`
