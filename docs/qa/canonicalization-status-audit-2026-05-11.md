---
title: Canonicalization status audit
doc_type: qa-note
status: reviewed
owner: devops
last_verified: 2026-05-11
source_of_truth:
  - docs/index/legacy-knowledge-inventory.md
  - docs/index/legacy-doc-classification.md
  - docs/index/legacy-path-mapping-review.md
  - docs/index/second-brain-index.md
related_docs:
  - docs/README.md
  - docs/rules/second-brain-governance.md
  - docs/rules/knowledge-ownership.md
  - docs/agent-workflows/second-brain-workflow.md
  - docs/agent-workflows/semi-automatic-knowledge-manager-workflow.md
tags:
  - second-brain
  - audit
  - canonicalization
  - migration
---

# Summary

This audit answers a practical question:

**How much of the existing project documentation has actually been normalized into the second-brain system, and what remains legacy or incomplete?**

Short answer:

- the **second-brain framework is operational**
- a **small set of canonical docs has been created and verified**
- the **majority of legacy documentation has not yet been fully canonicalized**
- the team can already work under the new workflow, but should **not assume old docs are automatically canonical truth**

# Current Operating Reality

## What is already true

The repo now has a functioning second-brain layer with:

- canonical destination folders in `docs/`
- governance and ownership rules
- templates for canonical artifacts
- migration inventory / mapping artifacts
- Knowledge Manager workflow
- semi-automatic Change Note queue
- a live `knowledge-manager` OpenClaw agent for stewardship

## What is not yet true

The repo has **not** completed bulk migration of older docs into canonical second-brain docs.

That means:

- old docs are still preserved in place
- many old docs are still useful inputs
- many old docs are **not yet normalized** to the new canonical structure
- some old docs are broad mixed snapshots that still need split-based migration

# Canonicalization Status by Group

## Group A — Canonicalized and already part of the new second-brain

These docs are already in the new structure and can be treated as real second-brain artifacts.

### Governance / operating rules

- `docs/README.md`
- `docs/rules/second-brain-governance.md`
- `docs/rules/knowledge-ownership.md`
- `docs/rules/retrieval-strategy.md`
- `docs/agent-workflows/second-brain-workflow.md`
- `docs/agent-workflows/knowledge-manager-workflow.md`
- `docs/agent-workflows/semi-automatic-knowledge-manager-workflow.md`
- `docs/agent-workflows/docs-lifecycle.md`
- `docs/agent-workflows/change-note-workflow.md`
- `docs/agent-workflows/config-contract-workflow.md`
- `docs/agent-workflows/adr-workflow.md`
- `docs/agent-workflows/conflict-resolution.md`
- `docs/agent-workflows/knowledge-workflow-validation.md`

### Templates / quality scaffolding

- all docs under `docs/templates/`
- `docs/qa/doc-status-conventions.md`
- `docs/qa/knowledge-acceptance-checklist.md`
- `docs/qa/knowledge-audit-process.md`
- `docs/qa/retrieval-smoke-test.md`

### Initial canonical domain docs

- `docs/combat/skill-combat-runtime.md`
- `docs/systems/phase1-runtime-flow.md`
- `docs/inventory/item-use-flow.md`
- `docs/rules/server-transaction-boundary.md`

### Canonical support artifacts

- `docs/conflicts/item-use-notifier-ordering-review.md`
- `docs/qa/knowledge-audit-2026-05-11-initial.md`
- `docs/change-notes/README.md`

### Status judgment

These materials are already inside the second-brain system and are usable as project memory.

## Group B — Usable legacy docs, but not yet normalized into canonical second-brain truth

These docs still provide meaningful project knowledge, but they should be treated as **legacy inputs or working references**, not automatically as the final canonical layer.

### Client / Unity

- `docs/client-unity/client-state-sync-rules.md`
- `docs/client-unity/world-scene-readiness.md`
- `docs/client-unity/CLIENT_REF_WIRING_RULE.md`
- `docs/client-unity/UNITY_CLIENT_SCENE_SETUP.md`
- `docs/client-unity/skill-presentation/SKILL_PRESENTATION_PHASE1_PHASE2_GUIDE.md`

### Reference / specs

- `docs/reference-and-specs/GAME_CONFIGS.md`
- `docs/reference-and-specs/ITEM_USE_FLOW_SPEC.md`
- `docs/reference-and-specs/SKILL_SYSTEM_COMBAT_FLOW.md`
- `docs/reference-and-specs/game_design_luyen_dan.md`
- `docs/reference-and-specs/DESCRIPTION_TEMPLATE_SYSTEM.md`

### Workflow / operations

- `docs/workflow-and-operations/HUONG_DAN_DOC_LOG_SERVER.md`
- `docs/workflow-and-operations/UNITY_TOOLING_NOTES.md`
- `docs/workflow-and-operations/server-transaction-rules.md`
- `docs/workflow-and-operations/WORKING_CONTEXT.md`

### Game design current state bundle

These files are valuable but mostly still act as migration seeds:

- `docs/game-design-current-state/01_game_overview.md`
- `docs/game-design-current-state/02_system_inventory.md`
- `docs/game-design-current-state/04_database_design.md`
- `docs/game-design-current-state/07_validation_and_rules.md`
- `docs/game-design-current-state/08_error_and_block_cases.md`

