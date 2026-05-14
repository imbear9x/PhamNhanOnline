---
title: Inventory Bag System — TechDesign refinement
doc_type: handoff
status: Ready
owner: techdesign
last_updated: 2026-05-14
---

# Goal

TechDesign review and refinement of the inventory bag system requirement spec to produce an implementation-ready technical spec for Dev handoff.

# Source of truth

- `docs/game-design-wp/requirements/inventory-bag-system.md` — requirement spec (primary)
- `docs/game-design-wp/features/inventory-bag-system.md` — canonical feature design

# Scope

## Must do
- Confirm whether bag is already modeled as a character attribute in current backend or needs new schema.
- Define schema for bag entity (character id, grade, slot count) if not already present.
- Define bag grade config table schema (grade, slot_count, upgrade_cost, display_name).
- Confirm character creation flow assigns default bag grade 1 — add if missing.
- Confirm NPC shop framework supports non-item transactions (bag upgrade); spec extension if needed.
- Spec bag upgrade transaction as atomic: validate grade, deduct linh thạch, replace grade, transfer items — no partial state on failure.
- Confirm item transfer on upgrade is safe: all items must appear in new bag, no loss possible.
- Confirm server-side downgrade rejection path exists or needs to be added.
- Confirm inventory-full check is performed server-side before active actions (harvest, extract) grant items.

## Out of scope
- Balance values (slot counts, prices) — data design.
- Client UI layout — Dev/UI team.
- Shared account storage — not in design.

# Known conflicts / caveats

- Bag may not yet exist as a distinct entity in backend; may currently be an implicit fixed slot count on character — TD must verify.
- NPC shop likely handles item purchases only; bag upgrade is a non-item transaction that may require framework extension.
- Item transfer on bag upgrade must be atomic or recoverable — crash mid-transaction must not cause item loss.

# Readiness gate for Dev handoff

Dev handoff is blocked until:
1. TD confirms bag schema (new or existing).
2. TD confirms character init flow assigns bag grade 1.
3. TD confirms NPC shop can handle bag upgrade transaction or specs the extension.
4. TD confirms atomic item transfer safety on upgrade.

# Related docs

- `docs/game-design-wp/requirements/inventory-bag-system.md`
- `docs/game-design-wp/features/inventory-bag-system.md`
- `docs/game-design-wp/features/npc-system.md`
- `docs/game-design-wp/features/inbox-mail-system.md`
- `docs/game-design-wp/shared-rules.md`
