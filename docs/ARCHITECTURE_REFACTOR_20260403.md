# Refactor kiến trúc 2026-04-03

## Mục tiêu

Đợt refactor này xử lý 4 cụm lớn:

1. Tách [MapInstance.cs](/F:/PhamNhanOnline/GameServer/World/MapInstance.cs) thành các partial file theo trách nhiệm.
2. Tách [ServiceCollectionExtensions.cs](/F:/PhamNhanOnline/GameServer/Extensions/ServiceCollectionExtensions.cs) để composition root dễ đọc và dễ debug hơn.
3. Tách [WorldPortalPresenter.cs](/F:/PhamNhanOnline/ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/WorldPortalPresenter.cs) và [WorldTargetActionController.cs](/F:/PhamNhanOnline/ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/WorldTargetActionController.cs) để giảm độ phình của file.
4. Dựng một lớp nền `presentation replication` ở client để gom các semantic event presentation vào một bus chung, thay vì mỗi feature lại dựng một đường sync presentation riêng.

Phạm vi của pass này là refactor cấu trúc và thêm nền tảng mới. Không đổi protocol gameplay chính, không đổi schema DB, không cố rewrite toàn bộ hệ thống sync.

## Những gì đã làm

### 1. Refactor `MapInstance`

File gốc:
- [MapInstance.cs](/F:/PhamNhanOnline/GameServer/World/MapInstance.cs)

Sau refactor:
- [MapInstance.cs](/F:/PhamNhanOnline/GameServer/World/MapInstance.cs)
- [MapInstance.Players.cs](/F:/PhamNhanOnline/GameServer/World/MapInstance.Players.cs)
- [MapInstance.Combat.cs](/F:/PhamNhanOnline/GameServer/World/MapInstance.Combat.cs)
- [MapInstance.GroundRewards.cs](/F:/PhamNhanOnline/GameServer/World/MapInstance.GroundRewards.cs)
- [MapInstance.Runtime.cs](/F:/PhamNhanOnline/GameServer/World/MapInstance.Runtime.cs)
- [MapInstance.Events.cs](/F:/PhamNhanOnline/GameServer/World/MapInstance.Events.cs)

Phân rã trách nhiệm:
- `MapInstance.cs`: field, property, constructor, snapshot read, `SpawnGroupRuntimeState`.
- `MapInstance.Players.cs`: join/leave player, cell index, nearby player query.
- `MapInstance.Combat.cs`: damage/heal/shield/stun/stat modifier, skill execution queue.
- `MapInstance.GroundRewards.cs`: add/allocate/claim ground reward.
- `MapInstance.Runtime.cs`: update loop, enemy state update, spawn group update, ground reward expiry, completion rule.
- `MapInstance.Events.cs`: dequeue runtime event queue và runtime event type definitions.

Kết quả:
- giảm file đơn khối rất lớn thành các nhóm rõ nghĩa.
- không đổi public API của `MapInstance`.
- không đổi lock model, queue model hay lifecycle runtime.

### 2. Refactor `ServiceCollectionExtensions`

File gốc:
- [ServiceCollectionExtensions.cs](/F:/PhamNhanOnline/GameServer/Extensions/ServiceCollectionExtensions.cs)

Sau refactor:
- [ServiceCollectionExtensions.cs](/F:/PhamNhanOnline/GameServer/Extensions/ServiceCollectionExtensions.cs)
- [ServiceCollectionExtensions.ConfigBuilders.cs](/F:/PhamNhanOnline/GameServer/Extensions/ServiceCollectionExtensions.ConfigBuilders.cs)

Phân rã trách nhiệm:
- file chính giữ phần đăng ký service/repository/handler/middleware.
- file `ConfigBuilders` giữ phần build bootstrap config và DB-backed config:
  - `BuildGameTimeBootstrapConfig`
  - `BuildCharacterCreateConfig`
  - `BuildGameRandomConfigFromDatabase`
  - `BuildGameConfigValuesFromDatabase`
  - `LoadConfig`
  - parser helper cho `int/float/double`
  - `BuildConfigJsonOptions`

