# Dev Handoff - Race giữa pre-check capacity và grant thực tế có thể làm overflow bag

- Owner: dev
- Created by: reviewer
- Status: Ready
- Severity: Required Fix
- Source review: reviewer technical review ngày 2026-05-15 trên working tree inventory bag follow-up chưa commit
- Related feature/handoff: `docs/agent-handoffs/active/20260515-inventory-bag-system-reviewer.md`
- Related files:
  - `GameServer/Network/Handlers/PickupGroundRewardHandler.cs`
  - `GameServer/Services/HerbService.cs`
  - `GameServer/Services/CraftService.cs`
  - `GameServer/Services/BagService.cs`
  - `GameServer/Services/ItemService.cs`
  - `GameServer/Services/PlayerInventoryTransactionService.cs`

## Problem

Một số flow mới đang check capacity ở thời điểm chưa giữ inventory lock, rồi mới grant item ở bước sau. Vì grant path hiện không tự enforce bag capacity ở tầng `ItemService`, khoảng hở giữa pre-check và grant có thể bị request khác chèn thêm item, khiến server vẫn add item thành công và bag vượt slot giới hạn.

Flow bị ảnh hưởng rõ nhất trong diff hiện tại:
- `PickupGroundRewardHandler`
- `HerbService.HarvestHerbAsync`

`CraftService` check capacity bên trong `PlayerInventoryTransactionService.ExecuteAsync`, nên path đó an toàn hơn và là mẫu đúng cho active-reject flow.

## Why It Matters

- Đây là race condition runtime thật trên inventory authority của server.
- Người chơi có thể gặp trạng thái bag vượt `TotalSlots`, làm hỏng invariant cốt lõi của feature bag.
- Vì overflow xảy ra sau khi action đã commit, QA rất khó bắt nếu không test concurrency; production sẽ thành bug ngẫu nhiên, khó debug.
- Với ground reward, race còn tạo mismatch giữa runtime claim logic và inventory state nếu nhiều action đồng thời cùng thêm đồ.

## Evidence

- `PickupGroundRewardHandler` gọi `TryBeginGroundRewardClaim(...)`, sau đó `BagService.CheckCapacityForAsync(...)`, nhưng check này chạy **ngoài** `PlayerInventoryTransactionService.ExecuteAsync(...)`.
- Ngay sau pre-check, handler mới vào `_inventoryTransactions.ExecuteAsync(...)` và bên trong chỉ gọi `_itemService.MoveGroundItemToInventoryAsync(...)`; path move này không re-check bag capacity.
- `HerbService.HarvestHerbAsync` gọi `_bagService.CheckCapacityForAsync(...)` trước `BeginTransactionAsync(...)` và trước bất kỳ inventory advisory lock nào.
- `ItemService.AddItemAsync` / `MoveGroundItemToInventoryAsync` hiện xử lý stack merge/create nhưng không enforce giới hạn bag slot.
- `PlayerInventoryTransactionService` chỉ đảm bảo serial hóa theo player **sau khi** caller đã vào `ExecuteAsync(...)`; pre-check ngoài vùng lock không được bảo vệ.

## Required Change

- Với mọi active flow dùng contract “full bag thì reject, không grant”, phải đưa capacity check vào cùng critical section với grant item:
  - hoặc bọc cả check + consume/grant trong `PlayerInventoryTransactionService.ExecuteAsync(...)`,
  - hoặc thêm một tầng grant API authoritative có kiểm tra capacity ngay trước mutate inventory khi đang giữ lock.
- Rà lại riêng các path trong diff này:
  - `PickupGroundRewardHandler`
  - `HerbService.HarvestHerbAsync`
- Đảm bảo sau khi sửa, không còn cửa sổ TOCTOU giữa `CheckCapacityForAsync` và mutate inventory.
- Nếu cần, thống nhất pattern để các flow khác không lặp lại bug này.

## Acceptance Criteria

- [ ] Ground reward pickup không thể làm bag overflow khi có request grant item đồng thời trên cùng character.
- [ ] Herb harvest không thể làm bag overflow khi có request grant item đồng thời trên cùng character.
- [ ] Capacity check và inventory mutation của các flow active reject diễn ra dưới cùng inventory lock/transaction boundary.
- [ ] Sau khi sửa, invariant `usedSlots <= totalSlots` vẫn giữ được dưới concurrency cho các flow đã hook.

## Verification Scope

- Build `GameServer` pass.
- Repro concurrency tối thiểu:
  - character gần full bag,
  - 2 request đồng thời cùng có thể thêm item,
  - xác nhận một request bị reject hoặc reward được giữ lại đúng contract, không có overflow slot.
- Kiểm tra log không còn exception/mismatch bất thường ở path ground reward và herb harvest.

## Out Of Scope

- Thiết kế inbox claim item đầy đủ cho passive rewards.
- Hook capacity enforcement cho mọi grant path toàn server ngoài các flow reviewer đang yêu cầu fix ngay.
- Cân bằng bag grade / cost.

## Notes

- Đây là Required Fix vì đụng trực tiếp server authority + race condition của inventory/bag invariant.