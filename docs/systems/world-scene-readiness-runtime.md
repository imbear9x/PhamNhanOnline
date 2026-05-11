---
title: World scene readiness runtime
doc_type: system
status: reviewed
owner: dev
code_status: legacy-doc-grounded
last_verified: 2026-05-11
source_of_truth:
  - docs/client-unity/world-scene-readiness.md
  - ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/WorldSceneReadinessService.cs
  - ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/WorldSceneBehaviour.cs
  - ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/WorldMapPresenter.cs
  - ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/WorldLocalPlayerPresenter.cs
  - ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/WorldRemotePlayersPresenter.cs
  - ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/WorldEnemiesPresenter.cs
related_docs:
  - docs/systems/phase1-runtime-flow.md
  - docs/rules/client-state-sync-runtime.md
tags:
  - client
  - world
  - readiness
  - runtime
---

# Summary

`WorldSceneReadinessService` là cơ chế chặn race condition trong scene `World` bằng load cycle và readiness keys, để các subsystem không đọc visual/runtime state quá sớm khi đổi map.

# Purpose

Readiness flow này tồn tại để tránh các lỗi kiểu:

- `MapChanged` đã về nhưng map visual chưa spawn xong
- camera / portal / player / enemy / remote player đọc state quá sớm
- component A phụ thuộc component B nhưng B chưa sẵn sàng

# Core model

## Load cycle model

Mỗi lần đổi map:

1. mở một `load cycle` mới
2. tăng `CurrentLoadVersion`
3. clear readiness keys của cycle cũ
4. để từng subsystem tự report ready khi hoàn thành phần việc của mình

## Base class model

`WorldSceneBehaviour` là base class gom logic readiness lặp lại:

- auto-wire world scene dependencies
- bind / unbind readiness events
- helper `IsReady`, `AreReady`
- helper `WaitFor`, `WaitForAll`

Nhờ đó component cụ thể chỉ cần:

- initialize base behaviour
- activate/deactivate readiness
- khai báo dependency trong `ConfigureReadyWaits()`
- reset state theo cycle trong `OnWorldLoadCycleStarted(...)`

# Ready keys

Các mốc readiness được legacy doc hiện tại mô tả:

- `MapVisual`
- `LocalPlayer`
- `RemotePlayers`
- `Enemies`

# Canonical dependency rules

## `MapVisual`

Chờ `MapVisual` nếu subsystem cần:

- map prefab
- playable bounds
- map world position
- world visual dependencies khác

## `LocalPlayer`

Chờ `LocalPlayer` nếu subsystem cần:

- local player transform
- local movement runtime
- targeting quanh local player

## `RemotePlayers`

Chờ `RemotePlayers` nếu subsystem cần remote player visual snapshot đã sync ban đầu.

## `Enemies`

Chờ `Enemies` nếu subsystem cần enemy visual snapshot đã sync ban đầu.

# Implementation rules

Không nên:

- rải `if (CurrentMapTransform == null) return;` khắp nơi để suy ra readiness
- lặp binding readiness event ở từng component nếu đã có base class
- giấu dependency trong chuỗi `if (key != ...) return;`

Nên:

- kế thừa `WorldSceneBehaviour`
- khai báo dependency tại `ConfigureReadyWaits()`
- clear state ở `OnWorldLoadCycleStarted(...)`
- report ready nếu subsystem là mốc cho subsystem khác

# Current confidence

Doc này đã được nâng từ legacy client explanation thành canonical runtime knowledge.
Nó được hỗ trợ bởi việc các file lớp readiness chính tồn tại đúng như legacy doc mô tả, nhưng chưa được đánh dấu `verified` cho từng dependency branch/runtime path chi tiết.
