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

## UI collaboration rule
- Với UI gameplay trong Unity, ưu tiên viết code khung/controller trước rồi để user tự dựng hierarchy/prefab/scene trong Editor và kéo ref bằng Inspector.
- Tránh sinh cả UI hierarchy bằng runtime code nếu user không yêu cầu rõ kiểu đó, vì sẽ làm scene khó nhìn, khó chỉnh và khó maintain.

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
- Da nang cap UX tiep theo:
  - grid generic support dropdown cho FK / enum pho bien
  - them 3 workspace chuyen biet:
    - `Martial Art Workspace`
    - `Craft Recipe Workspace`
    - `Equipment Workspace`
  - workspace hoat dong theo kieu master-detail: chon bang cha o tren, bang con o tab duoi tu dong loc theo parent dang chon
  - child editor tu dien FK mac dinh khi bam `Them Dong` o cac quan he truc tiep (`martial_art_id`, `craft_recipe_id`, `item_template_id`, ...)
- Huong chon cho phase nay:
  - uu tien designer workflow truoc cho 3 cum quan trong nhat: cong phap, craft recipe, equipment
  - van giu generic table editor cho cac bang con lai de mo rong nhanh
  - boss/drop moi dat san diem mo rong; khi schema co that thi them resource vao catalog la edit duoc ngay
- Build verify:
  - `dotnet build CientTest/AdminDesignerTool/AdminDesignerTool.csproj` -> pass, 0 warning, 0 error

## Session update 2026-03-18 alchemy herb cave foundation

- Da chot huong kien truc theo design moi:
  - tach rieng he `pill_recipe_*` khoi `craft_recipe_*`
  - `consume pill -> apply effect` de phase sau
  - them `ItemType` moi:
    - `PillRecipeBook = 8`
    - `HerbSeed = 9`
    - `HerbMaterial = 10`
    - `Soil = 11`
- Da them migration/schema moi cho cum `dan duoc / duoc vien / dong phu co ban`:
  - `player_caves`
  - `player_garden_plots`
  - `soil_templates`
  - `player_soils`
  - `herb_templates`
  - `herb_growth_stage_configs`
  - `player_herbs`
  - `herb_harvest_outputs`
  - `pill_templates`
  - `pill_effects`
  - `pill_recipe_templates`
  - `pill_recipe_inputs`
  - `player_pill_recipes`
  - `pill_recipe_mastery_stages`
- Da them `Entity` + `Repository` + DI registrations tuong ung trong `GameServer`.
- Da them runtime catalog/types/service:
  - `AlchemySystemTypes`
  - `AlchemyDefinitionCatalog`
  - `PillRecipeService`
  - `AlchemyService`
  - `HerbService`
- Quy tac da chot trong code phase nay:
  - character moi tao se duoc cap `home cave` rieng dua theo `MapCatalog.ResolveHomeDefinition()`
  - `home cave` hien luu thong tin co ban:
    - cua ai
    - map nao
    - zone nao
    - co 8 `garden plot` mac dinh
  - `soil` la item non-stackable, co row runtime rieng trong `player_soils`
  - dang trong cay se dung growth time tich luy + toc do tang truong cua soil
  - khi soil het thoi gian hieu luc se thanh `Depleted`
  - `required_herb_maturity` da co trong schema nhung `AlchemyService` se tu choi input kieu nay o phase hien tai
  - phase hien tai uu tien flow:
    - hoc dan phuong tu item sach
    - luyen dan tu item da nam trong inventory
    - trong cay / thu hoach / tra item ve inventory
  - chua noi effect cua pill vao pipeline dung vat pham/runtime buff
- Da nang cap admin tool de game design config duoc cum alchemy/herb:
  - them nhom resource `Dan Duoc & Duoc Vien`
  - them FK/enum dropdown cho:
    - `pill_templates`
    - `pill_effects`
    - `pill_recipe_templates`
    - `pill_recipe_inputs`
    - `pill_recipe_mastery_stages`
    - `soil_templates`
    - `herb_templates`
    - `herb_growth_stage_configs`
    - `herb_harvest_outputs`
