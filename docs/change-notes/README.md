# Change Notes Queue

## Purpose

`docs/change-notes/` là hàng đợi bán tự động cho Knowledge Manager.

## Folders

- `inbox/`
  - agent đặt Change Note mới ở đây
- `processed/`
  - Change Note đã được Knowledge Manager xử lý
- `needs-review/`
  - Change Note thiếu dữ kiện hoặc cần Manager/User quyết định

## Naming rule

Ưu tiên format:

- `YYYY-MM-DD-HHMM-agent-task-slug.md`

Ví dụ:

- `2026-05-11-0930-gamedesign-linh-thach-mining-rule.md`

## Processing rule

Knowledge Manager chỉ xử lý note có:

- `doc_type: change_note`
- `status: inbox`
- `requires_knowledge_manager: true`
