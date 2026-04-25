# Unity Client Codebase Audit Report

Ngày audit: 2026-04-25

Phạm vi đã đọc: `ClientUnity/PhamNhanOnline/Assets/Game`, các scene chính trong `Assets/Game/Scenes`, prefab/config chính trong `Assets/Game/Content`, `Packages/manifest.json`, shared packet source trong `GameShared`, và các luồng runtime chính: bootstrap, network, world, character, combat presentation, UI, pooling.

Không audit sâu third-party package trong `Assets/Plugins`, `Library/PackageCache`, animation controller binary/free-pack ngoài `Assets/Game`, hoặc toàn bộ texture/sprite/audio vì không phải code/prefab logic cốt lõi.

## 1. Executive Summary

Client hiện tại có nền tảng chia folder theo feature khá rõ: `Core`, `Network`, `Infrastructure`, `Features`, `UI`, `Content`, `Scenes`. Các phần world runtime mới như `WorldEntityMovementView`, `EnemyPresenter`, `RemoteCharacterPresenter`, `WorldSceneReadinessService`, `ClientPoolService` đang đi đúng hướng: có ý thức tách presentation nhỏ ra khỏi controller lớn.

Điểm yếu lớn nhất không phải là code bẩn ngay từ đầu, mà là code đang chuyển tiếp giữa hai style:

- Style mới: presenter/view nhỏ, serialized reference rõ, controller bind runtime có chủ đích.
- Style cũ: controller lớn tự polling, tự auto-wire, tự gọi `ClientRuntime`/singleton, tự xử lý business/action/network/UI cùng lúc.

Rủi ro phình code là **High** nếu tiếp tục thêm inventory, quest, boss, dungeon, shop, sect, dialog NPC theo kiểu hiện tại. Các class như `WorldCraftingPanelController`, `WorldInventoryPanelController`, `WorldTargetActionController`, `LocalCharacterActionController`, `ClientWorldState`, `WorldMenuController` sẽ thành điểm nghẽn vì mỗi tính năng mới đều dễ chui vào các class này.

Mức độ dễ sửa bug hiện tại: **Medium**. Bug world/movement còn trace được vì file chưa quá phân tán, nhưng UI/action/network đang có nhiều đường gọi chéo qua `ClientRuntime`, `WorldSceneController.Instance`, `WorldUIController.Instance`, `WorldModalUIManager.Instance`, `ClientPoolService.Instance`. Khi bug xảy ra trong UI/action, khó biết nguồn sự kiện thật sự đến từ packet, state, polling `Update`, hay popup singleton.

Ưu tiên Phase 1 không nên refactor lớn. Nên làm nhanh các việc: xóa auto-wire vi phạm rule client, chuẩn hóa lifecycle panel, giảm polling/alloc nóng, thêm timeout/correlation guard cho request/response, và đặt boundary rõ hơn cho character movement/presentation.

## 2. Current Architecture Overview

Module chính:

- `Bootstrap`: `ClientBootstrap` khởi tạo `ClientRuntime`, giữ object qua scene bằng `DontDestroyOnLoad`, gọi `ClientRuntime.Connection.Tick()` mỗi frame.
- `Core/Application`: `ClientRuntime` là service locator static, tạo toàn bộ state/service client.
- `Network`: `LiteNetLibClientTransport`, `ClientConnectionService`, `ClientPacketDispatcher`, shared `GameShared.Packets.PacketSerializer`.
- `Features/*/Application`: state/service theo domain: auth, character, inventory, alchemy, martial arts, skills, combat, world, targeting.
- `Features/*/Presentation`: presenter world/character/combat như local player, remote player, enemy, portal, reward, skill FX.
- `UI/*`: view/controller cho HUD, world menu, inventory, crafting, cultivation, potential, skill.
- `Content`: prefab, config ScriptableObject, catalog.
- `Scenes`: `Bootstrap`, `Login`, `World`.

UI flow:

`World.unity` chứa nhiều controller/view trực tiếp. Các UI controller đọc state từ `ClientRuntime`, bind button/view events, rồi gọi application service hoặc controller khác. Một số view đã tách tương đối tốt (`WorldCraftingPanelView`, list/slot views), nhưng nhiều view vẫn gọi singleton modal trực tiếp.

Gameplay flow:

Server gửi packet -> `ClientWorldService` cập nhật `ClientWorldState`/`ClientCharacterState`/`ClientCombatState` -> world presenters nhận event hoặc polling -> prefab/presenter update transform, targetable, skill FX, HUD. Local player đi theo `LocalCharacterActionController`, rồi `WorldLocalMovementSyncController` gửi vị trí client lên server.

Network flow:

`ClientConnectionService.Send()` serialize packet qua `PacketSerializer`, chọn delivery policy, gửi qua LiteNetLib. Inbound payload deserialize, log packet, dispatch theo type qua `ClientPacketDispatcher.DynamicInvoke`.

Character/animation flow:

- Local player: `WorldLocalPlayerPresenter` spawn prefab, cấu hình `LocalCharacterActionController`, `WorldTargetable`, `CharacterSkillPresenter`.
- Remote player: `RemoteCharacterPresenter` dùng `WorldEntityMovementView`, `PlayerView`, `CharacterSkillPresenter`, animator `MoveSpeed`.
- Enemy: `EnemyPresenter` dùng `WorldEntityMovementView`, `WorldTargetable`, `CharacterSkillPresenter`, ground snap. Enemy chưa có contract animation/facing rõ như remote player.
- Skill: `ClientSkillPresentationService` nhận combat state events, resolve catalog, gọi `CharacterSkillPresenter` spawn FX/projectile qua pool.

