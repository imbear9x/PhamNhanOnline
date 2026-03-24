# Unity Client Scene Setup

## 1. Bootstrap scene

Create a scene named `Bootstrap` and add it to Build Settings.

Create these root objects:
- `__App`

Add components on `__App`:
- `ClientBootstrap`

Create one `ClientBootstrapSettings` asset from:
- `Assets/Game/Runtime/Infrastructure/Config/ClientBootstrapSettings.cs`

Suggested asset location:
- `Assets/Game/Content/ScriptableObjects/Client/ClientBootstrapSettings.asset`

Assign in the asset:
- `Server Host`: your server IP or `127.0.0.1`
- `Server Port`: `7777`
- `Login Scene Name`: `Login`
- `World Scene Name`: `World`
- `Initial Scene Name`: `Login`

Assign that asset into the `settings` field of `ClientBootstrap`.

Before opening Play Mode, sync shared contracts into Unity plugins:
- Run `powershell -File .\scripts\sync-gameshared-to-unity.ps1`

## 2. Login scene

Create a scene named `Login` and add it to Build Settings.

Suggested roots:
- `__Scene`
- `__UI`

Under `__UI`, create a canvas and a panel for login.

Suggested login hierarchy:
- `Canvas`
- `LoginPanel`
- `UsernameInput`
- `PasswordInput`
- `ConnectButton`
- `OpenWorldButton`
- `StatusText`

Add components:
- On `LoginPanel`: `LoginScreenController`
- On `UsernameInput`: `TMP_InputField`
- On `PasswordInput`: `TMP_InputField`
- On `ConnectButton`: `Button`
- On `OpenWorldButton`: `Button`
- On `StatusText`: `TMP_Text`

Wire serialized fields of `LoginScreenController` to those objects.

## 3. World scene

Create a scene named `World` and add it to Build Settings.

Suggested roots:
- `__Scene`
- `MapRoot`
- `EntitiesRoot`
- `WorldUiRoot`
- `Main Camera`

Create one empty object:
- `WorldRoot`

Add component on `WorldRoot`:
- `WorldSceneController`

Wire serialized fields:
- `Map Root` -> `MapRoot`
- `Entities Root` -> `EntitiesRoot`
- `World Ui Root` -> `WorldUiRoot`
- `World Camera` -> `Main Camera`

### 3.1. World gameplay menu

Do not generate gameplay UI by runtime code when the goal is editor-friendly scene wiring.
For gameplay menu / HUD screens in `World`, prefer:
- write controller code only
- create the UI hierarchy manually in Unity
- wire serialized references in Inspector

Suggested hierarchy under `WorldUiRoot`:
- `HudCanvas`
- `SafeAreaRoot`
- `TopRightButtons`
- `MenuButton`
- `MenuButtonLabel`
- `ScreenCanvas`
- `WorldMenuUiController`
- `WorldMenuPanel`
- `DimmerButton`
- `Window`
- `Header`
- `TitleText`
- `CloseButton`
- `TabButtonsRoot`
- `QuestTabButton`
- `InventoryTabButton`
- `StatsTabButton`
- `EquipmentTabButton`
- `GuildTabButton`
- `TabContentRoot`
- `QuestPanel`
- `QuestContentText`
- `InventoryPanel`
- `InventoryContentText`
- `StatsPanel`
- `StatsContentText`
- `EquipmentPanel`
- `EquipmentContentText`
- `GuildPanel`
- `GuildContentText`

Suggested component placement:
- On `MenuButton`: `Button`
- On `CloseButton`: `Button`
- On `DimmerButton`: `Button`
- On each `*TabButton`: `Button`
- On each `*ContentText`: `TMP_Text`
- On `WorldMenuUiController`: `WorldMenuController`

Wire `WorldMenuController` like this:
- `Panel Root` -> `WorldMenuPanel`
- `Menu Button` -> `MenuButton`
- `Menu Button Text` -> `MenuButtonLabel`
- `Close Button` -> `CloseButton`
- `Dimmer Button` -> `DimmerButton`
- `Title Text` -> `TitleText`
- `Default Tab Id` -> `quest`

