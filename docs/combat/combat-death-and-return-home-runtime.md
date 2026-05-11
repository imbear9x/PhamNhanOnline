---
title: Combat death and return-home runtime
doc_type: system
status: verified
owner: knowledge-manager
last_verified: 2026-05-12
source_of_truth:
  - docs/implementation/extractions/combat-death-recovery-runtime-extraction.md
related_code:
  - GameServer/Runtime/CharacterCombatDeathRecoveryService.cs
  - GameServer/Network/Handlers/ReturnHomeAfterCombatDeathHandler.cs
---

# Combat death and return-home runtime

## Runtime behavior

- Combat-dead states are recoverable; permanently dead states are excluded.
- Online recovery rebuilds current state with recovered HP/MP, home-map zone/spawn, idle state, cleared cultivation start, and persisted timestamp.
- Online recovery updates runtime state, republishes world snapshot, flushes runtime save, clears action restrictions, and replaces map-entry context with default home spawn.
- Snapshot/disconnected recovery paths repair persistence without full online world publish.

## Client/server surface

- `ReturnHomeAfterCombatDeathPacket` / result packet is the explicit client-triggered recovery path.

## Guards and limits

- Recovery no-ops if base stats/current state are missing or state is not combat-dead.
- Handler rejects non-dead players.
- Recovery ratio is clamped to `[0,1]`.
- Recovery always returns to home default spawn; no death-location override was evidenced.

## Verification

Supported by `docs/implementation/extractions/combat-death-recovery-runtime-extraction.md`.