Asset flow:

Client dùng ScriptableObject catalog cho map/enemy/skill/UI icon. Addressables có trong package manifest nhưng trong runtime `Assets/Game` gần như chưa dùng; portal visual còn fallback `Resources.Load`.

## 3. Critical Issues

| Severity | Area | File/Prefab/Scene | Issue | Impact | Recommendation |
|---|---|---|---|---|---|
| High | Architecture | `ClientRuntime.cs`, nhiều UI/world classes | Static service locator và singleton được gọi rộng | Dễ gọi chéo, khó test, khó trace flow khi bug UI/network/action | Giữ tạm `ClientRuntime`, nhưng tạo facade theo feature và scene composition root explicit; giảm gọi trực tiếp từ view |
| High | Client Rule | `LocalCharacterActionController`, `WorldLocalPlayerPresenter`, `CharacterSkillPresenter`, `WorldTargetable`, `GroundRewardPresenter`, `WorldPortalPresenter` | Còn nhiều auto-wire/runtime `AddComponent`/`transform.Find` | Missing ref bị che, prefab/scene không còn là source of truth, trái rule "không auto-wire" | Phase 1: chuyển sang required serialized refs hoặc runtime bind do owner controller gọi rõ ràng |
| High | UI | `WorldCraftingPanelController.cs` | Controller 1584 dòng ôm station context, recipe, inventory projection, preview, timer, popup, action | Mỗi thay đổi crafting dễ sửa nhiều vùng; GC/polling tăng khi panel mở | Tách ViewModel/Presenter state, action service adapter, popup coordinator; giữ view chỉ bind |
| High | UI | `WorldInventoryPanelController*.cs`, `WorldMenuController.cs` | UI controller xử lý business/action và polling mỗi frame | Dễ duplicate item rule, khó reuse inventory grid ngoài world menu | Dọn action thành `InventoryPanelPresenter`/`InventoryActionCoordinator`; refresh bằng event/snapshot dirty |
| High | Network/Application | `ClientWorldTravelService`, `ClientCharacterService`, `ClientAlchemyService`, `ClientInventoryService`, `ClientSkillService` | `TaskCompletionSource` theo operation, thiếu timeout/correlation id | Request cùng loại chồng nhau có thể ghi đè pending task; lỗi packet/mất result làm UI treo | Thêm `ClientRequestTracker` có timeout, cancel on disconnect, optional request id |
| High | Movement | `LocalCharacterActionController`, `WorldEntityMovementView`, `WorldLocalMovementSyncController` | Local/remote/enemy movement chưa dùng chung policy rõ cho speed/time/interpolation | Dễ lệch tốc độ hiển thị giữa player/enemy, bug giật/tele khó debug | Chuẩn hóa `LocalEntityActionConfig`/movement presentation policy; speed conversion một nơi |
| Medium | Performance/GC | `ClientConnectionService`, `ClientPacketDispatcher`, `PacketSerializer` | Log mọi packet, `ToArray`, `MemoryStream.ToArray`, reflection invoke, `DynamicInvoke`, copy handler list mỗi packet | GC/log spam khi packet realtime nhiều | Tắt info log mặc định, thêm packet diagnostics sampling; thay DynamicInvoke bằng typed invoker cache |
| Medium | Performance/GC | `WorldAutoTargetSelectionController`, `WorldTargetableRegistry` | `GetSnapshot()` tạo array mỗi 0.2s; scan cả registry lẫn world state | Alloc và duplicate target candidate; dễ lệch source of truth | Registry expose non-alloc iteration hoặc cached list; chọn một nguồn target runtime |
| Medium | Scene/Prefab | `World.unity`, `WorldSceneController.cs` | Scene controller đang là composition root rộng, nhiều serialized dependency | Thêm feature mới sẽ phình inspector và controller | Chia sub-root theo World UI, World Entities, World Interaction, World Debug |
| Medium | Asset Loading | `WorldPortalPresenter.Visuals.cs` | Fallback `Resources.Load("World/Portals/PortalVisual_Default")` | Hardcode path, khó kiểm soát asset dependency/build | Đưa portal visual vào catalog/ScriptableObject, bỏ fallback path sau khi prefab đủ |
| Medium | Testability | `Assets/Game/Tests` | Chỉ có thư mục meta, chưa có EditMode/PlayMode test | Refactor movement/UI/network không có guard | Thêm test nhỏ cho state, request tracker, coordinate conversion, movement timing |

## 4. Detailed Findings

### Folder Structure

Vấn đề: Folder client đã chia theo feature, nhưng ranh giới chưa cứng.

Bằng chứng:

- `Assets/Game/Runtime/Features/*` có `Application` và `Presentation`.
- `Assets/Game/Runtime/UI/*` tồn tại song song với `Features/*/Presentation`.
- UI controller gọi trực tiếp application/service qua `ClientRuntime`, ví dụ `WorldCombatHudController`, `WorldCraftingPanelController`, `WorldInventoryPanelController`.
- Prefab enemy nằm trong `Content/Prefabs/Characters/Enermys`, folder bị typo `Enermys`.

Rủi ro: Khi thêm quest/shop/dungeon, code có thể rải giữa `Features`, `UI`, `World`, singleton mà không có boundary rõ. Developer mới khó biết logic nên đặt ở đâu.

Cách sửa đề xuất:

- Giữ folder hiện tại, chưa đổi lớn.
- Thêm rule: `UI/View` không gọi network/application service trực tiếp; chỉ bắn event lên controller/presenter.
- Với mỗi feature lớn, tạo ba lớp rõ: `Application`, `Presentation`, `UI`.
- Sửa typo folder `Enermys` chỉ khi có thời gian làm qua Unity Editor để giữ meta/GUID an toàn.