Add 5 entries into `Tabs`:

1. Quest
- `Tab Id` -> `quest`
- `Title` -> `Nhiem vu`
- `Button` -> `QuestTabButton`
- `Content Root` -> `QuestPanel`
- `Content Text` -> `QuestContentText`

2. Inventory
- `Tab Id` -> `inventory`
- `Title` -> `Kho đồ`
- `Button` -> `InventoryTabButton`
- `Content Root` -> `InventoryPanel`
- `Content Text` -> để trống nếu `InventoryPanel` đã có `WorldInventoryPanelController`

3. Stats
- `Tab Id` -> `stats`
- `Title` -> `Chỉ số`
- `Button` -> `StatsTabButton`
- `Content Root` -> `StatsPanel`
- `Content Text` -> `StatsContentText`

4. Equipment
- `Tab Id` -> `equipment`
- `Title` -> `Trang bị`
- `Button` -> `EquipmentTabButton`
- `Content Root` -> `EquipmentPanel`
- `Content Text` -> `EquipmentContentText`

5. Guild
- `Tab Id` -> `guild`
- `Title` -> `Bang hội`
- `Button` -> `GuildTabButton`
- `Content Root` -> `GuildPanel`
- `Content Text` -> `GuildContentText`

Setup note:
- Keep `WorldMenuPanel` inactive by default in scene.
- Keep `WorldMenuUiController` active so the script can wire button events from scene start.
- Place `MenuButton` in `HudCanvas` so it is always visible.
- Place `WorldMenuPanel` in `ScreenCanvas` so it overlays gameplay cleanly.
- If you want popup confirm dialogs later, put them in `ModalCanvas`, not inside `HudCanvas`.

### 3.2. Inventory grid and tooltip checklist

Mục tiêu:
- giữ `WorldInventoryPanelController` trên `InventoryPanel`
- để controller tự bind tên nhân vật, stat lines, inventory grid và tooltip
- không sinh inventory UI bằng code ở runtime

Hierarchy gợi ý bên trong `InventoryPanel`:

```text
InventoryPanel
  CharacterNameText
  StatsRow
    StatsList
      StatLineTemplate
  InventoryStatusText
  InventoryGrid
    Viewport
      Content
        InventoryItemTemplate
          Background
          Icon
          QuantityRoot
            QuantityText
          EquippedMarker
          EnhanceRoot
            EnhanceText
          SelectedHighlight
  ItemTooltipPanel
    TooltipBackground
    TooltipIcon
    TooltipNameText
    TooltipMetaText
    TooltipDescriptionText
    TooltipQuantityText
```

Checklist:

1. Khu vực nhân vật và chỉ số
- Giữ `WorldInventoryPanelController` trên `InventoryPanel`.
- `Character Name Text` -> `CharacterNameText`
- `Stat List View` -> object `StatsList` có `StatLineListView`
- Trong `StatsList`, dùng `StatLineTemplate` hiện có với `StatLineView`.

2. Text trạng thái inventory
- Tạo `InventoryStatusText` là một `TMP_Text` đơn giản.
- `Inventory Status Text` -> `InventoryStatusText`
- Text này dùng cho các trạng thái kiểu:
- `Đang tải kho đồ...`
- `Kho đồ đang trống.`
- `12 vật phẩm`

3. Root của inventory grid
- Thêm `ScrollRect` hoặc một container thường tên `InventoryGrid`.
- Bên trong tạo `Viewport/Content`.
- Thêm `GridLayoutGroup` trên `Content`.
- Giá trị khởi đầu gợi ý:
- `Cell Size`: around `64 x 64` or `72 x 72`
- `Spacing`: `6 x 6`
- `Constraint`: `Fixed Column Count`
- `Constraint Count`: `5` or `6`

