---
title: QA Report — Herb Farming System
doc_type: handoff
status: Blocked
owner: techdesign
source_agent: qa
last_updated: 2026-05-15
source_design_doc: docs/game-design-wp/requirements/herb-farming-system.md
source_tech_design_doc: docs/tech-design/herb-farming-system.md
expected_output: client-handoff-evaluation
queue_id: 25
feature_key: herb-farming-system
handoff_type: qa
source_handoff: docs/agent-handoffs/active/20260515-24-herb-farming-system-qa.md
response_to: docs/agent-handoffs/active/20260515-23-herb-farming-reviewer-blockers-response.md
iteration: 7
---

# QA Result Summary

**Passed**

Trong phạm vi handoff QA #24, QA xác nhận implementation herb farming hiện tại khớp phần scope reviewer vừa pass:
- flow tách 2 bước `HarvestAsync` và `ExtractHerbAsync`
- `Young` stage đã có trong runtime enum/path
- mầm non (`MamNonReturned`) không còn hardcode 100%, đã đi theo `ReplantReturnChance`
- expiry sweep chạy nền non-blocking, có overlap guard, batch delete
- guard `required_herb_maturity` trong `AlchemyService` đã được bỏ
- build pass

# Tested Scope

Theo handoff #24, QA kiểm tra:
1. Harvest/Extract 2 bước
2. Mầm non chance theo config
3. Background expiry sweep
4. Garden handlers chính
5. Young stage presence/path
6. Non-regression `AlchemyService`

# Source Handoffs / Specs Used

1. `docs/agent-handoffs/active/20260515-24-herb-farming-system-qa.md`
2. `docs/agent-handoffs/active/20260515-23-herb-farming-reviewer-blockers-response.md`
3. `docs/tech-design/herb-farming-system.md`
4. `docs/agent-handoffs/active/20260515-21-herb-farming-system-reviewer.md`
5. `docs/agent-handoffs/active/20260515-22-herb-farming-reviewer-blockers-fix.md`

# Environment / Setup

- Workspace: `/home/khoivu/Project/PhamNhanOnline`
- Verification mode:
  - code-path inspection trên runtime/handler/repository paths
  - focused build verification
- Build evidence:
  - `dotnet build GameServer/GameServer.csproj -v minimal` ✅ pass
- Giới hạn lượt QA này:
  - chưa có runtime harness tự động để chạy packet/DB end-to-end cho toàn bộ case manual trong handoff
  - evidence chính là implementation path + build

# Checklist Results

## 1) HarvestAsync — herb Mature/ThousandYear đang trồng
**Pass ở mức implementation evidence**

Evidence:
- `GameServer/Services/HerbService.cs`
- `HarvestAsync(...)`:
  - verify `state == Planting`
  - `MaterializeHerbProgressAsync(...)`
  - chỉ cho phép stage `Mature` hoặc `ThousandYear`
  - clear `plot.CurrentPlayerHerbId`
  - set `herb.State = InInventory`
  - set `CurrentPlotId = null`, `PlantedAt = null`
  - set `ExpireAt = DateTime.UtcNow + _gameConfig.HerbInventoryExpiry`
  - commit plot + herb trong cùng transaction

Expected:
- harvest chuyển herb sang inventory living herb, set expiry, clear plot, không grant item

Actual:
- code path khớp expectation

## 2) HarvestAsync — herb chưa đủ stage (Seedling/Young)
**Pass ở mức implementation evidence**

Evidence:
- `GameServer/Services/HerbService.cs`
- nếu stage không thuộc `Mature` hoặc `ThousandYear` thì:
  - `throw new GameException(MessageCode.GardenHerbNotHarvestable)`

Expected:
- fail `GardenHerbNotHarvestable`, herb/plot không đổi

Actual:
- code path khớp expectation

## 3) ExtractHerbAsync — herb InInventory còn hạn
**Pass ở mức implementation evidence**

Evidence:
- `GameServer/Services/HerbService.cs`
- `ExtractHerbAsync(...)`:
  - verify `state == InInventory`
  - check expiry trước
  - resolve output theo stage
  - roll output thành `grants`
  - roll `mamNonReturned` theo `herbDefinition.ReplantReturnChance`
  - grant item trong `_inventoryTransactions.ExecuteAsync(...)`
  - delete herb sau grant
  - return `HerbExtractionResult(items, mamNonReturned)`
- `GameServer/Network/Handlers/ExtractHerbHandler.cs`
  - map đúng `Items` và `MamNonReturned`

