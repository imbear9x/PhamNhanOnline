# Legacy Doc Classification

> Classification of pre-existing docs before canonical second-brain migration.

| File | Classification | Reason | Recommended Action |
|---|---|---|---|
| docs/archive/legacy-docs-agents-guide.md | Derived | archived mixed-scope guide, not a live rule source | keep only as legacy reference |
| docs/DOCS_INDEX.md | Derived | navigation artifact rather than design truth | keep, cross-link from new map |
| docs/architecture-and-roadmap/ARCHITECTURE_REFACTOR_20260403.md | Candidate Canonical | contains meaningful architecture decisions but may mix history/current state | extract stable parts later |
| docs/architecture-and-roadmap/ENEMY_BOSS_INSTANCE_FLOW_DRAFT.md | Unknown | explicit draft, not approved truth | keep as draft input |
| docs/architecture-and-roadmap/SERVER_SCALING_ROADMAP.md | Derived | roadmap, not current behavior truth | keep as planning artifact |
| docs/client-unity/CLIENT_REF_WIRING_RULE.md | Candidate Canonical | focused client rule doc | review and possibly migrate to rules/implementation |
| docs/client-unity/client-state-sync-rules.md | Canonical | focused operational rule with clear domain | keep and later map into rules/implementation |
| docs/client-unity/skill-presentation/SKILL_PRESENTATION_PHASE1_PHASE2_GUIDE.md | Candidate Canonical | likely strong implementation reference | split if needed later |
| docs/client-unity/skill-presentation/SKILL_PRESENTATION_PHASE3_ROADMAP.md | Derived | future roadmap | keep as roadmap |
| docs/client-unity/UNITY_CLIENT_SCENE_SETUP.md | Candidate Canonical | strong setup/implementation guidance | map into implementation docs later |
| docs/client-unity/world-scene-readiness.md | Candidate Canonical | focused behavioral doc | verify against code |
| docs/game-design-current-state/01_game_overview.md | Candidate Canonical | broad source but likely mixed snapshot | use as migration seed |
| docs/game-design-current-state/02_system_inventory.md | Candidate Canonical | system inventory useful for splitting | use as migration seed |
| docs/game-design-current-state/03_feature_flows.md | Candidate Canonical | valuable but probably too broad | split by system later |
| docs/game-design-current-state/04_database_design.md | Candidate Canonical | useful for db contract baseline | map to db-contracts later |
| docs/game-design-current-state/05_server_architecture.md | Candidate Canonical | architectural baseline | verify against code before trusting |
| docs/game-design-current-state/06_client_architecture.md | Candidate Canonical | architectural baseline | verify against code before trusting |
| docs/game-design-current-state/07_validation_and_rules.md | Canonical | closest thing to global gameplay/rule source currently | seed rules migration first |
| docs/game-design-current-state/08_error_and_block_cases.md | Derived | likely QA-facing support doc | keep and map into qa later |
| docs/game-design-current-state/09_design_gaps_and_questions.md | Stale | unresolved questions document ages quickly | keep as gap tracker, not truth |
| docs/game-design-current-state/10_agent_context_summary.md | Derived | summary/support artifact | keep only as context aid |
| docs/game-design-current-state/11_design_agent_review_addendum.md | Derived | review note, not canonical truth | keep as support doc |
| docs/game-design-current-state/12_prompt_ready_handoff.md | Derived | handoff artifact, not design truth | keep as workflow artifact |
| docs/game-design-wp/features/home-cave-defense-system.md | Candidate Canonical | working feature draft, maybe future canonical | keep in design workspace until approved |
| docs/game-design-wp/notes/*.md | Derived | exploratory note layer by design | keep in design workspace |
| docs/reference-and-specs/DESCRIPTION_TEMPLATE_SYSTEM.md | Canonical | explicit reusable contract | keep, later map into templates/reference |
| docs/reference-and-specs/GAME_CONFIGS.md | Canonical | strong current config reference | seed config contract migration |
| docs/reference-and-specs/game_design_luyen_dan.md | Candidate Canonical | focused gameplay design doc | verify and map later |
| docs/reference-and-specs/ITEM_USE_FLOW_SPEC.md | Candidate Canonical | focused flow spec | verify and map later |
| docs/reference-and-specs/PHASE1_SYSTEM_REFERENCE.md | Candidate Canonical | current system reference but phase-bound | verify and split later |
| docs/reference-and-specs/SKILL_SYSTEM_COMBAT_FLOW.md | Canonical | strong focused system spec | seed combat/skills canonical docs |
| docs/reports-and-testing/audits/Client Codebase Audit Phase 1.md | Derived | audit evidence only | keep as evidence |
| docs/reports-and-testing/audits/Server Codebase Audit Report Phase 1.md | Derived | audit evidence only | keep as evidence |
| docs/reports-and-testing/testing/Case test report Phase 1.md | Derived | QA evidence only | keep as evidence |
| docs/workflow-and-operations/HUONG_DAN_DOC_LOG_SERVER.md | Canonical | operational runbook | keep as ops doc |
| docs/workflow-and-operations/server-transaction-rules.md | Canonical | explicit current server rule | seed rules/implementation docs |
| docs/workflow-and-operations/UNITY_TOOLING_NOTES.md | Canonical | operational workflow reference | keep as ops doc |
| docs/workflow-and-operations/WORKING_CONTEXT.md | Derived | compact memory aid | keep but do not treat as sole truth |
| ClientUnity/PhamNhanOnline/docs/game-design-client-overview.md | Orphan | useful but outside main docs structure and not yet linked | later link or migrate |
| README.md | Canonical | repo entry point | keep |
| WORKFLOW_RULES.md | Canonical | repo workflow rule | later align with governance additions |
