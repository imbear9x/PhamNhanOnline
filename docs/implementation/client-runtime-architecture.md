---
title: Client runtime architecture
doc_type: implementation-note
status: reviewed
owner: dev
code_status: mixed
last_verified: 2026-05-11
source_of_truth:
  - docs/game-design-current-state/06_client_architecture.md
  - ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Core/Application/ClientRuntime.cs
  - ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/WorldSceneController.cs
  - ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/Character/Presentation/LocalCharacterActionController.cs
  - ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/WorldLocalMovementSyncController.cs
  - ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Application/ClientWorldState.cs
related_docs:
  - docs/rules/client-state-sync-runtime.md
  - docs/systems/world-scene-readiness-runtime.md
  - docs/systems/world-observer-and-movement-runtime.md
  - docs/implementation/unity-shared-sync-and-build-guide.md
tags:
  - client
  - architecture
  - runtime
---

# Summary

Client Unity hiện tại có kiến trúc runtime xoay quanh một container trung tâm (`ClientRuntime`), các feature states/services tách rời, và một lớp presentation world/UI tiêu thụ state đó.

**Graph links:** [[architecture-knowledge-map]] · [[runtime-knowledge-map]] · [[client-state-sync-runtime]] · [[world-scene-readiness-runtime]] · [[world-observer-and-movement-runtime]] · [[unity-shared-sync-and-build-guide]]

# Main client layers

## Scene/runtime phases

Các phase/scene chính thấy rõ trong tài liệu legacy và code:

- `Bootstrap`
- `Login`
- `World`

Runtime có thể được auto-initialize từ Login hoặc World nếu cần.

## Core runtime container

`ClientRuntime` là static service locator trung tâm, giữ:

- connection
- packet dispatcher
- auth state/service
- character state/service
- inventory state/service
- world state/service
- combat state/service
- targeting
- notifications
- alchemy
- martial arts
- skills
- skill presentation
- scene flow
- UI screen service

## Network/request-response layer

Client transport dùng `LiteNetLib` transport.
Nhiều feature service subscribe packet stream trực tiếp và wrap request/response bằng `TaskCompletionSource` hoặc flow tương đương.

## State sync model

Client chia state theo feature domain, ví dụ:

- character
- inventory
- martial arts
- skills
- combat
- world
- targeting
- notifications
- alchemy

Sau `EnterWorld`, client bootstrap-load các subsystem chính thay vì để UI panel tự fetch.

## World presentation layer

`WorldSceneController` là root orchestrator của scene `World`.
Dưới nó là các presenter/controller như:

- map presenter
- local player presenter
- local movement sync
- remote players presenter
- enemies presenter
- target selection/action
- portal / reward presentation

Readiness flow quyết định khi nào các presenter nên bắt đầu hành động theo dependency.

## UI layer

Client có nhiều panel/UI controllers cho:

- login
- world menu
- inventory/equipment/stats
- cultivation
- potential allocation
- skills / HUD
- alchemy / crafting
- zone switch
- notifications / modal popups

Phần lớn UI là consumer của state và action result, không phải source of truth gameplay.

# Authority boundary

Client chịu trách nhiệm tốt ở các phần:

- scene/world visuals
- movement presentation local
- target selection UX
- panel state / popup / HUD
- skill visual presentation

Nhưng không phải source of truth cho gameplay legality.
Server vẫn chốt các rule như:

- auth success
- world membership
- actual authoritative position
- combat legality
- inventory legality
- progression legality

# Known limitations from current phase

- local movement là optimistic presentation trước khi server settle
- một số UI tab/panel còn placeholder hoặc chưa nối logic đầy đủ
- scene hierarchy exact vẫn cần Unity editor để inspect sâu hơn ngoài code
- multi-character UX chưa thấy production-ready đầy đủ trong current phase

# Migration note

File này là canonical landing cho client architecture.
Legacy `06_client_architecture.md` vẫn giữ giá trị làm broad audit source, nhưng không nên là điểm vào đầu tiên nữa.
