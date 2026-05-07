# Game Design Client Overview

## Mục tiêu của file này

File này dùng để cho agent game design đọc nhanh client hiện tại đang là game gì, đang có những hệ thống nào, content nào đã thấy được trên client, và muốn xem sâu hơn thì vào đúng chỗ nào trong code/asset.

Repo hiện tại không có bộ `docs` client sẵn có như mô tả; tài liệu gameplay gốc duy nhất đang thấy trong repo là `Assets/Game/README.md`. File này bổ sung phần game design.

## Đây là game gì?

Đây là một game online 2D theo hướng tu tiên / võ học / fantasy phương Đông.

Bản chất gameplay hiện tại trên client:

- Đăng nhập tài khoản, vào nhân vật, vào world.
- Di chuyển trong map, chọn mục tiêu, tấn công bằng basic skill hoặc skill đã equip.
- Đánh quái, nhặt vật phẩm rơi trên đất.
- Mở inventory để mặc đồ, dùng consumable, dùng sách công pháp, dùng sách đan phương.
- Chọn công pháp chủ tu.
- Tu luyện để tăng tu vi.
- Khi đầy mốc cảnh giới thì đột phá.
- Dùng tiềm năng để cộng chỉ số gốc.
- Luyện chế đan dược theo kiểu practice session có thời gian chờ.

Nói ngắn gọn: đây là một game tu tiên có loop combat + loot + progression + cultivation + alchemy.

## Vòng lặp gameplay hiện tại

1. Đăng nhập.
2. Vào world bằng nhân vật đầu tiên có sẵn.
3. Ở world, người chơi có thể:
   - di chuyển và đổi map/khu,
   - target quái, portal, reward,
   - đánh quái,
   - loot đồ,
   - mở menu nhân vật,
   - đổi công pháp chủ tu,
   - tu luyện và đột phá,
   - phân bổ tiềm năng,
   - luyện đan.
4. Progression chính hiện tại đi qua 4 tầng:
   - inventory/equipment,
   - martial art active cho cultivation,
   - skill loadout cho combat,
   - cultivation / breakthrough / potential allocation.

## Content đang thấy được trong client

### Scene và world flow

- Scene boot: `Assets/Game/Scenes/Bootstrap/Bootstrap.unity`
- Scene login: `Assets/Game/Scenes/Auth/Login.unity`
- Scene world: `Assets/Game/Scenes/World/World.unity`

### Map đang được đăng ký trên client

Map prefab đang được map bằng `ClientMapCatalog.asset`:

- `map_home_01`
- `map_farm_01`
- `map_farm_03`
- `map_farm_04`

Xem tại:

- `Assets/Game/Content/ScriptableObjects/Maps/ClientMapCatalog.asset`
- `Assets/Game/Runtime/Features/World/Presentation/ClientMapCatalog.cs`
- `Assets/Game/Runtime/Features/World/Presentation/WorldMapPresenter.cs`

Lưu ý: tên hiển thị map, adjacency, spawn point, portal, kích thước map, zone count là dữ liệu server gửi về khi join map. Client asset này chỉ map `clientMapKey -> prefab`.

### Quái đang có visual trên client

Catalog visual quái đang có 3 code rõ ràng:

- `wood_doll`
- `enemy_soi_lang_bang`
- `enemy_gau_nau_tinh`

Xem tại:

- `Assets/Game/Content/ScriptableObjects/Character/EnemyPresentationCatalog.asset`
- `Assets/Game/Runtime/Features/World/Presentation/EnemyPresentationCatalog.cs`
- `Assets/Game/Runtime/Features/World/Presentation/WorldEnemiesPresenter.cs`

Lưu ý quan trọng: client chỉ biết visual/prefab cho quái. Quái spawn ở đâu, stat gì, drop gì, tần suất gì là do server snapshot quyết định.

### Portal / điểm tương tác local hiện có

Trong world hiện có local portal để mở UI:

