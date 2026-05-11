---
title: Auth, character, and world phase 1 runtime
doc_type: system
status: reviewed
owner: dev
code_status: mixed
last_verified: 2026-05-11
source_of_truth:
  - docs/reference-and-specs/PHASE1_SYSTEM_REFERENCE.md
  - GameServer/Network/Handlers/LoginHandler.cs
  - GameServer/Network/Handlers/GetCharacterListHandler.cs
  - ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/Auth/Application/ClientLoginFlowService.cs
  - ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/Character/Application/ClientCharacterService.cs
  - ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Application/ClientWorldService.cs
related_docs:
  - docs/systems/phase1-runtime-flow.md
  - docs/rules/client-state-sync-runtime.md
  - docs/implementation/unity-shared-sync-and-build-guide.md
tags:
  - auth
  - character
  - world
  - phase1
---

# Summary

Phase 1 runtime flow nối `login -> character list -> optional character creation -> enter world -> world scene bootstrap` giữa server và Unity client.

**Graph links:** [[runtime-knowledge-map]] · [[phase1-runtime-flow]] · [[client-state-sync-runtime]] · [[world-observer-and-movement-runtime]] · [[client-runtime-architecture]] · [[server-runtime-architecture]]

# Canonical flow

## Client startup

Client khởi động từ `Bootstrap`, sau đó đi vào login/world flow thông qua các runtime services chính.

## Login and character list

Flow chuẩn:

1. client đảm bảo kết nối server
2. gửi `LoginPacket`
3. nếu login thành công, load character list
4. nếu chưa có character, yêu cầu create character
5. nếu đã có character, chọn character và `EnterWorld`

## Character creation

Nếu account chưa có character:

- client gửi `CreateCharacterPacket`
- server tạo character + base stats + current state
- client thêm character vào local character list
- sau đó có thể tiếp tục `EnterWorld`

## Enter world

Flow chuẩn:

1. client gửi `EnterWorldPacket`
2. server load snapshot nhân vật
3. server attach player vào runtime world
4. server trả `EnterWorldResultPacket`
5. server publish world snapshot gồm `MapJoinedPacket`
6. client load scene `World`
7. world scene bootstrap map/local player/camera/UI

## World runtime continuation

Sau khi vào world:

- client áp bootstrap state
- movement local chạy phía client
- client sync position intent lên server
- server cập nhật runtime-authoritative state
- server broadcast movement/observer packets cho client khác

# Scope boundary

Doc này chỉ canonicalize phần phase 1 nền tảng cho:

- auth
- character lifecycle trước world
- world entry
- scene bootstrap ở mức hệ thống

Các flow chi tiết hơn nên sống ở docs riêng theo domain.

# Confidence and limitations

Doc này được normalize từ legacy phase-1 reference và một phần code đã được kiểm tra thật.
Nó nên được xem là canonical overview đã dọn gọn hơn legacy reference, nhưng chưa thay thế mọi chi tiết low-level packet-by-packet trong file legacy gốc.