- Build verify:
  - `dotnet build GameServer/GameServer.csproj` -> pass
  - `dotnet build CientTest/AdminDesignerTool/AdminDesignerTool.csproj` -> pass

## Admin tool rule

- Quy uoc tiep tuc cho cac buoi sau:
  - neu them bang moi thuoc nhom `config/template/balance/content` cua game thi mac dinh phai cap nhat `AdminDesignerTool` cung buoi do, khong de tool admin bi tre pha so voi schema moi
  - muc tieu la game design co the cau hinh du lieu bang tool admin thay vi phai sua SQL tay
  - toi thieu moi bang config moi can duoc xem xet:
    - them resource vao navigation neu phu hop
    - map FK/enum dropdown neu co
    - bo sung workspace chuyen biet neu bang do la cum resource duoc dung thuong xuyen

## Session update 2026-03-18 game random config to database

- Da chuyen he `gameRandom` tu doc file `GameServer/Config/gameRandomConfig.json` sang doc DB luc startup.
- Schema moi da them:
  - `game_random_tables`
  - `game_random_entries`
  - `game_random_entry_tags`
  - `game_random_fortune_tags`
- Da seed du lieu demo tu file cu sang DB:
  - `monster.drop.demo_slime`
  - `item.demo_herb`
  - `currency.spirit_stone_small`
  - tag `item_drop`, `currency_drop`
- `IGameRandomService` va `GameRandomService` duoc giu nguyen interface/runtime behavior; chi doi nguon config sang DB loader trong `ServiceCollectionExtensions`.
- Da xoa luong copy file config va xoa file `GameServer/Config/gameRandomConfig.json` khoi repo de tranh 2 nguon su that song song.
- Admin tool da duoc cap nhat de config duoc cum bang moi:
  - `Game Random Tables`
  - `Game Random Entries`
  - `Game Random Entry Tags`
  - `Game Random Fortune Tags`
- Build verify:
  - `dotnet build GameServer/GameServer.csproj` -> pass
  - `dotnet build CientTest/AdminDesignerTool/AdminDesignerTool.csproj` -> pass

## Session note 2026-03-18 local database schema sync

- Da kiem tra DB local `phamnhan_online` bang `psql` va xac nhan truoc do moi chi co nhom bang `Cong Phap`; cac bang `item / equipment / crafting / alchemy / herb / game_random` chua ton tai trong DB that.
- Da apply truc tiep vao DB local cac migration:
  - `20260318_add_item_equipment_crafting_foundation.sql`
  - `20260318_add_alchemy_herb_cave_foundation.sql`
  - `20260318_add_game_random_config_tables.sql`
- Sau khi apply, cac bang config moi da ton tai trong DB va admin tool moi co the load schema dung cho nhom resource tuong ung.

## Session update 2026-03-18 admin dependency UX

- Da them dependency check cho `AdminDesignerTool` de tranh game design bam `Them Dong` trong khi bang cha chua co du lieu.
- Cac bang phu thuoc FK/nguon du lieu cha (vi du `equipment_templates`, `soil_templates`, `pill_effects`, `pill_recipe_inputs`, `map_zone_slots`, `game_random_entries`, ...) gio se:
  - tu kiem tra DB khi mo resource
  - khoa nut `Them Dong` neu chua du dieu kien
  - hien thong bao ro rang trong man hinh ve bang/du lieu cha dang thieu
- Muc tieu la loai bo kieu UX "thu bam roi moi doan" va thay bang thong bao huong dan truc tiep.

## Session update 2026-03-18 admin field help UX

- Da them `Field Help` panel ngay trong `TableEditorControl`:
  - co combo chon field cua bang hien tai
  - co o mo ta read-only giai thich field dung de lam gi, can nhap gi