4. Template item trong inventory
- Tạo `InventoryItemTemplate` là con của `Content`.
- Gắn `InventoryItemSlotView` lên root của `InventoryItemTemplate`.
- Root nên có `Image` hoặc `Button` nếu bạn muốn dùng transition/selectable của Unity.
- Các ref con:
- `Background` -> ảnh nền/khung item
- `Icon` -> ảnh icon item
- `QuantityRoot` -> root badge nhỏ cho số lượng stack
- `QuantityText` -> text dưới `QuantityRoot`
- `EquippedMarker` -> badge góc tùy chọn như `E`
- `EnhanceRoot` -> root badge tùy chọn cho `+1`, `+2`
- `EnhanceText` -> text dưới `EnhanceRoot`
- `SelectedHighlight` -> object glow/viền cho item đang được chọn

5. Inventory item grid view
- Gắn `InventoryItemGridView` lên `InventoryGrid` hoặc `Content`.
- `Content Root` -> `Content`
- `Item Template` -> `InventoryItemTemplate`
- Giữ `Hide Template Object` ở trạng thái enabled.
- `Inventory Grid View` on `WorldInventoryPanelController` -> this `InventoryItemGridView`
- Có thể giữ `InventoryItemTemplate` active trong editor nếu dễ design hơn; script sẽ tự hide khi Play.

6. Tooltip panel
- Tạo `ItemTooltipPanel` ở đâu đó trong `InventoryPanel`, thường là bên phải.
- Gắn `InventoryItemTooltipView` lên `ItemTooltipPanel`.
- Có thể giữ `ItemTooltipPanel` active trong editor để dễ chỉnh style, nhưng khi Play nó sẽ bị hide cho tới khi có item được chọn hoặc hover.
- Các ref:
- `Panel Root` -> `ItemTooltipPanel`
- `Background Image` -> `TooltipBackground`
- `Icon Image` -> `TooltipIcon`
- `Name Text` -> `TooltipNameText`
- `Meta Text` -> `TooltipMetaText`
- `Description Text` -> `TooltipDescriptionText`
- `Quantity Text` -> `TooltipQuantityText`
- `Item Tooltip View` on `WorldInventoryPanelController` -> this `InventoryItemTooltipView`

7. Asset presentation catalog
- Tạo một `ScriptableObject` asset:
- `Create > PhamNhanOnline > UI > Inventory Item Presentation Catalog`
- Path gợi ý:
- `Assets/Game/Content/ScriptableObjects/UI/InventoryItemPresentationCatalog.asset`
- Kéo asset đó vào `Item Presentation Catalog` trên `WorldInventoryPanelController`

8. Điền mapping icon và background
- Trong catalog, thêm `iconEntries` với key khớp `item_templates.icon` từ DB.
- Ví dụ key:
- `item_kiem_sat`
- `item_hoi_linh_dan`
- `item_linh_thach`
- Trong catalog, thêm `backgroundEntries` với key khớp `item_templates.background_icon` từ DB.
- Ví dụ key:
- `bg_item_common`
- `bg_item_rare`
- `bg_item_pill`
- `bg_item_weapon_green`
- Đồng thời cấu hình fallback:
- `Default Icon Sprite`
- `Default Background Sprite`
- `Rarity Backgrounds`
- `Item Type Backgrounds`

9. Quy ước đặt tên ở phía DB/admin
- `item_templates.icon` nên là key icon item gửi sang client.
- `item_templates.background_icon` nên là key tùy chọn cho nền/khung item gửi sang client.
- Nếu `background_icon` để trống, client sẽ fallback theo `rarity` hoặc `item_type` trong catalog.

10. Wiring cuối cùng trên `WorldInventoryPanelController`
- `Character Name Text` -> `CharacterNameText`
- `Stat List View` -> `StatsList`
- `Inventory Status Text` -> `InventoryStatusText`
- `Inventory Grid View` -> `InventoryGridView`
- `Item Tooltip View` -> `InventoryItemTooltipView`
- `Item Presentation Catalog` -> `InventoryItemPresentationCatalog.asset`
- Thường nên giữ:
- `Auto Load Missing Inventory Data` enabled
- `Force Refresh Inventory On Enable` disabled
- `Inventory Reload Retry Cooldown Seconds` around `2`

