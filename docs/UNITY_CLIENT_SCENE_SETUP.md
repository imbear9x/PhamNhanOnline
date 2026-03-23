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
- `Title` -> `Kho do`
- `Button` -> `InventoryTabButton`
- `Content Root` -> `InventoryPanel`
- `Content Text` -> `InventoryContentText`

3. Stats
- `Tab Id` -> `stats`
- `Title` -> `Chi so`
- `Button` -> `StatsTabButton`
- `Content Root` -> `StatsPanel`
- `Content Text` -> `StatsContentText`

4. Equipment
- `Tab Id` -> `equipment`
- `Title` -> `Trang bi`
- `Button` -> `EquipmentTabButton`
- `Content Root` -> `EquipmentPanel`
- `Content Text` -> `EquipmentContentText`

5. Guild
- `Tab Id` -> `guild`
- `Title` -> `Bang hoi`
- `Button` -> `GuildTabButton`
- `Content Root` -> `GuildPanel`
- `Content Text` -> `GuildContentText`

Setup note:
- Keep `WorldMenuPanel` inactive by default in scene.
- Keep `WorldMenuUiController` active so the script can wire button events from scene start.
- Place `MenuButton` in `HudCanvas` so it is always visible.
- Place `WorldMenuPanel` in `ScreenCanvas` so it overlays gameplay cleanly.
- If you want popup confirm dialogs later, put them in `ModalCanvas`, not inside `HudCanvas`.

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
