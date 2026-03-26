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

7. Popup dùng item
- Reuse popup giống popup tiềm năng, hoặc tạo riêng một object popup có cùng cấu trúc với `PotentialUpgradeOptionsPopupView`.
- Gợi ý đặt tên: `InventoryItemOptionsPopup`.
- Gắn `PotentialUpgradeOptionsPopupView` lên popup đó.
- Cấu trúc con tối thiểu:
- `Panel Root` -> `InventoryItemOptionsPopup`
- `Title Text` -> text tiêu đề popup
- `Options Root` -> root chứa 2 nút
- `Option Template` -> 1 button template dùng `PotentialUpgradeOptionButtonView`
- Kéo ref trên `WorldInventoryPanelController`:
- `Inventory Panel Bounds` -> root `InventoryPanel`
- `Item Options Popup View` -> `InventoryItemOptionsPopup`
- Popup này hiện đang dùng cho 2 nút:
- `Su dung`
- `Vut ra`

8. Asset presentation catalog
- Tạo một `ScriptableObject` asset:
- `Create > PhamNhanOnline > UI > Inventory Item Presentation Catalog`
- Path gợi ý:
- `Assets/Game/Content/ScriptableObjects/UI/InventoryItemPresentationCatalog.asset`
- Kéo asset đó vào `Item Presentation Catalog` trên `WorldInventoryPanelController`

9. Điền mapping icon và background
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
- `Inventory Panel Bounds` -> `InventoryPanel`
- `Inventory Status Text` -> `InventoryStatusText`
- `Inventory Grid View` -> `InventoryGridView`
- `Item Tooltip View` -> `InventoryItemTooltipView`
- `Item Options Popup View` -> `InventoryItemOptionsPopup`
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

3. Danh sách row chỉ số
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

4. Popup lựa chọn nâng cấp
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

5. Catalog icon/tên stat
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

6. Wiring cuối cùng trên `WorldPotentialPanelController`
- `Realm Name Text` -> `RealmNameText`
- `Cultivation Progress Text` -> `CultivationProgressText`
- `Unallocated Potential Text` -> `UnallocatedPotentialText`
- `Status Text` -> `StatusText`
- `Row List View` -> `PotentialRowsList`
- `Options Popup View` -> `PotentialOptionsPopup`
- `Presentation Catalog` -> `PotentialStatPresentationCatalog.asset`
- `Max Visible Upgrade Options` -> `3` nếu muốn đúng phase hiện tại

7. Hành vi hiện tại của panel
- Header lấy từ server:
- tên cảnh giới hiện tại
- tu vi hiện tại
- tu vi tối đa của cảnh giới
- Hover vào một row sẽ hiện tối đa `3` lựa chọn nâng cấp lớn nhất từ cao xuống thấp
- Click một lựa chọn sẽ gọi `AllocatePotentialPacket`

8. Gợi ý raycast
- `PotentialRowTemplate` root nên có `Image` hoặc `Button` với `Raycast Target = true`
- `HoverHighlight`, icon trang trí, background trang trí không cần bắt chuột thì nên tắt `Raycast Target`
- popup root và nút lựa chọn nên bắt raycast để người chơi di chuột từ row sang popup mà không bị mất lựa chọn ngay

### 3.5. Martial art and cultivation panel checklist

Mục tiêu:
- hiện danh sách công pháp đã học
- có 1 ô `chủ tu` để kéo thả 1 công pháp vào
- server trả về `ước tính tu vi / tiềm năng` theo công pháp chủ tu hiện tại
- chỉ hiện estimate + nút `Tu luyện` khi đã có công pháp chủ tu
- panel này là panel riêng, không nằm chung trong `StatsPanel`

Hierarchy gợi ý cho panel riêng này:

```text
MartialArtPanelRoot
  HeaderRow
    OwnedCountText
    MartialArtStatusText
  ActiveMartialArtSection
    ActiveMartialArtSlot
      FrameBackground
      MartialArtIcon
  EstimateSection
    CultivationProgressBar
      Fill
    CultivationProgressText
    EstimateText
    EstimateDetailText
    StartCultivationButton
      StartCultivationButtonText
  OwnedMartialArtList
    Viewport
      Content
        MartialArtItemTemplate
          NameText
          DetailText
          QiRateText
          ActiveBadge
          SelectedHighlight
```

