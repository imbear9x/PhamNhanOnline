# AGENTS.md

## Mục đích

File này là working guide tổng hợp cho repo `PhamNhanOnline`, dùng để giúp agent hoặc developer mới vào phiên làm việc có thể:

- hiểu nhanh cấu trúc toàn repo
- biết source of truth của từng lớp dữ liệu
- tránh phá vỡ ranh giới giữa `GameServer`, `GameShared`, `ClientUnity`, `CientTest`
- làm đúng các quy ước đã chốt mà không phải lần mò lại qua nhiều session

Lưu ý:

- file này đang nằm trong `docs/`, nên về scope kỹ thuật của AGENTS thì nó chỉ áp dụng trực tiếp cho cây `docs/`
- nếu sau này muốn một AGENTS có hiệu lực repo-wide cho tooling/agent, cần đặt thêm một `AGENTS.md` ở root repo
- dù vậy, nội dung dưới đây được viết như repo guide chuẩn cho toàn dự án

## Second Brain Scope

Within `docs/`, the second-brain layer is the canonical AI-readable memory structure.

- Use the new topic folders (`systems/`, `combat/`, `skills/`, `data-design/`, `decisions/`, `change-notes/`, `implementation/`, `qa/`, `conflicts/`, `agent-workflows/`, `templates/`, `index/`) for durable project knowledge.
- Preserve legacy folders and docs until they are deliberately reconciled.
- Prefer status-based lifecycle (`draft`, `reviewed`, `verified`, `deprecated`) instead of destructive cleanup.
- If a doc claim cannot be grounded in code/runtime, keep it below `verified` or file a conflict report.

## Tổng quan repo

Các khối chính:

- `GameServer/`
  - server authoritative
  - chứa networking, runtime, world systems, repositories, entities, services
- `GameShared/`
  - shared contracts giữa server và client
  - chứa packets, messages, shared models, enums, logging abstractions
- `ClientUnity/PhamNhanOnline/`
  - Unity client chính
  - gameplay runtime, presentation, HUD, scene setup, world systems
- `CientTest/TestClient/`
  - client test dạng console để test networking / reconnect / packet flow
- `CientTest/AdminDesignerTool/`
  - tool local nối DB trực tiếp để game design/config authoring
- `CientTest/SkillWorldCatalogSyncTool/`
  - console tool đọc DB và trả payload sync cho `SkillWorldPresentationCatalog`
- `database/`
  - schema SQL gốc và các migration one-off
- `docs/`
  - tài liệu kỹ thuật, working context, scene setup, gameplay docs
- `scripts/`
  - script setup, sync, verify build
- `PacketGenerator/`
  - tooling/generator cho packet/shared artifacts nếu cần

## Nguyên tắc kiến trúc bắt buộc

### 1. Server là authoritative

- gameplay state, validation, combat, movement rules, portal validation phải chốt ở server
- client không tự suy gameplay rồi coi như đúng
- client chỉ giữ đủ state để render, dự đoán nhẹ nếu cần, và gửi intent/input

### 2. `GameShared` là cầu nối duy nhất giữa server và client

- client chỉ được phụ thuộc `GameShared`
- client không được phụ thuộc `GameServer`
- không copy tay source packet/model từ server sang Unity
- khi đổi contract chung trong `GameShared`, phải sync lại Unity plugin

Lệnh:

```powershell
powershell -File .\scripts\sync-gameshared-to-unity.ps1
```

### 3. Ưu tiên đúng ranh giới domain

- `GameServer`: domain logic, repositories, authoritative runtime
- `GameShared`: contract, packet, model dùng chung
- `ClientUnity`: presentation, input, UI, scene orchestration
- `CientTest`: tooling/test harness, không phải nơi đặt production runtime logic

### 4. Performance là constraint thật

- không chốt theo kiểu “chạy được là được”
- mọi thay đổi ở client hoặc server đều phải cân nhắc allocation, polling, loop frequency, query frequency, snapshot cost, DI graph cost

## Cấu trúc làm việc theo module

## `GameServer`

### Trách nhiệm

- load config / DB
- quản lý networking và game loop
- runtime authoritative cho world, combat, inventory, time, services
- compile gameplay description từ template

