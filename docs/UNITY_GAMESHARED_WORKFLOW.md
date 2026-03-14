# Unity GameShared Workflow

`GameShared` is the single source of truth for shared packets, models, message codes, and packet serialization contracts.

## Goals

- Server references `GameShared` directly as a project.
- Unity does **not** reference `GameServer`.
- Unity consumes the `netstandard2.1` build output of `GameShared`.
- We do not copy packet source files manually between projects.

## Current setup

- `GameShared` targets both `net8.0` and `netstandard2.1`
- `Mirror` has been removed from `GameShared`
- `PacketSerializer` no longer uses `dynamic`, which is safer for Unity/IL2CPP later
- Unity networking uses `LiteNetLib.dll` synced into `Assets/Plugins/LiteNetLib`

## Sync command

From the repo root, run:

```powershell
powershell -File .\scripts\sync-gameshared-to-unity.ps1
```

This will:

1. Build `GameShared` for `netstandard2.1`
2. Copy `GameShared.dll` into:
   `ClientUnity/PhamNhanOnline/Assets/Plugins/GameShared`
3. Copy `LiteNetLib.dll` into:
   `ClientUnity/PhamNhanOnline/Assets/Plugins/LiteNetLib`

## Team rule

When shared packets or shared models change:

1. Edit only `GameShared`
2. Build/sync with `sync-gameshared-to-unity.ps1`
3. Reopen or refresh Unity if needed

That keeps shared contracts centralized and avoids client/server drift.
