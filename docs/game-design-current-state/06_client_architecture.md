# 06. Client Architecture

## Scene structure

- Có ít nhất 3 scene/runtime phases thấy rõ trong code/docs:
  - `Bootstrap` hoặc auto-bootstrap runtime
  - `Login`
  - `World`
- `ClientRuntime` có thể auto-initialize từ Login scene hoặc World scene nếu chưa có runtime.

Source: `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/UI/Screens/Login/LoginScreenController.cs`, `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/WorldSceneController.cs`

## Core runtime container

- `ClientRuntime` là static service locator chính.
- Nó giữ:
  - connection
  - packet subscriptions
  - feature states/services
  - UI screen service
  - combat death recovery
  - connection recovery
  - skill presentation runtime

Source: `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Core/Application/ClientRuntime.cs`

## Network layer

- Client transport dùng `LiteNetLibClientTransport`.
- Feature services subscribe packet stream trực tiếp và thường wrap request/response bằng `TaskCompletionSource`.
- Ví dụ:
  - `ClientAuthService`
  - `ClientCharacterService`
  - `ClientInventoryService`
  - `ClientCombatService`
  - `ClientAlchemyService`
  - `ClientWorldTravelService`

Source: `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Network/Transport/LiteNetLibClientTransport.cs`, `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/*/Application/*.cs`

## State sync model

- Client giữ feature states tách rời:
  - character
  - inventory
  - martial arts
  - skills
  - combat
  - world
  - targeting
  - alchemy
  - notifications
- Sau `EnterWorld`, client bootstrap-load:
  - inventory
  - martial arts
  - skills
- World runtime snapshot và observed spawn/despawn packets cập nhật world state độc lập với UI.

Source: `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/Character/Application/ClientCharacterService.cs`, `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Application/ClientWorldService.cs`, `docs/client-unity/client-state-sync-rules.md`

## World scene composition

- `WorldSceneController` là root scene orchestrator.
- Nó inject context cho presenter hierarchy:
  - `WorldMapPresenter`
  - `WorldLocalPlayerPresenter`
  - `WorldLocalMovementSyncController`
  - `WorldRemotePlayersPresenter`
  - `WorldEnemiesPresenter`
  - target selection/action controllers
  - portal and ground reward presenters
- Scene có readiness service để các presenter biết khi map/local player đã sẵn sàng.

Source: `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/WorldSceneController.cs`

## Player input

- `LocalCharacterActionController` xử lý input local, movement presentation, facing, grounded/fly/fall animation state.
- `WorldLocalMovementSyncController` đọc transform local player rồi gửi `CharacterPositionSyncPacket`.
- `WorldClickTargetSelectionController` và `WorldAutoTargetSelectionController` điều phối target selection.
- `WorldTargetActionController` thực hiện move-to-range rồi request attack/interaction.

Source: `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/Character/Presentation/LocalCharacterActionController.cs`, `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/WorldLocalMovementSyncController.cs`, `WorldTargetActionController.cs`

## Entity rendering

- Local player, remote players, enemies, portals, ground rewards đều có presenter riêng.
- Client chỉ map `clientMapKey -> map prefab`, enemy/world visuals, selection indicators, popup damage, VFX.
- Server snapshot là source of truth cho entity runtime existence và HP/state.

Source: `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/*.cs`, `ClientUnity/PhamNhanOnline/docs/game-design-client-overview.md`

## Skill visual / presentation

- Combat gameplay request nằm ở `ClientCombatService`.
- Skill visual runtime tách riêng qua:
  - `ClientSkillPresentationService`
  - `ClientPresentationReplicationService`
  - `WorldPresentationRuntimeController`
- `WorldPresentationRuntimeController.Update()` tick presentation services theo thời gian thực.

Source: `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/WorldPresentationRuntimeController.cs`, `docs/client-unity/skill-presentation/SKILL_PRESENTATION_PHASE1_PHASE2_GUIDE.md`

## UI screens / panels

### Login UI

- `LoginScreenController` hiển thị status text, connect button, create-character screen switch.
- Hiện đang hardcode test credential trong `Awake`, đây là dấu hiệu môi trường dev/test.

Source: `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/UI/Screens/Login/LoginScreenController.cs`

### Persistent world UI

- `PersistentWorldUIController` có quick menu button và next target button.
- Label của quick menu đổi giữa `Menu` và `Dong` theo trạng thái.

Source: `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/UI/World/PersistentWorldUIController.cs`

### World menu

- `WorldMenuController` có tabs:
  - `quest`
  - `inventory`
  - `stats`
  - `equipment`
  - `guild`
- Chỉ `stats` là có nội dung runtime rõ; `quest`, `inventory`, `equipment`, `guild` trong tab text gốc còn mang tính placeholder/explanatory.

Source: `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/UI/World/WorldMenuController.cs`

### Inventory UI

- `WorldInventoryPanelController` dùng `InventoryItemGridView`, item popup options, quantity popup, tooltip.
- Supported use actions nhìn từ client popup hiện tại:
  - `Equipment`
  - `MartialArtBook`
  - `Consumable`
  - `PillRecipeBook`
- `Talisman` có method placeholder nhưng option build hiện không show use cho type này.

Source: `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/UI/World/WorldInventoryPanelController.cs`, `WorldInventoryPanelController.ItemActions.cs`

