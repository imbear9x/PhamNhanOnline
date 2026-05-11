# Project Knowledge Map

## Mục tiêu

File này là điểm vào tổng quan cho second-brain knowledge system của `PhamNhanOnline`.

## Knowledge Layers

### 1. Legacy knowledge

Các nhóm tài liệu đã tồn tại trước rollout second-brain:

- `docs/game-design-current-state/`
- `docs/reference-and-specs/`
- `docs/workflow-and-operations/`
- `docs/reports-and-testing/`
- `docs/client-unity/`
- `docs/architecture-and-roadmap/`
- `docs/game-design-wp/`

### 2. Canonical second-brain structure

Các nhóm tài liệu chuẩn đang được thiết lập:

- `docs/systems/`
- `docs/combat/`
- `docs/skills/`
- `docs/cultivation/`
- `docs/resource-mining/`
- `docs/inventory/`
- `docs/quests/`
- `docs/maps/`
- `docs/monsters/`
- `docs/npc/`
- `docs/economy/`
- `docs/rules/`
- `docs/data-design/config-contracts/`
- `docs/data-design/db-contracts/`
- `docs/decisions/`
- `docs/change-notes/`
- `docs/implementation/`
- `docs/qa/`
- `docs/conflicts/`
- `docs/agent-workflows/`
- `docs/templates/`
- `docs/index/`

## Source-of-truth model

- Docs / ADR / Config Contract = intended design
- Code / DB / runtime config = current implementation
- QA / logs / runtime behavior = actual behavior

Khi 3 lớp này lệch nhau, không được tự chọn im lặng. Phải tạo conflict report.

## Current migration status

- Legacy inventory: `docs/index/legacy-knowledge-inventory.md`
- Legacy classification: `docs/index/legacy-doc-classification.md`
- Path mapping: `docs/index/legacy-path-mapping.md`
- Canonicalization audit: `docs/qa/canonicalization-status-audit-2026-05-11.md`
- Knowledge health: đã có baseline audit, nhưng bulk migration vẫn còn tiếp diễn

## Working convention

- Legacy docs được giữ để truy vết lịch sử
- Docs mới theo template chuẩn sẽ là nền cho retrieval và governance
- Chỉ Knowledge Manager / role phù hợp mới được reconcile sang canonical docs theo workflow

## Obsidian graph entry

- [[knowledge-graph-entry]]
- [[runtime-knowledge-map]]
- [[architecture-knowledge-map]]
- [[config-and-contract-map]]
- [[workflow-and-governance-map]]

## Legacy knowledge backfill control points

- [[legacy-knowledge-backfill-master-checklist]]
- [[legacy-knowledge-backfill-runbook]]

## Canonical examples already established

- `docs/combat/skill-combat-runtime.md`
- `docs/inventory/item-use-flow.md`
- `docs/systems/phase1-runtime-flow.md`
- `docs/systems/auth-character-world-phase1.md`
- `docs/systems/world-scene-readiness-runtime.md`
- `docs/rules/server-transaction-boundary.md`
- `docs/rules/client-state-sync-runtime.md`
- `docs/rules/server-validation-and-runtime-rules.md`
- `docs/data-design/config-contracts/game-configs-phase1.md`
- `docs/implementation/unity-shared-sync-and-build-guide.md`
