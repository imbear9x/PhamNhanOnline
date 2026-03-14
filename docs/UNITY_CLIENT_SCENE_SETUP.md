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