- Da gan tooltip vao header cua tung cot bang grid de hover chuot vao ten field co the thay help text.
- Field help hien tai ket hop:
  - mo ta chung theo ten field (`id`, `code`, `name`, `item_template_id`, `chance_parts_per_million`, ...)
  - mo ta rieng cho mot so field quan trong (`item_type`, `pill_category`, `seed_item_template_id`, `game_random.mode`, ...)
- Muc tieu la giam phu thuoc vao viec nho schema/ID va giup game design tu tin hon khi nhap data.

## Session update 2026-03-18 admin field help content polish

- Da doi `tooltip` va `field help` sang tieng Viet co dau cho de doc va de dung.
- Da bo sung help text theo kieu:
  - y nghia cua field
  - cach nhap
  - vi du gia tri mau
- Da viet chi tiet hon cho 3 cum du kien dung nhieu:
  - `Item Templates`
  - `Pill Recipe`
  - `Game Random`
- Da them highlight cot dang duoc chon trong `Field Help` bang cach to mau header cua cot tuong ung.

## Session update 2026-03-18 martial art qi absorption rate

- Da bo sung truong `qi_absorption_rate` vao `public.martial_arts`.
- Da them migration `20260318_add_martial_art_qi_absorption_rate.sql` va cap nhat `initDatabase.sql`.
- Da noi truong nay vao `MartialArtEntity`, `MartialArtDefinition`, `CombatDefinitionCatalog`.
- Da moc no vao cong thuc tu luyen trong `CharacterCultivationService` tai diem thay the `GongPhapCoefficientStub`.
- Ban dau tung tam lay he so theo cong phap so huu cao nhat, nhung assumption nay da bi thay the boi he `active martial art` o muc ben duoi.
- Da apply migration nay vao DB local `phamnhan_online` de tool/server tren may local dung duoc ngay.

## Session update 2026-03-18 active martial art flow

- Da them `active_martial_art_id` vao `public.character_base_stats`.
- Da them migration `20260318_add_active_martial_art_id.sql` va cap nhat `initDatabase.sql`.
- Da mo rong `CharacterBaseStat`, `CharacterBaseStatsDto`, `CharacterBaseStatsModel`, `NetworkModelMapper`, `CharacterService` de luu/truyen `active_martial_art_id`.
- Da them model/DTO moi:
  - `GameShared/Models/PlayerMartialArtModel.cs`
  - `GameServer/DTO/PlayerMartialArtDto.cs`
- Da them service moi `MartialArtService` de xu ly:
  - lay danh sach cong phap nguoi choi dang so huu
  - dung item `Sach cong phap` de hoc cong phap
  - chon `cong phap tu luyen chinh` (active martial art)
- Da them packet/handler moi:
  - `GetOwnedMartialArtsPacket`
  - `UseMartialArtBookPacket`
  - `SetActiveMartialArtPacket`
- Da noi `StartCultivationAsync` vao rule moi:
  - neu chua co `active martial art` thi reject voi `MessageCode.CultivationRequiresActiveMartialArt`
  - he so `qi_absorption_rate` de tinh tu vi chi lay theo `active martial art` dang chon
- Luong gameplay hien tai:
  - nguoi choi dung item `MartialArtBook` de hoc cong phap
  - hoc xong van chua du dieu kien tu luyen neu chua active cong phap
  - nguoi choi co the doi cong phap active sau do
- Gia dinh hien tai:
  - doi active cong phap duoc phep bat ky luc nao
  - neu dang tu luyen thi cac lan settlement sau se dung `active martial art` moi nhat
- Da apply migration `20260318_add_active_martial_art_id.sql` vao DB local `phamnhan_online`.

## Session update 2026-03-18 herb replant item direction

- Da chot huong cho he herb:
  - `seed_item_template_id` dung cho trong moi tu hat giong
  - `replant_item_template_id` dung cho item cay song/co the nho vao tui roi trong lai
- Da them item type moi `HerbPlant = 12` trong runtime va admin tool.
- Da mo rong `herb_templates` voi truong `replant_item_template_id`.
- Da cap nhat `AlchemyDefinitionCatalog` de co the lookup herb theo:
  - `seed_item_template_id`
  - `replant_item_template_id`
