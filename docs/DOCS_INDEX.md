# Mục Lục Docs Hiện Tại

Tài liệu trong `docs/` hiện được chia theo 4 nhóm để dễ đọc và tránh trùng lặp.

## 1. Tài liệu tham chiếu cho hệ đang chạy

- `PHASE1_SYSTEM_REFERENCE.md`
  - luồng phase 1 đang chạy từ login tới world, movement và observer sync
- `world-scene-readiness.md`
  - cơ chế readiness trong scene `World`
- `SKILL_SYSTEM_COMBAT_FLOW.md`
  - combat skill system phía server và luồng runtime chính
- `ITEM_USE_FLOW_SPEC.md`
  - trạng thái và đặc tả hiện tại của luồng dùng vật phẩm
- `DESCRIPTION_TEMPLATE_SYSTEM.md`
  - contract mô tả cho `item`, `skill`, `martial art`: template, token, fallback và rule render TMP
- `GAME_CONFIGS.md`
  - danh sách `game_configs` đang có trong code
- `HUONG_DAN_DOC_LOG_SERVER.md`
  - cách đọc metrics/log server hiện tại
- `UNITY_CLIENT_SCENE_SETUP.md`
  - checklist dựng scene/hierarchy Unity theo client hiện tại

## 2. Tài liệu quy ước làm việc và tooling

- `WORKING_CONTEXT.md`
  - các rule ngắn, quyết định kiến trúc và lưu ý dễ quên giữa các session
- `UNITY_TOOLING_NOTES.md`
  - quy ước sync `GameShared`, build CLI và workflow Unity

## 3. Tài liệu roadmap hoặc draft tương lai

- `SERVER_SCALING_ROADMAP.md`
- `ENEMY_BOSS_INSTANCE_FLOW_DRAFT.md`
- `Skill Docs/SKILL_PRESENTATION_PHASE3_ROADMAP.md`

Các file trong nhóm này đều là định hướng tương lai, không phải danh sách tính năng đã hoàn thành.

## 4. Tài liệu lịch sử refactor

- `ARCHITECTURE_REFACTOR_20260403.md`
  - đã gộp cả nội dung refactor kiến trúc và refactor UI của ngày `2026-04-03`

## Ghi chú dọn tài liệu

- `CODEX_PERSISTENT_MEMORY.md` đã được gộp vào `WORKING_CONTEXT.md`
- `UNITY_GAMESHARED_WORKFLOW.md` đã được gộp vào `UNITY_TOOLING_NOTES.md`
- `UI_REFACTOR_20260403.md` đã được gộp vào `ARCHITECTURE_REFACTOR_20260403.md`
- `Skill Docs/SKILL_PRESENTATION_DOC_INDEX.md` đã được gộp vào `Skill Docs/SKILL_PRESENTATION_PHASE1_PHASE2_GUIDE.md`
