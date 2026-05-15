---
title: QA Report — Inventory Bag Herb Random Output Fix
doc_type: handoff
status: Ready
owner: release
source_agent: qa
last_updated: 2026-05-15
source_design_doc: docs/game-design-wp/requirements/inventory-bag-system.md
source_tech_design_doc: docs/tech-design/inventory-bag-system.md
expected_output: verification
queue_id: 17
feature_key: inventory-bag-system
handoff_type: qa
source_handoff: docs/agent-handoffs/active/20260515-16-inventory-bag-herb-random-output-fix-qa.md
response_to: docs/agent-handoffs/active/20260515-15-inventory-bag-herb-random-output-fix-reviewer.md
iteration: 7
---

# QA Result Summary

**Passed**

Trong phạm vi QA handoff #16, QA xác nhận fix `HarvestHerbAsync` đã sửa đúng contract random output theo TechDesign:
- roll toàn bộ output trước trong cùng inventory lock/transaction
- check capacity trên **full proc set**
- nếu không fit thì fail `InventoryFull` **trước** grant item / clear plot / delete herb
- nếu `procOutputs` rỗng thì harvest vẫn thành công, không có grant, không cần capacity check
- `PickupGroundReward` giữ nguyên non-regression path đã pass ở lượt trước

# Tested Scope

Theo handoff #16, QA kiểm tra:
1. `HarvestHerb` random output fix: roll-before-check
2. atomic reject khi full bag + có output proc
3. 0-proc case
4. non-regression `PickupGroundReward` full bag path
5. build regression tối thiểu

# Source Handoffs / Specs Used

1. `docs/agent-handoffs/active/20260515-16-inventory-bag-herb-random-output-fix-qa.md`
2. `docs/agent-handoffs/active/20260515-15-inventory-bag-herb-random-output-fix-reviewer.md`
3. `docs/agent-handoffs/active/20260515-14-inventory-bag-herb-random-output-fix-dev.md`
4. `docs/tech-design/inventory-bag-system.md`
5. `docs/agent-handoffs/active/20260515-13-inventory-bag-system-qa-followup-fail.md`

# Environment / Setup

- Workspace: `/home/khoivu/Project/PhamNhanOnline`
- Verification mode trong lượt này:
  - code-path inspection trên authoritative runtime paths
  - focused build verification
- Build evidence:
  - `dotnet build GameServer/GameServer.csproj -v minimal` ✅ pass
- Ghi chú:
  - lượt QA này không có automated runtime repro harness riêng cho herb random-output combinations
  - do đó evidence chính là code/runtime path authority + build pass

# Checklist Results

## 1) HarvestHerb full bag + random proc phải reject atomically
**Pass ở mức implementation evidence**

Evidence:
- `GameServer/Services/HerbService.cs`
- Trong `HarvestHerbAsync(...)`, bên trong `_inventoryTransactions.ExecuteAsync(...)`:
  - materialize `procOutputs` bằng cách roll toàn bộ `lockedOutputs` trước
  - `if (procOutputs.Length > 0)` thì gọi `_bagService.CheckCapacityForAsync(playerId, procOutputs, ct)`
  - nếu không fit: `throw new GameException(MessageCode.InventoryFull)`
  - chỉ sau check pass mới vào loop `_itemService.AddItemAsync(...)`
  - clear plot + delete herb nằm **sau** grant loop

Expected:
- khi có ít nhất 1 output proc nhưng full bag, action phải fail `InventoryFull`, không partial grant, không clear plot, không delete herb

Actual:
- current code path đáp ứng đúng ordering/atomicity theo spec

## 2) HarvestHerb bag đủ chỗ + random outputs proc phải grant đủ full proc set
**Pass ở mức implementation evidence**

Evidence:
- `procOutputs` được materialize trước thành full proc set
- grant loop duyệt toàn bộ `procOutputs`
- không còn logic cũ chỉ check guaranteed subset rồi grant random riêng lẻ

Expected:
- toàn bộ proc outputs đã roll phải được grant trong cùng action

Actual:
- code path phù hợp expectation

## 3) HarvestHerb 0-proc case
**Pass ở mức implementation evidence**