### Quy tắc khi sửa

- trước khi sửa `GameServer`, luôn phân tích phạm vi ảnh hưởng
- không chạm `GameShared` chỉ vì server đang tiện dùng
- thay đổi schema phải kéo theo entity/repository/DTO liên quan
- nếu gameplay text thay đổi, ưu tiên sửa pipeline server compile thay vì để client tự lắp description

### Transaction boundary

Rule transaction phía server đã chốt:

- transaction owner nằm ở tầng orchestration cấp cao:
  - thường là packet handler
  - hoặc application/orchestration service nếu flow không đi trực tiếp từ handler
- service cấp dưới không được mặc định assume mình là transaction owner
- mỗi flow business atomic chỉ nên có một transaction owner
- packet/notifier không được gửi trước khi write path chính đã commit xong
- method nào có side effect ghi DB phải lộ rõ ở tên/layer để dễ trace

Compatibility rule hiện tại:

- một số service cũ vẫn còn khả năng standalone và tự mở transaction
- tạm thời được phép giữ backward compatibility đó
- nhưng bắt buộc phải chịu được ambient transaction đã tồn tại bên ngoài

Không làm:

- không để handler mở transaction ngoài rồi service con lại mở transaction lồng trên cùng `GameDb`
- không để mỗi feature tự nghĩ ra transaction rule riêng
- không gửi packet changed trước commit rồi mới hy vọng DB thành công sau

Khi review flow ghi DB, luôn tự hỏi:

1. transaction owner là ai
2. có transaction lồng không
3. có write nào có thể partial khi lỗi giữa đường không
4. packet/result có đang đi trước commit không
5. flow này có đang tạo thêm ngoại lệ mới không

### DI và startup

Server dùng `ServiceCollection` trong `GameServer/Program.cs`.

Đã từng có vòng DI kiểu:

- `NetworkServer -> CharacterCombatDeathRecoveryService -> WorldInterestService -> INetworkSender -> NetworkServer`

Rule bắt buộc:

- không inject trực tiếp `WorldInterestService`, `CharacterRuntimeNotifier`, hoặc bất kỳ service nào phụ thuộc `INetworkSender/NetworkServer`
- vào các service mà chính `NetworkServer` cần để khởi tạo
- nếu thật sự cần, resolve lười trong method runtime bằng `IServiceScopeFactory`

### Command mode hiện có

`GameServer/Program.cs` đang có command mode:

- `--command=sync-game-time-config`
- `--command=preview-random-table`

Khi thêm command mới:

- giữ kiểu command rõ mục đích, không nhồi logic tạm bợ vào `Main`
- log đầu vào/đầu ra đủ để dùng từ CLI

## `GameShared`

### Trách nhiệm

- packet/message/model dùng chung giữa server và client
- enum, serializer contracts, logging abstractions

### Quy tắc khi sửa

- mọi thay đổi ở đây đều phải được xem là breaking risk cho cả server lẫn Unity client
- sau khi sửa phải sync sang Unity
- không đặt logic server-only hoặc Unity-only vào đây

### Build và sync

`GameShared` là source of truth cho shared assembly.

Khi đổi:

- packets
- shared models
- `MessageCode`
- serializer contracts

thì phải chạy:

```powershell
powershell -File .\scripts\sync-gameshared-to-unity.ps1
```

## `ClientUnity/PhamNhanOnline`

### Trách nhiệm

- gameplay client runtime
- world presentation
- HUD / UI / input
- state đọc từ shared model + authoritative packets

### Ownership đồng bộ state client

Rule sync state client đã chốt:

- `EnterWorld` luôn bootstrap snapshot cho các subsystem panel cần
- sau bootstrap, local state chỉ đổi theo hai nguồn:
  - packet push từ server
  - action result trả đủ data để update local state chắc chắn
- nếu action result không đủ data:
  - service xử lý action phải chủ động reload đúng subsystem của mình
  - không đẩy fallback reload xuống panel/controller
- panel/controller không tự fetch data
- reconnect thành công thì chạy lại bootstrap snapshot giống `EnterWorld`