### Assembly Definition

Vấn đề: Không thấy `.asmdef` trong `Assets/Game`.

Bằng chứng:

- Search `.asmdef` dưới `Assets/Game` không ra file.
- 257 script C# trong `Assets/Game` đang compile chung assembly mặc định.

Rủi ro: Compile chậm dần, dependency giữa UI/Feature/Infrastructure không được compiler chặn. UI có thể reference mọi thứ, application có thể vô tình reference presentation nếu sau này thêm nhầm.

Cách sửa đề xuất:

- Phase 2 thêm asmdef theo lớp: `Game.Client.Core`, `Game.Client.Network`, `Game.Client.Infrastructure`, `Game.Client.Features.Application`, `Game.Client.Features.Presentation`, `Game.Client.UI`.
- Làm từng bước, bắt đầu bằng `Game.Client.Core`/`Network` trước để ít phá prefab.

### Scene/Prefab Organization

Vấn đề: `World.unity` chứa rất nhiều controller/presenter trực tiếp, vừa composition root vừa runtime manager vừa debug surface.

Bằng chứng:

- `World.unity` reference nhiều script: `WorldSceneController`, `WorldMapPresenter`, `WorldLocalPlayerPresenter`, `WorldLocalMovementSyncController`, `WorldRemotePlayersPresenter`, `WorldEnemiesPresenter`, `WorldPortalPresenter`, `WorldGroundRewardPresenter`, HUD/menu/crafting/cultivation/skill/potential/debug controllers.
- `WorldSceneController` có nhiều serialized refs và inject `WorldSceneContext` qua `GetComponentsInChildren<MonoBehaviour>`.

Rủi ro: Khi thêm boss, dungeon, quest, NPC dialog, scene root dễ thành danh sách dependency khổng lồ. Lỗi thiếu ref khó thấy nếu vẫn có auto-wire fallback.

Cách sửa đề xuất:

- Giữ `WorldSceneController` làm composition root, nhưng tách nhóm con:
  - `WorldEntityRootController`
  - `WorldInteractionRootController`
  - `WorldUIRootController`
  - `WorldDebugRootController`
- Mọi ref scene-owned phải kéo trong inspector hoặc bind qua `WorldSceneContext`; không tự `Find`.

### UI Architecture

Vấn đề: Có split view/controller nhưng controller vẫn ôm business logic và state projection lớn.

Bằng chứng:

- `WorldCraftingPanelController.cs`: 1584 dòng, xử lý context luyện đan/rèn/bùa, recipe selection, inventory projection, preview, quantity popup, practice session, timer.
- `WorldInventoryPanelController.ItemActions.cs`: xử lý equip/use/drop/martial-art book trong UI controller.
- `WorldCombatHudController`: tự resolve target mode, cooldown, skill, gửi combat request.
- `WorldMenuController`: vừa tab manager, vừa register screen, vừa refresh content theo frame.

Rủi ro: UI thay đổi nhỏ dễ ảnh hưởng gameplay action. Logic item/craft/skill bị trộn vào UI nên sau này làm shortcut, hotbar, mobile UI sẽ phải copy rule.

Cách sửa đề xuất:

- Tạo Presenter/ViewModel không phải `MonoBehaviour` cho mỗi panel phức tạp.
- Controller chỉ nhận event từ view, gọi presenter/application facade, rồi bind `ViewState`.
- Business/action rule đặt ở service/facade tái sử dụng: `InventoryActionCoordinator`, `CraftingPanelPresenter`, `CombatActionPresenter`.

### UI Panel Lifecycle

Vấn đề: Lifecycle panel chưa thống nhất.

Bằng chứng:

- `UIScreenService` chỉ `Register`, `Show`, `Hide`, `ShowOnly` bằng string id, không có `Init/Open/Close/Refresh/Dispose`.
- `ViewModelBase` thực chất là visibility helper MonoBehaviour, không phải ViewModel.
- Panel dùng pattern khác nhau: `WorldCraftingPanelView.Show/Hide`, `WorldMenuController.ShowMenu/HideMenu`, `WorldCultivationPanelController.ShowPanel/HidePanel`, popup qua `WorldModalUIManager`.

Rủi ro: Panel mở/đóng khó đảm bảo unsubscribe, stop coroutine/tween, refresh once, hide modal phụ. Một panel mới dễ tự phát minh lifecycle riêng.

Cách sửa đề xuất:

- Đổi dần sang interface:
  - `IWorldPanel.Open(context)`
  - `IWorldPanel.Close(reason)`
  - `IWorldPanel.Refresh(force)`
  - `IWorldPanel.DisposePanel()`
- `ViewModelBase` nên đổi tên thành `UIViewBase` hoặc thay bằng base panel rõ nghĩa.
- `UIScreenService` giữ cho Login/simple screen, không dùng làm window stack chính của world UI.

### Character Controller

Vấn đề: Local movement đang tập trung quá nhiều trách nhiệm trong `LocalCharacterActionController`.

Bằng chứng:

- `LocalCharacterActionController.cs`: 658+ dòng.
- Có input, speed conversion, physics body, ground check, flight/fall, animation state, facing, authoritative correction, auto-wire refs.
- `AutoWireMissingReferences()` dùng `GetComponent`, `transform.Find("VisualRoot")`, `transform.Find("GroundCheck")`, `GetComponentInChildren<Animator>`, và `AddComponent<KeyboardCharacterActionInputSource>`.

Rủi ro: Sửa một hành động nhân vật có thể ảnh hưởng movement, input, animation, network correction cùng lúc. Vi phạm rule client mới: không auto-wire trong project client.

