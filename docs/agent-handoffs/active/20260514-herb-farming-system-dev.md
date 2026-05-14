---
title: Herb Farming System — Dev Implementation
doc_type: handoff
status: Ready
owner: dev
source_agent: techdesign
last_updated: 2026-05-14
source_design_doc: docs/game-design-wp/requirements/herb-farming-system.md
source_tech_design_doc: docs/tech-design/herb-farming-system.md
expected_output: implementation
---

# Goal

Implement the herb farming system per the TechDesign spec. Primary deliverables:
- DB migrations
- HerbService refactor (two-step harvest/extract)
- Background expiry sweep
- Network handlers + new packet types
- Alchemy guard removal

# Source Docs

- **TechDesign spec** (primary): `docs/tech-design/herb-farming-system.md`
- **Requirement**: `docs/game-design-wp/requirements/herb-farming-system.md`

Read the TechDesign spec first. It contains all DB/schema, packet, service, and test details.

# Implementation Slices (in order)

1. DB migration + entity update
2. HerbService refactor (HarvestAsync / ExtractHerbAsync)
3. Background expiry sweep (HerbExpiryBackgroundService)
4. Network handlers + packets (HerbPackets.cs + 6 handlers)
5. Alchemy guard removal

# Key Constraints

- `HarvestAsync` must NOT produce item outputs — only move herb entity to inventory with expire_at.
- `ExtractHerbAsync` produces items + deletes herb entity in one transaction.
- `CheckInventoryHasSpace` is a stub returning `true` — do not implement cap logic, just add the method.
- `HerbGrowthStage.Perfect` renamed to `ThousandYear` in C# — DB integer value 3 stays unchanged.
- New packet IDs: 200–215. New MessageCode range: 6000–6011.
- No broadcast packets needed for garden actions.
- Background sweep: silent delete, no player notification.

# Acceptance Criteria

See `docs/tech-design/herb-farming-system.md` — Dev Acceptance Criteria section.

# Out Of Scope

- Inventory slot cap system
- Herb drop from quái (EnemyRewardRuntimeService wiring)
- Client UI
- Balance values