Kết quả:
- composition root đỡ lẫn giữa “đăng ký DI” và “build config từ file/DB”.
- dễ tìm chỗ gây lỗi startup hơn.
- dễ cô lập rule chống DI loop hơn.

### 3. Refactor `WorldPortalPresenter`

File gốc:
- [WorldPortalPresenter.cs](/F:/PhamNhanOnline/ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/WorldPortalPresenter.cs)

Sau refactor:
- [WorldPortalPresenter.cs](/F:/PhamNhanOnline/ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/WorldPortalPresenter.cs)
- [WorldPortalPresenter.Interaction.cs](/F:/PhamNhanOnline/ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/WorldPortalPresenter.Interaction.cs)
- [WorldPortalPresenter.Visuals.cs](/F:/PhamNhanOnline/ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/WorldPortalPresenter.Visuals.cs)

Phân rã trách nhiệm:
- file chính: lifecycle, readiness hooks, interaction request routing, resolve portal world position.
- `Interaction`: touch portal polling, entry intent, log diagnostics, actual `UsePortalAsync`.
- `Visuals`: build/rebuild portal object, configure collider, label, visual root, runtime binding.

Kết quả:
- tách rõ “portal gameplay interaction” khỏi “portal visual build”.
- dễ debug hơn khi lỗi nằm ở `touch portal`, `double click`, `collider`, hoặc `rebuild visuals`.

### 4. Refactor `WorldTargetActionController`

File gốc:
- [WorldTargetActionController.cs](/F:/PhamNhanOnline/ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/WorldTargetActionController.cs)

Sau refactor:
- [WorldTargetActionController.cs](/F:/PhamNhanOnline/ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/WorldTargetActionController.cs)
- [WorldTargetActionController.Execution.cs](/F:/PhamNhanOnline/ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/WorldTargetActionController.Execution.cs)

Phân rã trách nhiệm:
- file chính: field, lifecycle, update loop, public request entrypoint.
- `Execution`: resolve action range, resolve distance, preferred approach, runtime wiring, event binding, dead-state gating, actual execution/cancel.

Kết quả:
- dễ theo dấu hơn từ `request` tới `execute`.
- logic range/approach không còn trộn lẫn với lifecycle controller.

### 5. Thiết kế lớp nền `presentation replication`

Mục tiêu của lớp này:
- không sync raw Unity component.
- không thay thế semantic gameplay packet hiện tại.
- chỉ chuẩn hóa các semantic event có ý nghĩa presentation thành một bus dùng chung ở client.

File mới:
- [ClientPresentationReplicationEventKind.cs](/F:/PhamNhanOnline/ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/PresentationReplication/Application/ClientPresentationReplicationEventKind.cs)
- [ClientPresentationReplicationEvent.cs](/F:/PhamNhanOnline/ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/PresentationReplication/Application/ClientPresentationReplicationEvent.cs)
- [ClientPresentationReplicationState.cs](/F:/PhamNhanOnline/ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/PresentationReplication/Application/ClientPresentationReplicationState.cs)
- [ClientPresentationReplicationService.cs](/F:/PhamNhanOnline/ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/PresentationReplication/Application/ClientPresentationReplicationService.cs)

Integration:
- [ClientRuntime.cs](/F:/PhamNhanOnline/ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Core/Application/ClientRuntime.cs)
- [WorldSceneController.cs](/F:/PhamNhanOnline/ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/WorldSceneController.cs)

Behavior hiện tại:
- gom các event sẵn có từ `ClientCombatState` và `ClientWorldState` thành normalized event:
  - `MapChanged`
  - `SkillCastStarted`
  - `SkillCastReleased`
  - `SkillImpactResolved`
  - `GroundRewardUpserted`
  - `GroundRewardRemoved`
- giữ một ring buffer event gần nhất.
- expose `EventPublished` để presenter/fx system khác có thể subscribe về sau.

Điểm quan trọng:
- đây là **client-side foundation**.
- chưa thêm packet/protocol mới vào `GameShared`.
- chưa sync raw particle/animator/trail.
- chưa thay semantic gameplay state bằng generic sync.