Checklist:

1. Root controller
- Gắn `WorldMartialArtPanelController` lên `MartialArtPanelRoot`.
- Panel này có thể đặt trong một tab riêng của `WorldMenuController`, ví dụ `martial-art`.
- Nếu bạn tạo tab riêng trong menu:
- `Content Root` -> panel chứa `MartialArtPanelRoot`
- `Content Text` -> để trống vì panel dùng controller thật
- Không cần đặt chung trong `StatsPanel` nữa.

2. Header
- `Owned Count Text` -> `OwnedCountText`
- `Status Text` -> `MartialArtStatusText`
- `MartialArtStatusText` dùng để hiện:
- đang tải danh sách công pháp
- chưa có công pháp chủ tu
- phải về `Home` mới bấm `Tu luyện`
- đang gửi packet đổi công pháp / bắt đầu tu luyện

3. Ô công pháp chủ tu
- Tạo object `ActiveMartialArtSlot`, gắn `ActiveMartialArtSlotView`.
- Wiring trên `ActiveMartialArtSlotView`:
- `Icon Image` -> `MartialArtIcon`
- Root `ActiveMartialArtSlot` nên có `Image` hoặc `Graphic` để nhận `Drop`.
- `MartialArtIcon` là `Image` con nằm trên 1 frame background cố định.
- Khi chưa có công pháp chủ tu, controller sẽ tự `SetActive(false)` cho `MartialArtIcon`.
- Khi có công pháp chủ tu, controller sẽ tự `SetActive(true)` cho `MartialArtIcon`.
- Sprite của `MartialArtIcon` sẽ được lấy từ `MartialArtPresentationCatalog`.
- Khi kéo 1 item công pháp từ list vào đây, client sẽ gọi `SetActiveMartialArtPacket`.

4. Vùng estimate và nút tu luyện
- `Presentation Catalog` -> tạo asset `MartialArtPresentationCatalog` rồi gắn vào `WorldMartialArtPanelController`
- Trong catalog này, map `icon key` của công pháp sang sprite. Server trả `PlayerMartialArtModel.Icon`; nếu trống thì client fallback sang `PlayerMartialArtModel.Code`.
- `Estimate Root` -> `EstimateSection`
- `Cultivation Progress Fill Image` -> object `Fill` dùng `Image` với `Fill Method`
- `Cultivation Progress Text` -> `CultivationProgressText`
- `Estimate Text` -> `EstimateText`
- `Estimate Detail Text` -> `EstimateDetailText`
- `Start Cultivation Button` -> `StartCultivationButton`
- `Start Cultivation Button Text` -> `StartCultivationButtonText`
- `EstimateSection` có thể để active trong editor; lúc Play controller sẽ tự bật/tắt theo dữ liệu server.
- `CultivationProgressText` sẽ hiện dạng `tu_vi_hien_tai/tu_vi_toi_da`
- `Fill.fillAmount` sẽ tự được controller cập nhật theo `Cultivation / RealmMaxCultivation`
- Estimate hiện tại gồm:
- `ước tính tu vi / phút`
- `ước tính tiềm năng / phút`
- `Qi rate` của công pháp
- `linh khí động phủ` đang dùng để tính preview

5. Danh sách công pháp đã học
- Tạo `OwnedMartialArtList`, gắn `MartialArtListView`.
- Trong `Content`, tạo `MartialArtItemTemplate`, gắn `MartialArtListItemView`.
- Wiring trên `MartialArtListItemView`:
- `Icon Image` -> `IconImage` nếu item row của bạn có icon công pháp
- `Name Text` -> `NameText`
- `Detail Text` -> `DetailText`
- `Qi Rate Text` -> `QiRateText`
- `Active Badge Root` -> `ActiveBadge`
- `Selected Highlight Root` -> `SelectedHighlight`
- Wiring trên `MartialArtListView`:
- `Content Root` -> `Content`
- `Item Template` -> `MartialArtItemTemplate`
- giữ `Hide Template Object` bật