- Luyện đan
- Luyện khí
- Luyện phù
- Mật thất / cultivation

Xem tại:

- `Assets/Game/Runtime/Features/World/Presentation/LocalFixPortalPresenter.cs`
- `Assets/Game/Runtime/UI/World/WorldUIController.cs`

Lưu ý:

- `Luyện đan` đã có flow đầy đủ hơn.
- `Luyện khí` và `Luyện phù` hiện mới là placeholder UI.

## Hệ thống cultivation / công pháp / đột phá / tiềm năng

### 1. Công pháp chủ tu

Người chơi có danh sách công pháp đã sở hữu, và chọn 1 công pháp active để chủ tu.

Mỗi martial art hiện client đang đọc/giữ các thông tin kiểu:

- `MartialArtId`
- `Name`
- `Category`
- `Description`
- `CurrentStage`
- `CurrentExp`
- `ExpRequired`
- `MaxStage`
- `QiAbsorptionRate`
- `IsActive`

Xem tại:

- `Assets/Game/Runtime/Features/MartialArts/Application/ClientMartialArtState.cs`
- `Assets/Game/Runtime/Features/MartialArts/Application/ClientMartialArtService.cs`
- `Assets/Game/Runtime/UI/World/WorldCultivationPanelController.cs`
- `Assets/Game/Runtime/UI/World/WorldCultivationPanelController.Actions.cs`
- `Assets/Game/Runtime/UI/World/WorldCultivationPanelController.ViewState.cs`

### 2. Tu luyện

Client đang thể hiện logic sau:

- Muốn start cultivation thì phải có active martial art.
- Cultivation đọc `CultivationPreview` do server trả về.
- Preview hiện các thành phần quan trọng:
  - `QiAbsorptionRate`
  - `SpiritualEnergyPerMinute`
  - `RealmAbsorptionMultiplier`
  - `EstimatedCultivationPerMinute`
  - `BlockedReason`
- UI hiện estimate theo giờ: `+X tu vi / h`.

Không được start cultivation nếu:

- đang cultivating,
- đang practicing,
- character ở state lifespan expired,
- đã full tu vi cảnh giới và cần đột phá,
- preview báo blocked.

### 3. Đột phá

Client coi đột phá khả dụng khi:

- `HasNextRealm == true`
- `RealmMaxCultivation > 0`
- `Cultivation >= RealmMaxCultivation`

UI cũng đọc:

- `BreakthroughChancePercent`
- `RealmDisplayName`

Ý nghĩa cho design: đây là mốc hard gate progression. Khi đầy thanh tu vi của cảnh giới hiện tại, người chơi không tu luyện tiếp được mà phải đột phá.

### 4. Tiềm năng

Client hiện cho phân bổ `UnallocatedPotential` vào 6 chỉ số gốc:

- BaseHp
- BaseMp
- BaseAttack
- BaseSpeed
- BaseLuck
- BaseSense

Mỗi stat có `PotentialUpgradePreview` do server gửi về, gồm các thông tin kiểu:

- tier hiện tại
- số lần nâng tiếp theo
- cost tiềm năng
- stat gain
- có được nâng hay không

UI hiện đang để người chơi chọn các mức spend option từ lớn đến nhỏ.

Xem tại:

- `Assets/Game/Runtime/UI/World/WorldPotentialPanelController.cs`
- `Assets/Game/Content/ScriptableObjects/UI/PotentialStatPresentationCatalog.asset`

## Hệ thống inventory và item

Inventory hiện support các nhóm item label trong client:

- Trang bị
- Đan dược / consumable
- Nguyên liệu
- Pháp bảo
- Công pháp
- Linh thạch
- Nhiệm vụ
- Đan phương
- Hạt giống
- Dược liệu
- Linh thổ
- Cây sống

Những action đang thấy rõ ở client:

- equip / unequip equipment,
- use martial art book,
- use consumable,
- use pill recipe book,
- drop item ra đất nếu `IsDroppable`.

