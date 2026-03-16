# PHASE 1 SYSTEM REFERENCE

## Mục tiêu tài liệu

Tài liệu này mô tả hệ thống phase 1 đã hoàn thành của dự án:
- đăng ký
- đăng nhập
- lấy danh sách nhân vật
- tạo nhân vật
- enter world
- load scene `World`
- travel giữa map private/public
- local movement
- sync movement lên server
- remote player spawn/move trên client

Mục tiêu là để người mới vào dự án có thể:
- đọc luồng hệ thống nhanh
- tìm đúng file cần sửa
- biết packet nào đi từ client sang server và packet nào server trả về
- tránh vô tình phá vỡ luồng hiện tại

## Nguyên tắc chung cần giữ

- Client Unity chỉ phụ thuộc `GameShared`, không phụ thuộc `GameServer`.
- Mọi thay đổi packet/shared contract phải sửa trong `GameShared`, sau đó sync lại cho Unity bằng:

```powershell
powershell -File .\scripts\sync-gameshared-to-unity.ps1
```

- Unity scene hiện tại giữ ít scene cố định:
  - `Bootstrap`
  - `Login`
  - `World`
- `MapTemplate`/`zone`/`instance` là runtime state, không phải Unity scene riêng.
- Local player và remote player không dùng chung controller movement:
  - local player dùng `LocalCharacterActionController`
  - remote player dùng `RemoteCharacterPresenter`
- Hiện tại observer visibility trên server đang tạm thời là "cùng instance/map thì thấy nhau". Chưa quay lại interest theo radius.

## Sơ đồ tổng quan phase 1

1. Client start trong `Bootstrap`
2. Login scene gọi login flow
3. Server xác thực account
4. Client lấy character list
5. Nếu chưa có character thì tạo character
6. Client gọi `EnterWorldPacket`
7. Server attach player vào runtime world
8. Server gửi `EnterWorldResultPacket` và `MapJoinedPacket`
9. Client load `World` scene
10. `World` scene spawn map + local player + camera
11. Local movement chạy trên client
12. Client sync vị trí lên server theo policy config
13. Server cập nhật runtime state và broadcast `ObservedCharacterMovedPacket`
14. Client khác spawn/move remote player presenter

---

# Tính năng 1: Khởi động client runtime

## Mục đích

Khởi tạo tất cả state/service client cần cho auth, character, world.

## File chính

- `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Core/Application/ClientRuntime.cs`
- `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Core/Application/ClientBootstrap.cs`
- `ClientUnity/PhamNhanOnline/Assets/Game/Content/ScriptableObjects/Client/ClientBootstrapSettings.asset`

## Flow

- `ClientRuntime.Initialize(...)` tạo và giữ singleton-style reference đến:
  - `Connection`
  - `Auth`
  - `Character`
  - `World`
  - `AuthService`
  - `CharacterService`
  - `WorldService`
  - `WorldTravelService`
  - `LoginFlow`
  - `UiScreens`

## Lưu ý

- Mọi logic presentation trong scene `World` và `Login` đều được phép đọc qua `ClientRuntime`.
- Nếu sửa packet/shared model, nhớ sync lại `GameShared.dll` cho Unity.

---

# Tính năng 2: Đăng ký account

## Mục đích

Tạo account mới bằng username/password.

## Packet

- Client gửi: `RegisterPacket`
- Server trả: `RegisterResultPacket`

## Client side

### File liên quan

- `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/UI/Screens/Login/LoginScreenController.cs`
- `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/Auth/Application/ClientAuthService.cs`

### Luồng

- UI nhận input username/password
- UI gọi auth/login flow hoặc register flow tùy button
- Packet được gửi qua `ClientConnectionService.Send(...)`

## Server side

### File liên quan

- `GameServer/Network/Handlers/RegisterHandler.cs`
- `GameServer/Services/AccountService.cs`

### Xử lý

- `RegisterHandler` nhận `RegisterPacket`
- gọi `AccountService.RegisterWithPasswordAsync(...)`
- nếu thành công:
  - trả `RegisterResultPacket { Success = true, Code = None }`
