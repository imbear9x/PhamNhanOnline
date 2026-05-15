---
title: Reviewer Handoff — Inventory Bag Herb Random Output Capacity Fix
doc_type: handoff
status: Done
owner: reviewer
source_agent: dev
last_updated: 2026-05-15
source_design_doc: docs/game-design-wp/requirements/inventory-bag-system.md
source_tech_design_doc: docs/tech-design/inventory-bag-system.md
expected_output: review
queue_id: 15
feature_key: inventory-bag-system
handoff_type: implementation
source_handoff: docs/agent-handoffs/active/20260515-14-inventory-bag-herb-random-output-fix-dev.md
response_to: docs/agent-handoffs/active/20260515-14-inventory-bag-herb-random-output-fix-dev.md
iteration: 5
---

# Goal

Reviewer xác minh required fix cho `HarvestHerbAsync` theo TechDesign section **Random output handling in active actions**: roll toàn bộ output trước, check capacity trên full proc set, reject atomically nếu không fit.

# Implementation Summary

Đã sửa `GameServer/Services/HerbService.cs` trong `HarvestHerbAsync(...)` bên trong `_inventoryTransactions.ExecuteAsync(...)`:

1. Bỏ logic cũ chỉ build/check `guaranteedOutputs` (`OutputChance >= 1d`).
2. Thay bằng materialize `procOutputs` bằng cách roll toàn bộ `lockedOutputs` trước trong cùng inventory lock/transaction.
3. Nếu `procOutputs.Length > 0` thì gọi `_bagService.CheckCapacityForAsync(playerId, procOutputs, ct)`.
4. Nếu không fit thì `throw new GameException(MessageCode.InventoryFull)` trước khi grant item, clear plot link, hoặc delete herb.
5. Nếu fit thì grant toàn bộ `procOutputs` qua `_itemService.AddItemAsync(...)` rồi mới clear plot link và delete herb như cũ.
6. Nếu `procOutputs` rỗng thì skip capacity check/grant path và vẫn harvest thành công: herb bị xóa, plot bị clear.

# Files / Modules Touched

- `GameServer/Services/HerbService.cs`

Không thêm file code mới. Không đổi packet/handler. Không đổi schema/repository.

# Build / Test Result

Focused build đã chạy:
- `dotnet build GameServer/GameServer.csproj -v minimal`
- Result: **pass**, `0 error`

Observed environment warning:
- `CS8032` analyzer load warning từ `Humanizer.Analyzers.NamespaceMigrationAnalyzer`
- Message: thiếu assembly `System.Collections.Immutable, Version=9.0.0.0`
- Đây là warning môi trường/analyzer, không phát sinh từ diff logic của fix này.

Không có automated runtime test/repro script trong lượt này; verification hiện ở mức code path + compile.

# DB / Schema / Seed Changes

- Không có

# Packet / Broadcast / Runtime Contract Changes

- Không đổi packet IDs, packet payload, hay broadcast contract.
- Runtime behavior thay đổi ở `HarvestHerbAsync`:
  - random outputs được roll trước thành `procOutputs`
  - capacity authority áp lên full proc set của action
  - active action fail `InventoryFull` trước mọi state mutation nếu proc set không fit

# QA Notes / Retest Guidance

Reviewer nếu pass thì QA nên retest tối thiểu:
1. `HarvestHerb` full bag + có ít nhất 1 random output proc → fail `InventoryFull`, herb còn, plot còn, không có item grant.
2. `HarvestHerb` bag đủ chỗ + random outputs proc → grant đủ toàn bộ proc outputs, herb bị xóa, plot bị clear.
3. `HarvestHerb` 0 proc → action thành công, herb bị xóa, plot bị clear, không có item grant.
4. `HarvestHerb` full bag + 0 proc → action vẫn thành công, không overflow.
5. `PickupGroundReward` full bag → vẫn fail atomically, claim còn nguyên (non-regression).

# Test Scope Completed By Dev

- Đọc spec + handoff chain liên quan (`#13`, `#14`, TechDesign inventory bag spec section random output handling)
- Code inspection `HerbService.HarvestHerbAsync`
- Focused compile verification cho `GameServer`

# Known Gaps

- Chưa có automated unit/integration test riêng cho herb harvest random output capacity path.
- Chưa giải quyết warning analyzer môi trường `CS8032`; warning này có sẵn ngoài phạm vi logic fix.

# Risks / Blockers

- Không có blocker code hiện tại cho reviewer.
- Rủi ro còn lại chủ yếu là thiếu automated runtime coverage cho các tổ hợp random output khác nhau; cần reviewer/QA xác nhận behavior thực tế.