- Da cap nhat admin tool:
  - dropdown rieng cho `replant_item_template_id` chi lay item type `HerbPlant`
  - dependency check yeu cau co ca `HerbSeed` va `HerbPlant` truoc khi tao `Herb Template`
  - help text/resource help mo ta ro khac nhau giua hat giong va cay song co the trong lai
- Migration moi:
  - `20260318_add_herb_replant_item_template.sql`

## Session update 2026-03-18 herb age years removal

- Da go bo truong `age_years` khoi bang config `herb_growth_stage_configs`.
- Da go bo truong `current_age_years` khoi bang runtime `player_herbs`.
- Da cap nhat entity/runtime/service lien quan de khong con doc/ghi hai truong nay.
- Tuoi hien thi cua linh duoc ve sau se duoc tinh dong tu:
  - thoi diem trong cay
  - game server time
- Khong coi `age_years` trong DB la nguon su that nua.
- Migration moi:
  - `20260318_drop_herb_age_years_columns.sql`

## Session update 2026-03-18 enemy boss instance reward foundation

- Da them draft flow gameplay/server cho enemy, boss, map instance, leash, respawn, reward vao `docs/ENEMY_BOSS_INSTANCE_FLOW_DRAFT.md`.
- Da chot cac rule gameplay chinh:
  - enemy/boss co skill, khong dung MP, chi dung cooldown va minimum cast interval
  - enemy thuong out-of-combat mot luc se hoi day mau, boss thi khong
  - reward tach 2 kieu:
    - `ground drop`
    - `direct reward`
  - target reward co the theo:
    - tat ca nguoi du dieu kien
    - last hit
    - top damage
  - instance solo phase dau
  - instance timed het gio thi dong runtime
  - instance farm neu vang nguoi mot thoi gian moi huy
- Da them schema/migration moi:
  - `20260318_add_enemy_instance_reward_foundation.sql`
- Da cap nhat `initDatabase.sql` va apply migration nay vao DB local `phamnhan_online`.
- Da them nhom bang config moi:
  - `enemy_templates`
  - `enemy_template_skills`
  - `enemy_reward_rules`
  - `map_enemy_spawn_groups`
  - `map_enemy_spawn_entries`
  - `map_instance_configs`
- Da them entity/repository/runtime definition catalog cho he enemy-instance.
- Da mo rong `MapManager` de support:
  - public map co spawn group
  - private home runtime co spawn group neu config scope phu hop
  - map instance solo co `map_instance_configs`
- Da mo rong `MapInstance` runtime de quan ly:
  - spawn groups
  - enemy runtime
  - pending death events
  - ground rewards
  - completion state cua instance
- Da them `EnemyRewardRuntimeService`:
  - xu ly pending death events trong game loop
  - phat reward direct vao inventory qua `ItemService`
  - tao `GroundRewardEntity` cho reward roi xuong dat
  - chia `cultivation_reward_total` va `potential_reward_total` theo damage contribution
- Da noi `GameLoop`, `RuntimeMaintenanceService`, `ServiceCollectionExtensions` vao he thong moi.
- Da mo rong admin tool de co the config duoc ngay:
  - `Enemy Workspace`
  - `Enemy Spawn Workspace`
  - cac bang generic cho `map_instance_configs`
  - enum/FK/dependency cho toan bo bang enemy-instance-reward moi
- Build verify:
  - `dotnet build GameServer/GameServer.csproj` pass

## Session update 2026-03-24 equipment slots foundation

- Da them foundation equip/unequip cho inventory theo huong scene-driven:
  - `EquipInventoryItemPacket`
  - `UnequipInventoryItemPacket`
  - result packet tra lai inventory snapshot moi
- `InventoryItemModel` da co them:
  - `EquipmentSlotType`
  - `EquipmentType`
  - `LevelRequirement`