Nếu item drop thành công, client coi đó là `GroundReward` trong world.

Xem tại:

- `Assets/Game/Runtime/Features/Inventory/Application/ClientInventoryService.cs`
- `Assets/Game/Runtime/Features/Inventory/Application/ClientInventoryState.cs`
- `Assets/Game/Runtime/UI/World/WorldInventoryPanelController.cs`
- `Assets/Game/Runtime/UI/World/WorldInventoryPanelController.ItemActions.cs`
- `Assets/Game/Runtime/UI/Inventory/InventoryItemPresentationCatalog.cs`

Ghi chú cho design:

- Dùng sách công pháp sẽ học martial art mới.
- Dùng sách đan phương sẽ mở recipe luyện đan.
- Logic exact của item effect nằm ở server; client chủ yếu render và gửi request.

## Hệ thống skill và combat

### 1. Skill loadout

Client tách rõ:

- danh sách skill người chơi sở hữu,
- loadout slot để equip skill vào combat,
- slot 1 là basic skill.

Xem tại:

- `Assets/Game/Runtime/Features/Skills/Application/ClientSkillState.cs`
- `Assets/Game/Runtime/Features/Skills/Application/ClientSkillService.cs`
- `Assets/Game/Runtime/UI/World/WorldSkillPanelController.cs`
- `Assets/Game/Runtime/UI/Hud/WorldCombatHudController.cs`

### 2. Skill config/presentation hiện có

Skill visual trên client đang config theo 2 tầng:

1. `skill group preset`
2. `skill override` theo `skillId` hoặc `skillCode`

Mỗi group có thể config:

- `skillGroupCode`
- `skillGroupName`
- `archetype`
- `cast/release/impact state name`
- `source socket`
- `impact socket`
- `cast/release/impact fx prefab`
- `fxLifetimeSeconds`
- `faceTargetOnCast`
- `iconSprite`

Archetype hiện client hiểu:

- `MeleeWeaponSwing`
- `WeaponProjectile`
- `HandProjectile`
- `SummonStrike`
- `SelfBuff`

Xem tại:

- `Assets/Game/Content/ScriptableObjects/Combat/SkillWorldPresentationCatalog.asset`
- `Assets/Game/Runtime/Features/Combat/Presentation/SkillWorldPresentationCatalog.cs`
- `Assets/Game/Runtime/Features/Combat/Presentation/SkillPresentationRuntimeTypes.cs`

### 3. Skill group đang thấy trong asset

Hiện asset đang có ít nhất các group này:

- `bang_chuy_thuat`
- `bang_liet_tram`
- `dam_xa`
- `hoa_dan_soi`
- `hoa_dan_thuat`
- `moc_mien_chuong`
- `xich_hoa_kiem_tram`

Đây là content nhìn thấy ở tầng presentation. Vẫn cần server data để biết skill này thuộc martial art nào, level nào, stat nào, target type nào, cast range/cooldown bao nhiêu.

### 4. Combat flow hiện tại

Combat trên client đang theo luồng:

- player select target,
- nếu target là enemy/boss thì basic skill có thể được dùng để attack,
- server trả `AttackEnemyResult`,
- sau đó có `SkillCastStarted`,
- rồi `SkillImpactResolved`,
- client cập nhật cooldown, cast bar và visual.

Target kind hiện client biết:

- Player
- Enemy
- Boss
- Npc/Portal
- GroundReward

Xem tại:

- `Assets/Game/Runtime/Features/Combat/Application/ClientCombatService.cs`
- `Assets/Game/Runtime/Features/Targeting/Application/ClientTargetState.cs`
- `Assets/Game/Runtime/Features/Targeting/Application/WorldTargetInteractionRules.cs`
- `Assets/Game/Runtime/Features/World/Presentation/WorldTargetActionController.cs`
- `Assets/Game/Runtime/Features/World/Presentation/WorldTargetActionController.Execution.cs`

## Hệ thống luyện chế

### Tổng quan

