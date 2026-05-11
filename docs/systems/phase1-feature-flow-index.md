---
title: Phase 1 feature flow index
doc_type: system
status: reviewed
owner: dev
code_status: legacy-doc-grounded
last_verified: 2026-05-11
source_of_truth:
  - docs/game-design-current-state/03_feature_flows.md
related_docs:
  - docs/systems/auth-character-world-phase1.md
  - docs/systems/world-observer-and-movement-runtime.md
  - docs/combat/skill-combat-runtime.md
  - docs/inventory/item-use-flow.md
  - docs/rules/server-validation-and-runtime-rules.md
tags:
  - phase1
  - feature-flows
  - index
---

# Summary

Đây là canonical index rút gọn từ legacy `03_feature_flows.md`.
Nó không cố nhét toàn bộ mọi chi tiết implementation vào một file nữa, mà chỉ giữ các flow chính và trỏ sang doc domain phù hợp.

# Core feature groups

## Auth / character / world entry

- login
- get character list
- create character
- enter world
- map join bootstrap

Canonical entry point:
- `docs/systems/auth-character-world-phase1.md`

## Movement / map / observer

- local movement
- movement sync local -> server
- observer spawn/despawn/move
- map change / travel / zone switch

Canonical entry point:
- `docs/systems/world-observer-and-movement-runtime.md`

## Combat

- attack / cast skill
- receive damage
- monster death

Canonical entry points:
- `docs/combat/skill-combat-runtime.md`
- `docs/rules/server-validation-and-runtime-rules.md`

## Inventory and items

- inventory sync
- equip item
- drop inventory item
- pickup ground reward
- use item

Canonical entry points:
- `docs/inventory/item-use-flow.md`
- `docs/rules/server-validation-and-runtime-rules.md`

## Cultivation / progression

- cultivation
- breakthrough
- potential allocation

Current canonical status:
- partial only
- still needs deeper domain docs under `docs/cultivation/`

## Quest

Legacy feature-flow doc itself states quest flow is not implemented in the current phase.
Do not treat quest as an implemented canonical runtime domain yet.

# Migration note

Legacy `03_feature_flows.md` should now be treated as a historical broad source file, not the preferred first-stop canonical doc.