Cách sửa đề xuất:

- Phase 1: bỏ runtime `AddComponent` và `transform.Find`; prefab local player phải có ref đủ.
- Phase 2: tách:
  - `CharacterInputReader`
  - `CharacterMovementMotor`
  - `CharacterAnimationDriver`
  - `CharacterFacingDriver`
  - `CharacterAuthoritativeCorrection`
- Giữ public facade `LocalCharacterActionController` làm coordinator mỏng để giảm rủi ro.

### Character State/Action System

Vấn đề: Chưa có action/state abstraction chung cho Player/NPC/Monster/Boss.

Bằng chứng:

- Local player dùng `LocalCharacterActionController`.
- Remote player dùng `RemoteCharacterPresenter` + `WorldEntityMovementView`.
- Enemy dùng `EnemyPresenter` + `WorldEntityMovementView`.
- Skill cast/impact dùng `CharacterSkillPresenter`.
- Target/action dùng `WorldTargetActionController`.

Rủi ro: Cùng một behavior như move/facing/animation/death/cast sẽ tiếp tục duplicate theo loại entity. Boss sau này dễ thành nhánh riêng.

Cách sửa đề xuất:

- Tạo contract presentation chung:
  - `IWorldEntityView`
  - `IWorldMovementView`
  - `IWorldCombatPresentation`
  - `IWorldTargetableBinding`
- `Player/Enemy/Boss/NPC` chỉ khác data adapter và prefab, không khác movement/skill/facing driver nếu không cần.

### Animation/VFX/SFX

Vấn đề: Animation contract chưa tập trung và enemy chưa rõ animation/facing.

Bằng chứng:

- `LocalCharacterActionController` tự cache state names `Idle`, `Run`, `Jump/Fly/Fall`, parameter `MoveSpeed`.
- `RemoteCharacterPresenter` cũng tự set `MoveSpeed`.
- `CharacterSkillPresenter` play state theo string từ `SkillWorldPresentationCatalog`.
- `EnemyPresenter` không set animation/facing tương đương remote player.
- `CharacterSkillPresenter.AutoWireReferences()` có `GetComponentInChildren<Animator>` và tự `AddComponent<CharacterPresentationSockets>`.

Rủi ro: Animator param/state sai string sẽ chỉ fail runtime. Enemy/player có thể nhìn khác nhau dù cùng movement. Skill animation phụ thuộc catalog string mà không có validator mạnh.

Cách sửa đề xuất:

- Tạo `CharacterAnimatorContract` ScriptableObject hoặc static hash contract cho `MoveSpeed`, common states.
- Tạo `CharacterAnimationDriver` dùng chung local/remote/enemy.
- Viết editor validator cho `SkillWorldPresentationCatalog` kiểm tra state name tồn tại trong animator controller của prefab mẫu nếu có.
- Không tự add `CharacterPresentationSockets`; prefab phải có sockets nếu skill cần.

### Network Client

Vấn đề: Packet flow chạy được nhưng còn nhiều điểm GC/log/error handling yếu.

Bằng chứng:

- `ClientConnectionService.Send()` log mọi packet outbound.
- `HandlePayloadReceived()` copy payload bằng `ToArray`, deserialize không try/catch, log mọi inbound packet.
- `ClientPacketDispatcher.Dispatch()` lock, copy handler list bằng `ToArray`, gọi `DynamicInvoke`.
- `PacketSerializer` dùng `MemoryStream`, `BinaryWriter/Reader`, `ms.ToArray()`, reflection `MethodInfo.Invoke` cho serialize/deserialize.

Rủi ro: Khi realtime packet tăng, log và alloc sẽ gây noise/GC. Malformed packet có thể throw trước dispatcher catch. DynamicInvoke làm stack trace kém rõ và chậm.

Cách sửa đề xuất:

- Phase 1: wrap deserialize bằng try/catch và log packet incident tối thiểu.
- Tắt packet info log mặc định; chỉ bật qua verbose/sampling.
- Phase 2: tạo generated serializer invoker hoặc cache delegate typed thay reflection invoke.
- Dispatcher giữ typed invoker list, tránh `DynamicInvoke`.

### Data/Config

Vấn đề: Config đã dùng ScriptableObject, nhưng một số giá trị chưa có source of truth rõ giữa server units và world units.

Bằng chứng:

- `LocalCharacterActionConfig.asset` đang có `baseMoveSpeed: 300`, `serverMoveSpeedScale: 0.013`.
- `LocalCharacterActionController.ResolveBaseWorldMoveSpeed()` nếu có server base speed thì convert server units -> world units, nếu không có thì dùng thẳng `actionConfig.BaseMoveSpeed`.
- `WorldEntityMovementView` tự tính speed/interpolation bằng magic numbers nội bộ.
- `WorldLocalMovementSyncConfig.asset` có nhiều ngưỡng sync riêng.

Rủi ro: Nếu thiếu server base speed, local fallback có thể bị hiểu thành 300 world units/second. Player/enemy cùng `base_move_speed` trên DB vẫn có thể khác hiển thị nếu local/remote/enemy dùng policy khác.

Cách sửa đề xuất:

- Đổi config thành `LocalEntityActionConfig` hoặc `WorldMovementPresentationConfig`.
- Tách rõ:
  - `serverUnitsPerSecond`
  - `serverToWorldScale`
  - `worldUnitsPerSecond`
  - `interpolationPolicy`
- Không dùng `baseMoveSpeed` fallback dạng mơ hồ; fallback cũng phải đi qua conversion.

### Asset Loading

Vấn đề: Addressables có trong package nhưng runtime chưa dùng; vẫn có fallback `Resources.Load`.