11. Luồng test
- Vào world bằng một character.
- Mở `Kho đồ`.
- Nếu inventory chưa được load, client sẽ gửi `GetInventoryPacket`.
- Nếu cache inventory đã có sẵn, panel sẽ dùng lại danh sách item đó.
- Hover hoặc click vào một item slot:
- icon, tên, rarity/type, description, quantity, equipped state sẽ hiện trong tooltip.

### 3.3. Equipment slots checklist

Mục tiêu:
- phần `ô trang bị` là UI cố định, tự dựng bằng scene hoặc prefab cho đẹp
- phần data bind, tooltip và drag/drop do code xử lý
- mở tab `Kho đồ` sẽ thấy cả item trong balo và 4 ô đang mặc

Hierarchy gợi ý bên trong `InventoryPanel`:

```text
InventoryPanel
  CharacterNameText
  StatsRow
    StatsList
      StatLineTemplate
  EquipmentSection
    EquipmentSlotsRoot
      WeaponSlot
        Background
        Icon
        EmptyStateRoot
          SlotLabelText
        OccupiedStateRoot
          EnhanceRoot
            EnhanceText
        SelectedHighlight
      ArmorSlot
        Background
        Icon
        EmptyStateRoot
          SlotLabelText
        OccupiedStateRoot
          EnhanceRoot
            EnhanceText
        SelectedHighlight
      PantsSlot
        Background
        Icon
        EmptyStateRoot
          SlotLabelText
        OccupiedStateRoot
          EnhanceRoot
            EnhanceText
        SelectedHighlight
      ShoesSlot
        Background
        Icon
        EmptyStateRoot
          SlotLabelText
        OccupiedStateRoot
          EnhanceRoot
            EnhanceText
        SelectedHighlight
  InventoryStatusText
  InventoryGrid
    Viewport
      Content
        InventoryItemTemplate
          Background
          Icon
          QuantityRoot
            QuantityText
          EquippedMarker
          EnhanceRoot
            EnhanceText
          SelectedHighlight
  InventoryDropZone
  ItemTooltipPanel
    TooltipBackground
    TooltipIcon
    TooltipNameText
    TooltipMetaText
    TooltipDescriptionText
    TooltipQuantityText
```

Checklist:

1. Root ô trang bị
- Tạo `EquipmentSlotsRoot`.
- Gắn `EquipmentSlotsPanelView` lên object này.
- Trong `slots`, khai báo đủ 4 binding:
- `Weapon -> WeaponSlot`
- `Armor -> ArmorSlot`
- `Pants -> PantsSlot`
- `Shoes -> ShoesSlot`
- Kéo `Equipment Slots View` trên `WorldInventoryPanelController` tới `EquipmentSlotsRoot`.

2. Từng ô trang bị cố định
- Gắn `EquipmentSlotView` lên từng root slot như `WeaponSlot`, `ArmorSlot`, `PantsSlot`, `ShoesSlot`.
- `Slot Type` phải đúng với ô:
- `WeaponSlot` -> `Weapon`
- `ArmorSlot` -> `Armor`
- `PantsSlot` -> `Pants`
- `ShoesSlot` -> `Shoes`
- Kéo ref:
- `Background Image` -> `Background`
- `Icon Image` -> `Icon`
- `Slot Label Text` -> `SlotLabelText` nếu bạn muốn hiện chữ tên ô
- `Empty State Root` -> `EmptyStateRoot`
- `Occupied State Root` -> `OccupiedStateRoot`
- `Enhance Root` -> `EnhanceRoot`
- `Enhance Level Text` -> `EnhanceText`
- `Selected Highlight Root` -> `SelectedHighlight`

3. Vùng thả để gỡ đồ về balo
- Tạo `InventoryDropZone` phủ lên khu vực lưới item hoặc toàn bộ khung balo bên phải.
- Gắn `InventoryDropZoneView` lên object này.
- Kéo `Inventory Drop Zone View` trên `WorldInventoryPanelController` tới `InventoryDropZone`.
- Khi kéo item từ ô trang bị và thả vào đây, client sẽ gọi `UnequipInventoryItemPacket`.

