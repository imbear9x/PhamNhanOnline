---
title: World instance membership invariant
doc_type: system
status: reviewed
owner: dev
code_status: code-verified
last_verified: 2026-05-11
source_of_truth:
  - GameServer/World/MapManager.cs
  - GameServer/World/WorldInterestService.cs
  - docs/implementation/extractions/map-runtime-extraction.md
related_docs:
  - docs/maps/map-instance-and-world-entry-runtime.md
  - docs/game-design-wp/clarifications/map-design-clarification.md
related_code:
  - GameServer/Runtime/MapInstanceLifecycleService.cs
tags:
  - second-brain
  - rules
  - world
  - instances
---

# Summary

Batch-1 invariant: một player không được tồn tại hợp lệ trong nhiều live map instance cùng lúc. Runtime hiện có repair behavior để xóa membership cũ trước khi add vào instance mới.

**Graph links:** [[map-instance-and-world-entry-runtime]] · [[server-runtime-architecture]]

## Purpose

Tách riêng một invariant quan trọng để downstream docs không phải lặp lại và để phân biệt rõ gameplay rule với runtime repair behavior.

## Scope

- membership uniqueness theo player
- rejoin/jump giữa private/configured/public instance
- cleanup membership cũ khi runtime phát hiện lệch trạng thái

## Non-goals

- không mô tả mọi rule observer visibility
- không thay thế doc world-entry flow đầy đủ

# Architecture / Flow

## Inputs

- player join/rejoin request vào target map instance
- runtime state hiện có trong `MapManager`

## Outputs

- player chỉ còn membership ở target live instance

## Runtime flow

1. Khi `MapManager` chuẩn bị add player vào private/configured/public instance, runtime gọi đường remove khỏi instance cũ nếu phát hiện player còn nằm ở instance khác của cùng map.
2. Sau đó runtime mới add player vào target instance.
3. Nếu player rời world hoặc bị remove khỏi world manager, service leave/remove sẽ gỡ membership hiện tại.

# Rules / Invariants

- Một player chỉ có một `mapId + instanceId + zoneIndex` hợp lệ tại một thời điểm.
- Runtime repair path hiện là **best-effort cleanup** trước khi add vào instance mới, không phải giấy phép cho state trùng tồn tại lâu dài.
- Nếu instance private rỗng sau khi player rời đi, instance bị hủy ngay.

# Data / Contracts

## Config

Không có config riêng cho invariant này.

## DB

Không có DB contract trực tiếp; đây là runtime ownership/state invariant.

## Network / messages

Invariant này ảnh hưởng gián tiếp đến `MapJoinedPacket`, `WorldRuntimeSnapshotPacket`, và visibility packets vì các packet đó giả định player chỉ thuộc một world context.

# Operational Notes

## Failure modes

- Nếu publish snapshot khi player trỏ vào instance đã mất, `WorldInterestService` chỉ log lỗi và bỏ publish.
- Nếu add player vào instance thất bại sau repair, `MapManager` có thể throw exception ở một số join path.

## Monitoring / logs

Nên kiểm tra các lỗi publish snapshot không resolve được instance nếu nghi có drift membership/runtime state.

# Verification

## Code checked

- `GameServer/World/MapManager.cs`
- `GameServer/World/WorldInterestService.cs`

## Docs checked

- `docs/implementation/extractions/map-runtime-extraction.md`
- `docs/game-design-wp/clarifications/map-design-clarification.md`

## Gaps / drift

- Invariant này đã rõ ở runtime, nhưng chưa có evidence về cross-map transactional guarantee rộng hơn ngoài các code path đã kiểm tra.