Expected:
- success, grant item theo roll, herb bị xóa, response trả item + cờ `MamNonReturned`

Actual:
- code path khớp expectation

## 4) ExtractHerbAsync — herb InInventory hết hạn
**Pass ở mức implementation evidence**

Evidence:
- `GameServer/Services/HerbService.cs`
- trước khi grant:
  - `if (IsHerbExpired(...)) { await _playerHerbs.DeleteAsync(...); throw new GameException(MessageCode.GardenHerbExpired); }`
- check này có cả trước lock và trong lock

Expected:
- fail `GardenHerbExpired`, herb bị xóa, không item grant

Actual:
- code path khớp expectation

## 5) ExtractHerbAsync — full bag + có proc output
**Pass ở mức implementation evidence**

Evidence:
- `GameServer/Services/HerbService.cs`
- trong `_inventoryTransactions.ExecuteAsync(...)`:
  - nếu `grants.Count > 0` thì gọi `_bagService.CheckCapacityForAsync(playerId, grants, ct)`
  - nếu không fit: `throw new GameException(MessageCode.GardenInventoryFull)`
  - grant chỉ chạy sau check pass
  - delete herb chỉ chạy sau grant xong

Expected:
- fail `GardenInventoryFull`, herb không bị xóa, không grant item

Actual:
- code path khớp expectation

## 6) ExtractHerbAsync — 0 proc output
**Pass ở mức implementation evidence**

Evidence:
- `grants` được build từ các output pass chance
- nếu `grants.Count == 0` thì skip capacity check và skip grant loop
- herb vẫn bị delete ở cuối transaction

Expected:
- success, herb bị xóa, không grant item

Actual:
- code path khớp expectation

## 7) Mầm non chance không còn always-true
**Pass ở mức implementation evidence**

Evidence:
- `GameServer/Entities/HerbTemplateEntity.cs`
  - có field `ReplantReturnChance`
- `GameServer/Runtime/AlchemyDefinitionCatalog.cs`
  - load field này vào runtime definition
- `GameServer/Runtime/AlchemySystemTypes.cs`
  - `HerbTemplateDefinition` có `ReplantReturnChance`
- `GameServer/Services/HerbService.cs`
  - dùng `CheckChance(ToPartsPerMillion(herbDefinition.ReplantReturnChance))`
  - không còn hardcode `1_000_000`

Expected:
- `MamNonReturned` phản ánh chance config, không phải luôn true

Actual:
- code path khớp expectation

## 8) Background expiry sweep non-blocking + batch delete + no overlap
**Pass ở mức implementation evidence**

Evidence:
- `GameServer/Runtime/HerbExpiryBackgroundService.cs`
  - `ScheduleSweepIfDue(...)` là sync void chỉ schedule
  - dùng `Interlocked.CompareExchange(ref _sweepInProgress, 1, 0)` chống overlap
  - `_ = SweepExpiredHerbsAsync(...)` fire-and-forget
  - `finally` reset `_sweepInProgress = 0`
  - log success: `"[HerbExpirySweep] Deleted {deleted} expired herb(s)"`
  - log fail: `"Herb expiry sweep failed."`
- `GameServer/Repositories/PlayerHerbRepository.cs`
  - `DeleteExpiredInventoryHerbsAsync(...)` là single batch delete SQL path
- `GameServer/Runtime/RuntimeMaintenanceService.cs`
  - gọi `ScheduleSweepIfDue(...)`, không block tick bằng `.GetAwaiter().GetResult()`

Expected:
- sweep chạy nền, không block hot tick, không overlap, batch delete

Actual:
- code path khớp expectation

## 9) Garden handlers wired đúng
**Pass ở mức implementation evidence**

Evidence:
- `GameServer/Extensions/ServiceCollectionExtensions.cs`
  - có đăng ký handlers:
    - `GetGardenPlotsHandler`
    - `InsertSoilHandler`
    - `PlantHerbSeedHandler`
    - `PlantExistingHerbHandler`
- `GameServer/Network/Handlers/GetGardenPlotsHandler.cs`
  - trả `HerbStage`, `SoilRemainingSeconds`, `NextStageRemainingSeconds`, `HerbExpireAtUnixMs` qua model mapping path
- `GameServer/Network/Handlers/HarvestHerbHandler.cs`
  - trả `ExpireAtUnixMs`
- `GameServer/Network/Handlers/ExtractHerbHandler.cs`
  - trả `Items` và `MamNonReturned`