Thiết kế này giữ đúng hướng cũ của project:
- server authoritative cho gameplay.
- client dựng visual/presentation từ event có nghĩa nghiệp vụ.
- generic hóa ở tầng event/state bus, không generic hóa ở tầng Unity component.

## Những gì cố ý chưa làm trong pass này

- Không tách thêm `WorldClickTargetSelectionController`.
- Không thiết kế full server-side presentation replication protocol.
- Không đổi rule gameplay cho portal/item/combat.
- Không thêm DB config, migration hay packet mới cho refactor này.
- Không rewrite `MapInstance` thành nhiều class runtime service khác nhau. Pass này mới dừng ở partial split an toàn.

## Khó khăn và lưu ý kỹ thuật

### 1. `Assembly-CSharp.csproj` local build

Trong máy local, `Assembly-CSharp.csproj` là project generated của Unity và đang dùng explicit compile list. Khi thêm file `.cs` mới, `dotnet build` chỉ thấy file mới sau khi project được regenerate hoặc compile list được cập nhật.

Ý nghĩa thực tế:
- source code và `.meta` mới là phần cần commit.
- nếu build CLI bằng `Assembly-CSharp.csproj`, nên mở Unity hoặc regenerate project trước.

### 2. Không để rơi DI loop ở server

Refactor này không đổi rule cũ:
- không inject ngược qua `NetworkServer` / `INetworkSender`.
- config builder và runtime service vẫn phải tránh vòng DI.

### 3. `presentation replication` hiện mới là nền

Lớp mới chưa thay thế các skill presenter hiện có. Nó chỉ chuẩn hóa event để bước sau:
- animation trigger
- teleport visual
- aura state
- projectile visual
có thể đi vào một đường presentation bus dùng chung.

## Verification đã chạy

Server:
```powershell
dotnet build GameServer/GameServer.csproj
```

Client:
```powershell
dotnet build ClientUnity/PhamNhanOnline/Assembly-CSharp.csproj
```

Kết quả:
- server build pass
- client build pass

## Rollback nếu có lỗi

### Mức 1: rollback từng cụm

Nếu lỗi chỉ nằm ở một cụm, rollback đúng cụm đó sẽ an toàn hơn.

#### Rollback `MapInstance`

Khôi phục về monolith:
- restore [MapInstance.cs](/F:/PhamNhanOnline/GameServer/World/MapInstance.cs)
- xoá:
  - [MapInstance.Players.cs](/F:/PhamNhanOnline/GameServer/World/MapInstance.Players.cs)
  - [MapInstance.Combat.cs](/F:/PhamNhanOnline/GameServer/World/MapInstance.Combat.cs)
  - [MapInstance.GroundRewards.cs](/F:/PhamNhanOnline/GameServer/World/MapInstance.GroundRewards.cs)
  - [MapInstance.Runtime.cs](/F:/PhamNhanOnline/GameServer/World/MapInstance.Runtime.cs)
  - [MapInstance.Events.cs](/F:/PhamNhanOnline/GameServer/World/MapInstance.Events.cs)

Git nhanh:
```powershell
git restore GameServer/World/MapInstance.cs
git clean -f GameServer/World/MapInstance.Players.cs GameServer/World/MapInstance.Combat.cs GameServer/World/MapInstance.GroundRewards.cs GameServer/World/MapInstance.Runtime.cs GameServer/World/MapInstance.Events.cs
```

#### Rollback `ServiceCollectionExtensions`

Khôi phục file cũ:
- restore [ServiceCollectionExtensions.cs](/F:/PhamNhanOnline/GameServer/Extensions/ServiceCollectionExtensions.cs)
- xoá [ServiceCollectionExtensions.ConfigBuilders.cs](/F:/PhamNhanOnline/GameServer/Extensions/ServiceCollectionExtensions.ConfigBuilders.cs)

Git nhanh:
```powershell
git restore GameServer/Extensions/ServiceCollectionExtensions.cs
git clean -f GameServer/Extensions/ServiceCollectionExtensions.ConfigBuilders.cs
```

