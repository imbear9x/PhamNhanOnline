---
title: Knowledge ownership
doc_type: system
status: reviewed
owner: devops
code_status: policy
last_verified: 2026-05-11
source_of_truth:
  - AGENTS.md
  - docs/rules/second-brain-governance.md
related_docs:
  - docs/agent-workflows/knowledge-manager-workflow.md
  - docs/agent-workflows/dev-documentation-workflow.md
tags:
  - second-brain
  - rules
  - ownership
---

# Summary

## Ownership

- `dev` owns first-pass implementation truth and code-linked canonical updates after technical changes
- `gamedesign` owns evolving design material in `docs/game-design-wp/` and may update related docs elsewhere under `docs/` when handoff or task coordination requires it
- `knowledge-manager` owns stewardship, canonicalization support, audits, conflict capture, and retrieval hygiene
- `devops` owns agent/runtime bootstrap and operational hardening of the knowledge system

## Maintenance rule

When code changes materially:

- update the relevant canonical doc, or
- create/update an implementation note when canonical placement is not stable yet, or
- create/update a change note, or
- create a conflict report if the implementation and doc are temporarily out of sync

## Anti-drift rule

Do not leave important truth only in chat.
Promote it into repo docs or an explicit conflict/change artifact.