- nếu thất bại nghiệp vụ:
  - trả `RegisterResultPacket { Success = false, Code = <GameException.Code> }`
- nếu exception khác:
  - trả `UnknownError`
  - ném exception tiếp để log

## Lưu ý

- Email hiện đang được chấp nhận ở network layer, nhưng `AccountService` hiện chưa dùng email cho register flow.

---

# Tính năng 3: Đăng nhập + lấy danh sách nhân vật

## Mục đích

Sau khi login thành công, client phải lấy character list trước khi vào world.

## Packet

- Client gửi: `LoginPacket`
- Server trả: `LoginResultPacket`
- Client gửi tiếp: `GetCharacterListPacket`
- Server trả: `GetCharacterListResultPacket`

## Client side

### File liên quan

- `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/Auth/Application/ClientLoginFlowService.cs`
- `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/Auth/Application/ClientAuthService.cs`
- `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/Character/Application/ClientCharacterService.cs`

### Luồng

`ClientLoginFlowService.ConnectLoginAndEnterWorldAsync(...)`
- đảm bảo kết nối server
- gọi `ClientAuthService.LoginAsync(...)`
- nếu login ok thì gọi `ClientCharacterService.LoadCharacterListAsync()`
- nếu list rỗng thì trả kết quả `RequiresCharacterCreationResult(...)`
- nếu list có character thì lấy character đầu tiên để `EnterWorld`

### State được cập nhật

- `ClientAuthState`
- `ClientCharacterState.CharacterList`

## Server side

### File liên quan

- `GameServer/Network/Handlers/LoginHandler.cs`
- `GameServer/Network/Handlers/GetCharacterListHandler.cs`
- `GameServer/Services/AccountService.cs`
- `GameServer/Services/CharacterService.cs`

### Xử lý login

- `LoginHandler` gọi `AccountService.LoginWithPasswordAsync(...)`
- nếu ok:
  - set `session.PlayerId`
  - set `session.IsAuthenticated = true`
  - issue `resumeToken`
  - trả `LoginResultPacket`

### Xử lý get character list

- `GetCharacterListHandler` gọi `CharacterService.GetCharactersByAccountAsync(session.PlayerId)`
- convert DTO -> `CharacterModel`
- trả `GetCharacterListResultPacket`

---

# Tính năng 4: Tạo nhân vật

## Mục đích

Nếu account chưa có character, client tạo nhân vật rồi mới vào world.

## Packet

- Client gửi: `CreateCharacterPacket`
- Server trả: `CreateCharacterResultPacket`

## Client side

### File liên quan

- `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/UI/Screens/Login/LoginScreenController.cs`
- `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/Character/Application/ClientCharacterService.cs`

### Luồng

- UI gọi `ClientCharacterService.CreateCharacterAsync(name, serverId, modelId)`
- nếu thành công:
  - append vào `ClientCharacterState.CharacterList`
  - có thể gọi tiếp `EnterWorldAsync(...)`

## Server side

### File liên quan

- `GameServer/Network/Handlers/CreateCharacterHandler.cs`
- `GameServer/Services/CharacterService.cs`
- `GameServer/Time/GameTimeService.cs`

### Xử lý

- `CreateCharacterHandler` gọi `CharacterService.CreateCharacterAsync(...)`
- nếu ok:
  - trả `CreateCharacterResultPacket`
  - packet gồm:
    - `Character`
    - `BaseStats`
    - `CurrentState`

---

# Tính năng 5: Enter world

## Mục đích

Chọn character và attach vào runtime world.

## Packet

- Client gửi: `EnterWorldPacket`
- Server trả: `EnterWorldResultPacket`
- Server gửi tiếp: `MapJoinedPacket`

## Client side

### File liên quan

- `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/Character/Application/ClientCharacterService.cs`
- `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/Auth/Application/ClientLoginFlowService.cs`
- `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Application/ClientWorldService.cs`

### Luồng