Ownership:

- `ClientCharacterService`
  - nhận `EnterWorldResult`
  - apply character state
  - kick bootstrap load cho `inventory`, `martial arts`, `skills`
- từng feature service
  - là owner local state của subsystem
  - nghe packet/result packet
  - tự quyết định có cần reload subsystem của mình hay không
- panel/controller
  - không được là owner của fetch/reload
  - chỉ đọc state, nghe event và render UI

Không làm:

- không thêm polling fallback trong panel để tự reload missing data
- không để mỗi panel tự nghĩ ra một rule sync riêng
- không đẩy cơ chế resync phức tạp nếu chưa có bug drift thật

### Quy tắc chung

- ưu tiên viết controller/logic trước để user tự setup hierarchy, prefab, scene qua Inspector
- không tự sinh UI hierarchy bằng runtime code nếu user chưa yêu cầu rõ
- mặc định dùng `UIButtonView` thay cho `UnityEngine.UI.Button`
- chỉ dùng Unity Button khi có lý do kỹ thuật rõ ràng hoặc user yêu cầu

### Quy tắc ref bắt buộc vs ref tùy chọn

Phải phân biệt rõ:

- ref lõi / bắt buộc:
  - ví dụ `WorldSceneController`, `WorldMapPresenter`, `WorldTargetActionController`, `WorldLocalPlayerPresenter`, panel/controller chính
- ref phụ / optional:
  - ví dụ text phụ, badge, status text, root hiển thị bổ sung

Rule:

- ref lõi thiếu thì không được im lặng `return`
- phải `ClientLog.Error` sớm để lộ lỗi setup scene/prefab
- chỉ ref phụ mới được phép “có thì hiển thị, không có thì thôi”

### World scene readiness

Mọi component world phụ thuộc readiness phải đi qua:

- `WorldSceneReadinessService`
- `WorldSceneBehaviour`

Không làm kiểu:

- đoán readiness bằng `null` checks rải khắp nơi
- tự bind event readiness lặp lại nếu đã có base class

Phải:

- khai báo dependency trong `ConfigureReadyWaits()`
- reset state trong `OnWorldLoadCycleStarted(...)`
- dùng `IsReady(...)` / `AreReady(...)` cho runtime gate

Tham khảo:

- `docs/client-unity/world-scene-readiness.md`
- `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/WorldSceneBehaviour.cs`

### Presentation replication

Không làm generic sync raw component kiểu “gắn script lên rồi sync mọi field”.

Hướng đúng:

- server authoritative cho gameplay
- semantic packet/state cho gameplay quan trọng
- client có lớp presentation replication dùng chung để chuẩn hóa event/state presentation

Foundation hiện tại:

- `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/PresentationReplication/Application/ClientPresentationReplicationService.cs`

### Scene-level decisions đã chốt

- Unity scene nên giữ ít:
  - `Bootstrap`
  - `Login`
  - `World`
- `MapTemplate` không đồng nghĩa với Unity scene
- `zone / khu / instance` là runtime state, không phải Unity scene
- portal flow đi theo `portal -> target spawn point`
- server là nơi validate cuối cùng việc dùng portal
- map root hiện tại không nên pooling; dùng `Destroy/Instantiate` khi đổi map
- local player và remote player không dùng chung movement controller:
  - local dùng `LocalCharacterActionController`
  - remote đi qua presenter riêng
- local player tự simulate movement trên client
- server position không được kéo ngược local player mỗi tick
- sync local -> server phải đi theo policy/config, không gửi mỗi frame
- remote player không được tự chạy input/physics local
- observer visibility phase hiện tại tạm thời là:
  - cùng `MapInstance` thì thấy nhau hết
  - đây chưa phải interest management cuối cùng

### World targeting rules đã chốt

Phần chọn target trong world đã được tách:

- click target
- auto target

Rule behavior hiện tại:

- click target có quyền cao hơn auto target
- nếu player click vào target hoặc click ra vùng trống:
  - auto target bị suppress
  - target có thể thành rỗng