#### Rollback `WorldPortalPresenter`

Khôi phục monolith:
- restore [WorldPortalPresenter.cs](/F:/PhamNhanOnline/ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/WorldPortalPresenter.cs)
- xoá:
  - [WorldPortalPresenter.Interaction.cs](/F:/PhamNhanOnline/ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/WorldPortalPresenter.Interaction.cs)
  - [WorldPortalPresenter.Visuals.cs](/F:/PhamNhanOnline/ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/WorldPortalPresenter.Visuals.cs)
  - `.meta` tương ứng

#### Rollback `WorldTargetActionController`

Khôi phục monolith:
- restore [WorldTargetActionController.cs](/F:/PhamNhanOnline/ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/WorldTargetActionController.cs)
- xoá:
  - [WorldTargetActionController.Execution.cs](/F:/PhamNhanOnline/ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/WorldTargetActionController.Execution.cs)
  - `.meta` tương ứng

#### Rollback `presentation replication`

Khôi phục tích hợp cũ:
- restore:
  - [ClientRuntime.cs](/F:/PhamNhanOnline/ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Core/Application/ClientRuntime.cs)
  - [WorldSceneController.cs](/F:/PhamNhanOnline/ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/WorldSceneController.cs)
- xoá:
  - [ClientPresentationReplicationEventKind.cs](/F:/PhamNhanOnline/ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/PresentationReplication/Application/ClientPresentationReplicationEventKind.cs)
  - [ClientPresentationReplicationEvent.cs](/F:/PhamNhanOnline/ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/PresentationReplication/Application/ClientPresentationReplicationEvent.cs)
  - [ClientPresentationReplicationState.cs](/F:/PhamNhanOnline/ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/PresentationReplication/Application/ClientPresentationReplicationState.cs)
  - [ClientPresentationReplicationService.cs](/F:/PhamNhanOnline/ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/PresentationReplication/Application/ClientPresentationReplicationService.cs)
  - folder `.meta` và file `.meta` tương ứng

### Mức 2: rollback toàn bộ pass refactor

Nếu đã commit pass này:
```powershell
git revert <commit_sha>
```

Nếu chưa commit:
```powershell
git restore GameServer/Extensions/ServiceCollectionExtensions.cs GameServer/World/MapInstance.cs ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Core/Application/ClientRuntime.cs ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/WorldPortalPresenter.cs ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/WorldSceneController.cs ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/WorldTargetActionController.cs
git clean -fd GameServer/Extensions/ServiceCollectionExtensions.ConfigBuilders.cs GameServer/World/MapInstance.Players.cs GameServer/World/MapInstance.Combat.cs GameServer/World/MapInstance.GroundRewards.cs GameServer/World/MapInstance.Runtime.cs GameServer/World/MapInstance.Events.cs ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/PresentationReplication ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/WorldPortalPresenter.Interaction.cs ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/WorldPortalPresenter.Visuals.cs ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/WorldTargetActionController.Execution.cs
```

### Mức 3: rollback Unity asset nếu editor bị cache state cũ

Sau rollback code:
1. Mở Unity để regenerate project/script database.
2. Nếu script reference bị stale, reimport folder:
   - `Assets/Game/Runtime/Features/World/Presentation`
   - `Assets/Game/Runtime/Features/PresentationReplication`
3. Build lại:
```powershell
dotnet build ClientUnity/PhamNhanOnline/Assembly-CSharp.csproj
```

## Khuyến nghị bước tiếp theo

Sau pass này, thứ đáng làm tiếp là:

1. Dùng `presentation replication` để gom một case thật:
   - `charge aura`
   - hoặc `teleport visual`
2. Tách `MapInstance` thêm một bước nữa nếu cần:
   - extract helper class cho spawn/runtime queue
3. Tách tiếp world interaction:
   - `WorldClickTargetSelectionController`
   - hoặc normalize range policy client/server

Pass hiện tại đủ để đi tiếp từng bước, chưa khóa project vào một hướng khó rollback.
