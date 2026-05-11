---
doc_type: change_note
status: inbox
created_by: "Dev Agent"
created_at: "2026-05-11 20:48"
task_id: "openclaw-local-architecture-doc"
agent: "dev"
change_type: "docs"
affected_systems:
  - openclaw
  - local-agent-runtime
  - telegram-routing
  - 9router-model-routing
affected_docs:
  - docs/workflow-and-operations/openclaw-local-architecture.md
affected_code: []
affected_configs:
  - ~/.openclaw/openclaw.json
affected_db: []
requires_knowledge_manager: true
knowledge_manager_status: pending
---

# Change Summary

Thêm tài liệu canonical mô tả kiến trúc OpenClaw local trên chính máy hiện tại, gồm:

- file config trung tâm
- vị trí các workspace agent
- vị trí session store / transcript / trace
- luồng Telegram -> binding -> agent -> model -> reply

# What Changed

- Tạo `docs/workflow-and-operations/openclaw-local-architecture.md`

# Why

- User cần một bản đồ dễ nhìn để hiểu end-to-end OpenClaw local stack trước khi đào sâu từng phần.
- Kiến thức này trước đó nằm rải rác trong chat và config machine-local, chưa có canonical doc trong repo docs.

# Evidence

Tài liệu được đối chiếu trực tiếp với:

- `~/.openclaw/openclaw.json`
- cây thư mục `~/.openclaw/`
- workspace files hiện có
- session store hiện có dưới `~/.openclaw/agents/`

# Risks / Follow-up

- Đây là tài liệu machine-specific, có thể drift nếu sau này đổi binding, model, bot account, hoặc layout runtime.
- Knowledge Manager nên quyết định sau này có cần thêm index entry hoặc cross-link từ docs vận hành khác hay không.
