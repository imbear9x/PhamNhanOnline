# title
Randomness runtime extraction

# scope
Compiled random tables, luck-modified exclusive rolls, direct chance checks, and current gameplay systems depending on this service.

# source files
- `GameServer/Randomness/GameRandomService.cs`
- `GameServer/Randomness/IGameRandomService.cs`
- `GameServer/Program.cs`
- `GameServer/Runtime/EnemyRewardRuntimeService.cs`
- `GameServer/Runtime/CharacterCultivationService.cs`

# current runtime behavior
- `GameRandomService` compiles configured random tables at startup into immutable in-memory tables keyed by `tableId` (`GameServer/Randomness/GameRandomService.cs`).
- Current implemented table mode is `Exclusive`; preview and roll both build effective entries first, then `Roll(...)` selects one entry by cumulative chance over a `ChanceScale` integer range (`GameServer/Randomness/GameRandomService.cs`).
- If explicit entry chances do not sum to 100%, exclusive tables auto-fill the remainder into either the configured none entry or an auto-created none entry (`GameServer/Randomness/GameRandomService.cs`).
- Luck can shift chance from the none entry into eligible tagged entries according to `GameRandomLuckModifierConfig`; this affects both full-table rolls and direct `CheckChance(...)` calls (`GameServer/Randomness/GameRandomService.cs`).
- Enemy reward runtime uses table rolls for item reward selection, while cultivation uses chance checks for breakthrough success (`GameServer/Runtime/EnemyRewardRuntimeService.cs`, `GameServer/Runtime/CharacterCultivationService.cs`).

# validations / guards
- Empty table ids, duplicate ids, empty entry ids, duplicate entry ids, invalid per-entry chance values, and over-100% total chance all throw during table compilation (`GameServer/Randomness/GameRandomService.cs`).
- Unsupported table modes throw when preview is requested (`GameServer/Randomness/GameRandomService.cs`).
- Final effective chance total must normalize back to `ChanceScale`, otherwise preview throws (`GameServer/Randomness/GameRandomService.cs`).

# config/data dependencies
- Random-table config loaded into `GameRandomConfig` at startup.
- Luck modifier metadata per table and per direct chance-check call.
- Consumer systems such as enemy rewards and breakthrough logic.

# client/server touch points
- No direct packet handler surface was visible; randomness is a server-internal dependency of gameplay outcomes.
- `Program.cs` includes preview usage for startup/runtime checks.

# edge cases
- If no eligible entries qualify for luck bonus, none-entry probability remains unchanged.
- Direct chance checks cap applied luck bonus so effective chance never exceeds 100%.
- Tables without explicit none entry still behave exclusively because remainder is auto-created as none.

# unclear or suspicious behavior
- Only `Exclusive` mode is implemented in this file even though enum/config surface may suggest broader future modes.
- Canonical docs should keep random-service semantics separate from reward-balance intent, which lives in data not in this service.

# suggested canonical target docs
- `docs/rules/random-table-and-luck-runtime.md`