### Equipment / summary UI

- Có `WorldCharacterEquipController`, `CharacterEquipmentLoadoutView`, `CharacterSummaryView`.
- Đây là presentation layer của state equip/final stats, không quyết định gameplay.

Source: `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/UI/Inventory/*.cs`, `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/UI/World/WorldCharacterEquipController.cs`

### Cultivation UI

- `WorldCultivationPanelController` là panel gameplay hoàn chỉnh hơn.
- Hỗ trợ:
  - active martial art slot
  - martial art list
  - start/stop cultivation
  - breakthrough
  - reward popup khi settle online
- Client khóa close panel khi đang cultivating.

Source: `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/UI/World/WorldCultivationPanelController.cs`

### Potential UI

- `WorldPotentialPanelController` hiển thị preview-based allocate options.
- Nó không tự tính rule; chỉ đọc preview trong `CharacterBaseStatsModel`.
- Popup cho user chọn mức spend potential gần như 1, 10, 100... tùy affordability.

Source: `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/UI/World/WorldPotentialPanelController.cs`

### Skill UI / HUD

- Skill loadout panel: `WorldSkillPanelController`, `WorldSkillPanelView`, `SkillLoadoutSlotsView`.
- Combat HUD: `WorldCombatHudController` và `CombatSkillButtonView`.
- Slot 1 được treat như basic skill trong target-action path.

Source: `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/UI/World/WorldSkillPanelController.cs`, `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/UI/Hud/WorldCombatHudController.cs`, `WorldTargetActionController.Execution.cs`

### Alchemy / crafting UI

- `WorldCraftingPanelController` là panel lớn nhất hiện tại.
- Alchemy flow có:
  - recipe list
  - recipe detail
  - drag/drop input
  - preview
  - quantity popup
  - active practice session display
  - pause/resume/cancel
- `Smithing` và `Talisman` chỉ show unsupported/placeholder state.

Source: `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/UI/World/WorldCraftingPanelController.cs`

### Zone UI

- `WorldMapZonePanelController` query zone list, hiển thị occupancy color, switch zone.
- Chỉ active khi map hiện tại hỗ trợ zone switching.

Source: `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/UI/World/WorldMapZonePanelController.cs`

### Notifications / modal popup

- `WorldModalUIManager` là trung tâm popup.
- `NotificationInboxController` hiển thị unread notifications và acknowledge.
- Death popup và connection recovery popup đều đi qua modal manager.

Source: `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/UI/Common/NotificationInboxController.cs`, `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/UI/World/WorldCombatDeathController.cs`, `WorldConnectionRecoveryController.cs`

## Visible / hidden UI behavior

- World menu bị hide khi disconnect hoặc local character defeated.
- Cultivation panel có thể khóa close trong lúc cultivating.
- Crafting panel auto-open lại sau login nếu có alchemy practice session active/paused/result-pending.
- Quantity popup trong inventory intentionally không auto-close qua polling, chỉ close từ explicit action flow.
- Quick menu button label đổi theo `WorldUIController.IsAnyMenuOpen`.

Source: `WorldMenuController.cs`, `WorldCultivationPanelController.cs`, `WorldUIController.cs`, `WorldInventoryPanelController.cs`

## Error display

- Login scene: status text + popup mất kết nối.
- World connection recovery: modal popup với status/reconnect info.
- Combat death: modal popup yêu cầu return home.
- Inventory/cultivation/potential/crafting panels đều giữ `lastStatusMessage` riêng để hiển thị lỗi/action result.

Source: `LoginScreenController.cs`, `WorldConnectionRecoveryController.cs`, `WorldCombatDeathController.cs`, `WorldInventoryPanelController.cs`, `WorldPotentialPanelController.cs`, `WorldCraftingPanelController.cs`

## UI đã có nhưng chưa nối logic đầy đủ

- Quest tab placeholder.
- Guild tab placeholder.
- Inventory/equipment tabs text placeholder trong world menu gốc.
- Smithing panel placeholder.
- Talisman panel placeholder.
- Local home station portals mở được panel nhưng không đại diện cho server gameplay content hoàn chỉnh.

Source: `WorldMenuController.cs`, `WorldCraftingPanelController.cs`, `LocalFixPortalPresenter.cs`

## Logic client tự quyết định có rủi ro cheat

- Client local movement/presentation tự di chuyển transform trước khi server settle; cheat impact được giảm nhờ server clamp nhưng presentation vẫn optimistic.
- Client target-action controller tự move-to-range và force sync trước khi cast; gameplay result vẫn do server chốt.
- Client decides UI availability for some actions, nhưng server vẫn là gate cuối. Rủi ro chính là UX mismatch, không phải authoritative breach lớn.

Source: `WorldLocalMovementSyncController.cs`, `WorldTargetActionController.cs`, `GameServer/Runtime/GameLoop.cs`, `GameServer/Runtime/WorldInteractionGate.cs`

## Unknown / Need confirmation

- Scene asset names và exact hierarchy đầy đủ cần mở trực tiếp trong Unity để xác nhận beyond code references.
- Chưa thấy production-ready character selection UI cho trường hợp nhiều character.
- Chưa thấy player-facing farming/cave UI dù server data layer đã có.

Source: repo code audit hiện tại
