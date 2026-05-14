---
title: Inventory Bag System — Dev Implementation
doc_type: handoff
status: Ready
owner: dev
source_agent: techdesign
last_updated: 2026-05-15
source_design_doc: docs/game-design-wp/requirements/inventory-bag-system.md
source_tech_design_doc: docs/tech-design/inventory-bag-system.md
expected_output: implementation
---

# Goal

Implement per-character bag capacity, bag grade config, default bag creation, slot-cap checks, and bag upgrade flow per the TechDesign spec.

# Primary Source

- `docs/tech-design/inventory-bag-system.md`

# Scope

1. Add schema: `bag_grade_configs`, `player_bags`
2. Backfill `player_bags` for existing characters
3. Add `BagService` and slot-count / capacity estimation helpers
4. Initialize bag grade 1 during character creation transaction
5. Add `GetBagState` + `UpgradeBag` packets/handlers
6. Enforce inventory-full rejection contract for active actions
7. Add passive overflow contract hook for inbox path

# Key Technical Decisions

- Do **not** add `bag_id` to `player_items`
- Do **not** physically move item rows on bag upgrade
- Used slots = count of occupied active inventory rows (`player_items` rows)
- Capacity estimation must simulate stack merge / stack spill behavior
- Upgrade currency uses linh thạch as inventory currency item via `ItemService.RemoveItemAsync` path
- Dedicated bag upgrade action is acceptable now; full NPC shop framework is deferred

# Important Constraints

- Character must always have exactly one `player_bags` row
- Upgrade target must be strictly higher than current grade
- Upgrade must be atomic: no partial grade update / no partial currency deduction
- Active actions must reject on full inventory; no inbox fallback
- Passive rewards should use overflow sink/inbox path when capacity insufficient

# Acceptance Criteria

See `docs/tech-design/inventory-bag-system.md`.

# Out Of Scope

- Full NPC shop system
- Client UI layout
- Shared storage
- Bag downgrade
- Balance tuning