6. Hành vi drag/drop hiện tại
- Kéo item công pháp từ list vào `ActiveMartialArtSlot` -> client gửi `SetActiveMartialArtPacket`
- Nếu thả lại đúng công pháp đang active -> client bỏ qua, không gửi packet
- Sau khi server set active thành công:
- `ClientRuntime.MartialArts` cập nhật active
- server trả luôn `CultivationPreview`
- panel tự refresh estimate và trạng thái nút `Tu luyện`

7. Dữ liệu server mà panel đang dùng
- `GetOwnedMartialArtsResultPacket`
- `SetActiveMartialArtResultPacket`
- `UseMartialArtBookResultPacket`
- `StartCultivationResultPacket`
- `PlayerMartialArtModel`
- `CultivationPreviewModel`

8. Rule hiển thị hiện tại
- Nếu chưa có công pháp chủ tu:
- ẩn estimate
- ẩn nút `Tu luyện`
- hiện status hướng dẫn kéo công pháp vào ô chủ tu
- Nếu đã có công pháp chủ tu:
- hiện estimate
- hiện nút `Tu luyện`
- nút chỉ bấm được khi nhân vật đang ở `Home` private map và không ở trạng thái bị khóa hành động

9. Gợi ý raycast
- `MartialArtItemTemplate` root nên có `Image` hoặc `Button` với `Raycast Target = true`
- `SelectedHighlight`, `ActiveBadge` và các object trang trí không cần bắt chuột thì nên tắt `Raycast Target`
- `ActiveMartialArtSlot` root nên có `Image` hoặc `Graphic` để nhận `IDropHandler`

10. Update for active-slot / breakthrough flow
- Add `BreakthroughSection` under `EstimateSection`.
- Wiring on `WorldMartialArtPanelController`:
- `Breakthrough Root` -> `BreakthroughSection`
- `Breakthrough Chance Text` -> `BreakthroughChanceText`
- `Breakthrough Button` -> `BreakthroughButton`
- `Breakthrough Button Text` -> `BreakthroughButtonText`
- The active martial art is shown only in `ActiveMartialArtSlot`; it is hidden from `OwnedMartialArtList`.
- While character state is `Idle`, you can drag the active martial art from slot back into the list area to unequip.
- After unequip, the martial art returns to the list and `Tu luyen` becomes disabled.
- While character state is `Cultivating`, the controller blocks equip/unequip changes.
- While character state is `Cultivating`, `StartCultivationButtonText` changes to `Dung tu luyen`.
- When cultivation reaches realm cap, the server now auto-switches state back to idle.
- When realm cap is reached, hide `StartCultivationButton` and show `BreakthroughSection` instead.

### 3.6. Skill panel checklist

Muc tieu:
- panel rieng cho skill, tach khoi `StatsPanel` va `MartialArtPanel`
- ben trai/phai la danh sach skill so huu va cum o skill de trang bi
- server tra ve:
- danh sach skill character dang so huu
- so o loadout toi da
- moi o dang gan skill nao hoac de trong
- phase hien tai server dang tra `5` o, nhung client da dung dong theo `MaxLoadoutSlotCount`

Hierarchy goi y:

```text
SkillPanelRoot
  HeaderRow
    OwnedCountText
    SkillStatusText
  SkillLoadoutSection
    LoadoutSlotsRoot
      SkillSlotTemplate
        FrameBackground
        EmptyStateRoot
          SlotIndexText
        OccupiedStateRoot
        SkillIcon
  OwnedSkillList
    Viewport
      Content
        SkillItemTemplate
          Icon
          NameText
          DetailText
          CooldownText
          SelectedHighlight
```

Checklist:

1. Root controller
- Gan `WorldSkillPanelController` len `SkillPanelRoot`.
- Neu them tab rieng trong `WorldMenuController`, goi y `tab id = skill`.
- `Content Root` -> panel chua `SkillPanelRoot`
- `Content Text` -> de trong

2. Header
- `Owned Count Text` -> `OwnedCountText`
- `Status Text` -> `SkillStatusText`
- `OwnedCountText` hien tong so skill dang so huu va so o loadout da dung.

