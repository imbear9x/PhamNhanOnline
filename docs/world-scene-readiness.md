# World Scene Readiness

## Mục tiêu

`WorldSceneReadinessService` dùng để tránh race condition trong World scene:

- `MapChanged` đã về nhưng map visual chưa spawn xong
- camera / portal / player / enemy / remote player đọc state quá sớm
- component A cần component B, nhưng B chưa sẵn sàng

Service này tạo ra một `load cycle` mới mỗi lần đổi map, reset readiness của cycle cũ, sau đó các subsystem tự `report ready` khi xong việc của mình.

## Ba mảnh chính của thiết kế mới

### 1. `WorldSceneReadinessService`

File:

- [WorldSceneReadinessService.cs](/F:/PhamNhanOnline/ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/WorldSceneReadinessService.cs)

Trách nhiệm:

- mở `load cycle` mới khi map đổi
- tăng `CurrentLoadVersion`
- clear các key đã ready của cycle cũ
- phát event:
  - `LoadCycleStarted(loadVersion, mapKey)`
  - `ReadyReported(loadVersion, key)`
- lưu trạng thái key nào đã ready trong cycle hiện tại

### 2. `WorldSceneBehaviour`

File:

- [WorldSceneBehaviour.cs](/F:/PhamNhanOnline/ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/WorldSceneBehaviour.cs)

Đây là base class mới cho các world component có phụ thuộc readiness.

Nó gom các phần lặp lại:

- auto-wire `WorldSceneController`
- auto-wire `WorldMapPresenter`
- auto-wire `WorldSceneReadinessService`
- bind / unbind readiness events
- helper check readiness:
  - `IsReady(key)`
  - `AreReady(keys)`
- helper khai báo dependency rõ ràng:
  - `WaitFor(key, action)`
  - `WaitForAll(action, keys...)`

### 3. Component cụ thể

Mỗi component chỉ cần:

- gọi `InitializeWorldSceneBehaviour(...)`
- gọi `ActivateWorldSceneReadiness()` / `DeactivateWorldSceneReadiness()`
- khai báo dependency trong `ConfigureReadyWaits()`
- reset state theo cycle trong `OnWorldLoadCycleStarted(...)` nếu cần

Nhờ vậy code đọc ra rõ hơn, không còn lặp quá nhiều đoạn:

- `GetComponent<WorldSceneReadinessService>()`
- `readinessService.LoadCycleStarted += ...`
- `if (key != ...) return;`

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
2. local player đã được spawn hoặc ensure
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

- [WorldCameraFollowController.cs](/F:/PhamNhanOnline/ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/WorldCameraFollowController.cs)
  - `ConfigureReadyWaits()` khai báo `WaitFor(MapVisual, TryRefreshCachedClampBoundsIfReady)`
- [WorldPortalPresenter.cs](/F:/PhamNhanOnline/ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/WorldPortalPresenter.cs)
  - `ConfigureReadyWaits()` khai báo `WaitFor(MapVisual, HandleMapVisualReady)`
- [WorldLocalPlayerPresenter.cs](/F:/PhamNhanOnline/ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/WorldLocalPlayerPresenter.cs)
  - `ConfigureReadyWaits()` khai báo `WaitFor(MapVisual, HandleMapVisualReady)`
- [WorldRemotePlayersPresenter.cs](/F:/PhamNhanOnline/ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/WorldRemotePlayersPresenter.cs)
  - `ConfigureReadyWaits()` khai báo `WaitFor(MapVisual, HandleMapVisualReady)`
- [WorldEnemiesPresenter.cs](/F:/PhamNhanOnline/ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/WorldEnemiesPresenter.cs)
  - `ConfigureReadyWaits()` khai báo `WaitFor(MapVisual, HandleMapVisualReady)`
- [WorldTargetSelectionIndicatorController.cs](/F:/PhamNhanOnline/ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/WorldTargetSelectionIndicatorController.cs)
  - không đợi bằng callback, nhưng gate runtime bằng `IsReady(MapVisual)`

### Chờ `LocalPlayer`

