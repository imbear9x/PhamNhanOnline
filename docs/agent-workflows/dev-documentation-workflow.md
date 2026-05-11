# Dev Documentation Workflow

## Purpose

This workflow defines what the `dev` agent must document before and after meaningful implementation work so implementation truth does not stay trapped in code diffs or chat.

## Ownership

- `dev` owns first-pass implementation truth after code changes
- `knowledge-manager` owns stewardship, cleanup, and canonicalization support
- `gamedesign` owns intended design and handoff intent, not final implementation truth
- `manager` resolves truth conflicts when intended design and implementation reality disagree materially

## Before Coding: `Implementation Spec` Trigger

Create or update an implementation-focused doc before coding when one or more of these are true:

- the handoff or requirement is clear on gameplay intent but not technical shape
- the task changes DB schema or persistence model
- the task adds or changes packets, shared contracts, or client-server interaction flow
- the task changes validation ownership, transaction flow, or authoritative runtime sequencing
- the task spans multiple modules and could be implemented in several materially different ways

## Acceptable Pre-Code Outputs

Use one of these, depending on scope:

- update the active handoff doc with the missing technical shape if the gap is small
- create an implementation note in `docs/implementation/` when the task needs a compact technical plan
- update an existing canonical runtime/config/system doc when it is already the obvious design surface

## After Coding: Definition Of Done For Docs

After meaningful implementation work, `dev` must do at least one of the following:

- update the relevant canonical doc under `docs/`
- create or update an implementation note when no stable canonical home exists yet
- create a conflict report if code reality and current docs are temporarily out of sync

For medium or large system changes, also create a Change Note in `docs/change-notes/inbox/` when `docs/agent-workflows/significant-change-threshold.md` says the change is significant and durable.

## Change Note Trigger

Create a Change Note when the implementation changes durable technical knowledge such as:

- runtime behavior across module boundaries
- DB or config meaning
- packet or shared contract shape
- validation or transaction rules
- operational rollout or migration expectations

Do not require a Change Note for trivial formatting, obvious local refactors, or tiny fixes with no durable doc value.

## Minimum Technical Coverage

When documenting the implementation, capture the parts that future agents would otherwise guess wrong:

- affected modules and key files
- main runtime flow
- DB writes or state transitions
- packets, requests, responses, or events involved
- validation and authority boundaries
- verification performed
- remaining risks or follow-up
