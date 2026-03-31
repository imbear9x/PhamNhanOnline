# World Scene Readiness

## Mục tiêu

`WorldSceneReadinessService` dùng để tránh race condition trong World scene:

- `MapChanged` đã về nhưng map visual chưa spawn xong
- camera / portal / player / enemy / remote player đọc state quá sớm
- component A cần component B, nhưng B chưa sẵn sàng

Service này tạo ra một `load cycle` mới mỗi lần đổi map, reset readiness của cycle cũ, sau đó các subsystem tự `report ready` khi xong việc của mình.

## Ready Keys

Hiện tại có 4 key:

- `MapVisual`
- `LocalPlayer`
- `RemotePlayers`
- `Enemies`

## Ai report key nào

### `MapVisual`

Reporter:

- [WorldMapPresenter.cs](/F:/PhamNhanOnline/ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/WorldMapPresenter.cs)

Điều kiện report:

1. map cũ đã clear
2. prefab map mới đã instantiate
3. `PlayableBounds` của map mới đã cache xong

### `LocalPlayer`

Reporter:

- [WorldLocalPlayerPresenter.cs](/F:/PhamNhanOnline/ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/WorldLocalPlayerPresenter.cs)

Điều kiện report:

1. `MapVisual` đã ready
2. local player đã được spawn/ensure
3. vị trí mới đã được apply vào map hiện tại

### `RemotePlayers`

Reporter:

- [WorldRemotePlayersPresenter.cs](/F:/PhamNhanOnline/ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/WorldRemotePlayersPresenter.cs)

Điều kiện report:

1. `MapVisual` đã ready
2. đã sync snapshot remote players ban đầu cho map hiện tại

Lưu ý:

- dù map hiện tại không có remote player nào thì vẫn report ready sau lần sync đầu tiên

### `Enemies`

Reporter:

- [WorldEnemiesPresenter.cs](/F:/PhamNhanOnline/ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/WorldEnemiesPresenter.cs)

Điều kiện report:

1. `MapVisual` đã ready
2. đã sync snapshot enemy ban đầu cho map hiện tại

Lưu ý:

- dù map hiện tại không có enemy nào thì vẫn report ready sau lần sync đầu tiên

## Component nào đang chờ key nào

### Chờ `MapVisual`

- [WorldPortalPresenter.cs](/F:/PhamNhanOnline/ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/WorldPortalPresenter.cs)
  - chỉ rebuild portal sau khi `MapVisual` ready
- [WorldCameraFollowController.cs](/F:/PhamNhanOnline/ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/WorldCameraFollowController.cs)
  - chỉ refresh clamp bounds sau khi `MapVisual` ready
- [WorldLocalPlayerPresenter.cs](/F:/PhamNhanOnline/ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/WorldLocalPlayerPresenter.cs)
  - chỉ spawn/apply player sau khi `MapVisual` ready
- [WorldRemotePlayersPresenter.cs](/F:/PhamNhanOnline/ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/WorldRemotePlayersPresenter.cs)
  - chỉ sync remote players sau khi `MapVisual` ready
- [WorldEnemiesPresenter.cs](/F:/PhamNhanOnline/ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/WorldEnemiesPresenter.cs)
  - chỉ sync enemies sau khi `MapVisual` ready
- [WorldTargetSelectionIndicatorController.cs](/F:/PhamNhanOnline/ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/WorldTargetSelectionIndicatorController.cs)
  - chỉ vẽ indicator khi `MapVisual` ready

### Chờ `MapVisual + LocalPlayer`

- [WorldLocalMovementSyncController.cs](/F:/PhamNhanOnline/ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/WorldLocalMovementSyncController.cs)
  - thực tế chỉ gate theo `LocalPlayer`; key này đã bao hàm `MapVisual`
- [WorldClickTargetSelectionController.cs](/F:/PhamNhanOnline/ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/WorldClickTargetSelectionController.cs)
  - auto select / click select chỉ chạy khi map và local player đã sẵn sàng
- [WorldTargetActionController.cs](/F:/PhamNhanOnline/ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/WorldTargetActionController.cs)
  - logic approach/interact/attack chỉ chạy khi map và local player đã sẵn sàng

## Load Cycle Flow

Khi đổi map:

1. [WorldSceneReadinessService.cs](/F:/PhamNhanOnline/ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/WorldSceneReadinessService.cs) mở `load cycle` mới
2. tất cả key của cycle cũ bị clear
3. [WorldMapPresenter.cs](/F:/PhamNhanOnline/ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/WorldMapPresenter.cs) rebuild map mới
4. `MapVisual` được report
5. local player / remote players / enemies chạy initial sync theo map mới
6. `LocalPlayer`, `RemotePlayers`, `Enemies` được report khi xong initial sync của từng subsystem

## Rule implement mới

Nếu một subsystem:

- cần map prefab / bounds / map world position
  - chờ `MapVisual`
- cần local player transform / local movement / targeting quanh player
  - chờ `LocalPlayer`
- cần remote player visual đã spawn ban đầu
  - chờ `RemotePlayers`
- cần enemy visual đã spawn ban đầu
  - chờ `Enemies`

Không nên:

- đoán `if (CurrentMapTransform == null) return;` ở khắp nơi để tự suy ra readiness
- đọc map bounds trong `Update` trừ khi subsystem đã biết dependency của nó ready

Nên:

- bind vào `WorldSceneReadinessService`
- clear state của subsystem khi `LoadCycleStarted`
- chỉ chạy initial sync khi key phụ thuộc đã ready
- report ready nếu subsystem của mình là một mốc cần thiết cho subsystem khác

## Component cố ý không gate readiness

Một số HUD/controller hiện tại vẫn đọc logic state trực tiếp và không cần visual resource:

- [TargetHudController.cs](/F:/PhamNhanOnline/ClientUnity/PhamNhanOnline/Assets/Game/Runtime/UI/Hud/TargetHudController.cs)
- [WorldCombatHudController.cs](/F:/PhamNhanOnline/ClientUnity/PhamNhanOnline/Assets/Game/Runtime/UI/Hud/WorldCombatHudController.cs)
- [WorldCombatValuePopupController.cs](/F:/PhamNhanOnline/ClientUnity/PhamNhanOnline/Assets/Game/Runtime/UI/Hud/WorldCombatValuePopupController.cs)

Lý do:

- các component này chủ yếu đọc world state / combat state / target state
- không cần đợi visual resource được instantiate xong mới có thể hoạt động

Nếu sau này một HUD nào bắt đầu phụ thuộc vào visual runtime thực sự, khi đó mới nên đưa vào readiness flow.