- chỉ khi player có manual move input thì mới nhả suppress để auto target hoạt động lại
- các luồng move-to-target do action/cast skill không được coi là reset auto target
- nếu không bị manual suppress, auto target phải chọn target gần nhất hợp lệ thay vì giữ cứng target cũ chỉ vì nó còn trong vùng

Khi sửa các file như:

- `WorldAutoTargetSelectionController`
- `WorldClickTargetSelectionController`
- `LocalCharacterActionController`
- `ClientTargetState`

phải giữ đúng contract này, tránh để click logic và auto logic chồng chéo lại.

### Skill world/UI presentation rules đã chốt

Catalog presentation của skill hiện tại đã được gộp về `SkillWorldPresentationCatalog`.

Rule hiện tại:

- `skill_group` là tầng config chuẩn
- resolve ưu tiên `skill_group preset` trước
- chỉ khi có `skill override` đúng skill cụ thể mới override lên group preset
- UI icon cũng nằm chung trong catalog này

Các điểm cần nhớ:

- `skillGroupName` chỉ là metadata để đọc trong Inspector
- không dùng `skillGroupName` làm key runtime
- DB là source of truth cho `skill_group_code` và danh sách skill override hợp lệ
- gameplay combat vẫn server-authoritative; không chôn logic FX/animation/HUD vào `ClientCombatService`
- `SkillExecutionId` là identity runtime của một lần cast; không dùng `SkillId` để phân biệt mọi execution
- `ClientSkillPresentationService` điều phối phase/timeline
- `CharacterSkillPresenter` chỉ nên chạm `Animator`, socket, FX spawn/cleanup
- không hardcode animation/VFX trong HUD hay `WorldTargetActionController`

Rule combat skill phase hiện tại:

- server dùng pipeline data-driven qua `skills` + `skill_effects`
- combat status runtime nằm trong RAM, không persist DB
- phase target support hiện tại mới support chắc:
  - `Self`
  - `SingleEnemy`
- target scope support chắc:
  - `Self`
  - `Primary`
- trigger timing support chắc:
  - `OnCastRelease`
  - `OnHit`
- các loại `Ally`, `GroundArea`, `EnemyArea`, `OnExpire` vẫn là phase sau hoặc đã chặn rõ bằng code

Tool hỗ trợ:

- custom inspector/menu sync:
  - `ClientUnity/PhamNhanOnline/Assets/Game/Editor/SkillWorldPresentationCatalogEditor.cs`
- console sync tool:
  - `CientTest/SkillWorldCatalogSyncTool/`

Rule sync:

- bấm sync sẽ thêm group mới từ DB
- xóa group hoặc skill override không còn tồn tại trong DB
- chuẩn hóa lại key override theo DB
- metadata `skillGroupName` lấy từ skill đầu tiên gặp trong group

### UI naming rule

- khi viết acronym trong tên type/file, giữ đúng casing đã chốt
- ví dụ: `SkillUIPresentation`, không dùng `SkillUiPresentation`

## `CientTest/TestClient`

### Trách nhiệm

- test networking flow
- reconnect behavior
- replay packet/tooling đơn giản cho debug

### Quy tắc

- không đẩy production logic vào đây
- dùng như harness/debug client
- giữ code đủ độc lập để chạy riêng từ CLI hoặc VS Code

Run:

```powershell
dotnet run --project CientTest/TestClient/TestClient.csproj
```

## `CientTest/AdminDesignerTool`

### Trách nhiệm

- tool local cho game design/config authoring trực tiếp trên PostgreSQL
- edit template/config tables mà không phải viết SQL tay

### Quy tắc

- đây là tool local nối DB thật, chưa có auth/permission
- mọi thay đổi schema/table hỗ trợ trong tool phải cân nhắc tác động lên designer workflow
- enum trong tool hiện được mirror local để build độc lập với `GameServer`
- moi field DB dang luu enum/int-code ma admin phai nhap tay phai co `AdminColumnBinding` voi `enumType` hoac lookup dropdown; khong de designer nhap so tho neu co enum ro rang trong code
- moi field moi hoac enum moi dua vao Admin Tool phai co help/tooltip trong `AdminFieldHelpCatalog`, giai thich nghia tung mode/value de tranh seed sai data

