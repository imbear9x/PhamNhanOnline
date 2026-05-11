---
title: Server validation and runtime rules
doc_type: system
status: reviewed
owner: dev
code_status: mixed
last_verified: 2026-05-11
source_of_truth:
  - docs/game-design-current-state/07_validation_and_rules.md
  - docs/rules/server-transaction-boundary.md
related_docs:
  - docs/inventory/item-use-flow.md
  - docs/combat/skill-combat-runtime.md
  - docs/systems/auth-character-world-phase1.md
tags:
  - server
  - validation
  - rules
  - runtime
---

# Summary

Đây là canonical rule index cho các validation/runtime guard quan trọng của server phase hiện tại. Nó gom các rule đã được legacy docs mô tả, nhưng chuẩn hóa lại thành nhóm dễ tra cứu hơn trong second-brain.

**Graph links:** [[runtime-knowledge-map]] · [[architecture-knowledge-map]] · [[server-runtime-architecture]] · [[auth-character-world-phase1]] · [[world-observer-and-movement-runtime]] · [[skill-combat-runtime]] · [[item-use-flow]]

# Rule groups

## Network and auth entry rules

- packet có `[RequireAuth]` phải qua auth middleware
- packet realtime có `MinIntervalMs > 0` phải qua rate limit middleware
- packet có validator phải qua validation middleware trước handler
- reconnect token phải tồn tại, chưa revoked, chưa expired, và phù hợp trạng thái session

## Character lifecycle and restriction rules

- current phase chỉ cho một character mỗi account
- character name phải qua normalize/validation rule
- packet bị chặn nếu character đang ở restricted state ngoài allowlist recovery/query

## Movement and interaction rules

- movement sync phải dùng finite coordinates và state hợp lệ
- server movement là authoritative-clamped theo tốc độ/effective step
- interaction/combat/pickup phải qua `WorldInteractionGate`
- portal / zone switch phải qua validation map/zone tương ứng

## Combat and skill rules

- skill loadout slot phải hợp lệ và skill phải được sở hữu
- `AttackEnemyPacket` phải qua target/range/cooldown validation

## Inventory and item rules

- equip/use/drop/consume/remove/move yêu cầu item tồn tại, đúng owner, đúng location, chưa expired
- item đang equipped hoặc inserted không được rời inventory bất hợp lệ
- equip/unequip phải tôn trọng slot count config
- `UseItemPacket` chỉ hỗ trợ các item type/effect đã được service support
- martial art book không được học trùng

## Cultivation / alchemy / practice rules

- bắt đầu cultivation chỉ hợp lệ ở private home instance và state cho phép
- breakthrough chỉ hợp lệ khi đang ở cap và có next realm
- allocate potential phải qua tier/preview/catalog rule
- alchemy yêu cầu learned recipe và ownership/quantity hợp lệ cho input
- recipe có `required_herb_maturity` hiện chưa support ở phase này
- practice session chỉ hợp lệ ở world/state hợp lệ

# Usage rule

Doc này là entry point canonical cho server-side rule landscape.
Khi cần chi tiết sâu hơn cho một domain cụ thể, hãy tách thành doc chuyên biệt thay vì để một file rules khổng lồ tiếp tục phình ra.

# Confidence and limitations

Doc này chuẩn hóa một legacy rule bundle lớn thành canonical index, nhưng chưa chuyển toàn bộ từng rule thành doc domain riêng đã code-verified từng cái. Vì vậy nó là `reviewed`, không phải `verified` toàn phần.
