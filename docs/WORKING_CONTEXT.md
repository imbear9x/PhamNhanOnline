# Working Context

## Mục đích

File này dùng để giữ ngữ cảnh làm việc giữa nhiều session Codex.
Mỗi session mới nên đọc file này trước khi tiếp tục.
Cuối mỗi buổi có thể cập nhật thêm các quyết định mới, trạng thái hiện tại và việc tiếp theo.

## Trọng tâm hiện tại của dự án

- Tạm dừng tối ưu/scaling sâu thêm trong một thời gian.
- Ưu tiên làm Unity client để kiểm chứng flow thật với GameServer.
- Ưu tiên logic chạy đúng trước, đồ họa và UI đẹp tính sau.

## Quy tắc cộng tác

- Trước khi sửa `GameServer` hoặc `GameShared`, phải phân tích trước.
- Chỉ sửa `GameServer` hoặc `GameShared` khi user đồng ý rõ ràng.
- Có thể sửa `ClientUnity` hoặc `TestClient` để test/verify khi hợp lý.
- Commit theo nhóm việc rõ ràng, không gom lẫn nhiều mục đích khác nhau.
- Client chỉ được phụ thuộc `GameShared`, không phụ thuộc `GameServer`.
- Nếu cần đổi protocol dùng chung, ưu tiên sửa trong `GameShared`.
- Các hệ thống cần config/balance nên ưu tiên đọc từ file `config*.json` trước để test nhanh; về sau sẽ chuyển cùng schema/load path đó sang DB thay vì hardcode trong code.
- Với các gameplay hot path như farm quái, tick reward, combat hit, loot roll: phải cân nhắc performance sớm. Tránh thiết kế mỗi hit/tick lại query DB để hỏi trạng thái nếu có thể chuyển sang runtime state hoặc field persisted đọc sẵn trên snapshot/base stats.

## Các quyết định kiến trúc đã chốt

- `GetCharacterData` không còn là packet vào world.
- `GetCharacterData` chỉ dùng để query snapshot nhân vật.
- `EnterWorldPacket` mới là packet dùng để select character và vào world.
- Login flow dùng như sau:
  - login thành công
  - lấy `GetCharacterList`
  - nếu list rỗng thì hiện create character
  - nếu list có character thì lấy character đầu tiên và gọi `EnterWorldPacket`
- Sau khi `EnterWorld` thành công, client mới load `World` scene.
- `MapTemplate` không đồng nghĩa với Unity scene.
- Unity scene nên giữ ít:
  - `Bootstrap`
  - `Login`
  - `World`
- `zone/khu/instance` là runtime state, không phải Unity scene.

## Trạng thái server

- Roadmap scaling đã đi qua phase 1, 2, 3.
- Phase 4 đã có nền tảng:
  - `MapTemplate`
  - `MapDefinition`
  - `MapInstance`
  - interest management theo map/vùng/khoảng cách
- Hệ thống map hiện hỗ trợ:
  - `Player Home` là map riêng
  - map public có chia khu/zone
  - zone rỗng có thể bị hủy khỏi memory
- DB local đã đủ migration map/zone:
  - `current_zone_index`
  - `map_templates`
  - `map_template_adjacent_maps`

## Trạng thái client

- Đã tạo Unity project trong `ClientUnity/PhamNhanOnline`.
- Đã có bộ khung `Assets/Game`.
- `GameShared` đã được làm Unity-friendly.
- Có script sync DLL dùng chung:
  - `scripts/sync-gameshared-to-unity.ps1`
- Scene/client flow hiện có:
  - `Bootstrap`
  - `Login`
  - `World`
- Login UI/controller hiện đã hỗ trợ:
  - login
  - nhận biết account không có character
  - create character
  - `Open World`

## Ghi chú kỹ thuật quan trọng

- Unity đang dùng DLL sync từ `GameShared`, không copy source packet sang client.
- Sau mỗi thay đổi trong `GameShared` mà client cần dùng, phải chạy:

```powershell
powershell -File .\scripts\sync-gameshared-to-unity.ps1
```

- VS Code đã được chỉnh để:
  - ẩn file `.meta`
  - `explorer.autoReveal = true`
- Cách start server local đã verify ổn để smoke test:

```powershell
Start-Process '.\GameServer\bin\Debug\net8.0\GameServer.exe' -WorkingDirectory '.\GameServer\bin\Debug\net8.0'
```

## Account / dữ liệu test đang dùng

- Account đã tạo thành công để test:
  - username: `admin123456`
  - password: `admin@admin`
  - email đăng ký đã dùng: `admin123456@test.com`
  - character: `Admin123456`
- Account mới để test nhánh create character:
  - username: `flowcreate0316a`
  - password: `Flow@12345`
  - email đăng ký đã dùng: `flowcreate0316a@test.com`
  - character: `FlowHero0316`

## Các lưu ý hiện tại

- Build solution và sync DLL đã pass.
- Đã verify được lại runtime bằng `TestClient`.
- Hai nhánh đã pass:
  - account đã có character: `login -> get list -> EnterWorld -> MapJoined`
  - account chưa có character: `register -> login -> get list -> create character -> EnterWorld -> MapJoined`
- `admin123456` hiện nhận `EnterWorld:CharacterLifespanExpired`, nên đây cũng là account tốt để test nhánh character bị giới hạn hành động.

## Cách bắt đầu session tiếp theo

Khi bắt đầu session mới:

1. Đọc file này.
2. Đọc thêm:
   - `docs/UNITY_CLIENT_SCENE_SETUP.md`
   - `docs/UNITY_GAMESHARED_WORKFLOW.md`
3. Xác nhận mục tiêu của buổi làm việc tiếp theo.
4. Nếu có đổi `GameShared`, nhớ sync lại DLL sang Unity.

## Các bước nhiều khả năng sẽ làm tiếp

- Test lại Unity login/create character/open world flow trong Editor.
- Dựng render world/player tối thiểu trong `World` scene.
- Quan sát packet `MapJoined` và observer packets trên client.
- Chỉ quay lại sửa server nếu client expose ra vấn đề thật sự.

## Thói quen cập nhật cuối buổi

Cuối mỗi buổi, nên bổ sung:

- việc đã xong
- quyết định mới vừa chốt
- bug/blocker mới
- việc ưu tiên cho buổi tiếp theo

## Tooling note
- If apply_patch fails with a Windows sandbox refresh error, switch to shell-based file editing immediately instead of retrying multiple times.
- When referencing files in Codex responses, use clickable markdown links with absolute workspace paths in the target, for example `[CharacterCultivationService.cs](/f:/PhamNhanOnline/GameServer/Runtime/CharacterCultivationService.cs)`. Avoid plain `F:\...` paths and avoid `f:/...` targets without the leading slash because they open in the browser instead of VSCode.

## Session update 2026-03-16

- Unity client world flow da chay duoc: login -> load scene `World` -> spawn map -> spawn local player -> camera follow.
- Da them `ClientMapCatalog`, `WorldMapPresenter`, `WorldLocalPlayerPresenter`, `WorldCameraFollowController`, `ClientMapView`.
- Moi map prefab nen co `ClientMapView` + `PlayableBounds` (`BoxCollider2D` trigger) de quy doi server coords -> Unity world coords va clamp camera.
- Server/client da chot he toa do logic map, khong dung art size de lam gameplay coords. `MapCatalog.cs` da duoc doi sang scale logic (`Player Home` = `1000 x 500`, `Starter Plains` = `1000 x 1000`). DB local cung da duoc sua tuong ung.
- Local movement/action da co trong client:
  - `LocalCharacterActionController`
  - `LocalCharacterActionConfig`
  - `PlayerView`
- Flow local action hien tai:
  - di trai/phai + quay huong dung
  - bay len / hover / roi
  - dang roi thi phai cham dat moi bay lai
  - hover dung yen tren khong, bay ngang roi dung lai thi hover timer reset lai tu dau
  - attack local co ban
- Animator locomotion dang phu hop voi animator controller cua prefab free:
  - `MoveSpeed` parameter duoc set tu code cho `Idle/Run`
  - `Jump/Fly/Fall` la optional states, co state thi play, khong co thi bo qua
- Da bo tri san hook cho logic sau nay:
  - `CanUseFlight()`
  - `ActivateFlightPresentation()`
  - `DeactivateFlightPresentation()`
  - `OnFlightPresentationActivated()`
  - `OnFallingPresentationActivated()`
