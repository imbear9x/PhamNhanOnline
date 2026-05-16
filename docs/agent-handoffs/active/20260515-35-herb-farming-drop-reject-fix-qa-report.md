---
title: QA Report — Herb Farming Drop Reject Fix
doc_type: handoff
status: Done
owner: techdesign
source_agent: qa
last_updated: 2026-05-15
source_design_doc: docs/game-design-wp/requirements/herb-farming-system.md
source_tech_design_doc: docs/tech-design/herb-farming-system.md
expected_output: client-handoff-evaluation
queue_id: 35
feature_key: herb-farming-system
handoff_type: qa
source_handoff: docs/agent-handoffs/active/20260515-34-herb-farming-drop-reject-fix-qa.md
response_to: docs/agent-handoffs/active/20260515-33-herb-farming-drop-reject-race-response.md
supersedes: docs/agent-handoffs/active/20260515-25-herb-farming-system-qa-report.md
iteration: 3
---

# QA Result Summary

**Passed**

QA đã retest correction round cho herb enemy direct-grant reject-on-full. Trong phạm vi authority của handoff #34, implementation hiện tại khớp reviewer verdict:
- herb capacity decision nằm **trong cùng inventory lock / transaction boundary** với actual grant
- full bag ở herb direct-grant path sẽ **reject herb**, gửi `InventoryFull`, **không** inbox fallback, **không** ground reward workaround
- mixed reward set vẫn giữ baseline: herb bị reject nhưng non-herb vẫn grant
- non-regression `HarvestAsync` và `ExtractHerbAsync` full-bag baseline vẫn giữ đúng
- build pass

# Tested Scope

Theo handoff #34, QA kiểm tra:
1. Enemy herb direct-grant full bag path
2. Enemy herb direct-grant fit path
3. Mixed direct-grant reward path herb + non-herb
4. Không có inbox fallback / ground workaround
5. Non-regression harvest full bag baseline
6. Non-regression extract full bag baseline
7. Build regression

# Source Handoffs / Specs Used

1. `docs/agent-handoffs/active/20260515-34-herb-farming-drop-reject-fix-qa.md`
2. `docs/agent-handoffs/active/20260515-33-herb-farming-drop-reject-race-response.md`
3. `docs/agent-handoffs/active/20260515-32-herb-farming-drop-reject-race-fix.md`
4. `docs/agent-handoffs/active/20260515-31-herb-farming-drop-reject-fix-reviewer.md`
5. `docs/tech-design/herb-farming-system.md`
6. `docs/agent-handoffs/active/20260515-25-herb-farming-system-qa-report.md`

# Environment / Setup

- Workspace: `/home/khoivu/Project/PhamNhanOnline`
- Verification mode:
  - code-path inspection trên runtime/service/handler paths
  - focused build verification
- Build evidence:
  - `dotnet build GameServer/GameServer.csproj -v minimal` ✅ pass
- Giới hạn lượt QA này:
  - chưa có runtime harness/fixture để bắn packet enemy reward end-to-end hoặc spam-kill concurrency test thật
  - evidence chính là implementation path + lock boundary + build

# Checklist Results

## 1) Full bag + enemy herb drop direct-grant
**Pass ở mức implementation evidence**

Evidence:
- `GameServer/Runtime/EnemyRewardRuntimeService.cs`
- `GrantDirectRewardItemsAsync(...)` gọi `inventoryTransactions.ExecuteAsync(...)`
- bên trong callback lock:
  - partition `rolledItems` thành `herbDirectGrantItems` và `itemsToGrant`
  - gọi `bagService.CheckCapacityForAsync(...)` cho herb set
  - nếu `!CanFit`:
    - `shouldNotifyInventoryFull = true`
    - `itemsToGrant.RemoveAll(IsHerbRewardItem)`
- sau transaction xong:
  - `_network.Send(... new PickupGroundRewardResultPacket { Success = false, Code = MessageCode.InventoryFull, RewardId = null })`

Expected:
- không grant herb item
- client nhận `InventoryFull`
- reject decision không nằm ngoài lock

Actual:
- code path khớp expectation

## 2) Bag đủ chỗ + enemy herb drop direct-grant
**Pass ở mức implementation evidence**

Evidence:
- cùng path `GrantDirectRewardItemsAsync(...)`
- nếu `herbCapacityCheck.CanFit == true` thì herb item vẫn còn trong `itemsToGrant`
- item grant chạy ngay trong cùng callback lock bằng `itemService.AddItemAsync(...)`

Expected:
- herb item được grant bình thường

Actual:
- code path khớp expectation

## 3) Mixed direct-grant reward: herb + non-herb, bag không fit herb rule
**Pass ở mức implementation evidence**

Evidence:
- `itemsToGrant = rolledItems.ToList()`
- khi herb không fit thì chỉ `RemoveAll(IsHerbRewardItem)`
- non-herb không bị loại khỏi `itemsToGrant`
- foreach grant theo `itemsToGrant.GroupBy(...)` vẫn tiếp tục cấp non-herb
- sau transaction có gửi `InventoryFull`

Expected:
- herb bị reject
- non-herb vẫn grant theo baseline
- client vẫn nhận `InventoryFull`

Actual:
- code path khớp expectation

## 4) Mixed direct-grant reward: herb + non-herb, bag đủ chỗ cho herb
**Pass ở mức implementation evidence**

Evidence:
- nếu herb fit thì không remove herb khỏi `itemsToGrant`
- cả herb và non-herb đều đi qua cùng loop grant trong callback lock

Expected:
- cả herb và non-herb grant bình thường

Actual:
- code path khớp expectation

## 5) Không có inbox fallback
**Pass**

