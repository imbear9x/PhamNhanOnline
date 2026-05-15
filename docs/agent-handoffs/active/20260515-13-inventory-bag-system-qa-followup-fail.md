---
title: QA Report — Inventory Bag System Follow-up After Reviewer TOCTOU Fix
doc_type: handoff
status: Done
owner: techdesign
source_agent: qa
last_updated: 2026-05-15
source_design_doc: docs/game-design-wp/requirements/inventory-bag-system.md
source_tech_design_doc: docs/tech-design/inventory-bag-system.md
expected_output: implementation
queue_id: 13
feature_key: inventory-bag-system
handoff_type: qa
source_handoff: docs/agent-handoffs/active/20260515-12-inventory-bag-system-qa-followup.md
response_to: docs/agent-handoffs/active/20260515-reviewer-bag-capacity-precheck-race-response.md
iteration: 3
---

# QA Result Summary

**Failed**

QA xác nhận reviewer TOCTOU fix đã được đặt đúng boundary lock/transaction cho `PickupGroundReward` và `HarvestHerb`, nhưng phát hiện **regression/known defect còn vi phạm authority của TechDesign** ở herb harvest random outputs:

- `HarvestHerbAsync(...)` chỉ pre-check capacity cho output có `OutputChance >= 1d`
- sau đó vẫn gọi `_itemService.AddItemAsync(...)` cho từng output random proc được
- `ItemService.AddItemAsync(...)` hiện **không có bag capacity guard**
- vì vậy active action `HarvestHerb` vẫn có đường dẫn có thể grant thêm slot sau pre-check và **không reject entirely khi full**

Điều này mâu thuẫn trực tiếp với TechDesign bag spec: **active action phải reject toàn bộ khi inventory không fit**.

# Tested Scope

Theo handoff QA #12, QA kiểm tra:
1. Ground reward pickup full-bag path sau TOCTOU fix
2. Herb harvest full-bag path sau TOCTOU fix
3. Concurrency/serialization boundary ở cùng player
4. Risk random herb outputs được reviewer yêu cầu QA attention
5. Hồi quy tối thiểu của bag APIs/build liên quan scope đang follow-up

# Source Handoffs / Specs Used

1. `docs/agent-handoffs/active/20260515-12-inventory-bag-system-qa-followup.md`
2. `docs/agent-handoffs/active/20260515-reviewer-bag-capacity-precheck-race-response.md`
3. `docs/agent-handoffs/active/20260515-inventory-bag-system-qa.md`
4. `docs/agent-handoffs/active/20260515-inventory-bag-system-dev.md`
5. `docs/tech-design/inventory-bag-system.md`

# Environment / Setup

- Workspace: `/home/khoivu/Project/PhamNhanOnline`
- Verification mode:
  - code inspection on authoritative runtime paths
  - focused build verification
  - limited DB/runtime probing attempted, but no usable DB output was returned via local `psql` in this QA run
- Build command evidence:
  - `dotnet build GameServer/GameServer.csproj -v minimal` ✅ pass

# Checklist Results

## 1) Ground reward pickup lock + fail path
**Pass**

Evidence:
- `GameServer/Network/Handlers/PickupGroundRewardHandler.cs`
- full grant path chạy trong `_inventoryTransactions.ExecuteAsync(...)`
- bên trong lock có:
  - materialize `ItemGrantRequest[]`
  - `_bagService.CheckCapacityForAsync(...)`
  - fail thì `throw new GameException(MessageCode.InventoryFull)`
  - success thì `MoveGroundItemToInventoryCoreUnlockedAsync(...)`
- outer catch gọi `instance.CancelGroundRewardClaim(...)`

Expected:
- full bag phải fail trước khi consume runtime claim
- claim vẫn còn để thử lại

Actual:
- code path phù hợp expectation ở mức implementation evidence

## 2) Herb harvest guaranteed-output path
**Pass một phần / đúng với guaranteed outputs**

Evidence:
- `GameServer/Services/HerbService.cs`
- `HarvestHerbAsync(...)` chạy trong `_inventoryTransactions.ExecuteAsync(...)`
- trong critical section có:
  - reload herb/state
  - resolve outputs lại
  - build `guaranteedOutputs = lockedOutputs.Where(x => x.OutputChance >= 1d)...`
  - `_bagService.CheckCapacityForAsync(...)`
  - nếu không fit thì `throw new GameException(MessageCode.InventoryFull)`
  - chỉ sau đó mới mutate reward + plot/herb state

Expected:
- guaranteed-output full bag phải fail trước delete herb / clear plot

Actual:
- code path đúng cho nhóm guaranteed outputs

## 3) Player-scoped inventory serialization boundary
**Pass**

