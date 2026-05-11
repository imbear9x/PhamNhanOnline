---
title: World observer and movement runtime
doc_type: system
status: reviewed
owner: dev
code_status: mixed
last_verified: 2026-05-11
source_of_truth:
  - docs/game-design-current-state/03_feature_flows.md
  - docs/reference-and-specs/PHASE1_SYSTEM_REFERENCE.md
  - GameServer/Runtime/GameLoop.cs
  - ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/WorldLocalMovementSyncController.cs
  - ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/WorldRemotePlayersPresenter.cs
  - ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/RemoteCharacterPresenter.cs
  - ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Application/ClientWorldState.cs
related_docs:
  - docs/systems/world-scene-readiness-runtime.md
  - docs/rules/client-state-sync-runtime.md
  - docs/rules/server-validation-and-runtime-rules.md
tags:
  - world
  - movement
  - observer
  - runtime
---

# Summary

Phase hiện tại dùng mô hình movement/presence như sau:

**Graph links:** [[runtime-knowledge-map]] · [[auth-character-world-phase1]] · [[world-scene-readiness-runtime]] · [[client-state-sync-runtime]] · [[server-validation-and-runtime-rules]] · [[client-runtime-architecture]]

- local player tự simulate presentation trên client
- client gửi movement intent theo policy config, không gửi mỗi frame
- server clamp authoritative movement theo runtime tick
- remote players được sync qua observed spawn/move/despawn packets

# Local movement model

## Client side

Local player:

- dùng controller local riêng
- tự simulate movement/presentation trên client
- không bị authoritative position kéo ngược mỗi tick

`WorldLocalMovementSyncController` đọc local transform, đổi sang server coordinates, rồi gửi `CharacterPositionSyncPacket` theo policy cấu hình.

## Server side

Server không chấp nhận raw movement như vị trí tuyệt đối để tin ngay.
Thay vào đó:

- position intent được clamp theo map bounds
- desired target được áp vào runtime
- `GameLoop` tick 50ms kéo player về phía target theo effective move speed và elapsed cap

# Observer model

## Current visibility rule

Phase 1 hiện dùng rule đơn giản:

- cùng `MapInstance` thì thấy nhau

Đây là rule test-friendly hiện tại, chưa phải interest management cuối cùng theo radius.

## Packet model

Server dùng nhóm packet observer để sync remote presence:

- `ObservedCharacterSpawnedPacket`
- `ObservedCharacterDespawnedPacket`
- `ObservedCharacterMovedPacket`
- `ObservedCharacterCurrentStateChangedPacket`

## Client side rendering rule

`ClientWorldState` giữ `observedCharacters` và phát event khi thay đổi.
`WorldRemotePlayersPresenter` dựa vào đó để spawn / update / despawn remote presenters.

Remote player:

- không dùng local input controller
- không dùng local physics simulation như player local
- được drive bằng remote presentation/interpolation

# Travel and map change rule

Travel/map change sẽ dẫn tới:

- update map/zone/runtime membership ở server
- publish snapshot world mới
- client rebuild map/world presentation theo `MapJoinedPacket` và world state mới

# Safety rules

- không gắn `LocalCharacterActionController` cho remote player
- không để remote player tự chạy local physics/input
- không kéo local player về authoritative position mỗi tick theo kiểu spam self-notify
- policy sync movement nên tune bằng config/scriptable object thay vì hardcode hành vi khắp nơi

# Confidence and limitations

Doc này hợp nhất các phần liên quan movement/observer bị rải trong legacy feature-flow và phase-1 reference.
Nó là canonical runtime overview tốt hơn để tra cứu, nhưng chưa thay thế mọi chi tiết code-level tuning parameter trong legacy docs và assets.
