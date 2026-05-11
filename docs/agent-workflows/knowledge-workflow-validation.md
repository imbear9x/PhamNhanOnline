# Knowledge Workflow Validation

## Goal

Validate that the second-brain workflow can operate end to end before bulk legacy canonicalization.

## Validation scenario completed

1. Created second-brain templates/workflows/governance.
2. Added canonical sample docs.
3. Bootstrapped `knowledge-manager` agent and workspace memory.
4. Indexed memory successfully.
5. Queried memory with `--agent knowledge-manager`.
6. Confirmed retrieval works, but currently prefers seed notes over direct repo docs.

## Result

- workflow is operational
- retrieval is usable at bootstrap level
- retrieval quality is not yet ideal
- this is acceptable for pre-bulk-canonicalization phase

## Decision

Proceed with bulk canonicalization only under this rule:

- repo docs remain source of truth
- workspace memory remains retrieval support
- each migrated domain should get a short retrieval seed if discoverability matters
