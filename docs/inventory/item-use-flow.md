---
title: Item use flow
doc_type: system
status: verified
owner: devops
code_status: partially-verified
last_verified: 2026-05-11
source_of_truth:
  - docs/reference-and-specs/ITEM_USE_FLOW_SPEC.md
  - GameServer/Network/Handlers/UseItemHandler.cs
  - GameServer/Services/ItemUseService.cs
  - GameServer/Services/PlayerInventoryTransactionService.cs
related_docs:
  - docs/rules/server-transaction-boundary.md
related_code:
  - GameServer/Services/EquipmentActionService.cs
  - GameServer/Services/ItemService.cs
tags:
  - second-brain
  - inventory
  - item-use
---

# Summary

**Graph links:** [[runtime-knowledge-map]] · [[server-validation-and-runtime-rules]] · [[server-transaction-boundary]] · [[server-runtime-architecture]]

## Purpose

Tài liệu hóa canonical flow cho generic `UseItemPacket` path hiện có.

## Scope

- validate `UseItemPacket`
- route use-item action theo item type
- transaction/lock behavior
- response/update shape ở mức flow chính

## Non-goals

- chưa verify các packet chuyên biệt như soil/herb/talisman
- chưa audit toàn bộ repository write path sâu tới từng bảng

# Architecture / Flow

## Inputs

- `UseItemPacket(playerItemId, quantity)`

## Outputs

- `UseItemResultPacket`
- có thể kèm inventory/base stats/current state/learned martial art/cultivation preview/cooldown

## Runtime flow

1. `UseItemHandler` check player đã enter world.
2. Handler gọi `ItemUseService.UseAsync(...)`.
3. `ItemUseService.UseAsync(...)` bọc flow qua `PlayerInventoryTransactionService.ExecuteAsync(...)`.
4. Core flow validate ownership, location inventory, expiration, quantity, item definition.
5. Service route theo `ItemType`:
   - `Equipment`
   - `MartialArtBook`
   - `PillRecipeBook`
   - `Consumable`
6. Handler gửi `UseItemResultPacket` sau khi service trả kết quả.
7. Nếu có `ChangedSkillSnapshot`, handler gọi notifier sau khi gửi result packet.

# Rules / Invariants

- invalid item/quantity phải fail không side effect
- `UseItemPacket` hiện chỉ support nhóm generic tự-contained đã được code route
- transaction wrapper hỗ trợ ambient transaction: nếu `_db.Transaction` đã tồn tại thì không mở transaction lồng mới
- inventory lock dùng advisory transaction lock theo player id

# Data / Contracts

## Config

Consumable path phụ thuộc alchemy/pill definition runtime.

## DB

Write path đi qua transaction service và các inventory/item-related services; chưa trace full repository chain trong lượt này.

## Network / messages

- input: `UseItemPacket`
- output: `UseItemResultPacket`

# Operational Notes

## Failure modes

- player chưa enter world
- invalid inventory item
- invalid quantity
- unsupported item use
- item cooldown

## Monitoring / logs

Nếu audit sâu hơn, nên trace thêm interaction giữa inventory write path và notifier ordering.

# Verification

## Code checked

- `GameServer/Network/Handlers/UseItemHandler.cs`
- `GameServer/Services/ItemUseService.cs`
- `GameServer/Services/PlayerInventoryTransactionService.cs`
- `GameServer/Services/EquipmentActionService.cs`
- `GameServer/Services/ItemService.cs`

## Docs checked

- `docs/reference-and-specs/ITEM_USE_FLOW_SPEC.md`

## Gaps / drift

- legacy spec nói thêm nhiều packet chuyên biệt ngoài generic flow; doc canonical này mới verify generic flow đang code rõ ràng
- handler đang gửi result packet rồi mới notify changed skills; điều này không tự động mâu thuẫn với rule chung, nhưng đáng audit thêm nếu downstream client cần ordering chặt hơn
