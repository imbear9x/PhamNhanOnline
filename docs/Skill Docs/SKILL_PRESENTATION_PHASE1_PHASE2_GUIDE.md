# Hệ Thống Skill Presentation Phase 1-2

## Mục tiêu

Tài liệu này mô tả hệ thống skill presentation phía Unity client sau khi đã:

- bổ sung `SkillExecutionId` từ server
- dựng xong Phase 1 + Phase 2
- chừa sẵn điểm mở rộng cho Phase 3

Mục tiêu của hệ thống là:

- gameplay vẫn server-authoritative
- animation, VFX, SFX, HUD/cast bar không chôn cứng trong `ClientCombatService`
- sau này thêm skill mới chủ yếu bằng data và presenter, không đập lại combat core

## Những gì đã có sau Phase 1-2

### 1. Server trả về `SkillExecutionId`

`SkillExecutionId` đã được đẩy xuyên suốt các packet:

- `AttackEnemyResultPacket`
- `SkillCastStartedPacket`
- `SkillImpactResolvedPacket`

Ý nghĩa:

- mỗi lần thi triển skill có một id runtime riêng
- client có thể nối đúng `cast -> release -> impact`
- đây là nền tảng để sau này support multi-hit, summon, projectile phức tạp

## 2. Combat authoritative vẫn giữ nguyên

Các file authoritative chính:

- `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/Combat/Application/ClientCombatService.cs`
- `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/Combat/Application/ClientCombatState.cs`
- `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/Combat/Application/CombatRuntimeNotices.cs`

Vai trò:

- gửi packet dùng skill
- nhận packet cast/impact/cooldown từ server
- phát notice runtime

Lưu ý:

- không cắm animation/VFX trực tiếp vào `ClientCombatService`
- `ClientCombatService` chỉ là tầng gameplay/network

## 3. Tầng presentation mới

Phase 1-2 thêm tầng presentation riêng:

- `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/Combat/Presentation/ClientSkillPresentationState.cs`
- `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/Combat/Presentation/ClientSkillPresentationService.cs`
- `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/Combat/Presentation/SkillPresentationRuntimeTypes.cs`
- `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/Combat/Presentation/SkillWorldPresentationCatalog.cs`
- `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/Combat/Presentation/CharacterSkillPresenter.cs`
- `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/Combat/Presentation/CharacterPresentationSockets.cs`
- `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/Combat/Presentation/CharacterSkillPresenterRegistry.cs`

### 4. Presenter đã được gắn vào actor

Các presenter world hiện tại đã tự gắn `CharacterSkillPresenter`:

- local player:
  `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/WorldLocalPlayerPresenter.cs`
- remote player:
  `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/RemoteCharacterPresenter.cs`
- enemy/boss:
  `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/EnemyPresenter.cs`

Như vậy cùng một tầng presentation có thể điều khiển:

- local caster
- remote caster
- target là player khác
- target là enemy/boss

## Flow runtime hiện tại

### Bước 1. Client yêu cầu dùng skill

UI hoặc target action gọi:

- `ClientRuntime.CombatService.TryUseSkill(...)`
- `ClientRuntime.CombatService.TryUseSkillOnTarget(...)`

### Bước 2. Server accept và phát packet

Server trả:

- `AttackEnemyResultPacket` có `SkillExecutionId`
- `SkillCastStartedPacket` có `SkillExecutionId`

### Bước 3. Combat layer publish notice

`ClientCombatService` chuyển packet thành:

- `SkillCastStartedNotice`
- `SkillImpactResolvedNotice`

và notice này đã có:

- `SkillExecutionId`
- `CasterCharacterId`
- `Target`
- `SkillId`
- `PlayerSkillId`
- `SkillSlotIndex`
- các mốc thời gian cast/impact

### Bước 4. Presentation layer tạo timeline

`ClientSkillPresentationService` subscribe notice combat và tạo execution theo key:

- `MapId`
- `InstanceId`
- `SkillExecutionId`

Nó sẽ:

- resolve definition từ catalog
- tạo snapshot runtime trong `ClientSkillPresentationState`
- gọi `CharacterSkillPresenter` của caster
- đến thời điểm `CastCompletedAtUtc` thì tự phát phase `Released`
- khi nhận `SkillImpactResolvedNotice` thì phát impact lên caster/target rồi complete execution

### Bước 5. Presenter điều khiển world presentation

`CharacterSkillPresenter` hiện chịu trách nhiệm:

- quay mặt về target khi cast
- play animation state nếu definition có cấu hình
- spawn cast FX
- spawn release FX
- spawn impact FX
- cleanup FX theo `SkillExecutionId`

## Các phase đang support

Hiện runtime presentation có các phase:

- `CastStarted`
- `Released`
- `ImpactResolved`
- `Completed`
- `Cancelled` để dành cho Phase 3

Ý nghĩa:

- `CastStarted`: packet cast đã tới, bắt đầu niệm/vung/charge
- `Released`: tới thời điểm thả skill, thường là nhả projectile hoặc kết thúc wind-up
- `ImpactResolved`: server đã xác nhận hit/resolve
- `Completed`: timeline này đã xong

## Archetype hiện tại

Hệ thống core không chia cứng theo kiếm/rìu/tán/chưởng bằng class riêng.

Thay vào đó, data presentation dùng các archetype:

- `MeleeWeaponSwing`
- `WeaponProjectile`
- `HandProjectile`
- `SummonStrike`
- `SelfBuff`