Evidence:
- `GameServer/Services/PlayerInventoryTransactionService.cs`
- service dùng `pg_advisory_xact_lock(...)` trong transaction boundary theo player
- nếu đã có transaction thì acquire cùng player lock rồi chạy action; nếu chưa có thì tự mở transaction
- `ItemService.AddItemAsync(...)` và ground move path đều đi qua cùng inventory transaction service

Expected:
- cùng player không mở TOCTOU window mới giữa capacity check và mutate ở 2 flow vừa fix

Actual:
- code structure đáp ứng mục tiêu reviewer fix này

## 4) Herb random outputs overflow risk
**Fail**

Evidence:
- `GameServer/Services/HerbService.cs`
  - capacity pre-check chỉ áp dụng cho:
    - `lockedOutputs.Where(x => x.OutputChance >= 1d)`
  - sau đó vẫn loop toàn bộ `lockedOutputs`
  - mỗi output proc sẽ gọi `_itemService.AddItemAsync(...)`
- `GameServer/Services/ItemService.cs`
  - `AddItemAsync(...)` / `AddItemCoreAsync(...)` hiện không check bag capacity
  - nếu item stackable/non-stackable cần slot mới, code vẫn tạo/update row inventory bình thường
- `docs/tech-design/inventory-bag-system.md`
  - `Full inventory behavior: active action -> reject entirely`
  - `capacity must be checked before active actions mutate state`
  - `Active action grant: if not fit: abort action with InventoryFull`

Expected:
- herb harvest là active action; nếu tổ hợp output thực tế không fit thì action phải reject entirely với `InventoryFull`, không partial grant, không overflow slot

Actual:
- current implementation chỉ bảo đảm pre-check subset guaranteed outputs
- random outputs có thể proc thêm sau pre-check và tạo thêm slot qua `AddItemAsync(...)`
- hệ quả có thể xảy ra:
  - used slots vượt total slots, hoặc
  - action grant partial rồi vẫn xóa herb/clear plot trong cùng transaction
- đây là defect hành vi theo authority TechDesign, không chỉ là reviewer residual risk nữa

## 5) Build regression
**Pass**

Evidence:
- `dotnet build GameServer/GameServer.csproj -v minimal`
- Result: build succeeded, 0 warnings, 0 errors

# Expected vs Actual

## Case A — PickupGroundReward full bag
- Expected: fail `InventoryFull`, reward claim không bị consume mất
- Actual: code path đáp ứng expectation ở mức implementation review
- Verdict: Pass

## Case B — HarvestHerb full bag với guaranteed outputs
- Expected: fail trước khi herb/plot mutate
- Actual: code path đáp ứng expectation cho guaranteed outputs
- Verdict: Pass một phần

## Case C — HarvestHerb với nhiều random outputs cùng proc làm vượt capacity
- Expected: vì là active action, action phải reject entirely với `InventoryFull`
- Actual: chỉ pre-check guaranteed outputs; output random vẫn có thể được add sau pre-check qua path không enforce capacity
- Verdict: Fail

# Concrete Evidence Pointers

- `GameServer/Network/Handlers/PickupGroundRewardHandler.cs`
- `GameServer/Services/HerbService.cs`
- `GameServer/Services/ItemService.cs`
- `GameServer/Services/PlayerInventoryTransactionService.cs`
- `docs/tech-design/inventory-bag-system.md` sections:
  - Goal / Full inventory behavior
  - Active action grant
  - capacity must be checked before active actions mutate state

# Authority / Conflict Note

Không có xung đột giữa reviewer handoff và TechDesign về bug này:
- Reviewer đã ghi rõ đây là **accepted risk cần QA test thêm**
- TechDesign là authority kỹ thuật cao hơn cho expected behavior active action
- QA xác định current code **chưa đạt** authority TechDesign ở herb random output path

# Result Summary

**Failed**

# Next Owner

**dev**

# Recommended Next Action

Dev cần sửa herb harvest để capacity check phản ánh **tập output thực tế có thể được grant** theo policy authoritative của active action. Ít nhất cần loại bỏ đường dẫn mà random output có thể tạo slot mới sau một pre-check không bao phủ đầy đủ.

Một số hướng kỹ thuật hợp lệ để Dev/Reviewer quyết định:
- materialize authoritative harvest result trước trong cùng lock rồi check capacity trên full grant set trước khi mutate state, hoặc
- thêm authoritative capacity enforcement ngay trong item grant path dùng cho active harvest, miễn vẫn bảo đảm reject entirely/atomic

# Retest Scope

QA sẽ retest tối thiểu các điểm sau sau khi Dev sửa và Reviewer pass lại:
1. `PickupGroundReward` full bag vẫn fail atomically, reward claim còn nguyên
2. `HarvestHerb` guaranteed outputs full bag vẫn fail atomically
3. `HarvestHerb` random outputs nhiều proc không thể vượt `usedSlots > totalSlots`
4. Không có partial grant + herb bị xóa trong cùng action fail
5. Build/runtime packet flow bag không bị regression