- `ClientCharacterService.EnterWorldAsync(characterId)` gửi `EnterWorldPacket`
- `ClientLoginFlowService` sau khi enter world thành công sẽ load scene `World`
- `ClientWorldService` lắng nghe `MapJoinedPacket` và cập nhật `ClientWorldState`

## Server side

### File liên quan

- `GameServer/Network/Handlers/EnterWorldHandler.cs`
- `GameServer/Runtime/CharacterRuntimeService.cs`
- `GameServer/Runtime/CharacterLifecycleService.cs`
- `GameServer/World/WorldInterestService.cs`

### Xử lý

- load snapshot nhân vật theo account + characterId
- chạy `PrepareSnapshotForWorldEntryAsync(...)`
- attach session vào runtime qua `CharacterRuntimeService.AttachPlayerSession(...)`
- `WorldInterestService.EnsurePlayerInWorld(player)`
- trả `EnterWorldResultPacket`
- gọi `WorldInterestService.PublishWorldSnapshot(player)`
  - trong đó gửi `MapJoinedPacket`
  - refresh observer visibility

## Packet trả về quan trọng

- `EnterWorldResultPacket`
  - `Character`
  - `BaseStats`
  - `CurrentState`
- `MapJoinedPacket`
  - `Map`
  - `ZoneIndex`

---

# Tính năng 6: Load World scene và dựng map/local player

## Mục đích

Sau khi vào world, scene `World` dựng map prefab, local player prefab, camera và UI root.

## File client chính

- `ClientUnity/PhamNhanOnline/Assets/Game/Scenes/World/World.unity`
- `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/WorldSceneController.cs`
- `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/WorldMapPresenter.cs`
- `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/WorldLocalPlayerPresenter.cs`
- `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/WorldCameraFollowController.cs`
- `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/ClientMapCatalog.cs`
- `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/ClientMapView.cs`

## Luồng map

- `ClientWorldState.ApplyMapJoin(...)` lưu:
  - `CurrentMapId`
  - `CurrentClientMapKey`
  - `CurrentMapWidth/Height`
  - `CurrentAdjacentMapIds`
- `WorldMapPresenter` nghe `MapChanged`
- `WorldMapPresenter` lấy prefab qua `ClientMapCatalog`
- spawn map vào `ActiveMapRoot`

## Quy đổi tọa độ

- Mỗi map prefab nên có `ClientMapView`
- `ClientMapView` trỏ đến `PlayableBounds` collider
- `WorldMapPresenter` dùng bounds này để:
  - map server coords -> Unity world
  - map Unity world -> server coords
  - clamp camera

## Luồng local player

- `WorldLocalPlayerPresenter` spawn `Player_Default.prefab` vào `LocalPlayerRoot`
- `WorldLocalPlayerPresenter` dùng `WorldMapPresenter.TryMapServerPositionToWorld(...)`
- chỉ snap local player khi force hoặc lệch quá `authoritativeSnapDistance`

## Lưu ý quan trọng

- Không đặt raw server coords thẳng vào `transform.position` nếu map scale khác hệ quy chiếu Unity.
- Muốn sửa map gameplay bounds thì sửa `PlayableBounds`, không dùng background art để suy ra vùng chơi.

---

# Tính năng 7: Travel giữa map private/public

## Mục đích

Cho local player rời `Player Home` private map để ra public map test observer.

## Packet

- Client gửi: `TravelToMapPacket`
- Server trả: `TravelToMapResultPacket`
- Server gửi tiếp lại `MapJoinedPacket`

## Client side

### File liên quan

- `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Application/ClientWorldTravelService.cs`
- `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/WorldTravelDebugController.cs`

### Luồng

- `ClientWorldTravelService.TravelToMapAsync(targetMapId)` gửi `TravelToMapPacket`
- `WorldTravelDebugController` hiện đang là debug hook để travel bằng phím test
- khi `MapJoinedPacket` mới đến, `ClientWorldState` tự động rebuild world presentation

## Server side

### File liên quan

- `GameServer/Network/Handlers/TravelToMapHandler.cs`
- `GameServer/Network/Validations/TravelToMapPacketValidator.cs`
- `GameServer/World/MapCatalog.cs`
- `GameServer/Runtime/CharacterRuntimeService.cs`
- `GameServer/World/WorldInterestService.cs`

