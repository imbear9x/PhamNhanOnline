---
title: Knowledge Manager bootstrap
doc_type: implementation-note
status: verified
date: 2026-05-09
owner: devops
related_docs:
  - docs/agent-workflows/knowledge-manager-workflow.md
  - docs/qa/retrieval-smoke-test.md
tags:
  - second-brain
  - implementation
---

# Goal

Bootstrap a dedicated Knowledge Manager agent/workspace without breaking the existing OpenClaw routing model.

# Implementation Summary

- use the real OpenClaw multi-agent layout already present on this machine
- create a standalone workspace under `~/.openclaw/workspaces/knowledge-manager`
- create an OpenClaw agent entry without binding any channel to it
- keep repo documentation canonical inside `PhamNhanOnline/docs/`
- keep memory bootstrap opt-in and lightweight

# Verification

- confirmed `openclaw agents add` exists in local docs/CLI
- confirmed current agents via `openclaw agents list --json`
- confirmed memory engine exists via `openclaw memory status --json`

# Remaining Risks

- no routing is attached yet, so the agent is available for explicit use but not direct user chat
- retrieval quality remains low until memory docs are seeded and indexed
