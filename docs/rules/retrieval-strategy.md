---
title: Retrieval strategy
doc_type: system
status: reviewed
owner: devops
code_status: runtime-verified
last_verified: 2026-05-11
source_of_truth:
  - openclaw memory status --agent knowledge-manager --json
  - openclaw memory search --agent knowledge-manager ...
related_docs:
  - docs/rules/second-brain-governance.md
  - docs/qa/retrieval-smoke-test.md
tags:
  - second-brain
  - retrieval
  - rules
---

# Summary

## Purpose

Chốt cách retrieval nên hoạt động trong giai đoạn hardening và trước bulk canonicalization.

## Strategy

- canonical project truth lives in repo `docs/`
- knowledge-manager workspace `memory/` is a retrieval aid, not the primary truth store
- memory should contain:
  - summary notes
  - canonical pointers
  - stewardship rules
  - audit cues
- do not fork or duplicate full project truth into workspace memory unless needed for retrieval quality

## Current runtime reality

- built-in memory engine works
- `knowledge-manager` memory indexing works
- search returns results when queried with `--agent knowledge-manager`
- current hits prefer workspace memory seed notes over repo docs
- `sqlite-vec` is missing, so vector recall is degraded

## Operational rule

During current rollout, use this model:

1. create/maintain canonical docs in repo `docs/`
2. add short memory seed summaries when a domain becomes important for retrieval
3. verify retrieval behavior with smoke-test queries
4. improve indexing strategy later if retrieval quality remains weak

## Non-goal for now

- no risky runtime/plugin surgery just to perfect RAG before the docs layer exists