### Xử lý

- validate `TargetMapId`
- check `MapCatalog.TryGet(...)`
- check `MapCatalog.CanTravel(currentMapId, targetMapId)`
- nếu hợp lệ:
  - `CharacterRuntimeService.UpdatePosition(...)` đến spawn mặc định của map đích
  - `WorldInterestService.PublishWorldSnapshot(player)`
  - trả `TravelToMapResultPacket { Success = true }`

---

# Tính năng 8: Local movement và local action

## Mục đích

Cho local player di chuyển trái/phải, bay lên, hover, rơi, attack local.

## File client chính

- `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/Character/Presentation/LocalCharacterActionController.cs`
- `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/Character/Presentation/LocalCharacterActionConfig.cs`
- `ClientUnity/PhamNhanOnline/Assets/Game/Content/ScriptableObjects/Character/LocalCharacterActionConfig.asset`
- `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/Character/Presentation/PlayerView.cs`
- `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/Character/Presentation/CharacterActionInputSource.cs`
- `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/Character/Presentation/KeyboardCharacterActionInputSource.cs`
- `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/Character/Presentation/VirtualCharacterActionInputSource.cs`

## Luồng

- `WorldLocalPlayerPresenter` sau khi spawn local player sẽ gắn/initialize `LocalCharacterActionController`
- controller đọc input từ `CharacterActionInputSource`
- movement phase hiện tại:
  - `Grounded`
  - `Takeoff`
  - `Flight`
  - `Falling`
- animator parameter locomotion hiện tại:
  - `MoveSpeed`
- các state optional:
  - `Jump`
  - `Fly`
  - `Fall`
  - `Attack`
  - `Attack2`

## Rule hiện tại cần giữ

- Local player tự simulate movement trên client.
- Server position không được kéo ngược local player mỗi tick.
- Local controller chỉ dùng cho local player, không dùng cho remote player.

## Hook đã để sẵn cho phase sau

- `CanUseFlight()`
- `ActivateFlightPresentation()`
- `DeactivateFlightPresentation()`
- `OnFlightPresentationActivated()`
- `OnFallingPresentationActivated()`

---

# Tính năng 9: Policy sync movement local -> server

## Mục đích

Sync vị trí local player lên server theo policy có điều tiết, không gửi mỗi frame.

## Packet

- Client gửi: `CharacterPositionSyncPacket`

## Client side

### File liên quan

- `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/WorldLocalMovementSyncController.cs`
- `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/WorldLocalMovementSyncConfig.cs`
- `ClientUnity/PhamNhanOnline/Assets/Game/Content/ScriptableObjects/Client/WorldLocalMovementSyncConfig.asset`

### Luồng

`WorldLocalMovementSyncController`
- đọc local player transform
- đổi sang server coords qua `WorldMapPresenter.TryMapWorldPositionToServer(...)`
- gửi `CharacterPositionSyncPacket` theo policy:
  - min interval
  - max interval
  - distance threshold
  - immediate state-change sync
  - final stop sync
- có theo dõi thêm:
  - facing left/right
  - movement phase
  để biết lúc nào cần sync ngay

## Server side

### File liên quan

- `GameServer/Network/Handlers/CharacterPositionSyncHandler.cs`
- `GameServer/Runtime/CharacterRuntimeService.cs`
- `GameServer/World/MapCatalog.cs`

### Xử lý

- `CharacterPositionSyncHandler` nhận packet
- clamp position theo `MapDefinition.ClampPosition(...)`
- gọi `CharacterRuntimeService.UpdatePosition(..., notifySelf: false)`

## Lưu ý

- `notifySelf: false` là chú ý quan trọng để local player không bị authoritative position spam kéo ngược mỗi tick.
- Việc sync nhiều hay ít hiện tại phụ thuộc chủ yếu vào `WorldLocalMovementSyncConfig.asset`, không cần sửa code mỗi lần tune.

---

# Tính năng 10: Observer sync và remote player presentation