Ý nghĩa:

- code core chỉ biết "đây là loại trình diễn nào"
- content cụ thể của kiếm/rìu/tán nằm ở definition data

## Catalog và definition

File chính:

- `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/Combat/Presentation/SkillWorldPresentationCatalog.cs`

Catalog resolve theo thứ tự:

1. `SkillId`
2. `SkillCode`
3. `SkillGroupCode`
4. preset theo `SkillGroupCode`
5. fallback heuristic

### `SkillWorldPresentationDefinition` đang có các field chính

- định danh:
  - `SkillId`
  - `SkillCode`
  - `SkillGroupCode`
- mô tả kiểu trình diễn:
  - `Archetype`
- animation:
  - `CastStateName`
  - `ReleaseStateName`
  - `TargetImpactStateName`
- socket:
  - `SourceSocket`
  - `ImpactSocket`
- effect:
  - `CastFxPrefab`
  - `ReleaseFxPrefab`
  - `ImpactFxPrefab`
  - `FxLifetimeSeconds`
- behavior:
  - `FaceTargetOnCast`

## Socket system

File:

- `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/Combat/Presentation/CharacterPresentationSockets.cs`

Socket type hiện có:

- `Root`
- `VisualRoot`
- `Weapon`
- `HandLeft`
- `HandRight`
- `Chest`
- `Head`
- `Ground`
- `TargetCenter`

Nếu prefab chưa cắm socket tay/chest/weapon rõ ràng, hệ thống có fallback tìm child theo tên phổ biến.

Mục đích:

- artist có thể cắm đúng anchor sau này mà không sửa service
- nhiều actor khác nhau vẫn dùng chung pipeline

## Vì sao `WorldSceneController` có reference tới catalog

`WorldSceneController` hiện đã có field:

- `SkillWorldPresentationCatalog`

và mỗi frame gọi:

- `ClientRuntime.SkillPresentationService.Tick(DateTime.UtcNow)`

Lý do:

- presentation layer cần một nơi scene-level để tick timeline release
- không muốn chôn logic tick trong HUD hoặc player controller

## `ClientSkillPresentationState` dùng để làm gì

Đây là state bàn giao cho các hệ sau này:

- UI skill timeline
- cast bar nâng cao
- debug runtime
- replay / combat log
- phase 3 multi-hit và channeling

Nó đang giữ các active execution và có event:

- `ExecutionStarted`
- `ExecutionPhaseChanged`
- `ExecutionCompleted`

## Hướng dẫn thêm skill mới trong Phase 1-2

### Trường hợp 1. Chỉ cần cắm nội dung mới, không đổi code

Làm theo thứ tự:

1. Tạo hoặc mở `SkillWorldPresentationCatalog`
2. Thêm `SkillWorldPresentationDefinition`
3. map bằng `SkillId` hoặc `SkillGroupCode`
4. chọn `Archetype`
5. điền animation state
6. điền FX prefab và socket
7. test local player, remote player, enemy target

### Trường hợp 2. Cần thêm archetype mới

Chỉ thêm archetype mới khi:

- behavior thật sự khác nhóm cũ
- không thể mô tả bằng data của archetype sẵn có

Các chỗ cần chạm:

1. thêm enum mới trong `SkillPresentationArchetype`
2. cập nhật logic resolve/fallback nếu cần
3. nếu archetype cần behavior đặc biệt thì thêm nhánh xử lý trong `CharacterSkillPresenter` hoặc `ClientSkillPresentationService`

Khuyến nghị:

- ưu tiên giữ số lượng archetype ít
- không tạo archetype chỉ vì đổi vũ khí từ kiếm sang rìu

## Những gì Phase 1-2 cố tình chưa làm sâu

- chưa có `multi-hit`
- chưa có `channeling` dài hơi
- chưa có `cancel/interrupt` thật sự
- chưa có projectile logic phức tạp kiểu nảy, xuyên, tách nhánh
- chưa có summon actor sống độc lập
- chưa có animation event marker chính xác từng frame

Các phần đó được chừa chỗ cắm cho Phase 3.

## Quy tắc mở rộng để không phá kiến trúc

### Nên làm

- thêm presentation bằng definition data trước
- chỉ dùng `ClientSkillPresentationService` để điều phối phase
- chỉ dùng `CharacterSkillPresenter` để chạm `Animator`, socket, FX spawn
- coi `SkillExecutionId` là identity của một lần cast

### Không nên làm

- không spawn VFX trực tiếp trong `ClientCombatService`
- không hardcode animation skill trong HUD
- không để `WorldTargetActionController` biết logic effect cụ thể
- không coi `SkillId` là đủ để phân biệt mọi execution runtime

## Tóm tắt nhanh cho dev tiếp quản

Nếu dev mới vào dự án, nên đọc code theo thứ tự:

1. `ClientCombatService.cs`
2. `CombatRuntimeNotices.cs`
3. `ClientSkillPresentationService.cs`
4. `ClientSkillPresentationState.cs`
5. `SkillWorldPresentationCatalog.cs`
6. `CharacterSkillPresenter.cs`
7. `WorldLocalPlayerPresenter.cs`
8. `RemoteCharacterPresenter.cs`
9. `EnemyPresenter.cs`

Sau khi hiểu chuỗi này, việc cắm anim/VFX mới sẽ dễ và ít rủi ro hơn rất nhiều.