4. Hành vi drag/drop hiện tại
- Kéo item từ balo sang đúng ô -> client gửi `EquipInventoryItemPacket`.
- Kéo item từ balo sang ô sai loại -> không gửi packet, item trở về chỗ cũ.
- Kéo item từ ô trang bị sang vùng `InventoryDropZone` hoặc thả lên một ô item trong grid -> client gửi `UnequipInventoryItemPacket`.
- Nếu slot đã có đồ và bạn kéo item đúng loại vào -> server sẽ tự thay thế món cũ.

5. Dữ liệu item cần có từ server
- `InventoryItemModel.EquipmentSlotType`
- `InventoryItemModel.EquipmentType`
- `InventoryItemModel.LevelRequirement`
- `InventoryItemModel.IsEquipped`
- `InventoryItemModel.EquippedSlot`
- `InventoryItemModel.EnhanceLevel`
- `InventoryItemModel.Durability`

6. Ghi chú phase hiện tại
- Phase này chỉ check `đúng slot`.
- Chưa validate `level_requirement` theo `realm_id`.
- Tooltip của item trong balo và item đang mặc dùng chung `InventoryItemTooltipView`.
- Grid balo chỉ hiện item chưa mặc; item đang mặc sẽ xuất hiện ở 4 ô trang bị cố định.

### 3.4. Potential panel checklist

Mục tiêu:
- dùng `StatsPanel` hiện tại làm panel tiềm năng
- không sinh UI gameplay bằng code runtime
- controller chỉ bind data, xử lý hover lựa chọn nâng cấp và gửi packet

Hierarchy gợi ý bên trong `StatsPanel`:

```text
StatsPanel
  PotentialPanelRoot
    HeaderSection
      RealmNameText
      CultivationProgressText
      UnallocatedPotentialText
      StatusText
    BreakthroughSection
      BreakthroughChanceText
      BreakthroughButton
        BreakthroughButtonText
    PotentialRowsSection
      PotentialRowsList
        PotentialRowTemplate
          Icon
          NameText
          ValueText
          HoverHighlight
    PotentialOptionsPopup
      PopupFrame
      PopupTitleText
      OptionsRoot
        OptionButtonTemplate
          OptionLabelText
```

Checklist:

1. Root controller
- Gắn `WorldPotentialPanelController` lên `PotentialPanelRoot` hoặc ngay trên `StatsPanel`.
- Trong `WorldMenuController`, tab `stats` nên để:
- `Content Root` -> `StatsPanel`
- `Content Text` -> để trống nếu `StatsPanel` đã có `WorldPotentialPanelController`

2. Header
- `Realm Name Text` -> `RealmNameText`
- `Cultivation Progress Text` -> `CultivationProgressText`
- `Unallocated Potential Text` -> `UnallocatedPotentialText`
- `Status Text` -> `StatusText`
- `CultivationProgressText` sẽ hiện dạng `xxx/xxxx`

3. Khu vực đột phá
- `Breakthrough Root` -> `BreakthroughSection`
- `Breakthrough Chance Text` -> `BreakthroughChanceText`
- `Breakthrough Button` -> `BreakthroughButton`
- `Breakthrough Button Text` -> `BreakthroughButtonText`
- `BreakthroughSection` có thể để `active` trong editor cho dễ chỉnh; lúc Play controller sẽ tự bật/tắt theo trạng thái viên mãn cảnh giới

4. Danh sách row chỉ số
- Tạo `PotentialRowsList`, gắn `PotentialUpgradeRowListView`
- Trong `PotentialRowsList` tạo `PotentialRowTemplate`, gắn `PotentialUpgradeRowView`
- Kéo ref trên `PotentialUpgradeRowView`:
- `Icon Image` -> `Icon`
- `Name Text` -> `NameText`
- `Value Text` -> `ValueText`
- `Hover Highlight Root` -> `HoverHighlight`
- Kéo ref trên `PotentialUpgradeRowListView`:
- `Content Root` -> `PotentialRowsList`
- `Item Template` -> `PotentialRowTemplate`
- giữ `Hide Template Object` bật

