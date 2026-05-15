---
title: Reviewer Response — Herb Farming Drop Reject Race Fix
doc_type: handoff
status: Done
owner: reviewer
source_agent: dev
last_updated: 2026-05-15
source_design_doc: docs/game-design-wp/requirements/herb-farming-system.md
source_tech_design_doc: docs/tech-design/herb-farming-system.md
expected_output: review
queue_id: 33
feature_key: herb-farming-system
handoff_type: required-fix-response
source_handoff: docs/agent-handoffs/active/20260515-32-herb-farming-drop-reject-race-fix.md
response_to: docs/agent-handoffs/active/20260515-32-herb-farming-drop-reject-race-fix.md
supersedes: docs/agent-handoffs/active/20260515-31-herb-farming-drop-reject-fix-reviewer.md
iteration: 2
---

# Goal

Reviewer re-check required fix `#32`: herb enemy direct-grant reject/full decision phải nằm trong cùng inventory lock/transaction boundary với actual grant để loại TOCTOU race.

# Fix Summary

Đã refactor `GameServer/Runtime/EnemyRewardRuntimeService.cs` để bỏ pre-check ngoài lock.

## Trước fix

`ProcessPendingEventsAsync(...)` làm:
1. tách `herbDirectGrantItems`
2. gọi `bagService.CheckCapacityForAsync(...)` **ngoài** inventory transaction
3. nếu fit thì gọi `GrantDirectRewardItemsAsync(...)`
4. `GrantDirectRewardItemsAsync(...)` mới vào `PlayerInventoryTransactionService.ExecuteAsync(...)`

=> check và mutation khác boundary, còn stale pre-check race.

## Sau fix

Đã chuyển toàn bộ herb capacity decision vào bên trong `GrantDirectRewardItemsAsync(...)`, và method này chạy toàn bộ logic bên trong cùng một `PlayerInventoryTransactionService.ExecuteAsync(...)` callback cho player đó:

1. vào inventory transaction/lock cho player
2. partition `rolledItems` thành herb / non-herb trong callback
3. gọi `BagService.CheckCapacityForAsync(...)` cho herb set **bên trong callback lock**
4. nếu không fit:
   - đánh dấu `shouldNotifyInventoryFull = true`
   - remove herb items khỏi `itemsToGrant`
5. grant phần còn lại ngay trong cùng callback lock bằng `ItemService.AddItemAsync(...)`
6. sau khi transaction xong, nếu có reject herb thì gửi:
   - `PickupGroundRewardResultPacket`
   - `Code = MessageCode.InventoryFull`
   - `RewardId = null`

## Authority result

- Không còn path herb direct-grant check outside lock rồi grant inside lock.
- Herb full/reject decision và actual grant còn lại nay cùng serialization boundary cho cùng player.
- Không thêm inbox fallback.
- Không tạo ground-drop workaround.
- Không broaden sang non-herb direct-grant authority ngoài phạm vi required fix.

# Files / Modules Touched

- `GameServer/Runtime/EnemyRewardRuntimeService.cs`

# Build / Test Result

Focused build đã chạy:
- `dotnet build GameServer/GameServer.csproj -v minimal`
- Result: **pass**, `0 error`

Observed warnings:
- nhiều warning generated `CS8629` trong `GameShared/Generated/PacketGenerator/...`
- `CS8032` từ `Humanizer.Analyzers.NamespaceMigrationAnalyzer` do thiếu `System.Collections.Immutable, Version=9.0.0.0`
- đều là warning sẵn có / ngoài scope required fix này

# DB / Schema / Seed Changes

- Không có.

# Packet / Broadcast / Runtime Changes

- Không thêm packet mới.
- Vẫn reuse `PickupGroundRewardResultPacket` + `MessageCode.InventoryFull` làm signal tối thiểu cho herb direct-grant reject.
- Runtime change chỉ là lock boundary của herb capacity decision.

# Verification Scope Completed By Dev

- đọc handoff `#32`
- audit lại `EnemyRewardRuntimeService`, `BagService`, `PlayerInventoryTransactionService`
- chuyển herb capacity check vào same inventory transaction callback với grant
- build verify compile toàn `GameServer`

# Reviewer Retest Guidance

1. Confirm `ProcessPendingEventsAsync(...)` không còn gọi `BagService.CheckCapacityForAsync(...)` cho herb direct-grant ở ngoài inventory lock.
2. Confirm `GrantDirectRewardItemsAsync(...)` nay là nơi thực hiện:
   - partition herb/non-herb
   - herb capacity decision
   - actual grant
   trong cùng `PlayerInventoryTransactionService.ExecuteAsync(...)` callback.
3. Confirm vẫn gửi `InventoryFull` signal khi herb set bị reject.
4. Confirm không inbox fallback, không ground-drop workaround.
5. Build pass.

# Known Gaps / Risks

- Vẫn reuse packet `PickupGroundRewardResultPacket` cho direct-grant reject signal; đây là accepted risk từ review trước, không phải blocker của lượt fix race này.
- Mixed reward set hiện vẫn cho non-herb direct grants đi tiếp nếu herb set bị reject; đây là chủ đích để tránh broad behavior regression ngoài authority correction scope.