## Mục đích

Cho các client trong cùng public map thấy nhau và thấy nhau di chuyển.

## Packet

- Server gửi: `ObservedCharacterSpawnedPacket`
- Server gửi: `ObservedCharacterDespawnedPacket`
- Server gửi: `ObservedCharacterMovedPacket`
- Server gửi: `ObservedCharacterCurrentStateChangedPacket`

## Server side

### File liên quan

- `GameServer/World/WorldInterestService.cs`
- `GameServer/World/MapInstance.cs`
- `GameServer/Runtime/CharacterRuntimeService.cs`

### Luồng

- mỗi khi player vào world/travel/map change:
  - `WorldInterestService.RefreshVisibility(...)`
  - observer được spawn/despawn
- mỗi khi player đổi vị trí:
  - `CharacterRuntimeService.UpdatePosition(...)`
  - `WorldInterestService.HandlePositionUpdated(...)`
  - `PublishMoveToExistingObservers(...)`
  - observer nhận `ObservedCharacterMovedPacket`

### Lưu ý hiện tại

- Hiện tại `RefreshVisibility(...)` đang lấy toàn bộ player trong cùng `MapInstance` qua `GetPlayersSnapshot(...)`
- Nghĩa là phase 1 đang theo rule:
  - cùng instance/map thì thấy nhau hết
- Đây là quyết định tạm thời để test phase 1 cho dễ, chưa phải interest management cuối cùng.

## Client side

### File liên quan

- `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Application/ClientWorldService.cs`
- `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Application/ClientWorldState.cs`
- `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/WorldRemotePlayersPresenter.cs`
- `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/RemoteCharacterPresenter.cs`

### Luồng state

- `ClientWorldService` subscribe packet observer
- `ClientWorldState` lưu `observedCharacters`
- `ClientWorldState` phát event `ObservedCharactersChanged`
- `WorldRemotePlayersPresenter` nghe event đó và:
  - spawn remote player nếu chưa có
  - cập nhật target position nếu đã có
  - despawn nếu observer rời list

### Luồng remote presentation

- `RemoteCharacterPresenter` tái sử dụng `Player_Default.prefab`
- nhưng disable local-only components:
  - `LocalCharacterActionController`
  - mọi `CharacterActionInputSource`
  - `Rigidbody2D.simulated`
  - body collider
- remote di chuyển bằng code, không dùng local physics simulation
- remote animator hiện tại set `MoveSpeed` theo movement intent/interpolation
- có `remoteMoveSmoothing` để tune mức độ bám target

## Rule cần giữ

- Không gắn `LocalCharacterActionController` cho remote player.
- Không để remote player tự chạy input/physics local.
- Nếu cần thêm combat state cho remote, làm presenter riêng cho remote chứ không dùng lại local controller.

---

# Tính năng 11: Editor tools hỗ trợ làm việc nhanh hơn

## Scene switcher window

- File: `ClientUnity/PhamNhanOnline/Assets/Game/Editor/SceneSwitcherWindow.cs`
- Menu: `Tools/Game/Scene Switcher`
- Mục đích: mở nhanh scene mà không cần tìm trong Project tree

## Scene switcher toolbar

- File: `ClientUnity/PhamNhanOnline/Assets/Game/Editor/SceneSwitcherToolbar.cs`
- Mục đích: có nút chuyển nhanh `Bootstrap/Login/World` ngay trên thanh toolbar editor
- Tool này sử dụng toolbar hook của Unity Editor, không phải runtime code

---

# Các ScriptableObject / config asset quan trọng

## 1. Client bootstrap settings
- `ClientUnity/PhamNhanOnline/Assets/Game/Content/ScriptableObjects/Client/ClientBootstrapSettings.asset`
- Dùng cho scene/boot config cơ bản của client

## 2. Local character action config
- `ClientUnity/PhamNhanOnline/Assets/Game/Content/ScriptableObjects/Character/LocalCharacterActionConfig.asset`
- Dùng tune movement/action local

