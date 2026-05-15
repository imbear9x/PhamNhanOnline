---
title: QA Handoff — Inventory Bag Herb Random Output Fix
doc_type: handoff
status: Done
owner: qa
source_agent: reviewer
last_updated: 2026-05-15
source_design_doc: docs/game-design-wp/requirements/inventory-bag-system.md
source_tech_design_doc: docs/tech-design/inventory-bag-system.md
expected_output: verification
queue_id: 16
feature_key: inventory-bag-system
handoff_type: qa
source_handoff: docs/agent-handoffs/active/20260515-15-inventory-bag-herb-random-output-fix-reviewer.md
response_to: docs/agent-handoffs/active/20260515-15-inventory-bag-herb-random-output-fix-reviewer.md
supersedes: docs/agent-handoffs/active/20260515-12-inventory-bag-system-qa-followup.md
iteration: 6
---

# Reviewer Verdict

**Pass with risks**

Fix `HarvestHerbAsync` random output capacity check đã được reviewer verify. Không có blocker. Fix đúng spec, atomic, không còn TOCTOU trên herb random output path.

# What Reviewer Verified

## 1) Roll-before-check đúng spec
- `procOutputs` được materialize bằng cách roll toàn bộ `lockedOutputs` trước.
- `CheckCapacityForAsync(playerId, procOutputs, ct)` được gọi trên **toàn bộ proc set**, không chỉ guaranteed subset.

## 2) Atomic reject
- `throw new GameException(MessageCode.InventoryFull)` xảy ra **trước** mọi `AddItemAsync`, **trước** `DeleteAsync(lockedHerb.Id)`, **trước** `UpdateAsync` plot.
- Toàn bộ nằm trong `_inventoryTransactions.ExecuteAsync(...)` — `pg_advisory_xact_lock` + transaction boundary → rollback toàn bộ nếu throw.

## 3) 0-proc case đúng
- `if (procOutputs.Length > 0)` guard: khi không có output nào proc, bỏ qua capacity check → herb bị xóa, plot bị clear, không overflow.

## 4) Lock boundary đúng
- `CheckCapacityForAsync` được gọi bên trong callback của `ExecuteAsync`, trong cùng pg advisory lock + transaction với grant và delete operations.
- Không còn TOCTOU window giữa check và grant.

# Accepted Risks

## Risk 1 — Dead code ngoài lock (pre-lock materialize bị bỏ phí)
- `HarvestHerbAsync` hiện làm `RequireOwnedHerbAsync` + `MaterializeHerbProgressAsync` + `ResolveHarvestOutputs` **hai lần**: một lần ngoài lock (dead code), một lần trong lock (canonical).
- Correctness không ảnh hưởng — code ngoài lock không được dùng lại bên trong callback.
- Risk: gây nhầm lẫn khi đọc code và tốn 2 lần DB I/O mỗi harvest.
- Không phải bug, không block push. Có thể cleanup ở sprint tiếp theo.

## Risk 2 — `EnsureDefaultBagAsync` INSERT bên trong lock (pre-existing)
- `CheckCapacityForAsync` gọi `EnsureDefaultBagAsync` — có path INSERT nếu player chưa có bag record.
- Với player đã có bag (flow bình thường): return ngay, không có INSERT.
- Risk chỉ xuất hiện ở edge case player chưa có bag khi harvest. Không phải regression của fix này.

## Risk 3 — `created` list khai báo ngoài ExecuteAsync (pattern dễ bẫy)
- Nếu tương lai có retry logic, `created` sẽ accumulate kết quả từ nhiều lần chạy.
- Hiện tại không có retry → không phải bug.

# QA Attention Points

## Must retest (acceptance criteria cho fix này)

1. **HarvestHerb full bag + ≥1 random output proc**
   - Expected: fail `InventoryFull`, herb **không bị xóa**, plot **không bị clear**, **không có item nào được grant**.
   - Verify không có partial grant.

2. **HarvestHerb bag đủ chỗ + random outputs proc**
   - Expected: **toàn bộ proc outputs** được granted, herb bị xóa, plot bị clear.

3. **HarvestHerb 0 proc (tất cả random output fail roll)**
   - Expected: action **thành công**, herb bị xóa, plot bị clear, **không có item grant**.

4. **HarvestHerb full bag + 0 proc**
   - Expected: action **thành công** (không có item cần grant nên không overflow), herb bị xóa, plot bị clear.

5. **PickupGroundReward full bag** (non-regression)
   - Expected: vẫn fail `InventoryFull` atomically, claim còn nguyên.

## Should test nếu có tool

6. **Concurrency/spam** HarvestHerb song song cùng character + bag gần đầy
   - Xác nhận không có path overflow `usedSlots > totalSlots`.

# Source Chain

- Dev fix handoff: `docs/agent-handoffs/active/20260515-14-inventory-bag-herb-random-output-fix-dev.md`
- Reviewer verify handoff: `docs/agent-handoffs/active/20260515-15-inventory-bag-herb-random-output-fix-reviewer.md`
- Superseded QA handoff: `docs/agent-handoffs/active/20260515-12-inventory-bag-system-qa-followup.md`
- TechDesign spec (section random output handling): `docs/tech-design/inventory-bag-system.md`

# Recommended QA Output

QA báo rõ:
- Pass/fail từng case ở mục "Must retest".
- Nếu thấy herb bị xóa hoặc có item grant khi `InventoryFull` → **blocker**, report lại ngay.
- Nếu thấy issue ở concurrency path → phân loại blocker/risk rõ ràng.
</content>
</invoke>