- Phase hien tai chi validate `dung slot`, chua validate `level_requirement` / `realm_id`.
- Client inventory UI da co nen cho:
  - grid item trong balo
  - 4 o trang bi co dinh
  - tooltip dung chung cho item trong balo va item dang mac
  - drag/drop tu balo sang o trang bi va nguoc lai
- Server da support:
  - equip item vao dung slot
  - replace item cu trong cung slot
  - unequip theo slot
  - tra lai inventory snapshot moi sau thao tac
- Seed `admin02` da duoc mo rong:
  - 4 mon dang mac san
  - item du phong trong balo de test thay the
  - consumable/material/talisman/currency de test tooltip va keo sai slot

## Session update 2026-03-24 inventory UI foundation

- Da them client-side inventory runtime:
  - `ClientInventoryState`
  - `ClientInventoryService`
  - cache inventory tren `ClientRuntime.Inventory`
- `WorldInventoryPanelController` hien:
  - van bind duoc character name + stat lines
  - tu load `GetInventoryPacket` khi chua co cache
  - neu da co cache thi dung lai, khong goi server lai moi lan mo tab
  - bind inventory grid va tooltip item
- Da them bo UI reusable cho inventory:
  - `InventoryItemPresentationCatalog`
  - `InventoryItemGridView`
  - `InventoryItemSlotView`
  - `InventoryItemTooltipView`
- Huong presentation data da chot:
  - `item_templates.icon` la key item icon cho client
  - `item_templates.background_icon` la key background slot/khung item cho client
  - neu `background_icon` trong DB de trong thi client fallback theo rarity hoac item_type trong catalog
- Da cap nhat:
  - `GameShared.Models.InventoryItemModel` them `BackgroundIcon`
  - `GameServer` mapper/entity/item definition de tra field nay
  - migration DB `20260324_add_item_template_background_icon.sql`
  - help text trong `AdminDesignerTool`
- Checklist setup Unity cho inventory grid/tooltip da duoc ghi vao `docs/UNITY_CLIENT_SCENE_SETUP.md`

## Session update 2026-03-19 hard-set admin02 test martial art

- Da seed rieng cho account test `admin02` bang file:
  - `database/seeds/20260319_seed_admin02_truong_xuan_cong.sql`
- Muc dich:
  - cho phep test combat/skill truoc khi hoan thien luong hoc cong phap va nhat sach cong phap trong client
- Seed nay hien:
  - tim character dau tien cua account `admin02`
  - them `Trường Xuân Công` vao `player_martial_arts`
  - them `Mộc Miên chưởng` vao `player_skills`
  - gan `slot_index = 1` trong `player_skill_loadouts`
  - set `character_base_stats.active_martial_art_id = 1`
- Da apply vao DB local `phamnhan_online`.
- State verify cho character `Admin02A`:
  - co `player_martial_arts.martial_art_id = 1`
  - co `player_skills.skill_id = 1`
  - co loadout slot `1`
  - active martial art hien la `1`
  - `dotnet build CientTest/AdminDesignerTool/AdminDesignerTool.csproj -o tmp_codex/admin_enemy_build` pass
- Luu y hien tai:
  - server foundation da co runtime spawn/death/reward, nhung packet/client flow combat, spawn sync, loot pickup, vao/ra instance va destroy instance khi van con player se duoc map tiep voi Unity o nhiep sau
  - reward entry id cho enemy reward rule hien support kieu thuc dung:
    - `item:<id>`
    - `item:<id>:<quantity>`
    - `item_code:<code>`
    - `item_code:<code>:<quantity>`
    - va co fallback doc theo item code truc tiep/entry dang `item.xxx`

## Session update 2026-03-18 skill cast range

- Da bo sung truong `cast_range` vao `public.skills`.
- Da cap nhat:
  - `20260318_add_martial_art_skill_foundation.sql`
  - `initDatabase.sql`
  - migration moi `20260318_add_skill_cast_range.sql`
- Da noi field nay vao:
  - `SkillEntity`
  - `SkillDefinition`
  - `SkillRuntimeDefinition`
  - `CombatDefinitionCatalog`
  - `SkillRuntimeBuilder`
