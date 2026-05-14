---
title: Herb Farming System — TechDesign refinement
doc_type: handoff
status: Ready
owner: techdesign
last_updated: 2026-05-14
---

# Goal

TechDesign review and refinement of the herb farming / linh dược requirement spec to produce an implementation-ready technical spec for Dev handoff.

# Source of truth

- `docs/game-design-wp/requirements/herb-farming-system.md` — requirement spec (primary)
- `docs/game-design-wp/features/herb-farming-system.md` — canonical feature design

# Scope

## Must do
- Review requirement spec for technical feasibility and flag any design-vs-implementation conflicts.
- Define schema/data model changes needed:
  - herb template table
  - linh thổ template table
  - plot/soil/herb lifecycle entity changes if any
  - linh dược item template table (one template per phẩm cấp)
- Plan and confirm migration path:
  - remove/migrate `required_herb_maturity` guard from `AlchemyService.cs`
  - recipe validation to use linh dược item template identity instead
- Confirm or spec herb item expiry timer behavior:
  - server-side expiry processing while player is offline
  - immediate deletion on expiry (no spoiled-item state)
- Confirm or spec harvest/extract rejection when inventory is full:
  - reject in full, no partial grant, no inbox fallback
- Define plot count config table (per blueprint grade) — data-driven, no runtime logic required.
- Identify any packet/handler gaps between current server-side `HerbService.cs` and client wiring.

## Out of scope
- Balance values (timers, drop rates, yields) — data design, not TD spec.
- Client UI layout — UI team / Dev.
- Wild herb nodes — not in design.

# Known conflicts / caveats

- `AlchemyService.cs` currently has active `required_herb_maturity` guard that blocks herb recipes — this must be refactored, not worked around.
- Client/network handlers for garden actions (insert soil, plant, harvest, extract) are not yet confirmed wired in live build — TD should flag whether this is a server gap or client gap.
- Living herb item expiry must be server-authoritative and processed offline; if current item expiry infrastructure does not support offline processing, this is a blocker for Dev handoff.

# Readiness gate for Dev handoff

Dev handoff is blocked until:
1. TD confirms or defines client/handler wiring for plot actions.
2. `required_herb_maturity` migration path is confirmed and spec'd.
3. Schema/data model changes are written.

# Related docs

- `docs/game-design-wp/requirements/herb-farming-system.md`
- `docs/game-design-wp/features/herb-farming-system.md`
- `docs/game-design-wp/features/multi-stage-crafting.md`
- `docs/game-design-wp/shared-rules.md`
