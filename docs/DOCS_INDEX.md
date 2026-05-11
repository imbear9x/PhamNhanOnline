# Mục Lục Docs

Tài liệu trong `docs/` đã được gom theo nhóm chức năng để dễ tìm nhanh theo ngữ cảnh làm việc.

## 1. Gốc `docs/`

- `AGENTS.md`
  - working guide cho agent/developer khi thao tác trong repo
- `DOCS_INDEX.md`
  - mục lục điều hướng này

## 2. Workflow Entry Points

- `../WORKFLOW_RULES.md`
  - điểm vào workflow đầu phiên giữa user và các agent
- `agent-handoffs/README.md`
  - quy ước giao việc và handoff giữa các agent
- `agent-handoffs/QUEUE.md`
  - hàng chờ handoff/đầu việc đang mở
- `agent-handoffs/SESSION_STARTERS.md`
  - câu mở đầu phiên cho các workflow chính
- `agent-handoffs/TEMPLATE.md`
  - template ghi handoff chuẩn
- `agent-workflows/second-brain-workflow.md`
  - workflow duy trì AI-readable project memory
- `agent-workflows/semi-automatic-knowledge-manager-workflow.md`
  - workflow bán tự động cho Change Note queue
- `rules/second-brain-governance.md`
  - governance ngắn cho lớp second-brain
- `index/second-brain-index.md`
  - mục lục của knowledge layer mới
- `change-notes/README.md`
  - hướng dẫn queue change note

## 3. `game-design-wp/`

- `README.md`
  - hướng dẫn workspace game design
- `features/README.md`
  - nơi gom mô tả feature
- `requirements/README.md`
  - nơi gom yêu cầu/constraint
- `notes/README.md`
  - quy ước ghi chú trong workspace
- `notes/conversation-log.md`
  - log trao đổi phục vụ game design workspace

## 4. `architecture-and-roadmap/`

- `ARCHITECTURE_REFACTOR_20260403.md`
  - tổng hợp refactor kiến trúc và UI ngày `2026-04-03`
- `ENEMY_BOSS_INSTANCE_FLOW_DRAFT.md`
  - draft thiết kế flow enemy, boss, map instance và reward tương lai
- `SERVER_SCALING_ROADMAP.md`
  - lộ trình gia cố nền server cho các phase tiếp theo

## 5. `reference-and-specs/`

- `PHASE1_SYSTEM_REFERENCE.md`
  - luồng hệ thống phase 1 đang chạy từ login tới world, movement và observer sync
- `SKILL_SYSTEM_COMBAT_FLOW.md`
  - combat skill system phía server và runtime flow chính
- `ITEM_USE_FLOW_SPEC.md`
  - đặc tả luồng dùng vật phẩm
- `DESCRIPTION_TEMPLATE_SYSTEM.md`
  - contract description template cho `item`, `skill`, `martial art`
- `GAME_CONFIGS.md`
  - danh sách `game_configs` và mapping hiện có trong code
- `game_design_luyen_dan.md`
  - game design cho tính năng luyện đan

## 6. `client-unity/`

- `UNITY_CLIENT_SCENE_SETUP.md`
  - checklist setup scene/hierarchy Unity
- `world-scene-readiness.md`
  - cơ chế readiness trong scene `World`
- `client-state-sync-rules.md`
  - quy tắc ownership và reload state phía client
- `CLIENT_REF_WIRING_RULE.md`
  - rule wiring reference trong scene/prefab
- `skill-presentation/SKILL_PRESENTATION_PHASE1_PHASE2_GUIDE.md`
  - trạng thái hệ thống skill presentation client đã chạy
- `skill-presentation/SKILL_PRESENTATION_PHASE3_ROADMAP.md`
  - roadmap mở rộng skill presentation cho phase sau

## 7. `workflow-and-operations/`

- `WORKING_CONTEXT.md`
  - rule ngắn, quyết định kiến trúc và lưu ý dễ quên giữa các session
- `UNITY_TOOLING_NOTES.md`
  - quy ước sync `GameShared`, build CLI và workflow Unity
- `server-transaction-rules.md`
  - transaction boundary rule phía server
- `HUONG_DAN_DOC_LOG_SERVER.md`
  - cách đọc metrics/log server hiện tại

## 8. `reports-and-testing/`

- `audits/Server Codebase Audit Report Phase 1.md`
  - audit server phase 1
- `audits/Client Codebase Audit Phase 1.md`
  - audit Unity client phase 1
- `testing/Case test report Phase 1.md`
  - checklist test theo phase và test run

## 9. Second Brain Layer

- `systems/`, `combat/`, `skills/`, `cultivation/`, `resource-mining/`, `inventory/`, `quests/`, `maps/`, `monsters/`, `npc/`, `economy/`, `rules/`
  - vùng canonical docs theo domain
- `data-design/config-contracts/`
  - contract tài liệu hóa config/data-driven rules
- `data-design/db-contracts/`
  - contract tài liệu hóa DB-facing structures
- `decisions/`
  - ADR và các quyết định bền vững
- `change-notes/`
  - queue bán tự động cho change note gồm `inbox/`, `processed/`, `needs-review/`
- `implementation/`
  - implementation note và bootstrap/migration note
- `qa/`
  - checklist, audit process, retrieval smoke test
- `conflicts/`
  - doc/code/runtime conflict report
- `templates/`
  - template chuẩn để tạo doc mới
- `index/`
  - index, project map, migration ledger

## Ghi chú

- Các file trong `architecture-and-roadmap/` có thể là lịch sử refactor hoặc định hướng tương lai; không mặc định coi là trạng thái đã implement.
- `CODEX_PERSISTENT_MEMORY.md` đã được gộp vào `workflow-and-operations/WORKING_CONTEXT.md`.
- `UNITY_GAMESHARED_WORKFLOW.md` đã được gộp vào `workflow-and-operations/UNITY_TOOLING_NOTES.md`.
- `UI_REFACTOR_20260403.md` đã được gộp vào `architecture-and-roadmap/ARCHITECTURE_REFACTOR_20260403.md`.
- `Skill Docs/SKILL_PRESENTATION_DOC_INDEX.md` đã được gộp vào `client-unity/skill-presentation/SKILL_PRESENTATION_PHASE1_PHASE2_GUIDE.md`.