- Admin tool da co:
  - cot hien thi `Tam Xa`
  - field help mo ta ro y nghia cua `cast_range`
- Chu y:
  - field nay chu yeu de client va AI biet khoang cach hop ly de dung skill
  - server khong can validate vi tri qua chat moi frame de tranh tang chi phi runtime
  - hien tai khong co packet/model `GameShared` nao dang serialize skill template rieng, nen khong mo rong packet o nhiep nay

## Session update 2026-03-18 enemy combat packets and runtime sync

- Da noi xong flow packet/runtime de client co the danh quai that:
  - packet moi:
    - `AttackEnemyPacket`
    - `AttackEnemyResultPacket`
    - `PickupGroundRewardPacket`
    - `PickupGroundRewardResultPacket`
    - `WorldRuntimeSnapshotPacket`
    - `EnemySpawnedPacket`
    - `EnemyDespawnedPacket`
    - `EnemyHpChangedPacket`
    - `GroundRewardSpawnedPacket`
    - `GroundRewardDespawnedPacket`
    - `MapInstanceClosedPacket`
- Da them model `GameShared` moi:
  - `EnemyRuntimeModel`
  - `GroundRewardItemModel`
  - `GroundRewardModel`
- Da cap nhat `PacketRegistry` va file serialize manual `GameShared/Packets/EnemyWorldPacketSerialization.cs`.
- Da bo sung `MessageCode` moi cho flow enemy/drop:
  - `EnemyRuntimeIdInvalid`
  - `EnemyNotFound`
  - `EnemyAlreadyDead`
  - `GroundRewardIdInvalid`
  - `GroundRewardNotFound`
  - `GroundRewardNotOwnedYet`
  - `GroundRewardExpired`
  - `CharacterNotInWorldInstance`
  - `MapInstanceClosed`
- `WorldInterestService.PublishWorldSnapshot(...)` gio gui them `WorldRuntimeSnapshotPacket` sau `MapJoinedPacket`, gom:
  - danh sach enemy dang song/da spawn trong instance
  - danh sach ground reward hien co
  - thong tin runtime kind / expire / completed timestamp cua instance
- Da mo rong `MapInstance` de co event queue runtime:
  - enemy spawn
  - enemy hp change
  - enemy despawn
  - ground reward spawn
  - ground reward despawn
- `GameLoop` hien:
  - update instance
  - process reward death events
  - phat cac packet sync runtime xuong player trong instance
  - xu ly lifecycle destroy/evacuate instance
- Ground drop ownership da doi tu `player id` sang `character id` de packet/client de xu ly hon.
- Da them `TryClaimGroundReward(...)` trong `MapInstance` va handler `PickupGroundRewardHandler`.
- Da them handler `AttackEnemyHandler`:
  - client gui `enemy_runtime_id`
  - server ap damage co ban theo `BaseAttack`
  - chua validate range chat o server
  - hp sync cho moi nguoi trong instance se duoc broadcast o tick tiep theo
- Da them `MapInstanceLifecycleService`:
  - neu instance het han/den luc huy ma van con player ben trong
  - server gui `MapInstanceClosedPacket`
  - roi day player ve home map
  - sau do publish world snapshot moi cho player
- Chu y hien tai:
  - phan nay moi map packet/shared + server runtime/handler, chua map UI/logic Unity client
  - `AttackEnemyPacket.MartialArtSkillId` moi de danh dau contract cho client, chua tham gia cong thuc damage that
  - ground drop free-for-all duoc client co the tu suy ra tu `FreeAtUnixMs`, server chua phat packet rieng khi owner lock vua het han
- Build verify:
  - `dotnet build GameServer/GameServer.csproj` pass
  - `dotnet build GameShared/GameShared.csproj` van co hien tuong CLI tra `Build FAILED` nhung khong co error; tuy nhien `GameServer` build thanh cong voi `GameShared` moi nen contract hien tai van compile duoc end-to-end

