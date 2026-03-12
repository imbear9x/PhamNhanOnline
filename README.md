# PhamNhanOnline

## Dev Setup (New Machine)

1. Install:
- VS Code
- .NET SDK `8.0.303` (pinned in `global.json`)
- Git

2. Clone project:
```powershell
git clone <your-repo-url>
cd PhamNhanOnline
```

3. One-command setup:
```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\dev-setup.ps1
```

4. Open in VS Code:
```powershell
code .
```

## VS Code Config Included In Git

- Workspace debug/tasks:
  - `.vscode/launch.json`
  - `.vscode/tasks.json`
  - `.vscode/settings.json`
  - `.vscode/extensions.json`
- Standalone client debug (open only `CientTest/TestClient`):
  - `CientTest/TestClient/.vscode/launch.json`
  - `CientTest/TestClient/.vscode/tasks.json`

## Run/Debug

- Run both server + client in one workspace:
  - VS Code Run and Debug -> `GameServer + TestClient`
- Run only client in separate window:
  - `code -n .\CientTest\TestClient`
  - Press `F5` with `TestClient: Launch`
