# UI Refactor 2026-04-03

## Mục tiêu

Pass này xử lý 2 việc:

1. Tách các controller UI lớn để giảm file đơn khối.
2. Dọn naming/docs/tooling ở phạm vi an toàn, không rename path lớn có rủi ro cao.

## Các controller đã tách

### Inventory

File gốc sau refactor:
- [WorldInventoryPanelController.cs](/F:/PhamNhanOnline/ClientUnity/PhamNhanOnline/Assets/Game/Runtime/UI/World/WorldInventoryPanelController.cs)

Partial mới:
- [WorldInventoryPanelController.ItemActions.cs](/F:/PhamNhanOnline/ClientUnity/PhamNhanOnline/Assets/Game/Runtime/UI/World/WorldInventoryPanelController.ItemActions.cs)
- [WorldInventoryPanelController.ViewState.cs](/F:/PhamNhanOnline/ClientUnity/PhamNhanOnline/Assets/Game/Runtime/UI/World/WorldInventoryPanelController.ViewState.cs)

Phân rã trách nhiệm:
- file gốc: lifecycle, refresh runtime, inventory reload
- `ItemActions`: click/hover/drop/equip/use item/popup actions
- `ViewState`: render state, tooltip, popup visibility, snapshot helper

### Martial arts

File gốc sau refactor:
- [WorldMartialArtPanelController.cs](/F:/PhamNhanOnline/ClientUnity/PhamNhanOnline/Assets/Game/Runtime/UI/World/WorldMartialArtPanelController.cs)

Partial mới:
- [WorldMartialArtPanelController.Actions.cs](/F:/PhamNhanOnline/ClientUnity/PhamNhanOnline/Assets/Game/Runtime/UI/World/WorldMartialArtPanelController.Actions.cs)
- [WorldMartialArtPanelController.ViewState.cs](/F:/PhamNhanOnline/ClientUnity/PhamNhanOnline/Assets/Game/Runtime/UI/World/WorldMartialArtPanelController.ViewState.cs)

Phân rã trách nhiệm:
- file gốc: lifecycle, refresh panel, reload trigger
- `Actions`: set active, clear active, cultivation, breakthrough, action state
- `ViewState`: render view, status, estimate, snapshot helper

## Tooling / docs cleanup

- bổ sung script [verify-solution-build.ps1](/F:/PhamNhanOnline/scripts/verify-solution-build.ps1)
- dọn và cập nhật lại [WORKING_CONTEXT.md](/F:/PhamNhanOnline/docs/WORKING_CONTEXT.md)
- thêm [UNITY_TOOLING_NOTES.md](/F:/PhamNhanOnline/docs/UNITY_TOOLING_NOTES.md)

## Những gì chưa làm trong pass này

- chưa rename path lớn như `CientTest`
- chưa tách tiếp `WorldPotentialPanelController` hoặc `WorldSkillPanelController`
- chưa đổi logic gameplay của các panel, chỉ đổi cấu trúc file

## Rollback

### Inventory UI

Khôi phục monolith:
- restore [WorldInventoryPanelController.cs](/F:/PhamNhanOnline/ClientUnity/PhamNhanOnline/Assets/Game/Runtime/UI/World/WorldInventoryPanelController.cs)
- xóa:
  - [WorldInventoryPanelController.ItemActions.cs](/F:/PhamNhanOnline/ClientUnity/PhamNhanOnline/Assets/Game/Runtime/UI/World/WorldInventoryPanelController.ItemActions.cs)
  - [WorldInventoryPanelController.ViewState.cs](/F:/PhamNhanOnline/ClientUnity/PhamNhanOnline/Assets/Game/Runtime/UI/World/WorldInventoryPanelController.ViewState.cs)
  - `.meta` tương ứng

### MartialArt UI

Khôi phục monolith:
- restore [WorldMartialArtPanelController.cs](/F:/PhamNhanOnline/ClientUnity/PhamNhanOnline/Assets/Game/Runtime/UI/World/WorldMartialArtPanelController.cs)
- xóa:
  - [WorldMartialArtPanelController.Actions.cs](/F:/PhamNhanOnline/ClientUnity/PhamNhanOnline/Assets/Game/Runtime/UI/World/WorldMartialArtPanelController.Actions.cs)
  - [WorldMartialArtPanelController.ViewState.cs](/F:/PhamNhanOnline/ClientUnity/PhamNhanOnline/Assets/Game/Runtime/UI/World/WorldMartialArtPanelController.ViewState.cs)
  - `.meta` tương ứng

### Docs / tooling

Có thể rollback độc lập:
- [WORKING_CONTEXT.md](/F:/PhamNhanOnline/docs/WORKING_CONTEXT.md)
- [UNITY_TOOLING_NOTES.md](/F:/PhamNhanOnline/docs/UNITY_TOOLING_NOTES.md)
- [verify-solution-build.ps1](/F:/PhamNhanOnline/scripts/verify-solution-build.ps1)

## Verify

```powershell
powershell -File .\scripts\verify-solution-build.ps1
```
