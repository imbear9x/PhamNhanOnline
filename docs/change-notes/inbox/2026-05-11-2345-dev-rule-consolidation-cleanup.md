---
doc_type: change_note
status: inbox
created_by: "dev"
created_at: "2026-05-11 23:45"
task_id: "rule-consolidation-cleanup"
agent: "dev"
change_type: "workflow-rule-update"
affected_systems:
  - agent-rules
  - docs-governance
  - second-brain-workflow
  - retrieval-policy
affected_docs:
  - AGENTS.md
  - WORKFLOW_RULES.md
  - docs/archive/legacy-docs-agents-guide.md
  - docs/game-design-wp/AGENTS.md
  - docs/agent-workflows/change-note-workflow.md
  - docs/agent-workflows/second-brain-workflow.md
  - docs/agent-workflows/dev-documentation-workflow.md
  - docs/agent-workflows/knowledge-manager-workflow.md
  - docs/agent-workflows/significant-change-threshold.md
  - docs/agent-workflows/retrieval-depth-policy.md
  - docs/agent-workflows/token-efficiency-policy.md
  - docs/rules/knowledge-ownership.md
  - docs/qa/rule-source-map.md
  - docs/qa/rule-consolidation-report-2026-05-11.md
affected_code: []
affected_configs: []
affected_db: []
requires_knowledge_manager: true
---

# Change Summary

Consolidate fragmented agent rules into one clear global source, one router file, explicit live vs shadow rule mapping, and trigger-based second-brain workflows.

# What Changed

- Replaced the old root rule bundle with a lighter global behavior source plus concise `dev` rules
- Converted `WORKFLOW_RULES.md` into a router instead of a second policy source
- Removed `docs/AGENTS.md` as a non-essential second AGENTS surface and archived the old mixed-scope guide
- Marked `dev` and `gamedesign` workspace shadow rule files as non-live
- Tightened `gamedesign` live rule boundaries around docs-only work and docs-level grounding
- Clarified `dev` vs `knowledge-manager` ownership around Change Notes and canonical reconciliation
- Added explicit threshold, retrieval-depth, and token-efficiency workflow docs
- Added QA reporting for live/shadow rule mapping and consolidation results

# Why

- The previous setup had rule fragmentation, duplicate policy, and mixed live vs non-live sources
- A second `AGENTS.md` under `docs/` was easy to misread as a live behavioral rule source
- `gamedesign` had conflicting guidance around how much repo grounding it could use
- Change Note triggering and retrieval depth were too broad and easy to over-apply

# Affected Systems

- Repo rule boundary
- Agent governance
- Second-brain trigger policy
- Retrieval policy

# Affected Docs

- `AGENTS.md`
- `WORKFLOW_RULES.md`
- `docs/archive/legacy-docs-agents-guide.md`
- `docs/game-design-wp/AGENTS.md`
- `docs/agent-workflows/change-note-workflow.md`
- `docs/agent-workflows/second-brain-workflow.md`
- `docs/agent-workflows/dev-documentation-workflow.md`
- `docs/agent-workflows/knowledge-manager-workflow.md`
- `docs/agent-workflows/significant-change-threshold.md`
- `docs/agent-workflows/retrieval-depth-policy.md`
- `docs/agent-workflows/token-efficiency-policy.md`
- `docs/rules/knowledge-ownership.md`
- `docs/qa/rule-source-map.md`
- `docs/qa/rule-consolidation-report-2026-05-11.md`

# Affected Code

- None

# Config / Data Changes

- None in repo runtime topology
- External workspace agent rule files were only clarified, not remapped in `openclaw.json`

# DB Changes

- None

# QA / Test Notes

- Verified rule-source mapping against `/home/vm-01/.openclaw/openclaw.json`
- Verified root/global/router/docs-scope files after consolidation
- Verified shadow headers on non-live `dev` and `gamedesign` workspace rule files

# Potential Conflicts / Risks

- Some existing docs outside the touched files may still mention older workflow wording
- Manager, DevOps, and Knowledge Manager still rely on their workspace rules plus a pointer back to repo root global behavior
- The cleanup intentionally did not rewrite broader OpenClaw runtime topology

# Questions For Manager

- Should the next cleanup pass also align older support docs such as `docs/DOCS_INDEX.md` and related indexes to the new lighter rule layout