3. Cum loadout slot dong
- Tao object `LoadoutSlotsRoot`, gan `SkillLoadoutSlotsView`.
- Ben trong tao 1 object `SkillSlotTemplate`, gan `SkillLoadoutSlotView`.
- Client se tu instantiate them slot tu template nay theo `MaxLoadoutSlotCount` server tra ve.
- Wiring tren `SkillLoadoutSlotView`:
- `Icon Image` -> `SkillIcon`
- `Slot Label Text` -> `SlotIndexText`
- `Empty State Root` -> `EmptyStateRoot`
- `Occupied State Root` -> `OccupiedStateRoot`
- `Selected Highlight Root` -> `SelectedHighlight` neu co
- Root cua tung slot nen co `Image` hoac `Graphic` de nhan `Drop`.
- Wiring tren `SkillLoadoutSlotsView`:
- `Content Root` -> `LoadoutSlotsRoot`
- `Slot Template` -> `SkillSlotTemplate`
- giu `Hide Template Object` bat
- Tren `WorldSkillPanelController`, keo `Loadout Slots View` -> object `LoadoutSlotsRoot`.

4. Danh sach skill so huu
- Tao `OwnedSkillList`, gan `SkillListView`.
- Trong `Content`, tao `SkillItemTemplate`, gan `SkillListItemView`.
- Wiring tren `SkillListItemView`:
- `Icon Image` -> `Icon` neu row cua ban co icon
- `Name Text` -> `NameText`
- `Detail Text` -> `DetailText`
- `Cooldown Text` -> `CooldownText`
- `Selected Highlight Root` -> `SelectedHighlight`
- Wiring tren `SkillListView`:
- `Content Root` -> `Content`
- `Item Template` -> `SkillItemTemplate`
- giu `Hide Template Object` bat

5. Skill presentation catalog
- Tao asset:
- `Create > PhamNhanOnline > UI > Skill Presentation Catalog`
- path goi y:
- `Assets/Game/Content/ScriptableObjects/UI/SkillPresentationCatalog.asset`
- Gan asset nay vao `Presentation Catalog` tren `WorldSkillPanelController`.
- Catalog map sprite theo:
- `skill_group_code`
- hoac fallback theo `skill.code`
- Neu chua co icon mapping, row va slot van chay, icon se tu an.

6. Hanh vi hien tai
- Mo panel skill, client gui `GetOwnedSkillsPacket` neu chua co cache.
- Skill dang duoc trang bi se chi hien trong cac o loadout, khong hien trong list.
- Keo skill tu list vao 1 o trong -> client gui `SetSkillLoadoutSlotPacket`.
- Keo skill tu o nay sang o khac -> client gui `SetSkillLoadoutSlotPacket` cho o dich.
- Keo skill dang equip tu slot tro lai list -> client gui `SetSkillLoadoutSlotPacket` voi `PlayerSkillId = 0` de clear o do.
- Server hien tai dang tra `5` o loadout, nhung client se render theo `MaxLoadoutSlotCount` thay vi hardcode UI.

7. Wiring cuoi cung tren `WorldSkillPanelController`
- `Owned Count Text` -> `OwnedCountText`
- `Status Text` -> `SkillStatusText`
- `Presentation Catalog` -> `SkillPresentationCatalog.asset`
- `Skill List View` -> `OwnedSkillList`
- `Loadout Slots View` -> `LoadoutSlotsRoot`
- Thuong nen giu:
- `Auto Load Missing Skills` enabled
- `Reload Retry Cooldown Seconds` around `2`

8. Goi y raycast
- `SkillItemTemplate` root nen co `Image` hoac `Button` voi `Raycast Target = true`
- `SelectedHighlight` va object trang tri khong can bat chuot thi nen tat `Raycast Target`
- Root cua tung `SkillSlot` nen co `Image` hoac `Graphic` de nhan `IDropHandler`

### 3.7. Combat HUD skill buttons checklist

Muc tieu:
- combat HUD dung du lieu loadout skill hien tai
- nut basic co dinh map voi `slot 1`
- 4 nut skill phu dat san vi tri quanh nut basic
- slot nao trong thi an nut do
- bam skill khi chua co target hop le thi im lang, khong doi UI
- khi server chap nhan cast, nut skill vao cooldown va chay `Image.fillAmount` giam dan

Hierarchy goi y:

