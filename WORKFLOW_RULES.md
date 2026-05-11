# Workflow Rules Router

This file is a router, not a second behavior policy source.

If the user says `đọc rule làm việc của chúng ta`, `đọc workflow rồi bắt đầu`, or equivalent:

1. Read `AGENTS.md`.
2. Read the live per-agent rule file for the current agent.
3. Read extra workflow docs only when the task triggers them.

Trigger map:

- handoff or cross-agent execution: `docs/agent-handoffs/README.md`
- significant durable change: `docs/agent-workflows/significant-change-threshold.md`
- second-brain documentation work: `docs/agent-workflows/second-brain-workflow.md`
- change-note details: `docs/agent-workflows/change-note-workflow.md`
- retrieval scoping: `docs/agent-workflows/retrieval-depth-policy.md`
- token-bloat troubleshooting: `docs/agent-workflows/token-efficiency-policy.md`

Do not duplicate policy from `AGENTS.md` here.
