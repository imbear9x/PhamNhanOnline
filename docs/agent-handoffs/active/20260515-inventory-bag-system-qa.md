---
title: Inventory Bag System — QA Verification Slice 1
doc_type: handoff
status: Ready
owner: qa
source_agent: dev
last_updated: 2026-05-15
source_design_doc: docs/game-design-wp/requirements/inventory-bag-system.md
source_tech_design_doc: docs/tech-design/inventory-bag-system.md
expected_output: review
---

# Goal

QA xác minh slice implementation hiện có của inventory bag system: bag persistence/config, default bag creation, bag state API, upgrade bag API, và inventory response có kèm bag state.

# Context To Keep

- Đây là **slice 1** của inventory bag system, chưa phải toàn bộ feature hoàn chỉnh theo TechDesign.
- Dev đã implement khung chính cho bag system và build server/shared thành công.
- Các phần **chưa hoàn tất** cần QA ghi rõ là out-of-scope hoặc known gap, không đánh fail sai phạm vi:
  - chưa nối đầy đủ inventory capacity enforcement cho mọi active action
  - chưa nối passive overflow sang inbox/notification path
  - failure-code mapping của bag upgrade mới ở mức cơ bản, chưa final polish
- Bag không phải item, không có `bag_id` trên `player_items`.
- Mỗi character phải có đúng 1 row trong `player_bags`.
- Character mới phải có bag grade 1.
- `GetInventoryResultPacket` nay có thêm `BagState`.
- Có packet/handler riêng cho `GetBagState` và `UpgradeBag`.

# Confirmed Decisions

- Không thêm `bag_id` vào `player_items`.
- Không di chuyển row item khi upgrade bag.
- Upgrade bag dùng linh thạch dạng inventory item, trừ qua `ItemService.RemoveItemAsync`.
- Upgrade target phải cao hơn grade hiện tại.
- Tạo bag mặc định grade 1 khi tạo character.
- Dùng `PlayerInventoryTransactionService` để bọc transaction nâng cấp bag.

# Scope

1. Verify migration/schema mới:
   - `bag_grade_configs`
   - `player_bags`
   - seed grade 1..4
   - backfill bag grade 1 cho character cũ
2. Verify character mới tạo có bag grade 1.
3. Verify API lấy inventory trả thêm `BagState` đúng dữ liệu.
4. Verify `GetBagState` hoạt động đúng khi player đã vào world.
5. Verify `UpgradeBag`:
   - nâng cấp thành công khi đủ linh thạch và target grade hợp lệ
   - từ chối khi target grade không hợp lệ hoặc không cao hơn grade hiện tại
   - từ chối khi không đủ linh thạch
   - sau khi nâng cấp, grade thay đổi đúng và bag state phản ánh đúng total slots
6. Verify build/runtime cơ bản không vỡ packet handling cho inventory hiện có.

# Out Of Scope

- Verify full inventory-capacity rejection trên mọi active action
- Verify passive overflow sang inbox/notification
- UI layout/visual polish phía client
- Full NPC/shop flow liên quan nâng cấp bag
- Balance tuning cost/slot count

# Acceptance Criteria

- DB có đầy đủ `bag_grade_configs` và `player_bags` sau migrate/init.
- Dữ liệu seed bag grade có đủ 4 grade.
- Character mới tạo xong có đúng 1 row `player_bags` grade 1.
- Character cũ sau migrate có row `player_bags` grade 1 nếu trước đó chưa có.
- `GetInventory` trả `BagState` với các field:
  - `Grade`
  - `UsedSlots`
  - `TotalSlots`
  - `DisplayName`
- `GetBagState` trả đúng bag state cho player online/in-world.
- `UpgradeBag` grade 1 -> 2 thành công khi có đủ linh thạch, đồng thời:
  - trừ đúng số lượng linh thạch
  - cập nhật grade trong DB
  - response trả `BagState` mới
- `UpgradeBag` không cho upgrade cùng grade hoặc thấp hơn grade hiện tại.
- `UpgradeBag` không nâng cấp nếu thiếu linh thạch.
- Server không crash khi gọi `GetInventory`, `GetBagState`, `UpgradeBag` theo flow hợp lệ.

# Test Cases

## TC01 — Migrate/init tạo đủ bảng bag
- Tiền điều kiện: DB sạch hoặc môi trường test mới.
- Bước test:
  1. Chạy init/migration hiện tại.
  2. Kiểm tra schema DB.
- Kỳ vọng:
  - có bảng `bag_grade_configs`
  - có bảng `player_bags`
  - có config `inventory.bag_upgrade_currency_code`

## TC02 — Seed bag grade config đủ 4 cấp
- Bước test:
  1. Query `bag_grade_configs`.
- Kỳ vọng:
  - có grade 1,2,3,4
  - mỗi grade có `slot_count`, `upgrade_cost_linh_thach`, `display_name`

## TC03 — Backfill bag cho character cũ
- Tiền điều kiện: có character được tạo trước migration, chưa có row `player_bags`.
- Bước test:
  1. Chạy migration.
  2. Query `player_bags` theo character cũ.
