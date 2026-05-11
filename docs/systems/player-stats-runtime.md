---
title: Player stats and final-stat runtime
doc_type: system
status: verified
owner: knowledge-manager
last_verified: 2026-05-12
source_of_truth:
  - docs/implementation/extractions/player-stats-runtime-extraction.md
related_code:
  - GameServer/Services/CharacterFinalStatService.cs
  - GameServer/Runtime/CharacterRuntimeService.cs
  - GameServer/Runtime/CharacterRuntimeCalculator.cs
  - GameServer/Runtime/PotentialStatCatalog.cs
---

# Player stats and final-stat runtime

## Runtime behavior

- Character snapshots expose raw base stats, final stats, current state, realm display metadata, and potential previews.
- Final stats are recomputed from raw base stats plus allocated-potential bonuses, equipped-item modifiers, and active martial-art stage bonuses.
- Integer stats apply raw base + potential flat + percent modifier + flat modifier. Percent values above `1` are normalized as percentages and truncated toward zero.
- Luck is accumulated as `double` with flat and percent bonuses before final assignment.
- Authoritative base-stat mutation clamps current HP/MP/Stamina to new maxima and emits base-stat/current-state notifications.
- Damage and resource mutations operate on current state only and can flip runtime state to `CombatDead` or `LifespanExpired`.

## Guards and edge cases

- Attach requires base stats and current state.
- Final-stat application no-ops when snapshot stats/state are missing.
- Missing active martial-art progress/definitions skip martial bonuses.
- Potential allocation validates supported targets, tier availability, positive amount, sufficient potential, and current-tier bounds.
- Percent normalization can mask inconsistent data formats.

## Verification

Supported by `docs/implementation/extractions/player-stats-runtime-extraction.md`.
