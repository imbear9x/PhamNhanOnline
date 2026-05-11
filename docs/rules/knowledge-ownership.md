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
  - docs/agent-workflows/significant-change-threshold.md
tags:
  - second-brain
  - rules
  - ownership
---

# Summary

## Ownership

- `dev` owns implementation changes and must create Change Notes for significant durable changes
- `dev` may update small implementation notes or directly related technical docs for the task at hand
- `gamedesign` owns evolving design material in `docs/game-design-wp/` and may update related docs elsewhere under `docs/` when handoff or coordination requires it
- `knowledge-manager` owns canonical docs reconciliation from Change Notes, stewardship, audits, conflict capture, and retrieval hygiene
- `devops` owns agent/runtime bootstrap and operational hardening of the knowledge system
- `manager` resolves truth conflicts when implementation, docs, and intended design disagree materially

## Maintenance rule

When code changes materially:

- `dev` should create a Change Note when `docs/agent-workflows/significant-change-threshold.md` says the change is significant and durable
- `knowledge-manager` reconciles canonical docs from those Change Notes when asked
- `dev` may add implementation notes or narrow technical doc updates tied directly to the task
- `dev` must not silently rewrite canonical design truth just to match an implementation shortcut
- if implementation and docs are temporarily out of sync, create a conflict report instead of silently choosing a side

## Anti-drift rule

Do not leave important truth only in chat.
Promote it into repo docs or an explicit conflict/change artifact.