Bằng chứng:

- `Packages/manifest.json` có `com.unity.addressables`.
- Search runtime chỉ thấy `Resources.Load` ở `WorldPortalPresenter.Visuals.cs`.
- Map/enemy/skill đang dùng ScriptableObject catalog.

Rủi ro: Hardcode path resource dễ mất asset ở build, khó hot update/cấu hình admin về sau.

Cách sửa đề xuất:

- Ngắn hạn: portal visual prefab phải kéo ref hoặc qua `ClientMapCatalog`/`WorldPresentationCatalog`.
- Dài hạn: nếu cần hot content, chọn một hướng: Addressables hoặc catalog prefab reference; không trộn ngẫu nhiên.

### Performance

Vấn đề: Có nhiều `Update` polling hợp lý cho realtime, nhưng UI và targeting cũng polling khá nhiều.

Bằng chứng:

- Runtime có nhiều `Update`: bootstrap tick, pool trim, local action, projectile, reward bobbing, movement view, target selection, map/portal/debug/UI controllers.
- `WorldCraftingPanelController.Update()` refresh panel khi visible.
- `WorldInventoryPanelController` refresh inventory theo frame khi active.
- `WorldMenuController.Update()` register/bind/refresh tab content.
- `WorldCombatHudController.Update()` refresh HUD/cooldown mỗi frame.

Rủi ro: Trên PC hiện tại chưa nghiêm trọng, nhưng khi UI nhiều item/recipe/skill và packet nhiều, GC + Canvas rebuild có thể gây giật.

Cách sửa đề xuất:

- Dùng dirty flag/event-driven cho panel data.
- Realtime HUD cooldown/cast bar có thể Update, nhưng button list/skill data không cần rebuild mỗi frame.
- Debug controller phải có build flag hoặc scene debug group tắt ở production.

### Memory/GC

Vấn đề: Có alloc lặp trong UI/network/targeting.

Bằng chứng:

- `WorldAutoTargetSelectionController` gọi `WorldTargetableRegistry.GetSnapshot()` tạo array.
- `WorldCraftingPanelController` dùng LINQ `.Where().ToArray()`, sorting/projection trong refresh.
- `InventoryItemGridView.BuildSnapshot()` tạo `string[]` và `string.Join`.
- `ClientConnectionService` copy packet payload; `PacketSerializer` copy `ToArray`.
- `ClientPresentationReplicationService.Tick()` tạo `List<PresentationExecutionKey>` khi có pending releases.

Rủi ro: GC spike khi UI mở lâu, nhiều target, nhiều packet.

Cách sửa đề xuất:

- Phase 1 chỉ giảm chỗ nóng dễ sửa: registry snapshot, packet logging, UI refresh dirty flag.
- Phase 2 tối ưu list/pool buffer ở panel lớn.

### Event/Coroutine/Tween Cleanup

Vấn đề: MonoBehaviour UI/presenter phần lớn có unsubscribe, nhưng service cấp application subscribe packet/state không dispose. Coroutine/tween có cleanup không đồng đều.

Bằng chứng:

- `ClientWorldService`, `ClientCharacterService`, `ClientAlchemyService`, `ClientSkillPresentationService`, `ClientPresentationReplicationService` subscribe trong constructor, không giữ `IDisposable`.
- `GroundRewardPresenter` dùng pickup/spawn coroutine, stop spawn khi cần nhưng destroy phụ thuộc Unity cleanup.
- `CombatValuePopupView` dùng DOTween và release qua pool; cần tiếp tục giữ pattern cleanup khi pooling.

Rủi ro: Nếu sau này logout/login/reinitialize runtime hoặc domain reload khác thường, subscription cũ có thể giữ state cũ. Pooled object nếu tween/coroutine không reset sạch sẽ tạo bug khó trace.

Cách sửa đề xuất:

- `ClientRuntime.Reset/Dispose` hoặc `IClientRuntimeService.Dispose`.
- Service giữ packet subscription disposable.
- Pooled views implement `IPooledLifecycle.OnSpawned/OnReleased`.

### Maintainability

Vấn đề: Nhiều class quá dài và nhiều trách nhiệm.

Bằng chứng line count:

- `WorldCraftingPanelController.cs`: 1584 dòng.
- `WorldTravelDebugController.cs`: 818 dòng.
- `LocalCharacterActionController.cs`: 658 dòng.
- `GroundRewardPresenter.cs`: 554 dòng.
- `ClientWorldState.cs`: 537 dòng.
- `ClientAlchemyService.cs`: 529 dòng.
- `SkillWorldPresentationCatalog.cs`: 517 dòng.
- `WorldCombatHudController.cs`: 456 dòng.
- `WorldInventoryPanelController.ItemActions.cs`: 443 dòng.

Rủi ro: Code chạy được nhưng developer phải nhớ nhiều ngữ cảnh khi sửa. Đây là nguồn chính của "vibe coding" mà user đang lo.

Cách sửa đề xuất:

- Phase 1: chỉ cắt những phần gây bug/vi phạm rule.
- Phase 2: mỗi class >400 dòng phải có plan tách theo trách nhiệm, không tách cơ học theo file partial nếu trách nhiệm vẫn lẫn.

### Extensibility

Vấn đề: Thêm tính năng mới sẽ làm nhanh nếu copy pattern hiện tại, nhưng khó giữ sạch.

Bằng chứng:

- Shop/quest/dialog có thể gọi `ClientRuntime` và `WorldModalUIManager.Instance` trực tiếp giống inventory/crafting hiện tại.
- Boss có thể copy `EnemyPresenter` và thêm nhánh riêng.
- Dungeon có thể thêm nhiều ref vào `WorldSceneController`.

