# title
Combat skill execution runtime extraction

# scope
Server-authoritative skill cast release, impact resolution, effect application, and result shaping for combat runtime.

# source files
- `GameServer/Runtime/SkillExecutionService.cs`
- `GameServer/Network/Handlers/AttackEnemyHandler.cs`
- `GameServer/Runtime/CombatDefinitionCatalog.cs`
- `GameServer/Runtime/WorldRuntimeSettlementService.cs`
- `GameServer/Runtime/CharacterRuntimeService.cs`
- `GameServer/Runtime/SkillRuntimeNotifier.cs`
- `GameShared/Packets/Packets/CombatPackets.cs`

# current runtime behavior
- `SkillExecutionService` resolves skill definitions from `CombatDefinitionCatalog` and applies ordered effects at `OnCastRelease` and `OnHit` timing (`GameServer/Runtime/SkillExecutionService.cs`).
- Cast/impact processing first resolves the caster from map-instance runtime context; missing skill or missing caster returns failure-style impact events instead of crashing (`GameServer/Runtime/SkillExecutionService.cs`).
- Effects are filtered by trigger timing, pass through chance checks, then dispatch by target scope between self and primary target (`GameServer/Runtime/SkillExecutionService.cs`).
- Player-side effects mutate runtime resources/statuses through `CharacterRuntimeService`; enemy-side effects mutate instance enemy runtime state and can produce kill/damage summaries (`GameServer/Runtime/SkillExecutionService.cs`).
- Current visible target handling is primarily self, primary player target, or primary enemy runtime id; broader multi-target/AOE semantics are not visible in this file (`GameServer/Runtime/SkillExecutionService.cs`).

# validations / guards
- Unknown skill id returns `SkillNotLearned` style failure in impact resolution (`GameServer/Runtime/SkillExecutionService.cs`).
- Missing caster in the instance returns `CharacterNotInWorldInstance` (`GameServer/Runtime/SkillExecutionService.cs`).
- Zero/unsupported effect magnitudes and unsupported resource/target combinations collapse to no-op summaries rather than throwing in many branches (`GameServer/Runtime/SkillExecutionService.cs`).
- Chance, duration normalization, and target resolution are guarded before applying status/stat effects (`GameServer/Runtime/SkillExecutionService.cs`).

# config/data dependencies
- Combat/skill definitions from `CombatDefinitionCatalog`.
- Runtime state from online player sessions and map-instance enemy runtime state.
- Packet/result surfaces from combat packet definitions.

# client/server touch points
- Attack handlers and combat packets invoke this service indirectly for cast and hit resolution.
- Skill/runtime notifiers publish changed owned-skill/runtime state when relevant.
- Result packets carry success/failure, damage, and enemy-kill outcomes derived from these runtime events.

# edge cases
- Skills with no `OnHit` effects can still resolve as success with zero applied impact.
- Self-target skills with a target payload still collapse back to caster application.
- Missing primary target runtime id yields empty/no-op effect summaries.

# unclear or suspicious behavior
- Broader target-scope coverage beyond `Self` and `Primary` is not visible here even though data model may imply richer skill semantics.
- Canonical combat docs should call out that unsupported effect/target combinations often fail silently as no-op rather than returning explicit errors.

# suggested canonical target docs
- `docs/combat/skill-cast-and-impact-runtime.md`
- `docs/combat/combat-effect-application-rules.md`