- [WorldLocalMovementSyncController.cs](/F:/PhamNhanOnline/ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/WorldLocalMovementSyncController.cs)
  - gate runtime bằng `IsReady(LocalPlayer)`

### Chờ `MapVisual + LocalPlayer`

- [WorldClickTargetSelectionController.cs](/F:/PhamNhanOnline/ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/WorldClickTargetSelectionController.cs)
  - gate runtime bằng `AreReady(MapVisual, LocalPlayer)`
- [WorldTargetActionController.cs](/F:/PhamNhanOnline/ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/WorldTargetActionController.cs)
  - gate runtime bằng `AreReady(MapVisual, LocalPlayer)`

## Load Cycle Flow

Khi đổi map:

1. [WorldSceneReadinessService.cs](/F:/PhamNhanOnline/ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/WorldSceneReadinessService.cs) mở `load cycle` mới
2. tất cả key của cycle cũ bị clear
3. [WorldMapPresenter.cs](/F:/PhamNhanOnline/ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/WorldMapPresenter.cs) rebuild map mới
4. `MapVisual` được report
5. local player / remote players / enemies chạy initial sync theo map mới
6. `LocalPlayer`, `RemotePlayers`, `Enemies` được report khi xong initial sync của từng subsystem

## Cách đọc code mới

### Nếu muốn biết component đang đợi ai

Xem `ConfigureReadyWaits()`.

Ví dụ:

- [WorldCameraFollowController.cs](/F:/PhamNhanOnline/ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/WorldCameraFollowController.cs)
  - `WaitFor(MapVisual, ...)`
- [WorldPortalPresenter.cs](/F:/PhamNhanOnline/ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/WorldPortalPresenter.cs)
  - `WaitFor(MapVisual, ...)`

### Nếu muốn biết component reset gì khi đổi map

Xem `OnWorldLoadCycleStarted(...)`.

Ví dụ:

- [WorldCameraFollowController.cs](/F:/PhamNhanOnline/ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/WorldCameraFollowController.cs)
  - clear clamp bounds cũ
- [WorldPortalPresenter.cs](/F:/PhamNhanOnline/ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/WorldPortalPresenter.cs)
  - clear portal runtime cũ
- [WorldLocalPlayerPresenter.cs](/F:/PhamNhanOnline/ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/WorldLocalPlayerPresenter.cs)
  - reset cờ `LocalPlayer ready` của cycle trước

### Nếu muốn biết component chỉ gate runtime chứ không cần callback

Xem chỗ `IsReady(...)` hoặc `AreReady(...)` trong `Update`, `LateUpdate`, hoặc action method.

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
- lặp lại binding readiness events trong từng component nếu đã có base class
- giấu dependency trong quá nhiều `if (key != ...) return;`

Nên:

- kế thừa [WorldSceneBehaviour.cs](/F:/PhamNhanOnline/ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/WorldSceneBehaviour.cs)
- khai báo dependency ở `ConfigureReadyWaits()`
- clear state ở `OnWorldLoadCycleStarted(...)`
- report ready nếu subsystem của mình là một mốc cần cho subsystem khác

## Component cố ý không gate readiness

Một số HUD/controller hiện tại vẫn đọc logic state trực tiếp và không cần visual resource:

- [TargetHudController.cs](/F:/PhamNhanOnline/ClientUnity/PhamNhanOnline/Assets/Game/Runtime/UI/Hud/TargetHudController.cs)
- [WorldCombatHudController.cs](/F:/PhamNhanOnline/ClientUnity/PhamNhanOnline/Assets/Game/Runtime/UI/Hud/WorldCombatHudController.cs)
- [WorldCombatValuePopupController.cs](/F:/PhamNhanOnline/ClientUnity/PhamNhanOnline/Assets/Game/Runtime/UI/Hud/WorldCombatValuePopupController.cs)

Lý do:

- các component này chủ yếu đọc world state / combat state / target state
- không cần đợi visual resource được instantiate xong mới có thể hoạt động

Nếu sau này một HUD nào bắt đầu phụ thuộc vào visual runtime thực sự, khi đó mới nên đưa vào readiness flow.