- Kỳ vọng:
  - có đúng 1 row tương ứng
  - grade = 1

## TC04 — Character mới tạo có bag mặc định
- Bước test:
  1. Tạo character mới.
  2. Query `player_bags`.
- Kỳ vọng:
  - có đúng 1 row cho character mới
  - grade = 1

## TC05 — GetInventory trả thêm BagState
- Bước test:
  1. Đăng nhập và enter world bằng character hợp lệ.
  2. Gọi `GetInventory`.
- Kỳ vọng:
  - packet response success
  - có `EquipmentSlotCount`
  - có `BagState`
  - `BagState.TotalSlots` khớp config grade hiện tại
  - `BagState.UsedSlots` không âm

## TC06 — GetBagState khi chưa enter world
- Bước test:
  1. Đăng nhập nhưng chưa enter world.
  2. Gọi `GetBagState`.
- Kỳ vọng:
  - fail với `CharacterMustEnterWorld`

## TC07 — GetBagState thành công khi đã vào world
- Bước test:
  1. Đăng nhập, enter world.
  2. Gọi `GetBagState`.
- Kỳ vọng:
  - success
  - `BagState.Grade = 1` với character mới chưa upgrade

## TC08 — UpgradeBag thất bại khi target grade <= current grade
- Bước test:
  1. Với character grade 1, gọi `UpgradeBag(targetGrade = 1)`.
- Kỳ vọng:
  - fail
  - grade DB không đổi
  - linh thạch không bị trừ

## TC09 — UpgradeBag thất bại khi target grade không tồn tại
- Bước test:
  1. Gọi `UpgradeBag(targetGrade = 999)`.
- Kỳ vọng:
  - fail
  - grade DB không đổi
  - linh thạch không bị trừ

## TC10 — UpgradeBag thất bại khi thiếu linh thạch
- Tiền điều kiện: character có ít linh thạch hơn cost grade target.
- Bước test:
  1. Gọi `UpgradeBag(targetGrade = 2)`.
- Kỳ vọng:
  - fail
  - grade DB không đổi
  - linh thạch không bị trừ

## TC11 — UpgradeBag thành công grade 1 -> 2
- Tiền điều kiện: character có đủ linh thạch item theo config.
- Bước test:
  1. Gọi `UpgradeBag(targetGrade = 2)`.
  2. Query DB `player_bags`.
  3. Query inventory currency trước/sau.
- Kỳ vọng:
  - success
  - `player_bags.grade = 2`
  - linh thạch bị trừ đúng cost
  - response có `RemainingLinhThach`
  - `BagState.TotalSlots` tăng theo config grade 2

## TC12 — GetInventory phản ánh bag state mới sau upgrade
- Tiền điều kiện: vừa upgrade thành công.
- Bước test:
  1. Gọi lại `GetInventory`.
- Kỳ vọng:
  - `BagState.Grade` là grade mới
  - `BagState.TotalSlots` là slot count mới

## TC13 — Hồi quy flow inventory cũ không vỡ
- Bước test:
  1. Gọi `GetInventory` với character có item sẵn.
  2. Equip/unequip 1 item nếu có thể.
- Kỳ vọng:
  - flow cũ vẫn hoạt động
  - không crash do bổ sung `BagState`

# Relevant Files Or Docs

- `docs/tech-design/inventory-bag-system.md`
- `docs/game-design-wp/requirements/inventory-bag-system.md`
- `GameServer/Services/BagService.cs`
- `GameServer/Services/CharacterService.cs`
- `GameServer/Network/Handlers/GetInventoryHandler.cs`
- `GameServer/Network/Handlers/GetBagStateHandler.cs`
- `GameServer/Network/Handlers/UpgradeBagHandler.cs`
- `GameShared/Packets/Packets/CharacterPackets.cs`
- `GameShared/Models/BagStateModel.cs`
- `database/migrations/20260515_add_inventory_bag_system.sql`
- `database/initDatabase.sql`

# Open Questions / Blockers

- Slice hiện chưa phủ full acceptance của TechDesign, nhất là active-capacity enforcement và passive overflow.
- Nếu QA test các phần ngoài scope trên, cần ghi nhận là known gap của implementation slice hiện tại, không phải regression ngoài mô tả.
- Cần xác nhận môi trường test có sẵn cách seed linh thạch inventory item cho character để test upgrade success/fail.

# Recommended Next Step

QA chạy test cho slice 1, báo rõ:
- pass/fail từng test case
- log packet/DB evidence ngắn gọn cho case fail
- regression nào chặn merge
- issue nào chỉ là gap do scope chưa xong

# Completion Output

QA tạo báo cáo ngắn gồm:
- danh sách test case pass/fail/not-run
- bug hoặc regression phát hiện được
- mức độ blocker/non-blocker
- đề xuất trả lại `dev` nếu có lỗi code hoặc `manager` nếu cần điều phối scope tiếp
