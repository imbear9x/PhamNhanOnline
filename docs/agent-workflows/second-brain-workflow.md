# Second Brain Workflow

## Purpose

This workflow defines how agents add and maintain AI-readable project memory inside `docs/` without breaking the current repo workflow.

## Core rules

- docs-first, code-verified
- do not delete legacy docs during migration
- do not silently resolve doc/code drift
- create a conflict report when docs and implementation disagree
- prefer small focused updates over giant rewrites

## Canonical doc flow

1. Read the relevant code and current docs.
2. Update or create the canonical doc in the appropriate second-brain folder.
3. If implementation changed, add a change note when the change is meaningful.
4. If the decision is architectural or long-lived, record an ADR.
5. If reality and docs disagree, create a conflict report instead of guessing.
6. Verify with code paths, commands, logs, or test evidence.

## Migration rule

Legacy docs stay in place until explicitly reconciled. Use:

- `docs/index/legacy-knowledge-inventory.md`
- `docs/index/legacy-doc-classification.md`
- `docs/index/legacy-path-mapping.md`

as the baseline migration ledger.
