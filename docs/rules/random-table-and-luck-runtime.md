---
title: Random table and luck runtime
doc_type: rule
status: verified
owner: knowledge-manager
last_verified: 2026-05-12
source_of_truth:
  - docs/implementation/extractions/randomness-runtime-extraction.md
related_code:
  - GameServer/Randomness/GameRandomService.cs
  - GameServer/Randomness/IGameRandomService.cs
  - GameServer/Runtime/EnemyRewardRuntimeService.cs
  - GameServer/Runtime/CharacterCultivationService.cs
---

# Random table and luck runtime

## Runtime behavior

- `GameRandomService` compiles configured random tables at startup into immutable in-memory tables by `tableId`.
- Implemented table mode is `Exclusive`; preview and roll build effective entries, then roll over a `ChanceScale` integer range.
- Exclusive tables auto-fill chance remainder into a configured none entry or an auto-created none entry.
- Luck shifts chance from the none entry into eligible tagged entries according to `GameRandomLuckModifierConfig`.
- Luck affects both full table rolls and direct `CheckChance(...)` calls.
- Enemy rewards consume table rolls; breakthrough consumes chance checks.

## Guards and edge cases

- Empty/duplicate table ids, empty/duplicate entry ids, invalid chance values, and totals above 100% throw during compilation.
- Unsupported table modes throw when previewed.
- Direct chance checks cap luck bonus so effective chance cannot exceed 100%.
- Reward balance intent lives in data/config, not in this runtime service.

## Verification

Supported by `docs/implementation/extractions/randomness-runtime-extraction.md`.