Expected:
- handler layer mỏng, map đúng service + packet result

Actual:
- code path khớp expectation

## 10) AlchemyService non-regression — bỏ guard required_herb_maturity
**Pass**

Evidence:
- `GameServer/Services/AlchemyService.cs`
- không còn block:
  - `if (recipe.Inputs.Any(...RequiredHerbMaturity...)) return Failed(...)`
- field DB/entity vẫn còn như dormant metadata nhưng không còn chặn validation path

Expected:
- craft recipe không còn fail chỉ vì `required_herb_maturity`

Actual:
- code path khớp expectation

## 11) Young stage presence/runtime enum
**Pass ở mức implementation evidence**

Evidence:
- `GameServer/Runtime/AlchemySystemTypes.cs`
  - `Young = 4`
- `HerbService.HarvestAsync(...)`
  - không cho harvest nếu stage chưa tới Mature/ThousandYear, nên Young naturally bị reject bằng `GardenHerbNotHarvestable`

Expected:
- Young tồn tại trong runtime và chưa harvest được

Actual:
- code path khớp expectation

## 12) Build regression
**Pass**

Evidence:
- `dotnet build GameServer/GameServer.csproj -v minimal`
- Result: build succeeded, 0 warnings, 0 errors

# Expected vs Actual

## Case A — Harvest 2-step
- Expected: harvest chỉ chuyển herb sang inventory entity + set expiry, không grant item
- Actual: `HarvestAsync` làm đúng path này
- Verdict: Pass

## Case B — Extract expired herb
- Expected: xóa herb, fail `GardenHerbExpired`, không grant item
- Actual: code path đúng
- Verdict: Pass

## Case C — Extract full bag có grant
- Expected: fail `GardenInventoryFull`, không xóa herb, không grant
- Actual: capacity check chạy trước grant/delete trong cùng inventory transaction
- Verdict: Pass

## Case D — Mầm non return chance
- Expected: theo config chance, không always-true
- Actual: dùng `ReplantReturnChance`
- Verdict: Pass

## Case E — Expiry sweep
- Expected: non-blocking, no overlap, batch delete
- Actual: implementation đúng reviewer acceptance
- Verdict: Pass

# Authority / Clarification Note

Có một điểm authority cần ghi nhận nhưng **không block** kết quả QA #24:
- `docs/tech-design/herb-farming-system.md` phần chi tiết runtime flow vẫn còn ghi `GardenInventoryFull = 6011 // placeholder — currently never triggered` và `CheckInventoryHasSpace` stub always true.
- Trong code hiện tại, `ExtractHerbAsync(...)` đã dùng authority mới từ bag system qua `_bagService.CheckCapacityForAsync(...)`, nên `GardenInventoryFull` **đã có thể trigger thực tế** ở extract path.
- QA không tự chọn sửa spec; chỉ ghi nhận đây là độ lệch tài liệu chi tiết so với implementation hiện tại. Nếu cần, TechDesign nên đồng bộ lại phần diễn giải runtime flow của herb spec.

# Known Limits / Residual Risks

1. Lượt QA này chủ yếu xác minh bằng code-path + build evidence; chưa có packet/runtime harness để chạy thống kê nhiều lần cho `MamNonReturned` hoặc chờ sweep thật 60s trong môi trường test.
2. Reviewer accepted risks vẫn còn tồn tại ngoài scope blocker fix:
   - `_nextSweepUtc` set trước khi sweep xong
   - dead code ngoài lock trong harvest path
   - `GetGardenPlots` có write-in-read do materialize progress
   - early-expiry delete ngoài transaction trong extract early-exit

# Result Summary

**Passed**

# Next Owner

**techdesign**

# Recommended Next Action

Không đưa thẳng về release. TechDesign phải xử lý post-QA authority/client synthesis trước. Lượt này đã bị supersede bởi correction round `#35`.

Song song, TechDesign nên cleanup lại phần runtime-flow doc của herb spec để phản ánh implementation bag-capacity hiện tại ở extract path.

# Retest Scope

Nếu sau này có thay đổi tiếp ở herb farming hoặc bag capacity, QA nên retest:
1. `HarvestAsync` Mature/Young gating
2. `ExtractHerbAsync` expired/full-bag/0-proc paths
3. `MamNonReturned` chance config path
4. background expiry sweep log + delete behavior
5. handler result payloads `ExpireAtUnixMs`, `Items`, `MamNonReturned`