Rủi ro: Dự án phình theo chiều ngang, mỗi feature có panel/controller/service riêng nhưng thiếu base pattern chung.

Cách sửa đề xuất:

- Trước khi thêm feature lớn tiếp theo, chốt pattern:
  - UI panel lifecycle.
  - Runtime bind owner.
  - Entity presentation contract.
  - Request/response tracker.
  - Catalog/config validator.

### Debuggability

Vấn đề: Có nhiều log/debug tools nhưng chưa có trace thống nhất theo packet/action.

Bằng chứng:

- `WorldTravelDebugController` rất lớn và gắn nhiều debug phím.
- `ClientConnectionService` log mọi packet nhưng thiếu payload/correlation/action context.
- `GameShared.Diagnostics.PacketIncidentCapture` tồn tại nhưng client chưa dùng rõ trong deserialize/handler lỗi.

Rủi ro: Log nhiều nhưng khi bug realtime xảy ra vẫn khó replay đúng chuỗi event.

Cách sửa đề xuất:

- Thêm `ClientRuntimeTrace` dạng ring buffer:
  - inbound packet type/time/size
  - outbound packet type/time/size
  - selected target changes
  - movement correction
  - UI action command
- Khi lỗi packet/UI action, dump 30 event gần nhất.

## 5. Duplication & Coupling Map

### UI Duplicate

Đang nằm ở:

- `WorldMenuController`, `WorldInventoryPanelController`, `WorldSkillPanelController`, `WorldPotentialPanelController`, `WorldCraftingPanelController`, `WorldCultivationPanelController`.
- Popup/tooltip gọi qua `WorldModalUIManager.Instance` từ nhiều view: inventory slot, skill slot, crafting view, common drop zone.

Nên gom về:

- `WorldPanelLifecycle`
- `WorldModalCoordinator`
- `InventoryActionCoordinator`
- `CraftingPanelPresenter`

Refactor:

- Giữ view hiện tại.
- Controller tạo ViewState và bind event.
- Chuyển item/craft action ra presenter/coordinator không phụ thuộc MonoBehaviour.

### Character Action Duplicate

Đang nằm ở:

- Local movement: `LocalCharacterActionController`.
- Remote/enemy movement: `WorldEntityMovementView`.
- Remote animation/facing: `RemoteCharacterPresenter`.
- Enemy life/move/targetable: `EnemyPresenter`.
- Skill animation/facing/FX: `CharacterSkillPresenter`.

Nên gom về:

- `WorldEntityMovementView` giữ vai trò movement view chung.
- `CharacterAnimationDriver`
- `CharacterFacingDriver`
- `WorldEntityPresentationBinder`.

Refactor:

- Phase 1 giữ controller hiện có nhưng bỏ auto-wire.
- Phase 2 tạo driver dùng chung, rồi migrate remote/enemy trước, local sau.

### Network Handler Duplicate

Đang nằm ở:

- Các service `Client*Service` đều tự subscribe packet, tự tạo `TaskCompletionSource`, tự complete on disconnect.

Nên gom về:

- `ClientRequestTracker`
- `PacketSubscriptionBag`

Refactor:

- Tạo helper generic timeout/cancel.
- Migrate `ClientWorldTravelService` trước vì đơn giản.
- Sau đó migrate alchemy/inventory/character/skill.

### Config/Data Duplicate

Đang nằm ở:

- `LocalCharacterActionConfig`
- `WorldLocalMovementSyncConfig`
- `WorldEntityMovementView` magic interpolation constants.
- Catalogs riêng cho map/enemy/skill/UI.

Nên gom về:

- `WorldMovementPresentationConfig`
- `ClientPresentationCatalogRoot`

Refactor:

- Không nhập tất cả config vào một God asset.
- Tạo root asset chỉ tham chiếu các config con để scene kéo một ref.

### Animation/VFX Trigger Duplicate

Đang nằm ở:

- `LocalCharacterActionController` play movement states.
- `RemoteCharacterPresenter` set `MoveSpeed`.
- `CharacterSkillPresenter` play skill states.
- Enemy chưa có driver tương đương.

Nên gom về:

- `CharacterAnimationDriver`
- `SkillPresentationDriver`

Refactor:

- Common animator param hashes.
- Editor validator cho skill state names.

## 6. Refactor Roadmap

### Phase 1: Quick Wins

Mục tiêu: giảm rủi ro immediate, không đập kiến trúc.

1. Xóa hoặc khóa toàn bộ auto-wire trái rule client:
   - `LocalCharacterActionController.AutoWireMissingReferences`
   - `WorldLocalPlayerPresenter` runtime `AddComponent<LocalCharacterActionController>` và `AddComponent<WorldTargetable>`
   - `CharacterSkillPresenter.AutoWireReferences` `AddComponent<CharacterPresentationSockets>`
   - `WorldTargetable` auto-create collider
   - `GroundRewardPresenter` auto-create visual hierarchy/targetable nếu không phải runtime-owned object có chủ đích
   - `WorldPortalPresenter` auto-add `WorldTargetable` cho label nếu chuyển được sang prefab binding
2. Chuẩn hóa movement config:
   - `LocalEntityActionConfig` hoặc `WorldMovementPresentationConfig`
   - mọi speed lấy từ server units/sec và convert qua một function.
3. Thêm packet deserialize guard và giảm packet info log mặc định.
4. Thêm timeout helper cho `ClientWorldTravelService` trước.
5. Đổi các UI polling dễ nhất sang dirty flag:
   - `WorldMenuController`
   - `WorldInventoryPanelController`
   - phần data list của `WorldCombatHudController`