Hệ thống craft được làm đầy đủ nhất hiện tại là alchemy / luyện đan.

`Smithing` và `Talisman` mới có khung panel và local portal, chưa có gameplay flow đầy đủ.

### Flow alchemy hiện tại

1. Load danh sách recipe đã học.
2. Chọn 1 recipe.
3. Load recipe detail.
4. Gắn nguyên liệu bắt buộc và phụ trợ từ inventory.
5. Preview craft.
6. Nếu hợp lệ thì gửi craft.
7. Server tạo `practice session` có thời gian.
8. Session có thể pause / resume / cancel.
9. Khi xong thì có pending result và cần acknowledge.

Client đang phân biệt rõ:

- recipe list,
- recipe detail cache,
- preview,
- current practice session,
- pending practice result.

Xem tại:

- `Assets/Game/Runtime/Features/Alchemy/Application/ClientAlchemyService.cs`
- `Assets/Game/Runtime/Features/Alchemy/Application/ClientAlchemyState.cs`
- `Assets/Game/Runtime/UI/World/WorldCraftingPanelController.cs`

### Cách hiểu design của hệ thống này

Từ code client hiện tại, alchemy đang là hệ thống:

- có learning gate qua `PillRecipeBook`,
- có nguyên liệu bắt buộc và optional input,
- có success rate segment,
- có boosted craft count,
- có duration theo số lượng craft,
- có consumed item tracking,
- có practice state thay vì instant craft.

Nói cách khác, luyện đan hiện tại không phải bấm nút ra item ngay, mà là một progress-based profession.

### Điều quan trọng cho game design

Client asset KHÔNG chứa database recipe full.

Client chỉ giữ:

- recipe list sau khi server trả,
- detail của recipe đang đã xem,
- preview craft,
- current session/result.

Nếu cần biết exact recipe formula, success rate, mutation rate, reward item, duration, optional input, boost logic, cần đọc server code hoặc DB.

## Travel / zone / loot

Client đang có:

- travel map,
- use portal,
- query zone của map,
- switch zone,
- pick ground reward.

Zone panel hiện màu occupancy theo mức đông người.

Xem tại:

- `Assets/Game/Runtime/Features/World/Application/ClientWorldTravelService.cs`
- `Assets/Game/Runtime/Features/World/Application/ClientGroundRewardService.cs`
- `Assets/Game/Runtime/UI/World/WorldMapZonePanelController.cs`
- `Assets/Game/Runtime/Features/World/Application/ClientWorldState.cs`

## Muốn xem gì thì vào đâu?

### Muốn hiểu tổng quan client đang có những hệ nào

- `Assets/Game/README.md`
- `Assets/Game/Runtime/Core/Application/ClientRuntime.cs`

### Muốn hiểu game loop login -> world

- `Assets/Game/Runtime/Features/Auth/Application/ClientLoginFlowService.cs`
- `Assets/Game/Runtime/UI/Screens/Login/LoginScreenController.cs`
- `Assets/Game/Runtime/Features/Character/Application/ClientCharacterService.cs`

### Muốn xem map nào đang có trên client

- `Assets/Game/Content/ScriptableObjects/Maps/ClientMapCatalog.asset`
- `Assets/Game/Content/Prefabs/World/Maps`

### Muốn xem quái nào đã có visual

- `Assets/Game/Content/ScriptableObjects/Character/EnemyPresentationCatalog.asset`
- `Assets/Game/Content/Prefabs/Characters/Enermys`

### Muốn xem skill đang config visual thế nào

- `Assets/Game/Content/ScriptableObjects/Combat/SkillWorldPresentationCatalog.asset`
- `Assets/Game/Runtime/Features/Combat/Presentation/SkillWorldPresentationCatalog.cs`

### Muốn xem skill loadout/combat button

- `Assets/Game/Runtime/UI/World/WorldSkillPanelController.cs`
- `Assets/Game/Runtime/UI/Hud/WorldCombatHudController.cs`
- `Assets/Game/Runtime/Features/Combat/Application/ClientCombatService.cs`

