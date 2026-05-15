---
title: QA Handoff — Herb Farming Drop Reject Fix

doc_type: handoff
status: Done
owner: qa
source_agent: reviewer
last_updated: 2026-05-15
source_design_doc: docs/game-design-wp/requirements/herb-farming-system.md
source_tech_design_doc: docs/tech-design/herb-farming-system.md
expected_output: verification
queue_id: 34
feature_key: herb-farming-system
handoff_type: qa
source_handoff: docs/agent-handoffs/active/20260515-33-herb-farming-drop-reject-race-response.md
response_to: docs/agent-handoffs/active/20260515-33-herb-farming-drop-reject-race-response.md
supersedes: docs/agent-handoffs/active/20260515-25-herb-farming-system-qa-report.md
iteration: 3
---

# Reviewer Verdict

**Pass with risks**

Reviewer đã verify required fix `#32`: herb enemy direct-grant reject/full decision nay nằm trong cùng inventory lock/transaction boundary với actual grant, không còn TOCTOU race của correction round này.

# What Reviewer Verified

## 1) Capacity check đã nằm trong same inventory lock boundary
- `EnemyRewardRuntimeService.GrantDirectRewardItemsAsync(...)` nay tự gọi `inventoryTransactions.ExecuteAsync(...)`.
- Bên trong callback lock:
  - partition `rolledItems` thành herb / non-herb
  - `BagService.CheckCapacityForAsync(...)` chạy cho herb set
  - nếu không fit thì remove herb items khỏi `itemsToGrant`
  - grant phần còn lại ngay trong cùng callback bằng `ItemService.AddItemAsync(...)`
- Không còn path `CheckCapacityForAsync(...)` outside lock rồi grant inside lock.

## 2) Inventory-full signal vẫn còn
- Sau khi transaction xong, nếu herb set bị reject thì runtime gửi:
  - `PickupGroundRewardResultPacket`
  - `Code = MessageCode.InventoryFull`
  - `RewardId = null`

## 3) Không có inbox fallback / ground-drop workaround
- Fix không thêm inbox route.
- Fix không spawn ground reward thay cho herb direct-grant bị reject.

## 4) Mixed reward baseline được giữ
- Nếu reward set có herb + non-herb direct-grant:
  - herb bị reject khi full bag theo authority mới
  - non-herb vẫn grant như baseline hiện tại
- Reviewer không thấy correction này broad-break reward path khác ngoài scope.

# Accepted Risks

## Risk 1 — Packet signal đang reuse `PickupGroundRewardResultPacket`
- Đây là accepted risk từ các review trước, không phải blocker của fix race này.
- QA nên lưu ý confirm client hiện tại xử lý signal này theo cách chấp nhận được ở path enemy direct-grant.

## Risk 2 — Mixed reward set không atomic-all-or-nothing
- Đây là chủ đích của correction round: chỉ herb-related direct-grant bị reject theo authority mới, non-herb giữ baseline cũ.
- Nếu gameplay sau này muốn toàn bộ mixed reward set reject cùng nhau thì đó là feature/spec change khác, không phải regression ở đây.

# QA Test Scope

## Must retest

1. **Full bag + enemy herb drop direct-grant**
   - Expected:
     - không nhận herb item
     - client nhận `InventoryFull`
     - không có inbox fallback
     - không có ground reward workaround

2. **Bag đủ chỗ + enemy herb drop direct-grant**
   - Expected: herb item vẫn được grant bình thường.

3. **Mixed direct-grant reward: herb + non-herb, bag không fit herb rule**
   - Expected:
     - herb bị reject
     - non-herb vẫn grant theo baseline
     - client vẫn nhận `InventoryFull`

4. **Mixed direct-grant reward: herb + non-herb, bag đủ chỗ cho herb**
   - Expected: cả herb và non-herb grant bình thường.

5. **Harvest full bag**
   - Non-regression: herb vẫn ở plot, fail như baseline cũ.

6. **Extract full bag**
   - Non-regression: herb vẫn ở inventory, fail như baseline cũ.

## Should test nếu có tool

7. **2 enemy reward events gần nhau / spam kill cùng player với bag sát ngưỡng đầy**
   - Expected: không có path stale pre-check khiến herb vẫn lọt vào inventory sau khi slot đã bị path khác chiếm.

# Source Chain

- Original herb farming QA baseline: `docs/agent-handoffs/active/20260515-25-herb-farming-system-qa-report.md`
- TechDesign correction: `docs/agent-handoffs/active/20260515-20-herb-farming-drop-reject-fix-techdesign.md`
- Dev correction: `docs/agent-handoffs/active/20260515-29-herb-farming-drop-reject-fix-dev.md`
- Reviewer fail on TOCTOU: `docs/agent-handoffs/active/20260515-31-herb-farming-drop-reject-fix-reviewer.md`
- Dev TOCTOU fix: `docs/agent-handoffs/active/20260515-32-herb-farming-drop-reject-race-fix.md`
- Reviewer pass on TOCTOU fix: `docs/agent-handoffs/active/20260515-33-herb-farming-drop-reject-race-response.md`

# Recommended QA Output

QA báo rõ:
- Pass/fail từng case ở mục Must retest
- Nếu full bag mà herb vẫn được grant → **blocker**
- Nếu full bag mà xuất hiện inbox fallback hoặc ground reward workaround → **blocker**
- Nếu mixed reward path làm mất non-herb grant ngoài authority scope → phân loại rõ regression/blocker