6. Thêm PlayMode/EditMode test nhỏ:
   - server units -> world units conversion.
   - `WorldEntityMovementView` đi từ A tới B đúng duration.
   - `ClientRequestTracker` timeout/disconnect.

### Phase 2: UI & Character Cleanup

Mục tiêu: giảm chồng chéo UI/action và chuẩn hóa entity presentation.

1. Tách `WorldCraftingPanelController` thành:
   - `CraftingPanelPresenter`
   - `CraftingInventoryProjection`
   - `CraftingPracticeSessionViewModel`
   - `CraftingPopupCoordinator`
2. Tách `WorldInventoryPanelController.ItemActions` thành `InventoryActionCoordinator`.
3. Đổi `ViewModelBase` thành `UIViewBase` hoặc tạo base panel lifecycle mới.
4. Tạo `CharacterAnimationDriver` dùng chung local/remote/enemy.
5. Tạo `WorldEntityPresentationBinder` cho targetable/movement/skill/death.

### Phase 3: Client Architecture Stabilization

Mục tiêu: giảm phụ thuộc singleton/global.

1. Tạo `WorldSceneCompositionRoot` chia sub-root.
2. Giới hạn `ClientRuntime` vào application/service layer; view không gọi thẳng nếu không cần.
3. Thêm `PacketSubscriptionBag` và `ClientRuntime.Dispose/Reset`.
4. Thêm asmdef theo layer.
5. Chuẩn hóa asset/config loading: catalog root, bỏ `Resources.Load`.
6. Thêm runtime trace ring buffer cho packet/action/UI.

### Phase 4: Long-term Scalability

Mục tiêu: chuẩn bị cho MMO nhiều hệ thống.

1. Entity presentation plugin-like theo kind: Player, Enemy, Boss, NPC, Reward, Portal.
2. Addressables hoặc catalog asset pipeline rõ cho map/enemy/VFX/UI icon.
3. Pooling policy cho enemy/remote/reward/portal nếu AOI churn tăng.
4. Feature module template cho quest/dungeon/boss/shop/dialog:
   - Application state/service.
   - Network packet handler.
   - UI presenter/view.
   - Catalog/config.
   - Tests.
5. Packet replay/debug tool để reproduce bug từ log.

## 7. Suggested Target Architecture

Folder đề xuất:

```text
Assets/Game/Runtime/
  Core/
    Runtime/
    Logging/
    Diagnostics/
  Network/
    Transport/
    Packets/
    Requests/
  Infrastructure/
    SceneLoading/
    Pooling/
    Config/
    Assets/
  Features/
    Character/
      Application/
      Presentation/
      UI/
    World/
      Application/
      Presentation/
      UI/
    Combat/
      Application/
      Presentation/
      UI/
    Inventory/
      Application/
      UI/
    Crafting/
      Application/
      UI/
  UI/
    Common/
    Panels/
    Modals/
  Content/
    Catalogs/
    Configs/
```

Assembly definition đề xuất:

- `Game.Client.Core`
- `Game.Client.Network`
- `Game.Client.Infrastructure`
- `Game.Client.Features.Application`
- `Game.Client.Features.Presentation`
- `Game.Client.UI`
- `Game.Client.Tests.EditMode`
- `Game.Client.Tests.PlayMode`

UI base pattern:

```csharp
public interface IPanelView<in TState>
{
    void Bind(TState state);
    void Show();
    void Hide(bool force);
}

public interface IPanelController
{
    void Open(object context);
    void Close();
    void Refresh(bool force);
}
```

Panel lifecycle:

- `Awake`: validate serialized refs only.
- `OnEnable`: subscribe view events.
- `OnDisable`: unsubscribe view events, hide transient popup/tween.
- `Open(context)`: load/prepare data.
- `Refresh(force)`: bind state.
- `Close`: stop action-specific UI and hide.

Character base/action/state pattern:

- `WorldEntityPresenter`: id/target/death/root binding.
- `WorldEntityMovementView`: only move command interpolation.
- `CharacterAnimationDriver`: `MoveSpeed`, action state play, common hash contract.
- `CharacterSkillPresenter`: only skill presentation; no ref discovery/add component.
- `LocalCharacterActionController`: coordinator for local input + motor + correction.

Network handler pattern:

- Packet handler updates application state only.
- UI calls application facade, not packet directly.
- Request/response uses `ClientRequestTracker`.
- Every request has timeout/cancel/disconnect behavior.

Event bus pattern:

- Avoid global event bus for everything.
- Use typed state events for gameplay.
- Add trace ring buffer for debug.
- Subscriptions stored in `SubscriptionBag`.

Config loading pattern:

- Scene pulls one `ClientPresentationConfigRoot`.
- Root references movement config, catalogs, UI config, debug config.
- No hardcoded `Resources.Load` path in runtime.

Pooling pattern:

- `ClientPoolService` remains, but pooled prefab implements:
  - `IPooledSpawnHandler`
  - `IPooledReleaseHandler`
- Use pool first for FX/damage popup/projectile.
- Add entity/reward pooling only after AOI churn proves necessary.

Debug tools cần có:

- Packet/action trace overlay.
- Target registry inspector.
- Movement sync inspector: server pos, world pos, speed, interpolation, correction.
- UI panel lifecycle log with panel id/context.

## 8. Final Verdict

Client hiện tại **không phải codebase hỏng**, nhưng chưa ổn để phát triển MMO lâu dài nếu tiếp tục mở rộng theo cách hiện tại.

Điểm tốt:

- Folder theo feature có nền.
- Shared packet/model giúp client/server thống nhất contract.
- World presenter đã bắt đầu tách local/remote/enemy/reward/portal.
- Pool service và catalog ScriptableObject là hướng đúng.

