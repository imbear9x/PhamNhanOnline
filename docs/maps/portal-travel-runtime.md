---
title: Portal travel runtime
doc_type: system
status: reviewed
owner: dev
code_status: code-verified-with-runtime-gaps
last_verified: 2026-05-11
source_of_truth:
  - GameServer/Entities/MapPortalEntity.cs
  - GameServer/World/MapCatalog.cs
  - GameServer/Network/Handlers/TravelToMapHandler.cs
  - GameServer/Runtime/WorldTargetResolver.cs
  - GameShared/Packets/Packets/WorldPackets.cs
  - docs/implementation/extractions/portal-runtime-extraction.md
related_docs:
  - docs/game-design-wp/clarifications/portal-design-clarification.md
  - docs/maps/map-instance-and-world-entry-runtime.md
  - docs/data-design/config-contracts/world-map-runtime-configs-batch1.md
  - docs/conflicts/portal-interaction-mode-runtime-gap.md
  - docs/conflicts/map-travel-topology-vs-portal-semantics.md
related_code:
  - GameServer/Network/Validations/TravelToMapPacketValidator.cs
  - GameServer/DTO/NetworkModelMapper.cs
  - GameShared/Models/MapPortalModel.cs
tags:
  - second-brain
  - maps
  - portal
  - travel
---

# Summary

Canonical batch-1 runtime cho portal travel: portal là travel point theo vị trí trên source map, có enabled flag, có target map + target spawn point, và travel thành công sẽ republish world snapshot.

**Graph links:** [[runtime-knowledge-map]] · [[map-instance-and-world-entry-runtime]] · [[config-and-contract-map]]

## Purpose

Xác nhận behavior tối thiểu đã đủ rõ của portal travel mà không thổi phồng phần thiết kế chưa được runtime enforce.

## Scope

- portal data loading
- validation path khi dùng portal
- target map/spawn resolution
- world snapshot republish sau travel

## Non-goals

- không coi legacy direct travel là UX portal chuẩn
- không chốt hoàn chỉnh semantic `Touch` vs `Interact` vì runtime chưa verify đủ

# Architecture / Flow

## Inputs

- `map_portals` data
- current player map/instance/position
- `TravelToMapPacket.PortalId`
- config buffer `world.portal_validation_buffer_server_units`

## Outputs

- `TravelToMapResultPacket`
- updated player world position/map/zone
- fresh `MapJoinedPacket` + `WorldRuntimeSnapshotPacket`

## Runtime flow

1. `MapCatalog` load portal rows, group theo `SourceMapTemplateId`, sort theo `OrderIndex` rồi `Id`.
2. `TravelToMapHandler` nhận `TravelToMapPacket`.
3. Nếu packet có `PortalId`, handler đi theo portal flow thay vì legacy direct-map flow.
4. Handler validate:
   - session có player
   - action start gate cho phép portal travel
   - portal tồn tại trong current map và đang enabled
   - target map tồn tại
   - target spawn point tồn tại
   - current live instance của player còn resolve được
   - player đủ gần portal theo `InteractionRadius + server buffer`
5. Nếu hợp lệ, runtime resolve target zone:
   - target private map -> zone `0`
   - target không private -> auto-select public zone
6. Runtime update vị trí player sang target map/spawn, set `MapEntryContext` reason = `Portal`, rồi publish world snapshot mới.
7. Handler trả `TravelToMapResultPacket { Success = true }`.

# Rules / Invariants

- Portal chỉ hợp lệ khi thuộc đúng source map và đang enabled.
- Portal target phải resolve được sang target map + target spawn point hợp lệ; nếu không, request fail.
- Validation chính dựa trên vị trí runtime của player so với vị trí portal server-side, không tin packet position từ client.
- Portal travel thành công phải tạo `MapEntryContext` kiểu `Portal`.
- Portal definitions được gửi xuống client như một phần của map model.
- Portal data hiện mang tính hard-fail startup integrity: portal row lỗi có thể làm catalog build fail.

# Data / Contracts

## Config

Xem `docs/data-design/config-contracts/world-map-runtime-configs-batch1.md` cho `world.portal_validation_buffer_server_units`.

## DB

Portal row hiện mang các field cốt lõi:

- source map
- source position
- interaction radius
- interaction mode
- target map
- target spawn point
- enabled flag
- order index

## Network / messages

- input: `TravelToMapPacket`
- result: `TravelToMapResultPacket`
- downstream world refresh: `MapJoinedPacket`, `WorldRuntimeSnapshotPacket`

# Operational Notes

## Failure modes

- portal missing/disabled -> `MapPortalInvalid`
- target map missing -> `MapIdInvalid`
- target spawn point missing -> `MapPortalInvalid`
- player không còn nằm trong live world instance -> `CharacterNotInWorldInstance`
- interaction gate từ chối -> failure code tùy gate result

## Monitoring / logs

Handler có structured log cho request, reject bởi interaction gate, và success path của portal travel.

# Verification

## Code checked

- `GameServer/Network/Handlers/TravelToMapHandler.cs`
- `GameServer/Runtime/WorldTargetResolver.cs`
- `GameServer/World/MapCatalog.cs`
- `GameServer/Entities/MapPortalEntity.cs`
- `GameServer/Network/Validations/TravelToMapPacketValidator.cs`

## Docs checked

- `docs/implementation/extractions/portal-runtime-extraction.md`
- `docs/game-design-wp/clarifications/portal-design-clarification.md`

## Gaps / drift

- `Portal Interaction Mode` (`Touch` vs `Interact`) đã có trong data model nhưng chưa thấy được `TravelToMapHandler` enforce trực tiếp; xem `docs/conflicts/portal-interaction-mode-runtime-gap.md`.
- `TravelToMapPacket` vẫn hỗ trợ legacy direct travel bằng `TargetMapId`; hiện coi đó là compatibility/runtime path, không canonicalize thành portal UX chính.
- Effective adjacency hiện còn dính portal target map; xem `docs/conflicts/map-travel-topology-vs-portal-semantics.md`.