### Workflow

Run:

```powershell
dotnet run --project CientTest/AdminDesignerTool/AdminDesignerTool.csproj
```

Tool sẽ tự tìm:

- `GameServer/Config/dbConfig.json`

### Khi mở rộng tool

- ưu tiên giữ metadata/help/description rõ ràng cho designer
- nếu có table/editor chuyên biệt tốt hơn grid generic thì làm workspace riêng
- không đẩy rule gameplay authoritative vào AdminTool; chỉ author data

## Tooling phụ

Ngoài `TestClient`, repo còn có vài tool phụ dùng cho debug/verify:

- `CientTest/InterestManagementVerifier/`
- `PacketGenerator/`

Rule chung:

- đây là tooling/test harness
- không để production runtime logic rơi vào đây
- nếu cần thêm verify tool mới, giữ nó độc lập, chạy được từ CLI và không kéo dependency vòng sang server/client production

## `CientTest/SkillWorldCatalogSyncTool`

### Trách nhiệm

- đọc `public.skills` từ DB
- trả payload JSON để sync `SkillWorldPresentationCatalog`

### Quy tắc

- DB là nguồn trust
- tool này chỉ phục vụ sync catalog/editor workflow
- không biến nó thành nơi giữ gameplay logic hoặc transform phức tạp không cần thiết

Run:

```powershell
dotnet run --project CientTest/SkillWorldCatalogSyncTool/SkillWorldCatalogSyncTool.csproj --no-launch-profile
```

## Database

### Source of truth

- schema gốc: `database/phamnhan_online.sql`
- DB runtime là nguồn truth cho data gameplay/config hiện hành

### Khi sửa schema

Phải cập nhật đồng bộ:

- entity liên quan
- repository liên quan
- DTO/model liên quan nếu có
- file schema gốc `database/phamnhan_online.sql`

Nếu cần migration one-off:

- tạo file SQL chạy trực tiếp trong `database/`
- format tên:

```text
migrate_YYYYMMDD_HHmm_mo_ta_ngan.sql
```

### Khi sửa data gameplay/config

- ưu tiên ghi rõ bằng migration hoặc doc
- không để knowledge chỉ tồn tại trong trí nhớ/session chat

### `game_configs`

`public.game_configs` là nơi chứa gameplay config có thể tinh chỉnh, thay cho hardcode rải trong server.

Rule hiện tại:

- key DB map sang typed snapshot trong code qua `GameConfigValues`
- server hiện load `GameConfigValues` một lần lúc startup
- sau khi sửa `public.game_configs`, phải restart server để giá trị mới có hiệu lực

Khi thêm config mới:

- thêm key trong DB
- thêm constant/property typed tương ứng
- cập nhật loader/repository/entity nếu cần
- cập nhật `docs/reference-and-specs/GAME_CONFIGS.md`

## Description template system

Rule đã chốt:

- `item`, `skill`, `martial art` dùng `description_template` ở DB
- server compile template thành `Description` cuối cùng
- client chỉ render text cuối cùng bằng TMP rich text
- client không tự tính gameplay để dựng description
- không phát minh syntax icon custom ở client nếu chưa thực sự cần; ưu tiên TMP tags gốc

Tham khảo:

- `docs/reference-and-specs/DESCRIPTION_TEMPLATE_SYSTEM.md`

## Item use flow

Rule item use phase hiện tại:

- generic `UseItemPacket(playerItemId, quantity)` chỉ dành cho item “self-contained”
- các item cần thêm context như plot, target, vị trí, combat state thì dùng packet chuyên biệt riêng
- validate ownership, inventory location, quantity, definition trước mọi side effect
- nếu fail:
  - `success = false`
  - `appliedQuantity = 0`
  - không được có side effect
- UI inventory không nên hiện nút `Use` chung cho các type vốn không đi qua generic flow, như:
  - `Material`
  - `HerbMaterial`
  - `Currency`
  - `QuestItem`

Không nên:

- không tạo một generic packet phình to để ôm mọi semantic action khác nhau

## Build, verify, tooling

### Dev setup

Yêu cầu cơ bản:

- .NET SDK `8.0.303` theo `global.json`
- Git
- VS Code
- Unity Editor phù hợp với project

Script setup:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\dev-setup.ps1
```

### Verify build nhanh

```powershell
powershell -File .\scripts\verify-solution-build.ps1
```

Script này build:

- `GameServer`
- `ClientUnity/PhamNhanOnline/Assembly-CSharp.csproj`

### Lưu ý về Unity generated project

`ClientUnity/PhamNhanOnline/Assembly-CSharp.csproj` là file generated của Unity.

Điều này có nghĩa:

- source of truth vẫn là script trong `Assets/`
- khi thêm file `.cs` mới, Unity cần regenerate project để compile list cập nhật
- nếu CLI build chưa thấy file mới, trước tiên kiểm tra việc regenerate project thay vì kết luận code sai

### Khi nào phải mở Unity / regenerate project

- vừa thêm script `.cs` mới
- vừa thêm `.meta`
- vừa đổi asmdef hoặc compile structure

Tham khảo:

- `docs/workflow-and-operations/UNITY_TOOLING_NOTES.md`

### Log và metrics server

Khi debug server, log là điểm đọc đầu tiên, không phải đoán code trước.

Rule thực dụng:

- ưu tiên xem dòng `ServerMetrics` mới nhất trong `Logs/`
- đọc theo thứ tự:
  1. `QueuedInboundPackets`, `MaxQueueDepth`
  2. `InboundPps`, `OutboundPps`, `InboundKBps`, `OutboundKBps`
  3. `WorldTickOverruns`, `AvgWorldTickMs`, `MaxWorldTickMs`
  4. `MaintenanceTickOverruns`, `AvgMaintenanceTickMs`
  5. `InboundDropped`, `InboundExceptions`
  6. `TopInboundPackets`, `TopOutboundPackets`

Dấu hiệu cần đào sâu:

- queue packet tăng qua nhiều lần log liên tiếp
- `WorldTickOverruns` tăng đều
- `MaintenanceTickOverruns` tăng đều
- `InboundDropped != 0`
- `InboundExceptions != 0`
- người chơi bắt đầu cảm nhận lag

Target hiện tại:

- `GameLoop`: khoảng `50ms` / tick (~`20 TPS`)
- `RuntimeMaintenanceService`: chu kỳ nền `50ms`
- metrics logger: `10` giây / lần

## Tài liệu

### Quy tắc viết doc

- viết bằng tiếng Việt có dấu
- nếu là roadmap/draft tương lai, phải ghi rõ chưa phải trạng thái đã implement
- khi gộp/xóa doc cũ, ưu tiên giữ phần giải thích còn đúng thay vì rút ngắn làm mất ngữ cảnh

### Phân loại docs cần hiểu đúng

Trong `docs/`, không phải file nào cũng là source of truth cho behavior hiện tại.

Nhóm tài liệu tham chiếu cho hệ đang chạy:

- `reference-and-specs/PHASE1_SYSTEM_REFERENCE.md`
- `client-unity/world-scene-readiness.md`
- `reference-and-specs/SKILL_SYSTEM_COMBAT_FLOW.md`
- `reference-and-specs/ITEM_USE_FLOW_SPEC.md`
- `reference-and-specs/DESCRIPTION_TEMPLATE_SYSTEM.md`
- `reference-and-specs/GAME_CONFIGS.md`
- `workflow-and-operations/HUONG_DAN_DOC_LOG_SERVER.md`
- `client-unity/UNITY_CLIENT_SCENE_SETUP.md`

Nhóm quy ước làm việc/tooling:

- `workflow-and-operations/WORKING_CONTEXT.md`
- `workflow-and-operations/UNITY_TOOLING_NOTES.md`
- `client-unity/client-state-sync-rules.md`
- `workflow-and-operations/server-transaction-rules.md`

Nhóm roadmap/draft tương lai, không được coi là trạng thái đã implement:

- `architecture-and-roadmap/SERVER_SCALING_ROADMAP.md`
- `architecture-and-roadmap/ENEMY_BOSS_INSTANCE_FLOW_DRAFT.md`
- `client-unity/skill-presentation/SKILL_PRESENTATION_PHASE3_ROADMAP.md`

Nhóm lịch sử/refactor:

- `architecture-and-roadmap/ARCHITECTURE_REFACTOR_20260403.md`

### Docs quan trọng nên đọc trước theo ngữ cảnh

- bắt đầu session mới:
- `docs/workflow-and-operations/WORKING_CONTEXT.md`
- nếu sửa flow client state/panel reload:
- `docs/client-unity/client-state-sync-rules.md`
- nếu sửa flow server có ghi DB:
- `docs/workflow-and-operations/server-transaction-rules.md`
- làm world scene:
- `docs/client-unity/world-scene-readiness.md`
- nếu sửa login/world/movement/observer:
- `docs/reference-and-specs/PHASE1_SYSTEM_REFERENCE.md`
- nếu sửa combat skill server:
- `docs/reference-and-specs/SKILL_SYSTEM_COMBAT_FLOW.md`
- nếu sửa skill presentation client:
- `docs/client-unity/skill-presentation/SKILL_PRESENTATION_PHASE1_PHASE2_GUIDE.md`
- nếu sửa item use/inventory action:
- `docs/reference-and-specs/ITEM_USE_FLOW_SPEC.md`
- nếu sửa/tune game config:
- `docs/reference-and-specs/GAME_CONFIGS.md`
- nếu debug server lag/metrics:
- `docs/workflow-and-operations/HUONG_DAN_DOC_LOG_SERVER.md`
- làm shared/build Unity:
- `docs/workflow-and-operations/UNITY_TOOLING_NOTES.md`
- làm gameplay description:
- `docs/reference-and-specs/DESCRIPTION_TEMPLATE_SYSTEM.md`
- làm client scene setup:
- `docs/client-unity/UNITY_CLIENT_SCENE_SETUP.md`

## Quy tắc commit / patch

- sửa đúng root cause, không vá bề mặt nếu tránh được
- không sửa bug ngoài scope chỉ vì thấy tiện
- thay đổi phải tối thiểu nhưng đủ đúng
- giữ style code nhất quán với khu vực đang sửa
- không thêm complexity nếu chưa có nhu cầu thật
- không commit `Assembly-CSharp.csproj` trừ khi có lý do rõ ràng và xác nhận đó không phải thay đổi generated rác
- khi thêm script Unity mới, nhớ chuyện regenerate project trước khi dùng `dotnet build` làm bằng chứng cuối

## Checklist trước khi kết thúc một task

- đã xác định đúng module cần sửa chưa
- có đụng `GameShared` không; nếu có thì đã sync Unity chưa
- có đụng schema/data DB không; nếu có thì đã cập nhật schema/migration/doc chưa
- có đụng flow server ghi DB không; nếu có thì transaction owner đã rõ chưa
- có đụng client panel/state flow không; nếu có thì service có đang giữ ownership fetch/reload đúng chỗ không
- với Unity world code:
  - ref lõi thiếu có log lỗi rõ chưa
  - readiness có đi qua `WorldSceneBehaviour` / `WorldSceneReadinessService` chưa
- với skill presentation:
  - có giữ rule group-first, override-after không
  - có vô tình tạo key rác lệch DB không
- với log/debug server:
  - đã soi `ServerMetrics` trước khi đoán bug hiệu năng chưa
- đã verify build hoặc command phù hợp với phạm vi thay đổi chưa

## Quy tắc khởi động session mới

1. Đọc `docs/workflow-and-operations/WORKING_CONTEXT.md`.
2. Nếu sửa flow client state hoặc flow server ghi DB, đọc thêm:
- `docs/client-unity/client-state-sync-rules.md`
- `docs/workflow-and-operations/server-transaction-rules.md`
3. Nếu làm world scene, đọc thêm `docs/client-unity/world-scene-readiness.md`.
4. Nếu có sửa `GameShared`, chạy sync sang Unity.
5. Nếu làm feature presentation mới, kiểm tra trước xem có nên đi qua presentation replication hiện có hay không.
6. Nếu làm skill presentation/world catalog, nhớ DB là source of truth và ưu tiên sync thay vì nhập tay.