- Local player khong nen bi authoritative server position keo nguoc lien tuc. `WorldLocalPlayerPresenter` hien chi snap khi force hoac lech qua nguong.
- `LocalCharacterActionController` chi nen dung cho local player. Remote players ve sau nen co presenter/controller rieng, nhe hon, khong doc input va khong chay full local movement logic nay.
- Da them smoothing cho bay/roi:
  - `Rigidbody2D.Interpolate`
  - `CollisionDetectionMode2D.Continuous`
  - `VerticalVelocityChangeRate` trong `LocalCharacterActionConfig`

## Session follow-up

- Viec hop ly nhat cho buoi sau:
  - tach input source khoi `LocalCharacterActionController` de de support mobile touch / virtual joystick
  - hoac lam remote player presenter rieng
  - hoac dinh nghia policy network movement sync (client simulate, server validate khi can)
- Neu tiep tuc phan movement/network, uu tien giu rule:
  - local player tu simulate
  - server khong push vi tri local player ve client lien tuc moi tick
  - correction chi dung khi spawn/map change/teleport/lechsai lon

## Session update 2026-03-17

- Da chot semantics map/zone/cave:
  - `Home` van la private map rieng cua character, `zone_index = 0`, khong ai thay va khong ai vao duoc.
  - Chi map co `supports_cave_placement = true` moi co zone co dinh va moi co linh khi theo zone.
  - Map public thuong khong can `supports_zone_selection` nua; flag nay da bo.
- Zone/cave design hien tai:
  - `MapTemplate` chi la dinh nghia tinh.
  - Public zone co the duoc persist metadata de sau nay dung cho linh khi, dong phu, shuffle linh khi.
  - Runtime instance van co the bi huy khi trong de tranh phi memory.
- Tu luyen:
  - Phase hien tai van dang tu luyen trong `Home`.
  - Toc do tu luyen da doi sang cong thuc data-driven:
    - base spiritual energy cua map nam trong `map_templates.spiritual_energy`
    - zone multiplier nam trong `spiritual_energy_templates` / zone slot
    - he so canh gioi nam trong `realm_templates.absorption_multiplier`
  - `cultivation` van la so nguyen, phan le duoc giu trong `cultivation_progress`.
  - Tick tu luyen co online settlement + offline settlement; login lai se duoc tinh bu phan chua nhan.
- Potential / AllocatePotential:
  - Da bo chi so `Physique` khoi phase nay.
  - `AllocatePotentialPacket` khong con `amount`; moi request chi nang 1 lan cho 1 stat.
  - Server la source of truth:
    - client chi gui `target_stat`
    - server check tier hien tai, cost/gain cua lan nang tiep theo, du potential thi moi nang
  - Server gui kem preview cho client trong `CharacterBaseStats`:
    - next upgrade count
    - tier index
    - potential cost
    - stat gain
    - can upgrade hay khong
  - UI/debug client da show duoc preview lan nang tiep theo.
- Potential config:
  - Da bo model `gain_per_point` / `base + step` cu.
  - Hien dung bang tier `potential_stat_upgrade_tiers` voi semantics:
    - `max_upgrade_count = 5` nghia la tier ap dung cho lan nang `1..5`
    - lan nang `6` se nhay sang tier tiep theo
  - User se tiep tuc tu config tier trong DB cho balance.
- Unity debug scene:
  - `WorldTravelDebugController` da support:
    - `U` toggle cultivation
    - `I` switch zone theo input
    - `P` allocate 1 lan theo stat dang chon
  - Scene `World.unity` da duoc wiring de test flow nay.
- Random system direction da chot:
  - GameServer se co `GameRandomService` dung chung, cac logic can random se goi qua service nay thay vi tu `new Random`.
  - Phase dau config random dat trong `Config/gameRandomConfig.json`.
  - Semantics uu tien cho loot/drop la `Exclusive`: moi lan roll chi ra 1 ket qua.
  - `Co duyen` buff tat ca entry hop le theo huong A: tang chance cac entry va rut phan do tu `None`, de de balance.

## Session update 2026-03-18

