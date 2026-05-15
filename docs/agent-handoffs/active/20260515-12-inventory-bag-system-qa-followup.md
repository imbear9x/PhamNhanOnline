---
title: Inventory Bag System — QA Follow-up After Reviewer TOCTOU Fix Pass
doc_type: handoff
status: Done
owner: qa
source_agent: reviewer
last_updated: 2026-05-15
source_design_doc: docs/game-design-wp/requirements/inventory-bag-system.md
source_tech_design_doc: docs/tech-design/inventory-bag-system.md
expected_output: verification
queue_id: 12
feature_key: inventory-bag-system
handoff_type: qa
source_handoff: docs/agent-handoffs/active/20260515-reviewer-bag-capacity-precheck-race-response.md
response_to: docs/agent-handoffs/active/20260515-inventory-bag-system-qa.md
supersedes: docs/agent-handoffs/active/20260515-inventory-bag-system-qa.md
iteration: 2
---

# Reviewer Verdict

**Pass with risks**

Reviewer đã verify follow-up fix cho Required Fix `reviewer-bag-capacity-precheck-race` và không thấy blocker còn lại trong phạm vi bug này.

# What Reviewer Verified

## 1) Ground reward pickup
- `PickupGroundRewardHandler` hiện gọi `_inventoryTransactions.ExecuteAsync(...)` bao toàn bộ đoạn:
  - materialize `ItemGrantRequest[]` từ `reward.Items`
  - `_bagService.CheckCapacityForAsync(...)`
  - move từng ground item vào inventory
- Move path dùng `ItemService.MoveGroundItemToInventoryCoreUnlockedAsync(...)`, nghĩa là caller đã giữ inventory transaction/lock không mở nested race window mới.
- Khi capacity fail, code throw `GameException(MessageCode.InventoryFull)`; outer catch gọi `instance.CancelGroundRewardClaim(...)`, nên runtime claim không bị consume mất.

## 2) Herb harvest
- `HerbService.HarvestHerbAsync(...)` hiện chạy dưới `_inventoryTransactions.ExecuteAsync(...)`.
- Bên trong critical section có:
  - re-load herb ownership/state
  - `MaterializeHerbProgressAsync(...)`
  - resolve output lại theo trạng thái đã lock
  - capacity check cho guaranteed outputs
  - add item reward
  - clear plot link
  - delete harvested herb
- Như vậy không còn cửa sổ TOCTOU giữa pre-check bag capacity và mutate inventory/herb state trên cùng player.

## 3) Transaction/lock behavior
- `PlayerInventoryTransactionService` dùng `pg_advisory_xact_lock(...)` trong transaction boundary cho từng player.
- Nếu đang có transaction sẵn thì service chỉ acquire cùng player inventory lock rồi chạy action; nếu chưa có thì tự mở transaction rồi commit sau action.
- `ItemService.AddItemAsync(...)` và helper move-ground path đều đi qua cùng inventory transaction service, nên nested inventory mutation trong cùng player vẫn nằm trong cùng serialization boundary.

## 4) Build verification
- `dotnet build GameServer/GameServer.csproj -v minimal` ✅ pass
- Lượt reviewer này không thấy warning/error build.

# Accepted Risks

## Risk 1 — Herb harvest chỉ pre-check guaranteed outputs
- Code hiện chỉ check capacity trước cho outputs có `OutputChance >= 1d`.
- Nếu herb có nhiều output ngẫu nhiên và nhiều output cùng proc, tổng slot thực tế phát sinh có thể lớn hơn pre-check guaranteed set.
- Đây **không phải** regression của fix TOCTOU vừa review, nhưng QA nên test các herb có reward ngẫu nhiên để xác định runtime behavior hiện tại có chấp nhận được không.

## Risk 2 — Phạm vi fix chưa biến mọi item-grant path thành authoritative capacity API chung
- Fix hiện đúng scope reviewer yêu cầu: ground reward pickup và herb harvest.
- Các flow grant item khác ngoài scope này vẫn cần tiếp tục được rà riêng nếu feature bag capacity được mở rộng kiểm soát toàn server.

# QA Attention Points

1. Re-run case full bag cho `PickupGroundReward`:
   - fail `InventoryFull`
   - ground reward vẫn còn để thử lại
   - không có item grant partial
2. Re-run case full bag cho `HarvestHerb` guaranteed-output:
   - fail trước khi herb bị xóa
   - plot/herb state không bị commit dở dang
3. Chạy thêm concurrency/manual stress cơ bản nếu có tool:
   - spam pickup/harvest song song trên cùng character
   - xác nhận không vượt `usedSlots > totalSlots`
4. Kiểm tra risk ngẫu nhiên của herb output:
   - herb có output chance < 100%
   - nếu nhiều output cùng nổ, xem có path overflow/partial grant bất ngờ hay không
5. Giữ nguyên các QA scope trước đó của bag system row #5, nhưng dùng handoff này làm canonical follow-up sau khi reviewer pass fix.

# Source Chain

- Dev implementation handoff: `docs/agent-handoffs/active/20260515-inventory-bag-system-dev.md`
- Reviewer previous pass/fail context: `docs/agent-handoffs/active/20260515-inventory-bag-system-reviewer.md`
- Dev required-fix completion: `docs/agent-handoffs/active/20260515-reviewer-bag-capacity-precheck-race-response.md`
- Superseded blocked QA handoff: `docs/agent-handoffs/active/20260515-inventory-bag-system-qa.md`

# Recommended QA Output

QA báo rõ:
- pass/fail từng case rerun liên quan full bag pickup/harvest
- có hay không tái hiện vượt slot dưới spam song song
- nếu thấy issue ở herb random outputs thì phân loại rõ:
  - blocker nếu gây mất tính atomic / overflow sai invariant
  - risk nếu chỉ là giới hạn policy ngoài scope fix hiện tại
