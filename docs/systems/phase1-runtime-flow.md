---
title: Phase 1 runtime flow
doc_type: system
status: verified
owner: devops
code_status: partially-verified
last_verified: 2026-05-11
source_of_truth:
  - docs/reference-and-specs/PHASE1_SYSTEM_REFERENCE.md
  - GameServer/Network/Handlers/LoginHandler.cs
  - GameServer/Network/Handlers/GetCharacterListHandler.cs
related_docs:
  - docs/workflow-and-operations/WORKING_CONTEXT.md
related_code:
  - GameServer/Network/Handlers/CreateCharacterHandler.cs
  - ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/Auth/Application/ClientLoginFlowService.cs
tags:
  - second-brain
  - system
  - phase1
---

# Summary

## Purpose

Giữ một canonical map ngắn cho flow phase 1 từ login tới character list và enter world.

## Scope

- login account
- load character list
- nền tảng flow trước khi enter world

## Non-goals

- không thay thế tài liệu phase 1 legacy đầy đủ
- chưa verify mọi bước client-side trong lượt này

# Architecture / Flow

## Inputs

- `LoginPacket`
- `GetCharacterListPacket`

## Outputs

- `LoginResultPacket`
- `GetCharacterListResultPacket`

## Runtime flow

1. Client gửi `LoginPacket`.
2. `LoginHandler` gọi `_accountActionService.LoginAsync(...)`.
3. Nếu thành công, session được set `PlayerId`, `IsAuthenticated`, và issue `ResumeToken`.
4. Sau đó client có thể gọi `GetCharacterListPacket`.
5. `GetCharacterListHandler` lấy danh sách nhân vật từ `CharacterService.GetCharactersByAccountAsync(...)`.
6. Server map DTO sang model và trả `GetCharacterListResultPacket`.

# Rules / Invariants

- login thành công mới được vào phase load character list
- session auth state được giữ ở server connection session
- shared contract vẫn phải đi qua `GameShared`

# Data / Contracts

## Config

Không có config rule đặc biệt được verify trong lượt này.

## DB

Danh sách nhân vật đi qua `CharacterService`, nhưng truy vết repository/DB chưa audit sâu trong lượt này.

## Network / messages

- `LoginPacket` -> `LoginResultPacket`
- `GetCharacterListPacket` -> `GetCharacterListResultPacket`

# Operational Notes

## Failure modes

- login thất bại trả `Success = false`
- exception path trả `UnknownError`

## Monitoring / logs

Nên audit thêm login/register/create-character chain khi cần complete canonicalization phase 1.

# Verification

## Code checked

- `GameServer/Network/Handlers/LoginHandler.cs`
- `GameServer/Network/Handlers/GetCharacterListHandler.cs`

## Docs checked

- `docs/reference-and-specs/PHASE1_SYSTEM_REFERENCE.md`
- `docs/workflow-and-operations/WORKING_CONTEXT.md`

## Gaps / drift

- legacy phase 1 doc còn rộng hơn nhiều: create character, enter world, map join, movement sync; các phần đó chưa verify trực tiếp ở lượt này
