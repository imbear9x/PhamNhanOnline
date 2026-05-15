---
title: Reviewer Fix Response — Bag Capacity Precheck Race
doc_type: handoff
status: Done
owner: reviewer
source_agent: dev
last_updated: 2026-05-15
source_design_doc: docs/game-design-wp/requirements/inventory-bag-system.md
source_tech_design_doc: docs/tech-design/inventory-bag-system.md
expected_output: review
---

# Goal

Phản hồi Reviewer cho Required Fix `reviewer-bag-capacity-precheck-race`: loại bỏ TOCTOU giữa pre-check bag capacity và grant item thực tế ở active flows.

# What Was Fixed

## 1) Ground reward pickup

Đã chuyển `capacity check` vào **cùng** `_inventoryTransactions.ExecuteAsync(...)` với bước move item vào inventory.

Cụ thể:
- `PickupGroundRewardHandler` không còn check capacity ở ngoài lock nữa.
- Bên trong inventory transaction/lock:
  - build grants từ `reward.Items`
  - gọi `_bagService.CheckCapacityForAsync(...)`
  - nếu full → throw `GameException(MessageCode.InventoryFull)`
  - nếu fit → gọi helper unlocked của `ItemService` để move từng ground item vào inventory mà không mở nested lock path mới

Hiệu quả:
- cùng một character không còn cửa sổ để request khác chen vào giữa lúc check và lúc mutate.
- claim runtime vẫn được cancel khi transaction fail.

## 2) Herb harvest

Đã chuyển harvest sang chạy dưới `_inventoryTransactions.ExecuteAsync(...)` cho toàn bộ phần active award + herb state mutation.

Cụ thể bên trong lock:
- re-load herb ownership/state
- materialize progress lại
- resolve outputs lại
- check capacity cho guaranteed outputs
- grant created items
- clear plot link nếu có
- delete harvested herb

Hiệu quả:
- không còn pre-check capacity bên ngoài critical section.
- active reject `InventoryFull` xảy ra trước mutate inventory/herb state trong cùng boundary.

## 3) Tránh circular dependency

Một hướng sửa tạm thời đã đụng nguy cơ vòng phụ thuộc:
- `BagService -> ItemService`
- `ItemService -> BagService`

Hướng cuối cùng đã **không** inject `BagService` vào `ItemService`.
Thay vào đó:
- giữ `ItemService` không biết về bag capacity authority
- thêm helper core `MoveGroundItemToInventoryCoreUnlockedAsync(...)` để caller đã giữ inventory lock có thể dùng trực tiếp
- authority check vẫn nằm ở caller active flow trong cùng lock boundary

# Files Touched

- `GameServer/Network/Handlers/PickupGroundRewardHandler.cs`
- `GameServer/Services/HerbService.cs`
- `GameServer/Services/ItemService.cs`

# Build / Verification

Focused build:
- `dotnet build GameServer/GameServer.csproj -v minimal` ✅ pass

Observed warning only:
- `CS8032` from local analyzer load (`Humanizer.Analyzers` / missing `System.Collections.Immutable 9.0.0.0`) — non-blocking.

Không có repro concurrency tự động trong lượt này; fix được xác nhận ở mức code path + lock boundary + compile.

# Reviewer Verification Focus

1. `PickupGroundRewardHandler`
- xác nhận `CheckCapacityForAsync` nằm trong `_inventoryTransactions.ExecuteAsync(...)`
- xác nhận move item dùng helper core không tự mở race window mới
- xác nhận fail `InventoryFull` vẫn cancel runtime claim đúng

2. `HerbService.HarvestHerbAsync`
- xác nhận check capacity và item grant đều ở trong cùng inventory lock
- xác nhận herb/plot mutate chỉ diễn ra sau check trong cùng critical section

3. Invariant
- dưới concurrency cho cùng player, `usedSlots <= totalSlots` vẫn giữ cho 2 flow reviewer nêu.

# Known Limits

- Fix này chỉ nhắm các path reviewer yêu cầu ngay: ground reward pickup và herb harvest.
- Chưa biến mọi grant path toàn server thành authoritative bag-capacity API chung.
- Herb path hiện vẫn pre-check guaranteed outputs theo policy đã có trước; không mở rộng sang exhaustive simulation cho mọi tổ hợp output random trong lượt này.
