# Legacy Path Mapping Review

## Purpose

Hardening review before bulk canonicalization.

## Findings

### Safe mappings

These look safe enough to migrate in bulk once verification work is scheduled:

- `docs/reference-and-specs/GAME_CONFIGS.md` -> `docs/data-design/config-contracts/...`
- `docs/client-unity/client-state-sync-rules.md` -> `docs/rules/...`
- `docs/workflow-and-operations/HUONG_DAN_DOC_LOG_SERVER.md` -> `docs/implementation/...`
- `docs/workflow-and-operations/UNITY_TOOLING_NOTES.md` -> `docs/implementation/...`
- `docs/reference-and-specs/ITEM_USE_FLOW_SPEC.md` -> `docs/inventory/...`

### Mappings that should expect split, not 1:1 migration

These legacy docs are too broad or mixed to treat as a single canonical destination:

- `docs/game-design-current-state/03_feature_flows.md`
- `docs/game-design-current-state/05_server_architecture.md`
- `docs/game-design-current-state/06_client_architecture.md`
- `docs/reference-and-specs/PHASE1_SYSTEM_REFERENCE.md`

Rule: mark these as **split expected** during migration.

### Naming adjustments recommended

Current mapping file uses a few destination names that are too close to legacy naming or too vague.

Recommended canonical naming direction:

- favor runtime/system intent over legacy title mirroring
- prefer `*-runtime`, `*-flow`, `*-rules`, `*-guide`, `*-contract` naming
- avoid implying `verified current truth` for roadmap/draft docs

### Migration safety rule

Do not move files physically as part of early canonicalization.
Create canonical docs first, keep legacy docs in place, and link them through change notes or mapping docs.
