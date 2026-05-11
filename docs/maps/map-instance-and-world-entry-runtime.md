---
title: Map instance and world entry runtime
doc_type: system
status: reviewed
owner: dev
code_status: code-verified-with-open-design-questions
last_verified: 2026-05-11
source_of_truth:
  - GameServer/World/MapCatalog.cs
  - GameServer/World/MapDefinition.cs
  - GameServer/World/MapTemplate.cs
  - GameServer/World/MapManager.cs
  - GameServer/World/WorldInterestService.cs
  - GameServer/Runtime/MapInstanceLifecycleService.cs
  - docs/implementation/extractions/map-runtime-extraction.md
related_docs:
  - docs/game-design-wp/clarifications/map-design-clarification.md
  - docs/maps/portal-travel-runtime.md
  - docs/rules/world-instance-membership-invariant.md
  - docs/data-design/config-contracts/world-map-runtime-configs-batch1.md
  - docs/conflicts/map-travel-topology-vs-portal-semantics.md
related_code:
  - GameServer/World/MapTravelTopologyTypes.cs
  - GameShared/Packets/Packets/WorldPackets.cs
  - GameServer/DTO/NetworkModelMapper.cs
tags:
  - second-brain
  - maps
  - world
  - instances
  - zones
---

# Summary

Canonical batch-1 runtime cho map loading, instance selection, world entry, full snapshot republish, và safe redirect khi instance đóng.

**Graph links:** [[runtime-knowledge-map]] · [[server-runtime-architecture]] · [[phase1-runtime-flow]] · [[world-instance-membership-invariant]] · [[portal-travel-runtime]]

## Purpose

Xác nhận runtime hiện tại đưa player vào đúng map instance, giữ mỗi player ở một live instance duy nhất, và republish đầy đủ world state khi vào hoặc đổi map.

## Scope

- map catalog loading
- public/private/configured instance join
- auto-select public zone
- world entry context + snapshot publish
- instance close redirect

## Non-goals

- không canonicalize mọi packet combat trong world
- không chốt semantic thiết kế cuối cùng cho travel topology ngoài phần evidence đã rõ

# Architecture / Flow

## Inputs

- map templates, adjacent maps, zone slots, spawn points, portals từ DB
- enemy instance config dùng để quyết định map có chạy theo configured-instance path hay không
- player runtime state hiện tại (`mapId`, `zone`, `position`)

## Outputs

- live `MapInstance` phù hợp cho player
- `MapJoinedPacket`
- `WorldRuntimeSnapshotPacket`
- `MapInstanceClosedPacket` khi instance bị đóng

## Runtime flow

1. `MapCatalog` load toàn bộ map, spawn point, portal, zone slot vào immutable lookup tại startup.
2. `WorldInterestService.EnsurePlayerInWorld(...)` resolve map hiện tại hoặc fallback về home map nếu `mapId` không hợp lệ.
3. `MapManager.JoinInstance(...)` chọn một trong ba đường:
   - private-per-player map -> owner-bound private instance
   - map có `MapInstanceConfigDefinition` -> owner-bound configured instance
   - còn lại -> public zone instance
4. Với public map, runtime ưu tiên zone public đang có người nhưng chưa đầy; nếu không có thì dùng `DefaultZoneIndex`.
5. Nếu runtime state local của player lệch map/zone/position mục tiêu, server sync lại current state trước khi publish snapshot.
6. `PublishWorldSnapshot(...)` gửi `MapJoinedPacket`, rồi `WorldRuntimeSnapshotPacket`, rồi rebuild visible-player state cho session đó.
7. Khi instance hết vòng đời và còn player bên trong, `MapInstanceLifecycleService` gửi `MapInstanceClosedPacket`, redirect player về home map default spawn, publish snapshot mới, rồi destroy instance.

# Rules / Invariants

- Mỗi player chỉ nên thuộc **một live map instance** tại một thời điểm.
- `mapId` không hợp lệ hoặc `0` được fallback về home map thay vì fail cứng.
- Public zone hợp lệ phải nằm trong `1..MaxPublicZoneCount`.
- Private map dùng `zoneIndex = 0`; public map dùng zone dương.
- Public instance dùng `zoneIndex` làm `instanceId`; private/configured instance dùng id tăng dần riêng.
- Empty private instance bị xóa ngay khi player cuối rời đi.
- Empty public instance chỉ bị dọn bởi cleanup/lifecycle rule.
- Redirect khi instance đóng hiện canonicalized như **safe default hiện tại**, chưa phải quyết định thiết kế cuối cùng cho mọi loại special instance.

# Data / Contracts

## Config

Xem `docs/data-design/config-contracts/world-map-runtime-configs-batch1.md`.

## DB

Map runtime phụ thuộc các nguồn chính:

- map templates
- adjacent maps
- zone slots
- spawn points
- map portals
- spiritual energy templates

## Network / messages

- world join snapshot: `MapJoinedPacket`, `WorldRuntimeSnapshotPacket`
- forced close redirect: `MapInstanceClosedPacket`
- serialized map definition gửi xuống client có spawn point và portal

# Operational Notes

## Failure modes

- thiếu home map -> startup/runtime failure (`ResolveHomeDefinition`)
- join public zone ngoài giới hạn -> exception
- publish snapshot khi player trỏ tới instance không còn tồn tại -> log error và bỏ publish
- map data lỗi có thể làm catalog build fail sớm

## Monitoring / logs

Hiện có log lỗi rõ khi publish world snapshot không resolve được live instance cho player.

# Verification

## Code checked

- `GameServer/World/MapCatalog.cs`
- `GameServer/World/MapManager.cs`
- `GameServer/World/WorldInterestService.cs`
- `GameServer/Runtime/MapInstanceLifecycleService.cs`
- `GameServer/World/MapDefinition.cs`
- `GameServer/World/MapTemplate.cs`

## Docs checked

- `docs/implementation/extractions/map-runtime-extraction.md`
- `docs/game-design-wp/clarifications/map-design-clarification.md`

## Gaps / drift

- Effective adjacency hiện gộp cả configured adjacency và enabled portal target. Semantic thiết kế chưa đủ rõ để xem đây là topology rule cuối cùng; xem `docs/conflicts/map-travel-topology-vs-portal-semantics.md`.
- Fallback redirect khi instance đóng hiện luôn về home map default spawn. Chưa có evidence đủ mạnh cho per-instance fallback rule khác.
- `DefaultSpawnPosition` fallback path chưa clamp ở cùng cách với explicit spawn point path; hiện coi đây là data-quality dependency, chưa canonicalize thành gameplay promise cứng.
