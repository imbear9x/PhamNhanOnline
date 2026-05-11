---
title: Ground reward and loot runtime
doc_type: system
status: verified
owner: knowledge-manager
last_verified: 2026-05-12
source_of_truth:
  - docs/implementation/extractions/loot-ground-reward-runtime-extraction.md
related_code:
  - GameServer/Runtime/EnemyRewardRuntimeService.cs
  - GameServer/Runtime/GroundItemRuntimeService.cs
  - GameServer/Network/Handlers/PickupGroundRewardHandler.cs
  - GameServer/Network/Handlers/DropInventoryItemHandler.cs
---

# Ground reward and loot runtime

## Runtime behavior

- Enemy death reward processing grants progression first, then evaluates reward rules for eligible targets.
- Reward tables roll through `IGameRandomService.Roll(...)` with player luck passed in options.
- Direct-grant rules add items straight to inventory; ground-drop rules create ground item rows and runtime `GroundRewardEntity` objects.
- Ground rewards can be owner-only until `freeAtUtc`, then destruct at `destroyAtUtc`.
- Inventory drops also create ground rewards, but ownership/default timings differ from enemy drops.
- `GroundItemRuntimeService` cleans residual ground items on startup and destroys persisted rows for despawned rewards when appropriate.

## Pickup guards

Pickup validates world presence, action gating, map instance, reward id, interaction range, runtime claim ownership, and inventory mutation. Claim cancel runs if inventory grant fails after claim start.

## Edge cases

- `none` reward entries skip item creation.
- Unsupported reward entry ids are logged and ignored.
- Runtime claim completion failure after DB grant is logged, but success may still be returned once grant commits.

## Verification

Supported by `docs/implementation/extractions/loot-ground-reward-runtime-extraction.md`.