```text
CombatHudRoot
  BasicSkillButton
    Icon
    CooldownFill
    CooldownText
    DisabledOverlay
  SkillButton2
    Icon
    CooldownFill
    CooldownText
    DisabledOverlay
  SkillButton3
    Icon
    CooldownFill
    CooldownText
    DisabledOverlay
  SkillButton4
    Icon
    CooldownFill
    CooldownText
    DisabledOverlay
  SkillButton5
    Icon
    CooldownFill
    CooldownText
    DisabledOverlay
  CastBarRoot
    CastBarFill
    CastBarText
```

Checklist:

1. Root controller
- Gan `WorldCombatHudController` len `CombatHudRoot`.
- Gan `Presentation Catalog` -> `SkillPresentationCatalog.asset`.
- `Basic Skill Button` -> `BasicSkillButton`.
- `Additional Skill Buttons` -> `SkillButton2`, `SkillButton3`, `SkillButton4`, `SkillButton5`.

2. Tung nut skill
- Gan `CombatSkillButtonView` len moi nut.
- Tren `BasicSkillButton`:
- `Skill Slot Index` -> `1`
- `Always Visible` -> bat
- Tren 4 nut con lai:
- `Skill Slot Index` -> `2`, `3`, `4`, `5`
- `Always Visible` -> tat
- Wiring tren moi `CombatSkillButtonView`:
- `Content Root` -> root cua nut
- `Button` -> component `Button` tren root
- `Icon Image` -> `Icon`
- `Cooldown Fill Image` -> `CooldownFill` voi `Image Type = Filled`
- `Cooldown Text` -> `CooldownText`
- `Disabled State Root` -> `DisabledOverlay`
- Neu co layout rieng:
- `Occupied State Root` -> khu icon that su co skill
- `Empty State Root` -> frame trong, chi can cho nut basic neu muon

3. Cast bar tuy chon
- Neu muon thay cast time ngay:
- `Cast Bar Root` -> `CastBarRoot`
- `Cast Bar Fill Image` -> `CastBarFill`
- `Cast Bar Text` -> `CastBarText`
- Neu chua keo cast bar, co the de trong 3 field nay.

4. Rule hien tai cua code
- Nut `slot 1` luon hien.
- Nut `slot 2-5` chi hien khi slot do dang equip skill.
- Bam nut skill:
- neu chua chon enemy/boss hop le -> khong gui packet, khong doi UI
- neu dang cast hoac dang cho server xac nhan cast -> cac nut tam thoi khong bam duoc
- neu skill dang cooldown -> nut bi khoa va `CooldownFill.fillAmount` giam dan den 0

5. Packet/client state da co san
- Client gui `AttackEnemyPacket` voi `SkillSlotIndex` va `CombatTarget`

### 3.8. Target selection checklist

Muc tieu:
- di gan doi tuong trong ban kinh thi tu dong tro vao muc tieu phu hop
- co the bam nut UI de doi muc tieu lan luot
- co the ghim muc tieu de auto-target khong tu nhay sang doi tuong khac
- panel target hien duoc player, quai, boss va NPC

Root/controller:
- `WorldSceneController` tren `WorldRoot` se tu dam bao co `WorldClickTargetSelectionController`.
- Khong dung `OnClick()` tren `Button`.
- UI se theo rule chung cua project: controller giu ref `Button` va tu `AddListener`.

Priority rule mac dinh cua auto-target:
- `Npc`
- `Boss`
- `Enemy`
- `Player`

Ban co the doi priority ngay tren component `WorldClickTargetSelectionController` neu muon.

Hierarchy goi y cho HUD target:

```text
TargetHudRoot
  TargetStatusPanel
  NextTargetButton
```

Checklist:

1. Target status panel
- Gan `TargetStatusPanelController` len `TargetStatusPanel`.
- Gan them `TargetHudController` len `TargetStatusPanel` hoac `TargetHudRoot`.
- Wiring toi thieu:
- `Content Root` -> root hien thi thong tin target
- `Avatar Image` -> anh dai dien muc tieu
- `Name Text` -> ten muc tieu
- `Primary Bar Root` + `Primary Bar`
- `Secondary Bar Root` + `Secondary Bar` neu muon hien them mana/resource
- Tren `TargetHudController`:
- `World Scene Controller` -> `WorldRoot`
- `Target Status Panel` -> `TargetStatusPanel`
- Neu co nut target HUD, keo vao `TargetHudController`:
- `Next Target Button`