### Muốn xem cultivation / breakthrough / martial art

- `Assets/Game/Runtime/UI/World/WorldCultivationPanelController.cs`
- `Assets/Game/Runtime/UI/World/WorldCultivationPanelController.Actions.cs`
- `Assets/Game/Runtime/UI/World/WorldCultivationPanelController.ViewState.cs`
- `Assets/Game/Runtime/Features/MartialArts/Application/ClientMartialArtService.cs`
- `Assets/Game/Runtime/Features/MartialArts/Application/ClientMartialArtState.cs`

### Muốn xem potential allocation

- `Assets/Game/Runtime/UI/World/WorldPotentialPanelController.cs`
- `Assets/Game/Content/ScriptableObjects/UI/PotentialStatPresentationCatalog.asset`

### Muốn xem inventory / item use / item type

- `Assets/Game/Runtime/UI/World/WorldInventoryPanelController.cs`
- `Assets/Game/Runtime/UI/World/WorldInventoryPanelController.ItemActions.cs`
- `Assets/Game/Runtime/Features/Inventory/Application/ClientInventoryService.cs`
- `Assets/Game/Runtime/UI/Inventory/InventoryItemPresentationCatalog.cs`

### Muốn xem luyện chế

- `Assets/Game/Runtime/UI/World/WorldCraftingPanelController.cs`
- `Assets/Game/Runtime/Features/Alchemy/Application/ClientAlchemyService.cs`
- `Assets/Game/Runtime/Features/Alchemy/Application/ClientAlchemyState.cs`

### Muốn xem target / interaction / portal / loot

- `Assets/Game/Runtime/Features/Targeting/Application/ClientTargetState.cs`
- `Assets/Game/Runtime/Features/Targeting/Application/WorldTargetInteractionRules.cs`
- `Assets/Game/Runtime/Features/World/Presentation/WorldTargetActionController.cs`
- `Assets/Game/Runtime/Features/World/Presentation/LocalFixPortalPresenter.cs`
- `Assets/Game/Runtime/Features/World/Application/ClientGroundRewardService.cs`

## Những chỗ hiện tại mới là placeholder hoặc chưa xong

- Quest tab chỉ là placeholder text.
- Guild tab chỉ là placeholder text.
- Luyện khí chỉ có local portal/panel text placeholder.
- Luyện phù chỉ có local portal/panel text placeholder.
- CharacterSelect screen folder đang rỗng.
- Loading screen folder đang rỗng.
- Test EditMode/PlayMode đang rỗng.

## Nếu agent đọc được server code hoặc DB thì ưu tiên đọc thêm gì

Client repo này không chứa schema DB.

Nếu agent có quyền đọc server code/DB, ưu tiên tìm các model/bản ghi đúng với những contract mà client đang dùng:

- `CharacterModel`
- `CharacterBaseStatsModel`
- `CharacterCurrentStateModel`
- `InventoryItemModel`
- `PlayerMartialArtModel`
- `CultivationPreviewModel`
- `PlayerSkillModel`
- `SkillLoadoutSlotModel`
- `LearnedPillRecipeModel`
- `PillRecipeDetailModel`
- `AlchemyCraftPreviewModel`
- `PracticeSessionModel`
- `PracticeCompletionResultModel`
- `MapDefinitionModel`
- `MapZoneSummaryModel`
- `EnemyRuntimeModel`
- `GroundRewardModel`

Đồng thời đọc các packet server tương ứng để biết:

- recipe thực tế đến từ đâu,
- quái spawn/drop ở đâu,
- spiritual energy theo map/zone tính thế nào,
- breakthrough chance tính thế nào,
- potential preview tính thế nào,
- skill cast range/cooldown/target type đến từ đâu.

Nếu không có server repo, thì vẫn có thể lần theo client usage ở các `Client*Service.cs` để biết tên packet và tên model cần tìm.