5. Popup lựa chọn nâng cấp
- Tạo `PotentialOptionsPopup`, gắn `PotentialUpgradeOptionsPopupView`
- Tạo `OptionButtonTemplate`, gắn `PotentialUpgradeOptionButtonView`
- Kéo ref trên `PotentialUpgradeOptionsPopupView`:
- `Panel Root` -> `PotentialOptionsPopup`
- `Panel Transform` -> `PotentialOptionsPopup`
- `Title Text` -> `PopupTitleText`
- `Options Root` -> `OptionsRoot`
- `Option Template` -> `OptionButtonTemplate`
- Kéo ref trên `PotentialUpgradeOptionButtonView`:
- `Button` -> `OptionButtonTemplate`
- `Label Text` -> `OptionLabelText`
- `PotentialOptionsPopup` nên có `Image` hoặc `Graphic` ở root nếu bạn muốn nó bắt được raycast ổn định khi rê chuột vào popup

6. Catalog icon/tên stat
- Tạo asset:
- `Create > PhamNhanOnline > UI > Potential Stat Presentation Catalog`
- path gợi ý:
- `Assets/Game/Content/ScriptableObjects/UI/PotentialStatPresentationCatalog.asset`
- Với từng stat, điền:
- `Target`
- `Display Name`
- `Icon Sprite`
- `Value Format`
- `Gain Format`
- Mapping tối thiểu nên có:
- `BaseHp`
- `BaseMp`
- `BaseAttack`
- `BaseSpeed`
- `BaseFortune`
- `BaseSpiritualSense`

7. Wiring cuối cùng trên `WorldPotentialPanelController`
- `Realm Name Text` -> `RealmNameText`
- `Cultivation Progress Text` -> `CultivationProgressText`
- `Unallocated Potential Text` -> `UnallocatedPotentialText`
- `Status Text` -> `StatusText`
- `Breakthrough Root` -> `BreakthroughSection`
- `Breakthrough Chance Text` -> `BreakthroughChanceText`
- `Breakthrough Button` -> `BreakthroughButton`
- `Breakthrough Button Text` -> `BreakthroughButtonText`
- `Row List View` -> `PotentialRowsList`
- `Options Popup View` -> `PotentialOptionsPopup`
- `Presentation Catalog` -> `PotentialStatPresentationCatalog.asset`
- `Max Visible Upgrade Options` -> `3` nếu muốn đúng phase hiện tại

8. Hành vi hiện tại của panel
- Header lấy từ server:
- tên cảnh giới hiện tại
- tu vi hiện tại
- tu vi tối đa của cảnh giới
- tỉ lệ đột phá
- cờ còn cảnh giới kế tiếp hay không
- Hover vào một row sẽ hiện tối đa `3` lựa chọn nâng cấp lớn nhất từ cao xuống thấp
- Click một lựa chọn sẽ gọi `AllocatePotentialPacket`
- Nếu đang viên mãn cảnh giới và còn cảnh giới kế tiếp, panel sẽ hiện nút đột phá
- Click nút đột phá sẽ gọi `BreakthroughPacket`
- Thành công hay thất bại đều tự refresh lại UI theo packet server trả về

9. Gợi ý raycast
- `PotentialRowTemplate` root nên có `Image` hoặc `Button` với `Raycast Target = true`
- `HoverHighlight`, icon trang trí, background trang trí không cần bắt chuột thì nên tắt `Raycast Target`
- popup root và nút lựa chọn nên bắt raycast để người chơi di chuột từ row sang popup mà không bị mất lựa chọn ngay

## Naming rules

Use clear scene roots so another developer can understand quickly:
- `__App`: persistent app/system object
- `__Scene`: scene-local orchestration object
- `__UI`: scene-local UI root
- `MapRoot`: loaded map visuals go here
- `EntitiesRoot`: player, monster, npc instances go here
- `WorldUiRoot`: HUD, nameplates, floating labels

## Important note

The client runtime now expects `GameShared.dll` and `LiteNetLib.dll` in `Assets/Plugins`.
Those are synced by the PowerShell script above, so packet contracts stay shared between server and Unity without copying source files.
