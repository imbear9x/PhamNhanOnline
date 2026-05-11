---
title: Inventory runtime
doc_type: system
status: verified
owner: knowledge-manager
last_verified: 2026-05-12
source_of_truth:
  - docs/implementation/extractions/inventory-runtime-extraction.md
related_code:
  - GameServer/Services/ItemService.cs
  - GameServer/Services/PlayerInventoryTransactionService.cs
  - GameServer/Network/Handlers/GetInventoryHandler.cs
  - GameServer/Network/Handlers/DropInventoryItemHandler.cs
---

# Inventory runtime

## Runtime behavior

- `ItemService` owns add/split/consume/move operations and wraps mutating work in `PlayerInventoryTransactionService.ExecuteAsync(...)`.
- Stackable items merge into existing non-expired stacks with matching bind/expiry metadata before new rows are created.
- Non-stackable items create one row per instance.
- Inventory reads join item definitions, player items, equipment rows, equipment bonuses, and soil rows, then compile descriptions.
- `GetInventoryHandler` returns item models plus configured equipment slot count.
- Dropping inventory moves item rows to ground location, then wraps them in runtime `GroundRewardEntity` objects in the active map instance.

## Guards and edge cases

- Mutations reject non-positive quantities, wrong owner, wrong location, expired items, and invalid stack splits.
- Non-droppable definitions cannot be dropped.
- Non-stackable items can only move one instance at a time to ground.
- `GetInventoryHandler` requires entered-world state.
- Partial stack drop splits first, then relocates the split row to ground.

## Verification

Supported by `docs/implementation/extractions/inventory-runtime-extraction.md`.
