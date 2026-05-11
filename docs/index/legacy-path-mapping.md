# Legacy Path Mapping

> Current migration mapping from legacy doc groups into the second-brain structure.
>
> Phase 0 intentionally avoids destructive moves. This file records the planned canonical destination before any heavy reshuffle.

| Old Path | New Path | Reason |
|---|---|---|
| docs/game-design-current-state/01_game_overview.md | docs/systems/game-overview.md | broad gameplay overview should become a canonical system overview |
| docs/game-design-current-state/02_system_inventory.md | docs/index/system-inventory.md | inventory/index artifact fits index layer |
| docs/game-design-current-state/03_feature_flows.md | docs/systems/phase1-feature-flow-index.md + docs/systems/world-observer-and-movement-runtime.md | broad flow doc has been split into canonical runtime docs |
| docs/game-design-current-state/04_database_design.md | docs/data-design/db-contracts/database-design-overview.md | best fit for db contract baseline |
| docs/game-design-current-state/05_server_architecture.md | docs/implementation/server-runtime-architecture.md | canonical server architecture landing |
| docs/game-design-current-state/06_client_architecture.md | docs/implementation/client-runtime-architecture.md | canonical client architecture landing |
| docs/game-design-current-state/07_validation_and_rules.md | docs/rules/server-validation-and-runtime-rules.md | canonicalized into grouped runtime rule index |
| docs/game-design-current-state/08_error_and_block_cases.md | docs/qa/error-and-block-cases.md | QA / validation artifact |
| docs/game-design-current-state/09_design_gaps_and_questions.md | docs/index/design-gaps-and-open-questions.md | gap tracker/index artifact |
| docs/reference-and-specs/GAME_CONFIGS.md | docs/data-design/config-contracts/game-configs-phase1.md | canonicalized phase-1 config contract |
| docs/reference-and-specs/SKILL_SYSTEM_COMBAT_FLOW.md | docs/combat/skill-combat-runtime.md | focused combat system doc canonicalized |
| docs/reference-and-specs/ITEM_USE_FLOW_SPEC.md | docs/inventory/item-use-flow.md | item/inventory behavior spec canonicalized |
| docs/reference-and-specs/game_design_luyen_dan.md | docs/cultivation/luyen-dan.md | system doc candidate for cultivation/alchemy branch |
| docs/client-unity/client-state-sync-rules.md | docs/rules/client-state-sync-runtime.md | canonical runtime sync rules |
| docs/client-unity/world-scene-readiness.md | docs/systems/world-scene-readiness-runtime.md | canonical runtime readiness doc |
| docs/workflow-and-operations/server-transaction-rules.md | docs/rules/server-transaction-boundary.md | operational rule canonicalized |
| docs/workflow-and-operations/HUONG_DAN_DOC_LOG_SERVER.md | docs/implementation/server-log-reading-guide.md | operational implementation guide |
| docs/workflow-and-operations/UNITY_TOOLING_NOTES.md | docs/implementation/unity-shared-sync-and-build-guide.md | canonical unity tooling guide |
| docs/architecture-and-roadmap/ARCHITECTURE_REFACTOR_20260403.md | docs/decisions/ADR-candidate-architecture-refactor-20260403.md | historical decision source; may need ADR extraction |
| docs/architecture-and-roadmap/ENEMY_BOSS_INSTANCE_FLOW_DRAFT.md | docs/monsters/enemy-boss-instance-flow.md | draft system candidate |
| docs/architecture-and-roadmap/SERVER_SCALING_ROADMAP.md | docs/implementation/server-scaling-roadmap.md | implementation/roadmap artifact |
| ClientUnity/PhamNhanOnline/docs/game-design-client-overview.md | docs/implementation/client-overview-from-unity-docs.md | currently orphan outside main docs tree |
| docs/reference-and-specs/PHASE1_SYSTEM_REFERENCE.md | docs/systems/auth-character-world-phase1.md | canonical overview extracted for auth/character/world flow |

## Notes

- These are planned canonical destinations, not yet authoritative moves.
- Legacy files stay in place until migration of content, links, and ownership is complete.
- If a legacy file is too broad, the eventual destination may be split across multiple second-brain docs.
