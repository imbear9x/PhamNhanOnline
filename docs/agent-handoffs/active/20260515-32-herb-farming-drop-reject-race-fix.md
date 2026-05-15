# Dev Handoff - Herb Enemy Direct-Grant Reject-On-Full Must Check Capacity Inside Same Inventory Lock

- Owner: dev
- Created by: reviewer
- Status: Done
- Severity: Required Fix
- Source review: row #31 `herb-farming-drop-reject-fix-reviewer`
- Related feature/handoff: `docs/agent-handoffs/active/20260515-31-herb-farming-drop-reject-fix-reviewer.md`
- Related files:
  - `GameServer/Runtime/EnemyRewardRuntimeService.cs`
  - `GameServer/Services/BagService.cs`
  - `GameServer/Services/PlayerInventoryTransactionService.cs`

## Problem

Fix hiện tại cho herb enemy direct-grant vẫn còn **TOCTOU race** giữa `CheckCapacityForAsync(...)` và `GrantDirectRewardItemsAsync(...)`.

Code hiện làm:
1. ngoài inventory transaction/lock: tách `herbDirectGrantItems`
2. ngoài inventory transaction/lock: `bagService.CheckCapacityForAsync(...)`
3. nếu fit thì sau đó mới gọi `GrantDirectRewardItemsAsync(...)`
4. `GrantDirectRewardItemsAsync(...)` mới vào `PlayerInventoryTransactionService.ExecuteAsync(...)`

Như vậy check và grant không nằm trong cùng serialization boundary.

## Why It Matters

- Cùng player có thể nhận 2 reward events gần nhau hoặc song song.
- Cả hai path đều check `CanFit = true` trước lock.
- Path A grant xong chiếm slot; path B vẫn grant theo check cũ → vượt authority “full bag thì reject herb reward”.
- Đây là đúng class bug reviewer đã fail trước đó ở bag/herb active-action paths: pre-check outside lock.
- Khi authority mới của correction round là **reject herb direct-grant nếu túi đầy**, path hiện tại vẫn có cửa grant herb sau stale check.

## Evidence

- `GameServer/Runtime/EnemyRewardRuntimeService.cs`
  - `ProcessPendingEventsAsync(...)`:
    - `bagService.CheckCapacityForAsync(...)` chạy trước
    - `GrantDirectRewardItemsAsync(...)` mới acquire inventory transaction/lock sau đó
- `GrantDirectRewardItemsAsync(...)`:
  - chỉ wrap `ItemService.AddItemAsync(...)` trong `inventoryTransactions.ExecuteAsync(...)`
  - không tự re-check capacity cho herb items bên trong lock
- Expected technical behavior:
  - herb direct-grant reject/full decision phải được compute **trong cùng inventory lock/transaction boundary** với actual grant của player đó
- Actual behavior:
  - check outside lock, grant inside lock

## Required Change

Chuyển herb direct-grant capacity decision vào cùng `PlayerInventoryTransactionService.ExecuteAsync(...)` boundary với actual item grant cho cùng player.

Một hướng hợp lý:
- trong 1 callback `inventoryTransactions.ExecuteAsync(playerId, ...)`:
  - partition `directGrantItems` thành herb / non-herb
  - run `BagService.CheckCapacityForAsync(...)` cho herb set **bên trong callback**
  - nếu không fit:
    - signal `InventoryFull`
    - bỏ herb items khỏi grant set
  - grant phần còn lại ngay trong cùng callback

Hoặc refactor `GrantDirectRewardItemsAsync(...)` để nhận callback/option xử lý herb check ngay trước grant, miễn sao authority decision và mutation cùng lock.

## Acceptance Criteria

- [ ] Không còn path herb enemy direct-grant check capacity outside inventory lock rồi grant inside lock.
- [ ] Herb direct-grant reject/full decision và grant còn lại chạy trong cùng `PlayerInventoryTransactionService.ExecuteAsync(...)` boundary cho cùng player.
- [ ] Khi 2 reward events cạnh tranh trên cùng player, không có path stale pre-check làm herb item vẫn được grant dù túi đã đầy.
- [ ] `InventoryFull` signal vẫn được gửi khi herb direct-grant bị reject.
- [ ] Không tạo inbox fallback hoặc ground-drop workaround.
- [ ] Build pass, 0 error.

## Verification Scope

- Review lại `EnemyRewardRuntimeService.ProcessPendingEventsAsync(...)`
- Confirm `BagService.CheckCapacityForAsync(...)` được gọi bên trong same inventory transaction/lock path với herb grant decision
- Focused build `dotnet build GameServer/GameServer.csproj -v minimal`

## Out Of Scope

- Không yêu cầu redesign packet signal cho enemy reward path ở lượt này.
- Không yêu cầu broad-fix toàn bộ non-herb direct-grant reward authority.
- Không yêu cầu thay đổi baseline harvest/extract herb flows đã pass QA trước đó.

## Notes

- Reuse `PickupGroundRewardResultPacket` hiện tại được xem là risk chấp nhận được trong correction round này; blocker duy nhất là race authority giữa check và grant.