Evidence:
- guard `if (procOutputs.Length > 0)`
- khi `procOutputs` rỗng, flow bỏ qua capacity check và grant loop
- sau đó vẫn clear plot và delete herb

Expected:
- action thành công, herb bị xóa, plot bị clear, không có item grant

Actual:
- code path phù hợp expectation

## 4) HarvestHerb full bag + 0 proc
**Pass ở mức implementation evidence**

Evidence:
- cùng guard `if (procOutputs.Length > 0)`
- nếu không có proc output thì không có item cần grant, nên không có lý do fail `InventoryFull`

Expected:
- action vẫn thành công, không overflow

Actual:
- code path phù hợp expectation

## 5) PickupGroundReward full bag non-regression
**Pass ở mức implementation evidence**

Evidence:
- `GameServer/Network/Handlers/PickupGroundRewardHandler.cs`
- vẫn giữ logic:
  - build grants trong `_inventoryTransactions.ExecuteAsync(...)`
  - `_bagService.CheckCapacityForAsync(...)`
  - fail thì `throw new GameException(MessageCode.InventoryFull)`
  - outer catch gọi `instance.CancelGroundRewardClaim(...)`

Expected:
- fail atomically, reward claim không bị consume mất

Actual:
- code path không bị regression

## 6) Build regression
**Pass**

Evidence:
- `dotnet build GameServer/GameServer.csproj -v minimal`
- Result: build succeeded, 0 warnings, 0 errors

# Expected vs Actual

## Case A — HarvestHerb random proc + không fit capacity
- Expected: reject entirely với `InventoryFull`
- Actual: code hiện roll full proc set trước, check capacity trên full proc set, throw trước grant/mutation
- Verdict: Pass

## Case B — HarvestHerb random proc + fit capacity
- Expected: grant toàn bộ proc outputs rồi mới clear/delete herb state
- Actual: code hiện grant theo full `procOutputs`, sau đó mới clear plot/delete herb
- Verdict: Pass

## Case C — HarvestHerb 0 proc
- Expected: harvest success, không grant item
- Actual: code path skip check/grant khi `procOutputs.Length == 0`, vẫn clear/delete herb
- Verdict: Pass

## Case D — PickupGroundReward full bag
- Expected: fail atomically, claim còn nguyên
- Actual: path giữ nguyên logic pass của follow-up trước
- Verdict: Pass

# Concrete Evidence Pointers

- `GameServer/Services/HerbService.cs`
- `GameServer/Network/Handlers/PickupGroundRewardHandler.cs`
- `GameServer/Services/ItemService.cs`
- `docs/tech-design/inventory-bag-system.md`
- Build log from `dotnet build GameServer/GameServer.csproj -v minimal`

# Known Limits / Residual Risks

1. QA lượt này chủ yếu xác minh bằng code-path + build evidence; chưa có automated runtime harness tái hiện đủ 4 tổ hợp random-output trong môi trường test.
2. Reviewer risk đã nêu vẫn còn:
   - dead code/pre-lock materialize ngoài lock trong `HarvestHerbAsync(...)` gây dư I/O và dễ gây nhầm khi đọc code
   - không ảnh hưởng correctness của fix hiện tại
3. `ItemService.AddItemAsync(...)` vẫn không tự enforce bag capacity globally; authority capacity cho path này hiện do caller active flow chịu trách nhiệm. Trong phạm vi fix hiện tại, `HarvestHerbAsync` đã làm đúng contract.

# Result Summary

**Passed**

# Next Owner

**release**

# Recommended Next Action

Có thể cho feature/fix này quay lại release flow hoặc user flow theo queue hiện tại.

Nếu muốn tăng độ chắc chắn sau release candidate, nên bổ sung automated integration test cho các tổ hợp:
- random proc + full bag
- random proc + fit bag
- 0 proc
- concurrent harvest cùng player gần full bag

# Retest Scope

Nếu sau này có thay đổi tiếp ở `HerbService`, QA nên retest:
1. `HarvestHerb` random-output capacity path
2. `PickupGroundReward` full-bag atomic reject
3. invariant `usedSlots <= totalSlots` cho active grant paths liên quan