Điểm nguy hiểm:

- Auto-wire/runtime fallback còn nhiều, trái rule client và che thiếu ref prefab/scene.
- UI controller đang là nơi phình nhanh nhất.
- Movement/presentation chưa có policy chung đủ chặt.
- Network request/response thiếu timeout/correlation.
- Singleton/global call khiến bug khó trace.

Không nên refactor lớn ngay. Nên bắt đầu bằng Phase 1 để khóa nền: ref rõ ràng, movement config chung, packet guard, request timeout, UI dirty refresh. Sau đó mới tách dần crafting/inventory/character.

Ưu tiên sửa đầu tiên: **xóa auto-wire vi phạm rule client và chuẩn hóa movement presentation config**, vì đây là phần đang ảnh hưởng trực tiếp tới bug enemy/player movement và chất lượng prefab/scene.

## 9. Phase 1 Implementation Checklist

Mục tiêu Phase 1 là dọn phần rủi ro cao nhưng ít đụng kiến trúc lớn.

Làm theo thứ tự:

1. Lập danh sách auto-wire/runtime add còn tồn tại trong client.
2. Với mỗi chỗ, phân loại:
   - Prefab-owned ref: bắt buộc kéo trong prefab.
   - Scene-owned ref: bắt buộc kéo trong scene/composition root.
   - Runtime-owned binding: owner controller bind rõ qua method, không gọi `Find`/`AddComponent` ngầm.
3. Sửa local player prefab để có đủ `LocalCharacterActionController`, `WorldTargetable`, `CharacterSkillPresenter`, sockets/collider cần thiết.
4. Sửa `CharacterSkillPresenter` để không tự add `CharacterPresentationSockets`.
5. Sửa `WorldTargetable` để tắt auto-create collider mặc định; log lỗi thiếu collider thay vì tạo ngầm.
6. Tạo hoặc đổi `LocalCharacterActionConfig` thành movement config chung cho local/remote/enemy presentation.
7. Đưa speed conversion server units -> world units vào một utility/config chung.
8. Thêm deserialize guard trong `ClientConnectionService.HandlePayloadReceived`.
9. Tắt packet info log mặc định hoặc chỉ log khi verbose.
10. Tạo `ClientRequestTracker` nhỏ và migrate `ClientWorldTravelService`.
11. Đổi refresh của `WorldMenuController`/`WorldInventoryPanelController` sang dirty flag ở mức tối thiểu.
12. Thêm test nhỏ cho movement conversion/request timeout.

Không làm trong Phase 1:

- Không tách lớn `WorldCraftingPanelController`.
- Không thêm asmdef ngay nếu chưa dọn dependency.
- Không đổi toàn bộ packet serializer.
- Không thay toàn bộ UI framework.
- Không chuyển sang Addressables toàn bộ asset.

Acceptance criteria:

- Không còn runtime `AddComponent`/`transform.Find` trong local player/enemy core presentation, trừ chỗ được document là runtime-owned object.
- Prefab/scene thiếu ref sẽ log lỗi rõ và fail visible, không tự sửa ngầm.
- Player và enemy cùng server `base_move_speed` dùng chung conversion policy.
- Packet deserialize lỗi không làm văng update/network loop.
- Travel request timeout hoặc disconnect trả result thất bại, không treo task.
- UI inventory/menu không rebuild toàn bộ mỗi frame khi state không đổi.

## 10. Progress Tracking

| Phase | Status | Verified By User | Commit | Notes |
|---|---|---|---|---|
| Phase 1: Quick Wins | Not Started | No | - | Ưu tiên no auto-wire, movement config chung, packet guard, request timeout, UI dirty refresh |
| Phase 2: UI & Character Cleanup | Not Started | No | - | Chỉ làm sau khi Phase 1 chạy ổn |
| Phase 3: Client Architecture Stabilization | Not Started | No | - | Chỉ làm sau khi UI/character core đã rõ boundary |
| Phase 4: Long-term Scalability | Not Started | No | - | Chuẩn bị cho quest/dungeon/boss/shop/dialog/AOI content |

## 11. Session Handoff Rule

Khi một session sau bắt đầu làm theo report này, trước tiên phải đọc mục `Progress Tracking` để biết phase nào đã xong và phase tiếp theo là gì.

Khi hoàn thành một phase, Codex phải tự cập nhật file này, không chờ user tự đánh dấu:

1. Đổi `Status` của phase vừa làm từ `Not Started` hoặc `In Progress` sang `Completed`.
2. Nếu user đã test và nói ổn, đổi `Verified By User` sang `Yes`.
3. Ghi commit hash vào cột `Commit` nếu code đã được commit.
4. Ghi note ngắn ở cột `Notes`: đã sửa gì chính, còn rủi ro nào, hoặc phase sau cần chú ý gì.
5. Nếu phase mới làm một phần, để `Status` là `In Progress` và ghi rõ phần đã xong/phần còn lại trong `Notes`.

Quy ước status:

- `Not Started`: chưa bắt đầu phase.
- `In Progress`: đã sửa một phần nhưng chưa đủ acceptance criteria.
- `Completed`: đã hoàn thành acceptance criteria của phase.
- `Blocked`: đang bị chặn bởi lỗi, thiếu dữ liệu, thiếu quyết định thiết kế, hoặc cần user xác nhận.

Quy tắc quan trọng: session sau không được đoán phase hiện tại chỉ dựa vào git diff. Phải ưu tiên đọc `Progress Tracking`, sau đó mới kiểm tra git log/diff/code để xác nhận trạng thái thực tế.