Evidence:
- `GameServer/Runtime/EnemyRewardRuntimeService.cs`
  - direct grant path chỉ có grant item hoặc gửi `PickupGroundRewardResultPacket` với `InventoryFull`
  - không có inbox service / mail / deferred delivery path trong correction scope
- `docs/tech-design/herb-farming-system.md`
  - authority ghi rõ: no inbox fallback cho herb-related overflow

Expected:
- không redirect herb overflow vào inbox/mail

Actual:
- không thấy path inbox fallback

## 6) Không có ground reward workaround cho herb reject
**Pass**

Evidence:
- `ProcessPendingEventsAsync(...)`
  - ground reward chỉ tạo từ `rewardRule.DeliveryType == RewardDeliveryType.GroundDrop`
- `GrantDirectRewardItemsAsync(...)`
  - khi herb full thì chỉ remove herb khỏi `itemsToGrant` + notify `InventoryFull`
  - không spawn `GroundRewardEntity`, không chuyển direct-grant herb sang ground-drop

Expected:
- herb direct-grant bị reject không bị biến thành ground reward workaround

Actual:
- code path khớp expectation

## 7) Non-regression — Harvest full bag baseline
**Pass ở mức current baseline authority**

Evidence:
- `GameServer/Services/HerbService.cs`
- `HarvestAsync(...)`:
  - chỉ move herb từ plot sang inventory-living-herb entity
  - không grant item output ở bước này
  - clear plot, set `State = InInventory`, set `ExpireAt`
- baseline post-correction authority cho full-bag reject nằm ở enemy herb direct-grant và extract path; reviewer handoff #34 chỉ yêu cầu giữ harvest/extract baseline không regress

Expected theo baseline hiện tại:
- correction này không làm hỏng harvest path hiện có

Actual:
- `EnemyRewardRuntimeService` fix không chạm `HarvestAsync(...)`
- harvest code path giữ nguyên baseline trước đó

## 8) Non-regression — Extract full bag baseline
**Pass**

Evidence:
- `GameServer/Services/HerbService.cs`
- `ExtractHerbAsync(...)` trong `_inventoryTransactions.ExecuteAsync(...)`:
  - `CheckCapacityForAsync(playerId, grants, ct)`
  - nếu không fit: `throw new GameException(MessageCode.GardenInventoryFull)`
  - grant và delete herb chỉ chạy sau capacity check pass

Expected:
- herb vẫn ở inventory, fail `GardenInventoryFull`, không grant item

Actual:
- code path giữ đúng baseline đã pass ở QA #24/#17

## 9) TOCTOU race correction đúng boundary
**Pass**

Evidence:
- `GameServer/Runtime/EnemyRewardRuntimeService.cs`
- QA không thấy bất kỳ call `CheckCapacityForAsync(...)` cho herb direct-grant ở `ProcessPendingEventsAsync(...)` trước khi vào `GrantDirectRewardItemsAsync(...)`
- call capacity check duy nhất cho correction path nằm trong callback `inventoryTransactions.ExecuteAsync(...)`

Expected:
- không còn stale pre-check outside lock rồi grant inside lock

Actual:
- code path khớp expectation

## 10) Packet signal path cho reject
**Pass với accepted risk**

Evidence:
- `GameServer/Runtime/EnemyRewardRuntimeService.cs`
  - reject herb direct-grant gửi `PickupGroundRewardResultPacket` + `MessageCode.InventoryFull`
- Reviewer #33 ghi đây là accepted risk, không blocker correction round này

Expected:
- vẫn có signal tối thiểu cho client khi herb bị reject

Actual:
- signal vẫn tồn tại đúng reviewer scope

## 11) Build regression
**Pass**

Evidence:
- `dotnet build GameServer/GameServer.csproj -v minimal`
- Result: build succeeded, 0 warnings, 0 errors

# Expected vs Actual

## Case A — Herb direct-grant full bag
- Expected: herb không được grant; gửi `InventoryFull`; không inbox; không ground workaround
- Actual: code path đúng
- Verdict: Pass

## Case B — Herb direct-grant fit bag
- Expected: herb grant bình thường
- Actual: code path đúng
- Verdict: Pass

## Case C — Mixed reward, herb không fit
- Expected: herb reject, non-herb vẫn grant, có `InventoryFull`
- Actual: code path đúng
- Verdict: Pass

## Case D — Mixed reward, herb fit
- Expected: herb + non-herb đều grant
- Actual: code path đúng
- Verdict: Pass

## Case E — Extract full bag baseline
- Expected: fail `GardenInventoryFull`, herb không bị xóa
- Actual: code path đúng
- Verdict: Pass

# Result Summary

**Passed**

# Next Owner

**techdesign**

# Recommended Next Action

TechDesign đánh giá client impact và tạo/cập nhật handoff `dev-client` nếu client cần implement hoặc đổi behavior. Với lượt này, TechDesign đã tạo handoff `#36`.

# Retest Scope

Nếu có thay đổi tiếp ở enemy reward runtime / bag capacity / herb reward classification, QA nên retest:
1. herb direct-grant full/fit bag
2. mixed herb + non-herb direct-grant
3. reject signal `InventoryFull`
4. không inbox fallback / không ground workaround
5. extract full-bag baseline
6. concurrency case khi có test harness spam kill gần-simultaneous

# Known Limits / Residual Risks

1. Chưa có runtime test harness để chứng minh bằng packet/log thực tế case spam-kill concurrency; verdict hiện dựa trên serialization boundary trong code.
2. Packet signal vẫn reuse `PickupGroundRewardResultPacket` cho direct-grant reject; đây là accepted risk từ reviewer, không phải blocker trong lượt QA này.
3. Mixed reward set vẫn không atomic all-or-nothing; đây là authority đã chấp nhận cho correction round này, không phải regression.
