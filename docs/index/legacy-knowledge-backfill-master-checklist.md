---
title: Legacy knowledge backfill master checklist
doc_type: index
status: reviewed
owner: devops
last_verified: 2026-05-12
tags:
  - second-brain
  - legacy
  - canonicalization
  - checklist
---

# Legacy Knowledge Backfill Master Checklist

Mục tiêu của checklist này là theo dõi coverage thật của các domain **có evidence trong code** và trạng thái tri thức tương ứng trong second-brain.

## Cách dùng

Mỗi domain đi qua 4 trạng thái vận hành chính theo audit hiện tại:

- `good` = đã có canonical tri thức đủ dùng
- `partial` = đã có evidence và có tri thức hỗ trợ, nhưng canonical coverage hoặc scope decision chưa đủ chốt
- `needs-review` = có evidence code nhưng còn conflict/ambiguity chưa thể chốt sạch
- `missing` = có evidence code nhưng chưa có tri thức đủ để canonicalize

## Master domain inventory

| Domain | Extraction | Clarification | Canonical | Status | Ghi chú |
|---|---|---|---|---|---|
| auth / account / login / register | done | n/a | done | good | Canonical coverage đủ dùng qua `docs/systems/auth-character-world-phase1.md`, `docs/systems/phase1-runtime-flow.md` |
| character creation / bootstrap | done | n/a | done | good | Canonicalized in `docs/systems/character-creation-and-bootstrap-runtime.md` |
| session reconnect / resume | done | n/a | done | good | Canonicalized in `docs/systems/reconnect-and-session-resume-runtime.md` |
| world entry / world snapshot publish | done | n/a | done | good | Covered by phase-1 + map runtime docs |
| map instances / world membership / zone selection | done | done | done | good | Covered by `docs/maps/map-instance-and-world-entry-runtime.md` and rule/config docs |
| portal travel | done | done | done | needs-review | Canonical doc exists, nhưng conflict mở về interaction mode và topology semantics |
| world movement / observer sync | done | n/a | done | good | Covered by movement/observer/runtime docs |
| enemy runtime / spawn / patrol / death / rewards | done | done | done | needs-review | Canonical doc exists, nhưng spawn/reset semantics còn mở |
| combat skill execution | done | n/a | done | good | Canonicalized in `docs/combat/skill-combat-runtime.md` |
| skill ownership / loadout / equipment-granted skills | done | n/a | done | good | Canonicalized in `docs/combat/skill-ownership-and-loadout-runtime.md` |
| inventory / item read model / inventory transactions | done | n/a | done | good | Canonicalized in `docs/inventory/inventory-runtime.md` |
| equipment / equipment slots / stat modifiers | done | partial | done | good | Canonicalized in `docs/inventory/equipment-runtime.md` |
| generic item use | done | n/a | done | needs-review | Canonical flow exists, nhưng notifier ordering conflict còn mở |
| player stats / final stat recompute / state clamping | done | partial | done | good | Canonicalized in `docs/systems/player-stats-runtime.md` |
| martial arts / active martial art progression hooks | done | n/a | done | good | Canonicalized in `docs/progression/martial-art-ownership-and-activation-runtime.md` |
| cultivation / breakthrough | done | partial | done | good | Canonicalized in `docs/progression/cultivation-breakthrough-and-potential-runtime.md` |
| potential allocation | done | partial | done | good | Canonicalized inside progression/stats runtime docs |
| combat death recovery / return home | done | n/a | done | good | Canonicalized in `docs/combat/combat-death-and-return-home-runtime.md` |
| loot / ground reward / pickup | done | n/a | done | good | Canonicalized in `docs/loot/ground-reward-runtime.md` |
| notifications / inbox / acknowledgement | done | n/a | done | good | Canonicalized in `docs/systems/player-notification-runtime.md` |
| practice sessions / pause-resume-cancel / result acknowledgement | done | done | done | partial | Canonical lifecycle doc exists in `docs/systems/practice-session-runtime.md`, but taxonomy/scope remains open |
| alchemy crafting / recipe preview / craft start | done | done | done | good | Canonicalized in `docs/alchemy/alchemy-recipe-and-craft-runtime.md`; deferred herb-maturity behavior explicitly documented |
| home cave / garden / herb farming | done | done | partial | needs-review | Extraction + clarification exist, but player-facing accessibility remains unverified |
| descriptions / template text compilation | done | pending | partial | partial | Extraction exists; canonical-vs-reference scope still undecided |
| game config contract surface | partial | n/a | done | good | Config-contract docs currently sufficient for surfaced keys |
| server validation / transaction / runtime rule layer | done | n/a | done | good | Covered by shared rule docs |
| random tables / reward RNG | done | n/a | done | good | Canonicalized in `docs/rules/random-table-and-luck-runtime.md` |
| metrics / diagnostics / server observability | done | pending | partial | partial | Extraction exists; canonical scope still undecided |

## Current audit summary

### good
- auth / account / login / register
- character creation / bootstrap
- session reconnect / resume
- world entry / world snapshot publish
- map instances / world membership / zone selection
- world movement / observer sync
- combat skill execution
- skill ownership / loadout / equipment-granted skills
- inventory / item read model / inventory transactions
- equipment / equipment slots / stat modifiers
- player stats / final stat recompute / state clamping
- martial arts / active martial art progression hooks
- cultivation / breakthrough
- potential allocation
- combat death recovery / return home
- loot / ground reward / pickup
- notifications / inbox / acknowledgement
- alchemy crafting / recipe preview / craft start
- game config contract surface
- server validation / transaction / runtime rule layer
- random tables / reward RNG

### partial
- practice sessions / pause-resume-cancel / result acknowledgement
- descriptions / template text compilation
- metrics / diagnostics / server observability

### needs-review
- portal travel
- enemy runtime / spawn / patrol / death / rewards
- generic item use
- home cave / garden / herb farming

### missing
- none in the current code-evidenced audit

## Operational rule

- Chỉ tính domain có evidence trực tiếp trong code.
- Không suy domain từ chat hoặc ví dụ người dùng nêu ra.
- Nếu code/design chưa đủ rõ, giữ `needs-review` hoặc `partial` thay vì đoán.
- Extraction note là nguyên liệu hỗ trợ, không tự động đồng nghĩa với canonical coverage.
