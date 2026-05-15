---
title: NPC System — TechDesign refinement
doc_type: handoff
status: Ready
owner: techdesign
last_updated: 2026-05-15
---

# Goal

TechDesign review and refinement of the NPC system requirement spec to produce an implementation-ready technical spec for Dev handoff.

# Source of truth

- `docs/game-design-wp/requirements/npc-system.md` — requirement spec (primary)
- `docs/game-design-wp/features/npc-system.md` — canonical feature design

# Scope

## Must do
- Confirm existing NPC interaction runtime coverage in code.
- Define technical base NPC interaction module:
  - range validation
  - action panel dispatch
  - NPC availability state
- Define timed NPC lifecycle support:
  - scheduled spawn
  - event-triggered spawn
  - countdown source
  - `closing_soon` transition at T-3m
  - forced close of non-dialogue UIs
  - despawn/hidden behavior
- Define shop modules:
  - unlimited stock semantics
  - linh thạch-only currency path
  - buy rejection when bag full
  - concurrent multi-player use of same NPC
  - sell list filtering and server validation
- Define entry-action module:
  - unlock gating
  - destination resolution
  - transfer execution
- Confirm whether current quest/NPC systems already support `talk_to_npc` objective events cleanly.
- Identify which parts are reusable NPC base framework vs per-action plugin logic.

## Out of scope
- Balance/pricing values.
- New NPC action types outside current supported set.
- Quest-dynamic dialogue.

# Known conflicts / caveats

- Timed NPC `closing_soon` force-closing active UIs may need explicit client/server coordination.
- Current shop implementation, if present, may already assume another currency path or stock semantics — must be reconciled.
- Entry-to-map/dungeon actions may currently be implemented outside NPC runtime; TD should ground where responsibility belongs.

# Readiness gate for Dev handoff

Dev handoff is blocked until:
1. TD confirms current NPC runtime coverage vs missing pieces.
2. TD specifies timed NPC lifecycle and UI-close behavior.
3. TD grounds shop transaction semantics and concurrency handling.
4. TD grounds map/dungeon entry action path.

# Related docs

- `docs/game-design-wp/requirements/npc-system.md`
- `docs/game-design-wp/features/npc-system.md`
- `docs/game-design-wp/features/main-progression-quest-chain.md`
- `docs/game-design-wp/shared-rules.md`
