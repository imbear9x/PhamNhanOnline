# Dev Handoff - Bag slot counting sai với item hết hạn và tạo bag mặc định có race insert

- Owner: dev
- Created by: reviewer
- Status: Ready
- Severity: Required Fix
- Source review: reviewer technical review ngày 2026-05-15 trên diff inventory bag system trong working tree
- Related feature/handoff: `docs/agent-handoffs/active/20260515-inventory-bag-system-dev.md`
- Related files:
  - `GameServer/Services/BagService.cs`
  - `GameServer/Repositories/PlayerItemRepository.cs`
  - `GameServer/Repositories/PlayerBagRepository.cs`
  - `database/initDatabase.sql`

## Problem

Có 2 lỗi kỹ thuật quan trọng trong slice bag mới:

1. Server đang đếm `usedSlots` bằng toàn bộ row inventory, kể cả item đã hết hạn.
2. `EnsureDefaultBagAsync` check-then-insert ngoài transaction/lock riêng cho bag row, nên request đồng thời trên character chưa có bag có thể cùng insert và nổ unique violation.

## Why It Matters

- Sai `usedSlots` làm trạng thái túi trả về không đúng thực tế, dễ khóa nhầm người chơi ở trạng thái “đầy túi”, lệch với rule capacity trong TechDesign.
- Race tạo bag mặc định là lỗi runtime thật: ở login/enter world hoặc packet đồng thời đầu phiên, server có thể fail ngẫu nhiên thay vì self-heal tạo bag mặc định.
- Cả 2 lỗi đều ảnh hưởng trực tiếp tính ổn định feature nền inventory/bag, không nên đẩy QA/push khi chưa xử lý.

## Evidence

- `GameServer/Services/BagService.cs:75-80` lọc expired item khi check capacity (`inventory = inventory.Where(x => !IsExpired(x.ExpireAt)).ToList();`).
- Nhưng `GameServer/Services/BagService.cs:136-139` lại build `BagStateDto` bằng `CountInventoryActiveAsync`.
- `GameServer/Repositories/PlayerItemRepository.cs:71-74` hiện `CountInventoryActiveAsync` chỉ lọc `player_id` + `location_type = Inventory`, không loại item expired.
- TechDesign `docs/tech-design/inventory-bag-system.md` ghi rõ: “expired items do not count toward used slots”.
- `GameServer/Services/BagService.cs:47-62` đang `GetByPlayerIdAsync` rồi `CreateAsync` nếu null, không có upsert/idempotent path.
- `database/initDatabase.sql:1424-1429` đặt `player_bags.player_id` là PK, nên 2 luồng cùng insert sẽ đụng unique constraint.

## Required Change

- Sửa thống nhất rule “active inventory row” ở tầng repository/service:
  - `usedSlots` trong bag state phải loại item expired giống logic capacity check.
  - Nếu cần, tách method rõ nghĩa như `CountInventoryUsedSlotsAsync` / `ListInventoryActiveAsync` để tránh hiểu sai về sau.
- Làm `EnsureDefaultBagAsync` an toàn khi gọi đồng thời:
  - hoặc chạy trong transaction + player inventory/bag lock phù hợp,
  - hoặc dùng insert-if-not-exists / upsert idempotent ở repository,
  - hoặc bắt/nuốt unique violation có kiểm soát rồi re-read row.
- Rà lại tất cả call site lấy `BagState` / `GetInventory` / `GetBagState` để chắc `usedSlots` dùng cùng một nguồn logic.

## Acceptance Criteria

- [ ] `BagState.UsedSlots` không tính item inventory đã hết hạn.
- [ ] `CheckCapacityForAsync` và `GetBagStateAsync` dùng cùng rule đếm slot, không còn lệch logic.
- [ ] Gọi đồng thời tạo/default-load bag cho cùng character không làm request fail vì duplicate key trên `player_bags`.
- [ ] Character cũ chưa có bag vẫn được self-heal an toàn khi nhiều request đầu phiên cùng chạm vào bag service.

## Verification Scope

- Build server/shared sau khi sửa.
- Chạy test hoặc repro tay với character có item expired để xác nhận `UsedSlots` giảm đúng.
- Chạy repro đồng thời nhiều request `GetInventory`/`GetBagState` trên character chưa có row `player_bags` để xác nhận không còn duplicate-key failure.
- Nếu có log lỗi DB/exception mapping, kiểm tra log sạch trong case race.

## Out Of Scope

- Full capacity enforcement cho toàn bộ active/passive reward flow.
- Thiết kế NPC shop hoặc client UI.
- Balance cost bag upgrade.

## Notes

- Đây là Required Fix vì là lỗi nhất quán dữ liệu runtime + race condition ở feature nền inventory/bag.