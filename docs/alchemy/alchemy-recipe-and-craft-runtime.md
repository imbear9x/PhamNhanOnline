---
title: Alchemy recipe and craft runtime
doc_type: system
status: verified
owner: knowledge-manager
last_verified: 2026-05-12
source_of_truth:
  - docs/implementation/extractions/alchemy-runtime-extraction.md
related_code:
  - GameServer/Services/AlchemyService.cs
  - GameServer/Services/AlchemyCraftQueryService.cs
  - GameServer/Services/AlchemyCraftActionService.cs
  - GameServer/Services/AlchemyPracticeService.cs
---

# Alchemy recipe and craft runtime

## Runtime behavior

- Craft validation resolves the recipe, verifies learned ownership, rejects unsupported required-herb-maturity recipes, validates selected inventory/catalyst ids, computes max craftable count, allocates inputs, and builds a craft rate plan.
- Query handlers expose learned recipe sets, recipe details, and previewed cost/success information before craft start.
- `CraftPillHandler` enters through `AlchemyCraftActionService.StartCraftAsync`; response can include created practice session data, consumed items, refreshed inventory, and recipe payload.
- Success-rate planning uses recipe mastery progression, optional inputs, and segmented rates.
- Completion integrates with practice and notification systems; craft start does not imply synchronous item output.

## Guards and limits

- Unknown or unlearned recipes fail immediately.
- Required herb maturity is intentionally blocked as phase-later functionality.
- Handler layer requires entered-world player session.

## Verification

Supported by `docs/implementation/extractions/alchemy-runtime-extraction.md`.