2. Nut doi muc tieu
- Khong gan `OnClick` trong Inspector.
- Keo `NextTargetButton` vao field `Next Target Button` cua `TargetHudController`.
- Nut nay se doi sang muc tieu tiep theo trong tap candidate gan nhat, het vong moi quay lai muc dau.

3. Hanh vi click tren world
- Click 1 lan vao target = chon target do.
- Double click vao cung target = thu basic skill slot 1 tren target do.
- Click vao khoang trong = clear target.

4. Doi tuong trong world
- Player khac: `RemoteCharacterPresenter` da tu gan `WorldTargetable`.
- NPC: gan `WorldTargetable` thu cong len object NPC, chon dung `Target Kind = Npc` va `Target Id`.
- Quai/Boss:
- auto-target da doc tu `ClientWorldState.Enemies`, nen khong can collider de van co the tu dong tro vao muc tieu gan nhat.
- neu muon click truc tiep tren world vao quai/boss sau nay, presenter/prefab cua no cung nen co `WorldTargetable`.

5. Hanh vi hien tai
- Auto-target chi chay trong ban kinh cau hinh tren `WorldClickTargetSelectionController`.
- Local player bi loai khoi danh sach candidate.
- Neu `Keep Current Target While Still Nearby` dang bat, target hien tai se duoc giu nguyen de tranh nhay lien tuc.
- Khi target dang bi ghim, auto-target se khong tu doi muc tieu.
- Neu khong co muc tieu nao khac trong tap candidate, nut `Doi muc tieu` se giu nguyen target hien tai.
- Khi target roi world / despawn / khong con resolve duoc, pin se duoc go va target co the duoc chon lai theo auto-target.
- Client dung `AttackEnemyResultPacket` de lay:
- `CooldownMs`
- `CooldownEndsUnixMs`
- `CastStartedUnixMs`
- `CastCompletedUnixMs`
- `ImpactUnixMs`
- Client co san `ClientCombatState` / `ClientCombatService` de theo doi:
- pending cast request
- local cast time
- cooldown theo `playerSkillId`

6. Mui ten target tren dau doi tuong
- Neu muon hien marker tren dau target dang chon, gan `WorldTargetSelectionIndicatorController` len `WorldRoot`.
- Wiring:
- `World Map Presenter` -> object dang gan `WorldMapPresenter`
- `White Indicator` -> object mui ten trang trong world
- `Red Indicator` -> object mui ten do trong world
- Rule hien tai:
- `Enemy/Boss` -> mui ten do
- `Player/Npc` -> mui ten trang
- Khi target mat khoi world hoac bi clear, ca hai marker se tu an
- `WorldTargetable` da co san helper lay vi tri neo tren dau collider/renderer, nen marker se bam theo target dang chon

6. Luu y layout
- Khong can radial layout dong o phase nay.
- Dat san 4 nut skill phu quanh nut basic bang tay trong Unity la on nhat.
- Co equip thi bat nut.
- Unequip thi tat nut.

### 3.9. Home wood doll checklist

Muc tieu:
- trong `Player Home` private se tu spawn 1 `wood_doll`
- `wood_doll` dung yen, khong tan cong, co `1000 HP`
- chet xong `2s` se respawn lai
- player thay duoc no trong scene, click/target duoc no, va co the dung no de test damage / VFX / popup

Server/config da san sang:
- DB da co `enemy_templates.code = wood_doll`
- DB da co `map_enemy_spawn_groups.code = home_wood_doll_spawn`
- spawn group nay chi dung cho map `Player Home` private, vi tri tam thoi o giua chieu ngang map (`x = 500`, `y = 125`)
- `wood_doll` duoc cau hinh `base_attack = 0`, `patrol_radius = 0`, `detection_radius = 0`, `combat_radius = 0`, nen no dung yen va khong danh

Client scene/prefab can them:

