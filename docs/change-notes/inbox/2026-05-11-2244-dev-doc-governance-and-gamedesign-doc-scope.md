---
doc_type: change_note
status: inbox
created_by: "dev"
created_at: "2026-05-11 22:44"
task_id: "rule-update-dev-doc-workflow-gamedesign-doc-scope"
agent: "dev"
change_type: "workflow-rule-update"
affected_systems:
  - agent-rules
  - second-brain-workflow
  - docs-governance
affected_docs:
  - AGENTS.md
  - docs/game-design-wp/AGENTS.md
  - docs/agent-workflows/dev-documentation-workflow.md
  - docs/agent-workflows/change-note-workflow.md
  - docs/rules/knowledge-ownership.md
affected_code: []
affected_configs: []
affected_db: []
requires_knowledge_manager: true
---

# Change Summary

Làm rõ contract tài liệu cho `dev` sau các task implementation có ý nghĩa, đồng thời hợp thức hóa quyền của `gamedesign` được làm việc và ghi docs trong toàn bộ `docs/` thay vì bị kẹt ở `docs/game-design-wp/`.

# What Changed

- Thêm workflow mới `docs/agent-workflows/dev-documentation-workflow.md`
- Bổ sung root `AGENTS.md` để `dev` phải theo workflow docs implementation khi task đủ lớn
- Mở rộng rule của `gamedesign` để được chỉnh sửa tài liệu ở mọi nơi dưới `docs/`, nhưng vẫn giữ `docs/game-design-wp/` là khu làm việc chính
- Cập nhật `docs/rules/knowledge-ownership.md` để phản ánh ownership mới rõ hơn
- Mở rộng `docs/agent-workflows/change-note-workflow.md` để ăn khớp hơn với implementation-side documentation

# Why

- Rule cũ chưa đủ rõ để `dev` luôn biết khi nào cần `Implementation Spec`, khi nào update canonical docs, và khi nào chỉ cần Change Note
- Rule live của `gamedesign` đang mâu thuẫn với workflow handoff vì cấm sửa ngoài `docs/game-design-wp/` nhưng lại yêu cầu cập nhật `docs/agent-handoffs/`
- Cần làm quyền của `gamedesign` danh chính ngôn thuận thay vì sống bằng ngoại lệ ngầm

# Affected Systems

- Governance của agent rules
- Implementation-side documentation workflow
- Shared docs ownership giữa `dev`, `gamedesign`, và `knowledge-manager`

# Affected Docs

- `AGENTS.md`
- `docs/game-design-wp/AGENTS.md`
- `docs/agent-workflows/dev-documentation-workflow.md`
- `docs/agent-workflows/change-note-workflow.md`
- `docs/rules/knowledge-ownership.md`

# Affected Code

- None

# Config / Data Changes

- None

# DB Changes

- None

# QA / Test Notes

- Verified by reading the updated rule files and checking the resulting rule alignment against the live agent workspace mapping described in `docs/workflow-and-operations/openclaw-local-architecture.md`

# Potential Conflicts / Risks

- `WORKFLOW_RULES.md` and some older support docs still use older phrasing and may need later cleanup if you want full wording consistency
- Actual OpenClaw runtime mount/write restrictions for `gamedesign` may still depend on machine/runtime config even though repo rules are now aligned

# Questions For Manager

- Có muốn tiếp tục rút gọn `WORKFLOW_RULES.md` và hợp nhất các rule second-brain trùng lặp trong đợt cleanup tiếp theo không