## 3. Client map catalog
- `ClientUnity/PhamNhanOnline/Assets/Game/Content/ScriptableObjects/Maps/ClientMapCatalog.asset`
- Map `ClientMapKey` -> map prefab

## 4. World local movement sync config
- `ClientUnity/PhamNhanOnline/Assets/Game/Content/ScriptableObjects/Client/WorldLocalMovementSyncConfig.asset`
- Dùng tune packet sync local -> server

---

# Thư mục / file newcomer nên đọc trước khi sửa code

## Nếu sửa login/create/enter world
- `ClientLoginFlowService.cs`
- `ClientAuthService.cs`
- `ClientCharacterService.cs`
- `RegisterHandler.cs`
- `LoginHandler.cs`
- `GetCharacterListHandler.cs`
- `CreateCharacterHandler.cs`
- `EnterWorldHandler.cs`

## Nếu sửa world/map/travel
- `ClientWorldService.cs`
- `ClientWorldState.cs`
- `ClientWorldTravelService.cs`
- `WorldMapPresenter.cs`
- `TravelToMapHandler.cs`
- `WorldInterestService.cs`
- `MapCatalog.cs`

## Nếu sửa local movement
- `LocalCharacterActionController.cs`
- `LocalCharacterActionConfig.cs`
- `PlayerView.cs`
- `WorldLocalPlayerPresenter.cs`
- `WorldLocalMovementSyncController.cs`

## Nếu sửa remote sync / observer
- `WorldRemotePlayersPresenter.cs`
- `RemoteCharacterPresenter.cs`
- `ClientWorldService.cs`
- `ClientWorldState.cs`
- `CharacterPositionSyncHandler.cs`
- `WorldInterestService.cs`

---

# Danh sách packet phase 1 cần nhớ

## Auth / account
- `RegisterPacket`
- `RegisterResultPacket`
- `LoginPacket`
- `LoginResultPacket`
- `ReconnectPacket`
- `ReconnectResultPacket`

## Character
- `GetCharacterListPacket`
- `GetCharacterListResultPacket`
- `CreateCharacterPacket`
- `CreateCharacterResultPacket`
- `EnterWorldPacket`
- `EnterWorldResultPacket`
- `CharacterBaseStatsChangedPacket`
- `CharacterCurrentStateChangedPacket`

## World
- `MapJoinedPacket`
- `TravelToMapPacket`
- `TravelToMapResultPacket`
- `CharacterPositionSyncPacket`
- `ObservedCharacterSpawnedPacket`
- `ObservedCharacterDespawnedPacket`
- `ObservedCharacterMovedPacket`
- `ObservedCharacterCurrentStateChangedPacket`

---

# Những điểm dễ vỡ luồng nhất nếu người mới không biết

1. Không đặt vị trí server thẳng vào Unity world mà không qua `PlayableBounds` mapping.
2. Không dùng local movement controller cho remote player.
3. Không sửa packet ID/generated file bằng tay mà không hiểu `GameShared` workflow.
4. Không quên sync `GameShared.dll` sang Unity sau khi đổi packet/shared model.
5. Không để server push vị trí local player về client mỗi tick nếu local đang tự simulate.
6. Hiện tại observer là full-map same-instance. Nếu thay logic này, phải hiểu nó sẽ ảnh hưởng trực tiếp đến test sync phase 1.
7. `World.unity` đã được wiring với các presenter chính. Nếu sửa scene, cần giữ đúng reference root.

---

# Trạng thái phase 1 hiện tại

Phase 1 đã đạt được các mục tiêu chính:
- đăng ký
- đăng nhập
- tạo nhân vật
- enter world
- load map trên client
- local movement
- travel private/public map
- remote player spawn/despawn
- sync move giữa nhiều client

Những thứ chưa phải phase 1 final architecture:
- interest management theo radius/zone chi tiết
- combat system dùng chung local/remote
- prediction/reconciliation đầy đủ
- mobile touch UI bridge đầy đủ
- animation/state machine multiplayer hoàn chỉnh

Tài liệu này nên được cập nhật mỗi khi phase 1 có thay đổi luồng packet, scene wiring hoặc movement policy.
