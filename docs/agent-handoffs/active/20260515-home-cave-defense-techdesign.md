---
title: Home Cave Defense / Raid / Looting — TechDesign refinement
doc_type: handoff
status: Ready
owner: techdesign
last_updated: 2026-05-15
---

# Goal

TechDesign review and refinement of the home-cave-defense requirement spec to produce an implementation-ready technical spec for Dev handoff.

# Source of truth

- `docs/game-design-wp/requirements/home-cave-defense.md` — requirement spec (primary)
- `docs/game-design-wp/features/home-cave-defense.md` — canonical feature design

# Scope

## Must do
- Define technical state machine for:
  - private home
  - world deployment cast
  - normal world cave
  - under attack
  - collapse / looting window
  - packed/destroyed return-to-blueprint state
- Confirm data model for blueprint-carried contents/layout persistence.
- Confirm how spirit-sense threshold gating is evaluated for visibility, interaction, and attack initiation.
- Define attack runtime with matching-grade phá phủ charm:
  - charm validation
  - timer lifecycle
  - attacker cooldown persistence
  - owner compensation payout path
- Define door-defense map runtime:
  - 10-player cap enforcement
  - attacker/defender/guest role handling
  - free-for-all PvP rules inside contested map
- Define collapse resolution:
  - immediate defender ejection
  - close entry from outside
  - structure asset random-drop roll path
  - blueprint reattachment for undropped assets
  - looting-window timer and forced teleport end
- Define disconnect/offline behavior during looting window:
  - drop picked loot immediately
  - persistence/recovery on crash/restart
- Define exact fallback path if returned blueprint cannot fit owner inventory.
- Identify which parts already exist in code versus net-new implementation.

## Out of scope
- Balance values (HP, ratios, charm durations, cooldown days).
- Final UI art/layout.
- Advanced anti-abuse/anti-alt solutions beyond explicit requirement rules.

# Known conflicts / caveats

- Large parts of the runtime are not yet code-grounded from GD side; TD must separate confirmed-existing behavior from net-new implementation.
- Blueprint return on inventory-full is unresolved at technical delivery-path level.
- Attacker cooldown exists in feature intent but needs persistence/state design.
- Guest handling at attack start may need explicit runtime role semantics.

# Readiness gate for Dev handoff

Dev handoff is blocked until:
1. TD writes a technical state machine for all cave phases.
2. TD grounds blueprint content persistence / return path.
3. TD defines contested-map runtime and disconnect handling.
4. TD identifies existing code coverage vs new implementation slices.

# Related docs

- `docs/game-design-wp/requirements/home-cave-defense.md`
- `docs/game-design-wp/features/home-cave-defense.md`
- `docs/game-design-wp/features/spirit-beast.md`
- `docs/game-design-wp/features/death-penalty.md`
- `docs/game-design-wp/features/spirit-sense.md`
- `docs/game-design-wp/features/crafting-talisman-formation.md`
- `docs/game-design-wp/shared-rules.md`