```text
WorldRoot
  WorldEnemiesPresenter (component)

Assets/Game/Content/Prefabs/World/WoodDoll.prefab
Assets/Game/Content/ScriptableObjects/World/EnemyPresentationCatalog.asset
```

Checklist:

1. Tao prefab `WoodDoll`
- Tao prefab world 2D don gian, root dat ten `WoodDoll`.
- Them visual tuy y (`SpriteRenderer`, child art, shadow...).
- Gan `EnemyPresenter` len root prefab.
- Khong can viet script AI rieng cho `wood_doll`.
- Neu prefab chua co `WorldTargetable`, `EnemyPresenter` se tu them vao runtime.
- Neu prefab chua co collider, `WorldTargetable` se tu tao `BoxCollider2D` tu bounds cua renderer de click/target.

2. Tao `EnemyPresentationCatalog`
- `Create > PhamNhanOnline > World > Enemy Presentation Catalog`
- Trong asset nay, them entry:
- `Code` -> `wood_doll`
- `Prefab` -> `WoodDoll.prefab`
- Co the gan `Default Enemy Prefab` neu muon dung chung cho enemy khac ve sau.

3. Gan `WorldEnemiesPresenter`
- Gan `WorldEnemiesPresenter` len `WorldRoot` hoac mot object world controller trong scene.
- Wiring:
- `Presentation Catalog` -> `EnemyPresentationCatalog.asset`
- `Enemies Root` -> `EntitiesRoot`
- `World Map Presenter` -> object dang gan `WorldMapPresenter`

4. Behavior hien tai cua `EnemyPresenter`
- Doc runtime state cua enemy tu server.
- Map `PosX/PosY` sang toa do Unity qua `WorldMapPresenter`.
- Tu cau hinh `WorldTargetable` voi `runtimeId` cua enemy.
- Khi enemy chet:
- target/click vao no se bi khoa
- neu `Hide When Dead` bat, visual root se an trong 2s cho den luc respawn
- neu `Hide When Dead` tat, ban van thay visual xac trong 2s do

5. Luu y khi test
- Sau khi seed DB xong, hay restart `GameServer` de `EnemyDefinitionCatalog` nap lai template/spawn group moi.
- Sau khi restart server, vao lai `Player Home` de instance private moi duoc tao voi spawn group `wood_doll`.
- `wood_doll` hien dang dat tam o `x = 500`, `y = 125` theo he toa do server cua map home.
- Sau khi ban chot prefab/home layout, ta se tinh tiep vi tri chinh xac trong map.

## Naming rules

Use clear scene roots so another developer can understand quickly:
- `__App`: persistent app/system object
- `__Scene`: scene-local orchestration object
- `__UI`: scene-local UI root
- `MapRoot`: loaded map visuals go here
- `EntitiesRoot`: player, monster, npc instances go here
- `WorldUiRoot`: HUD, nameplates, floating labels

## Layer va sorting layer goi y

Project da duoc them san cac `Unity Layer`:
- `WorldMap`: collider/background map
- `WorldEntity`: root entity tong quat neu can dung chung
- `Targetable`: collider de click/chon target
- `GroundReward`: vat pham roi tren dat
- `WorldIndicator`: mui ten target, vong chan, marker
- `WorldTrigger`: portal, vung trigger map
- `LocalPlayer`: local player
- `RemotePlayer`: player khac
- `Enemy`: quai, boss, hinh nom
- `Npc`: NPC tuong tac

`WorldClickTargetSelectionController` da duoc chinh de neu co layer `Targetable` thi mac dinh chi raycast vao layer nay.

Sorting layer da duoc them san:
- `Background`
- `MapBack`
- `Ground`
- `Characters`
- `Effects`
- `TargetIndicator`
- `WorldUi`
- `Foreground`

Goi y nhanh:
- map/background -> `Background`, `MapBack`, `Ground`
- player/quai/NPC -> `Characters`
- VFX skill, hit flash -> `Effects`
- mui ten target trang/do -> `TargetIndicator`
- world-space label, marker UI -> `WorldUi`

## Important note

The client runtime now expects `GameShared.dll` and `LiteNetLib.dll` in `Assets/Plugins`.
Those are synced by the PowerShell script above, so packet contracts stay shared between server and Unity without copying source files.
