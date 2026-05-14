# Tech Design

This folder contains implementation-ready technical design specs produced by `techdesign`.

Tech design sits between GameDesign and Dev:

- GameDesign defines player-facing intent.
- TechDesign maps that intent onto the current codebase, DB, packet, broadcast, runtime, and seed-data patterns.
- Dev implements from the technical spec.

## Rules

- Read the source gameplay docs first.
- Read existing code before proposing packets, broadcasts, schema, services, repositories, or runtime flow.
- Use packet/broadcast language that matches the project, not generic API language.
- Define seed data and test cases as part of the spec.
- Do not change gameplay intent silently. Route design ambiguity back to GameDesign or the user.

## Template

Use:

`docs/templates/techdesign-spec-template.md`

## Suggested File Names

- `sect-weekly-task-pool.md`
- `sect-treasury-escrow.md`
- `spirit-sense-visibility-runtime.md`
- `cave-blueprint-runtime.md`
- `main-quest-chain-runtime.md`

## Handoff Contract

When a spec is ready for implementation, `techdesign` must create a Dev handoff in:

`docs/agent-handoffs/active/YYYYMMDD-<feature-or-slice>-dev.md`

The Dev handoff must link:

- source GameDesign doc
- this TechDesign spec
- expected implementation slices
- DB/schema/seed requirements
- packet/broadcast requirements
- automated and manual test plan

Then update:

`docs/agent-handoffs/QUEUE.md`

with `Owner = dev` and `Status = Ready`.