- Da them foundation server-side cho he thong `cong phap -> skill` theo huong data-driven, chua pha flow Unity/client hien tai:
  - migration/schema moi cho `martial_arts`, `martial_art_stages`, `martial_art_stage_stat_bonuses`
  - `skills`, `skill_effects`, `martial_art_skills`, `martial_art_skill_scalings`
  - runtime progress/loadout: `player_martial_arts`, `player_skills`, `player_skill_loadouts`
- Da them `Entity` + `Repository` + DI registration tuong ung trong `GameServer`.
- Da them runtime foundation:
  - `CombatDefinitionCatalog`
  - `MartialArtProgressionService`
  - `SkillRuntimeBuilder`
  - bo enum/record typed cho stat bonus, effect, scaling, runtime skill
- Giu nguyen quyet dinh an toan:
  - KHONG bat buoc character phai co cong phap moi duoc `StartCultivation` o phase nay, de tranh pha flow da verify.
  - KHONG tiep tuc day vao combat packet/client UI o buoi nay.
  - bonus cong phap hien chi moi co foundation runtime; chua tu dong cong vao `CharacterBaseStatsComposer`.
- Diem can chot o buoi sau neu muon day tiep:
  - co muon cultivation realm hien tai phai gan voi 1 cong phap dang active khong
  - bonus `value_type` nao se duoc support o phase 1 ngoai `Flat`
  - phap bao/vu khi se la he thong rieng song song hay di qua chung skill/equipment pipeline

## Session update 2026-03-18 item foundation

- Da them foundation server-side cho `item / equipment / crafting`, chua noi vao `CharacterBaseStats` runtime:
  - migration/schema moi:
    - `item_templates`
    - `player_items`
    - `equipment_templates`
    - `equipment_template_stats`
    - `player_equipments`
    - `player_equipment_stat_bonuses`
    - `craft_recipes`
    - `craft_recipe_requirements`
    - `craft_recipe_mutation_bonuses`
    - `martial_art_book_templates`
- Da them `Entity` + `Repository` + DI registration tuong ung trong `GameServer`.
- Da them runtime catalog/types:
  - `ItemDefinitionCatalog`
  - `ItemSystemTypes`
- Da them service foundation:
  - `ItemService`
  - `EquipmentService`
  - `EquipmentStatService`
  - `CraftService`
- Quy tac da chot trong code phase nay:
  - equipment sinh theo huong `template base + instance bonus`
  - item equipment khi tao moi se co `player_equipment` row di kem
  - item dang equip khong duoc xoa/craft consume
  - craft item non-stackable bat buoc chi dinh `player_item_id`
  - craft currency cost chua noi he thong tien te, nen service se tu choi recipe co `cost_currency_value > 0`
  - mutation chi sinh bonus instance cho output la equipment
  - CHUA noi `EquipmentStatService` vao `CharacterBaseStatsComposer` hay runtime player snapshot

## Session update 2026-03-18 admin designer tool

- Da tao project moi `CientTest/AdminDesignerTool` theo huong WinForms `.NET 8` de game design co the config template truc tiep tren DB.
- Tool hien tai dung co che generic table editor qua `NpgsqlDataAdapter`, co navigation theo nhom:
  - `Cong Phap`
  - `Item & Equipment`
  - `Che Tao`
  - `Balance`
  - `World`
  - `Mo Rong Sau Nay`
- Resource dang mo san trong tool:
  - cong phap / tang / bonus / skill / skill effect / unlock / scaling
  - item template / equipment template / equipment stats / martial art book
  - craft recipe / requirements / mutation bonuses
  - realm / potential upgrade tiers / spiritual energy / map / map zone
- Tool tu tim `GameServer/Config/dbConfig.json` tu thu muc chay len tren; khong can hard-code connection string trong project.
- UI hien co:
  - cay navigation ben trai
  - bang editor ben phai
  - mo ta + huong dan cho tung resource
  - `Tai Lai`, `Them Dong`, `Nhan Ban Dong`, `Xoa Dong`, `Luu Thay Doi`
  - bo loc nhanh tren bang dang mo
- Huong chon cho phase nay:
  - uu tien ship nhanh mot MVP designer-friendly, chua lam form chuyen biet theo tung resource
  - boss/drop moi dat san diem mo rong; khi schema co that thi them resource vao catalog la edit duoc ngay
- Build verify:
  - `dotnet build CientTest/AdminDesignerTool/AdminDesignerTool.csproj` -> pass, 0 warning, 0 error