### Status judgment

These docs are **useful and often important**, but they are not yet consistently expressed as:

- canonical destination docs
- stable runtime-verified truth
- normalized config contracts / rules / system docs

## Group C — Legacy docs that likely need split-based migration, not direct adoption

These are the most important backlog items if the goal is to convert older knowledge into durable second-brain form.

### Split-expected docs

- `docs/game-design-current-state/03_feature_flows.md`
- `docs/game-design-current-state/05_server_architecture.md`
- `docs/game-design-current-state/06_client_architecture.md`
- `docs/reference-and-specs/PHASE1_SYSTEM_REFERENCE.md`

### Why they are not ready as-is

They are too broad, too mixed, or contain multiple truth types in one place, such as:

- historical notes mixed with current behavior
- architecture mixed with implementation detail
- rules mixed with unresolved assumptions
- many systems in one file

### Required treatment

These should be:

- split by system / runtime concern / contract type
- re-written into smaller canonical docs
- linked back to legacy source material for traceability
- verified against code/config/runtime before being marked `reviewed` or `verified`

## Group D — Working design space, not canonical project memory yet

These files are allowed and useful, but they are not the canonical second-brain layer.

### Working design workspace

- `docs/game-design-wp/features/home-cave-defense-system.md`
- `docs/game-design-wp/notes/...`
- `docs/game-design-wp/requirements/...`

### Status judgment

This remains the design workpad / incubation zone.
It is valid working material, but it only becomes stable project memory after canonicalization into the second-brain structure or through a Change Note workflow.

## Group E — Derived / support / evidence artifacts

These should be preserved, but not treated as primary design truth.

### Derived artifacts

- `docs/game-design-current-state/09_design_gaps_and_questions.md`
- `docs/game-design-current-state/10_agent_context_summary.md`
- `docs/game-design-current-state/11_design_agent_review_addendum.md`
- `docs/game-design-current-state/12_prompt_ready_handoff.md`
- roadmap / draft files in `docs/architecture-and-roadmap/`

### Evidence artifacts

- `docs/reports-and-testing/audits/...`
- `docs/reports-and-testing/testing/...`

### Status judgment

These matter for context, audit trail, and evidence, but they are not the canonical source of intended system truth.

# What This Means For Daily Work

## Can the user and `gamedesign` work normally now?

Yes.

The new workflow is usable now because:

- `gamedesign` can keep using `docs/game-design-wp/` for evolving design work
- significant changes can be emitted as Change Notes
- Knowledge Manager can review and canonicalize them
- canonical project memory has a defined destination and lifecycle

## What should not be assumed

Do **not** assume that every old doc under `docs/` is already compliant with the new second-brain rules.

In practice:

- some old docs are already strong seeds
- some old docs are still only legacy references
- some old docs need split-based migration
- some old docs are evidence only

# Direct Answer To The User's Key Questions

## Have old docs already been fully reviewed and moved into the new system?

No.

They have been:

- inventoried
- classified
- mapped at a planning level
- partially canonicalized in a few representative domains

But they have **not** been fully migrated or normalized yet.

## Are previous docs from the user and `gamedesign` already considered project knowledge?

Partially.

They are currently in one of several states:

- already canonicalized into second-brain docs
- still valid legacy input material
- still exploratory design workspace material
- still mixed/broad docs that require splitting

## Are the old docs already standardized?

Not as a whole.

Only a subset has been standardized into the new second-brain form.

## Has the system already achieved the intended “second brain” goal?

Partially, and importantly:

- **yes** for the framework, operating model, ownership, templates, and first canonical memory layer
- **not yet** for complete legacy knowledge absorption

# Readiness Assessment

## Ready now

- second-brain operating model
- Knowledge Manager stewardship workflow
- Change Note intake workflow
- new canonical doc creation
- controlled migration from now onward

## Not ready to claim yet

- full historical knowledge normalization
- complete canonical source-of-truth coverage across all domains
- broad confidence that any old docs folder is already fully second-brain compliant

# Recommended Next Migration Sequence

## Highest-value next batch

1. `docs/client-unity/client-state-sync-rules.md`
2. `docs/client-unity/world-scene-readiness.md`
3. `docs/reference-and-specs/GAME_CONFIGS.md`
4. `docs/workflow-and-operations/UNITY_TOOLING_NOTES.md`
5. split portions of `docs/reference-and-specs/PHASE1_SYSTEM_REFERENCE.md`

## After that

6. `docs/game-design-current-state/07_validation_and_rules.md`
7. split portions of `docs/game-design-current-state/03_feature_flows.md`
8. split portions of `docs/game-design-current-state/05_server_architecture.md`
9. split portions of `docs/game-design-current-state/06_client_architecture.md`
10. targeted gameplay domains such as mining / cultivation / player-state / economy

# Final Verdict

The project now has a **real second-brain system**, not just a docs folder rebranding.

However, the system is currently in the stage of:

- **framework complete enough to use**
- **seed canonical memory established**
- **legacy migration still incomplete**

So the correct mental model is:

> from this point onward, the project can work in a real second-brain workflow,
> but the backlog of older knowledge still needs structured canonicalization before it can all be treated as normalized project memory.