## Session update 2026-03-19 seed enemy test data for map 03

- Da seed bo data test quai cho `Map 03` bang file:
  - `database/seeds/20260319_seed_map03_enemy_test.sql`
- Da apply vao DB local `phamnhan_online`.
- Data vua seed:
  - enemy:
    - `1001 enemy_soi_lang_bang`
    - `1002 enemy_gau_nau_tinh`
  - skill:
    - `2001 hoa_dan_soi`
    - `2002 dam_xa`
  - reward random table:
    - `enemy.drop.soi_lang_bang`
    - `enemy.drop.gau_nau_tinh`
  - spawn group:
    - wolf o giua map `(500, 500)`
    - bear o `(700, 500)`
    - respawn `5s`
- Rule da chot khi seed:
  - ca 2 la `EnemyKind.Normal`
  - `enemy_templates.code` chinh la key model cho client
  - reward mac dinh `GroundDrop`
  - target rule `EligibleAll`
  - ownership `30s`, free-for-all tiep `30s`
  - map 03 la map farm public, khong tao `map_instance_configs`
  - skill tam thoi de `cast_range = 2000` de test de dang
- Drop table:
  - Soi lang bang: tong `50%` roi `1-3 linh thach`
  - Gau nau tinh: tong `20%` roi `1-3 linh thach`
  - ca 2 dang chia deu theo 3 muc so luong
- Luu y:
  - du lieu skill/effect cua enemy da co de client/AI dung sau nay
  - runtime hien tai da support player danh enemy, enemy chet va roi do
  - enemy gay damage nguoc lai vao player chua duoc noi runtime combat day du

## Session update 2026-03-19 inventory packet and enemy retaliation

- Da bo sung packet inventory de client co the mo balo va dong bo sau khi nhat do:
  - `GetInventoryPacket`
  - `GetInventoryResultPacket`
- Da them model `GameShared` moi:
  - `InventoryItemModel`
- Da cap nhat:
  - `GameShared/Packets/PacketRegistry.cs`
  - `GameShared/Packets/InventoryPacketSerialization.cs`
  - `GameServer/DTO/NetworkModelMapper.cs`
  - handler moi `GameServer/Network/Handlers/GetInventoryHandler.cs`
  - DI registration trong `ServiceCollectionExtensions`
- `GetInventoryHandler` hien:
  - yeu cau session da `EnterWorld`
  - doc inventory qua `ItemService.GetInventoryAsync(...)`
  - tra danh sach item stack/non-stack, equipment state, icon, description, slot, do ben...
- Da noi enemy combat phase 1 theo huong toi gian:
  - enemy khong chu dong tan cong khi player di vao tam
  - enemy chi khoa muc tieu va danh tra sau khi bi player danh
  - neu player ra khoi `combat_radius`, roi instance, hoac mat ket noi thi enemy bo target va ve `Patrol`
- `MonsterEntity` hien co them:
  - `CombatTargetPlayerId`
  - `NextAttackAtUtc`
  - logic schedule don danh tiep theo theo `MinimumSkillIntervalMs`
- `MapInstance` hien:
  - tao `PlayerDamageRuntimeEvent`
  - moi tick neu target van trong tam danh thi queue damage len player
  - neu out-of-combat lau van giu rule cu:
    - boss ve patrol
    - enemy thuong hoi day HP roi ve patrol
- `GameLoop` hien drain `PlayerDamageRuntimeEvent` va ap damage that len player qua `CharacterRuntimeService.ApplyDamage(...)`
- He qua cho test Unity phase 1:
  - co the vao map, load quai/drop snapshot
  - player danh quai -> quai mat mau
  - quai danh tra player neu con trong combat range
  - player chay xa qua tam -> quai ngung danh va ve patrol
  - danh chet quai -> roi do xuong dat
  - client co the gui `PickupGroundRewardPacket`
  - client co the gui `GetInventoryPacket` de refresh tui do
- Build verify:
  - `dotnet build GameServer/GameServer.csproj` pass

