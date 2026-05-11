---
title: Server runtime architecture
doc_type: implementation-note
status: reviewed
owner: dev
code_status: mixed
last_verified: 2026-05-11
source_of_truth:
  - docs/game-design-current-state/05_server_architecture.md
  - GameServer/Program.cs
  - GameServer/Network/NetworkServer.cs
  - GameServer/Runtime/GameLoop.cs
  - GameServer/Runtime/RuntimeMaintenanceService.cs
  - GameServer/Runtime/WorldRuntimeSettlementService.cs
  - GameServer/World/WorldManager.cs
  - GameServer/World/MapManager.cs
  - GameServer/Runtime/CharacterRuntimeSaveService.cs
  - GameServer/Services/PlayerInventoryTransactionService.cs
related_docs:
  - docs/rules/server-validation-and-runtime-rules.md
  - docs/rules/server-transaction-boundary.md
  - docs/data-design/config-contracts/game-configs-phase1.md
tags:
  - server
  - architecture
  - runtime
---

# Summary

Server hiện tại là một process authoritative kết hợp:

**Graph links:** [[architecture-knowledge-map]] · [[runtime-knowledge-map]] · [[server-validation-and-runtime-rules]] · [[server-transaction-boundary]] · [[game-configs-phase1]] · [[auth-character-world-phase1]]

- network server + middleware + handlers
- service layer cho persistent/domain logic
- runtime world layer sống trong RAM và tick liên tục
- persistence flush/maintenance chạy song song với world tick

# Main runtime layers

## Process bootstrap

`GameServer/Program.cs`:

- cấu hình logging
- build DI container
- load service graph
- start `NetworkServer`
- start `GameLoop`
- start `RuntimeMaintenanceService`
- start metrics logger
- hỗ trợ command modes như `sync-game-time-config` và `preview-random-table`

## Network / middleware / handler layer

`NetworkServer` + middleware stack chịu trách nhiệm:

- nhận packet transport
- auth gate
- rate limit
- packet validation
- character action restriction gate
- dispatch packet tới handlers

Handlers nên giữ mỏng:

- đọc packet/session
- gọi service/runtime phù hợp
- đóng gói result packet

## Service layer

Service layer chứa logic persistent/domain-oriented:

- account / character
- item / inventory / equipment
- martial art / skill
- alchemy / practice / notifications
- một phần domain gameplay khác gắn với DB

## Runtime / world layer

Runtime layer giữ state sống trong RAM:

- online players
- map instances
- enemies
- ground rewards
- desired movement targets
- combat status
- pending skill executions

`GameLoop` tick 50ms để:

- áp desired movement
- process runtime settlement cho instances
- chạy lifecycle follow-up sau world tick

## Maintenance / save layer

`RuntimeMaintenanceService` và save services đảm nhiệm:

- periodic save
- dirty-state flush
- cleanup / refresh / settlement support ngoài world tick chính

# State model

## Online player state

`WorldManager` giữ online players.
Một player runtime thường mang theo:

- runtime base/current state
- connection ownership
- map / instance / zone state
- desired movement target
- visibility/observer state
- combat status

## Map instance state

`MapManager` giữ map instances.
`MapInstance` mang các nhóm state như:

- players trong instance
- monsters
- ground rewards
- pending runtime queues/events
- lifecycle metadata

## Config state

Runtime config đến từ:

- JSON bootstrap config
- DB-backed `game_configs`
- DB-backed definition catalogs load eager lúc startup

# Persistence model

## Inventory mutation boundary

Inventory mutation đi qua `PlayerInventoryTransactionService`, dùng transaction + advisory lock theo player để giảm race condition.

## Runtime persistence boundary

Runtime state không save mỗi tick.
Dirty state được flush theo:

- periodic save
- disconnect
- explicit key transitions

# Authoritative boundary

Server là source of truth cho:

- auth / ownership
- world membership / instance selection
- actual authoritative position/map/zone
- combat legality / target / range / cooldown
- damage / defeat / rewards
- inventory ownership/location/quantity
- equipment final effect
- cultivation / breakthrough / alchemy acceptance

# Risks and limitations

Legacy architecture doc cũng chỉ ra một số điểm chưa phải final architecture:

- movement vẫn bắt đầu từ client intent nên hardening dựa nhiều vào clamp/gate
- chưa có dedicated anti-cheat module
- interest management vẫn còn đơn giản ở phase này
- một số UX/debug paths phía client không đại diện cho production gameplay cuối cùng

# Migration note

File này là canonical architecture landing cho server side.
Legacy `05_server_architecture.md` vẫn giữ lại như historical broad source, nhưng không nên là điểm vào đầu tiên nữa.
