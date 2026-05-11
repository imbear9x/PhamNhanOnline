---
title: Client state sync runtime rules
doc_type: system
status: reviewed
owner: dev
code_status: legacy-doc-grounded
last_verified: 2026-05-11
source_of_truth:
  - docs/client-unity/client-state-sync-rules.md
  - ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/Character/Application/ClientCharacterService.cs
  - ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/Auth/Application/ClientLoginFlowService.cs
  - ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Application/ClientWorldService.cs
related_docs:
  - docs/systems/phase1-runtime-flow.md
  - docs/systems/auth-character-world-phase1.md
  - docs/systems/world-scene-readiness-runtime.md
tags:
  - client
  - state-sync
  - runtime
---

# Summary

Client runtime phải giữ một rule đồng bộ state đơn giản, truy vết được, và không đẩy trách nhiệm fetch/reload xuống panel UI.

# Canonical rules

## Bootstrap rule

`EnterWorld` là mốc bootstrap state đầu tiên cho các subsystem chính.

- `character` lấy từ `EnterWorldResult`
- `inventory`, `martial arts`, `skills` được load ngay sau `EnterWorld`
- reconnect thành công phải chạy lại bootstrap theo cùng nguyên tắc

## Ownership rule

### Feature service owns subsystem state

Mỗi feature service:

- sở hữu local state của subsystem mình
- nghe packet push từ server
- nghe action result
- quyết định khi nào cần reload đúng subsystem của mình

### Panel/controller is not a fetch owner

Panel/controller:

- không tự fetch dữ liệu
- không polling để vá state thiếu
- chỉ nghe state/event và render

## Runtime update rule

Sau bootstrap, local state chỉ nên đổi bởi 2 nguồn:

1. packet push từ server
2. action result trả đủ dữ liệu để update local state chắc chắn

Nếu action result không đủ dữ liệu để update chắc chắn:

- reload lại đúng subsystem trong service xử lý action
- không đẩy fallback reload xuống UI panel/controller

## Anti-drift rules

Không làm:

- không thêm fallback polling trong panel
- không để mỗi panel tự nghĩ rule sync riêng
- không tạo resync phức tạp hơn nếu chưa có drift bug lặp lại

# Intended service roles

## Character bootstrap owner

`ClientCharacterService` chịu trách nhiệm:

- nhận `EnterWorldResult`
- apply `character` state
- kick bootstrap load cho các subsystem liên quan

## Feature service role

Từng feature service chịu trách nhiệm:

- duy trì local state subsystem
- áp packet/result packet
- phát hiện khi nào cần reload subsystem mình

# Current confidence

Doc này được canonicalized từ legacy runtime rule doc và sự tồn tại của các client service chính.
Nó là intended runtime rule đã được normalize vào second-brain, nhưng chưa được đánh dấu `verified` bằng client code audit đầy đủ từng branch cập nhật state.
