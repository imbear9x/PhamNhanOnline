---
title: QA Handoff — Herb Farming System
doc_type: handoff
status: Done
owner: qa
source_agent: reviewer
last_updated: 2026-05-15
source_design_doc: docs/game-design-wp/requirements/herb-farming-system.md
source_tech_design_doc: docs/tech-design/herb-farming-system.md
expected_output: verification
queue_id: 24
feature_key: herb-farming-system
handoff_type: qa
source_handoff: docs/agent-handoffs/active/20260515-23-herb-farming-reviewer-blockers-response.md
response_to: docs/agent-handoffs/active/20260515-23-herb-farming-reviewer-blockers-response.md
iteration: 6
---

# Reviewer Verdict

**Pass with risks**

Cả 2 blocker từ lượt review #21 đã được fix đúng:
1. Herb expiry sweep không còn blocking sync trong hot maintenance tick.
2. Mầm non roll dùng `ReplantReturnChance` từ config definition, không còn hardcode 100%.

# What Reviewer Verified

## Blocker 1 — Expiry sweep non-blocking
- `ScheduleSweepIfDue(...)` là `void` sync, chỉ check thời gian + `Interlocked` guard → fire-and-forget async sweep.
- `SweepExpiredHerbsAsync` là `async Task` thuần, không có `.GetAwaiter().GetResult()`.
- Batch delete: `DeleteExpiredInventoryHerbsAsync` dùng single SQL `DELETE WHERE` thay vì per-row loop.
- Overlap guard: `Interlocked.CompareExchange` đúng, `finally` reset về `0`.
- Log: `[HerbExpirySweep] Deleted {n} expired herb(s)` giữ lại.

## Blocker 2 — Mầm non chance từ config
- `HerbService.ExtractHerbAsync`: `CheckChance(ToPartsPerMillion(herbDefinition.ReplantReturnChance))` — không còn `1_000_000`.
- Chain hoàn chỉnh: `herb_templates.replant_return_chance` → `HerbTemplateEntity` → `AlchemyDefinitionCatalog` → `HerbTemplateDefinition.ReplantReturnChance` → `HerbService`.
- Schema: `ALTER TABLE ... ADD COLUMN IF NOT EXISTS replant_return_chance double precision NOT NULL DEFAULT 0.3` — idempotent, safe.

# Accepted Risks

## Risk 1 — `_nextSweepUtc` set trước khi sweep xong
- Nếu sweep fail, interval vẫn được đẩy lên → không retry sớm hơn.
- Behavior chấp nhận được cho background sweep. QA nên confirm log `Herb expiry sweep failed` xuất hiện đúng khi có lỗi.

## Risk 2 — Dead code ngoài lock trong `HarvestAsync` (pre-existing)
- `RequireOwnedHerbAsync` + `MaterializeHerbProgressAsync` chạy ngoài lock, kết quả không được reuse.
- Correctness không ảnh hưởng. Cleanup ở sprint sau.

## Risk 3 — `GetGardenPlotsHandler` N+1 write-in-read (pre-existing)
- `GetNextStageRemainingSecondsAsync` → `MaterializeHerbProgressAsync` → write DB.
- `GetGardenPlots` là write operation ẩn, không atomic, không trong inventory lock.

## Risk 4 — `grants` list ngoài lock, reuse trong lock (pre-existing)
- Nếu tương lai có retry, `grants` không được recompute. Hiện tại safe.

## Risk 5 — Expiry delete ngoài transaction trong `ExtractHerbAsync` early-exit (pre-existing)
- `DeleteAsync` trong early-exit không có tx. Herb đã hết hạn, không có item grant risk, nhưng là inconsistency nhỏ.

# QA Test Scope

## Must test — Harvest/Extract 2 bước

1. **HarvestAsync** — herb Mature/ThousandYear đang trồng:
   - Expected: herb chuyển sang `InInventory`, `expire_at` được set, plot bị clear, **không có item grant**.

2. **HarvestAsync** — herb chưa đủ stage (Seedling/Young):
   - Expected: fail `GardenHerbNotHarvestable`, herb vẫn trồng, plot không thay đổi.

3. **ExtractHerbAsync** — herb InInventory còn hạn:
   - Expected: roll output, grant items, herb bị xóa, trả đúng `Items` và `MamNonReturned`.

4. **ExtractHerbAsync** — herb InInventory hết hạn:
   - Expected: fail `GardenHerbExpired`, herb bị xóa, **không có item grant**.

5. **ExtractHerbAsync** — full bag + có proc output:
   - Expected: fail `GardenInventoryFull`, herb **không bị xóa**, **không grant**.

6. **ExtractHerbAsync** — 0 proc output:
   - Expected: thành công, herb bị xóa, không grant item.

## Must test — Mầm non chance

7. **ExtractHerbAsync** nhiều lần với herb có `replant_return_chance = 0.3`:
   - Expected: `MamNonReturned` không phải luôn `true`. Xác nhận không phải 100%.
   - Nếu test môi trường có thể override chance về 0 → confirm không có mầm non.

## Must test — Expiry sweep

8. **Background sweep**:
   - Tạo herb InInventory với `expire_at` đã qua.
   - Chờ sweep interval (60s default).
   - Confirm herb row bị xóa khỏi DB.
   - Confirm log `[HerbExpirySweep] Deleted 1 expired herb(s)` xuất hiện.

9. **Sweep không overlap**:
   - Nếu có thể trigger sweep thủ công hoặc giảm interval test: confirm sweep không chạy 2 instance song song.

## Should test — Garden handlers

10. **InsertSoil** — plot đã có soil → fail `GardenPlotAlreadyHasSoil`.
11. **PlantHerbSeed** — plot không có soil → fail `GardenPlotNoSoil`.
12. **PlantHerbSeed** — plot đã có herb → fail `GardenPlotAlreadyHasHerb`.
13. **GetGardenPlots** — trả đúng `HerbStage`, `SoilRemainingSeconds`, `NextStageRemainingSeconds`, `HerbExpireAtUnixMs`.
14. **PlantExistingHerb** — herb không thuộc player → fail `GardenHerbNotOwned`.
15. **AlchemyService** — craft recipe không còn bị block bởi `required_herb_maturity` guard (non-regression).

## Should test — Young stage

16. **Young stage progression**:
   - DB fast-forward `accumulated_growth_seconds` đến threshold Young.
   - Confirm herb ở stage Young, không thể harvest (`GardenHerbNotHarvestable`).
   - Fast-forward tiếp đến Mature → harvest thành công.

# Source Chain

- Dev implementation handoff: `docs/agent-handoffs/active/20260514-herb-farming-system-dev.md`
- Reviewer initial fail: `docs/agent-handoffs/active/20260515-21-herb-farming-system-reviewer.md`
- Dev blocker fix handoff: `docs/agent-handoffs/active/20260515-22-herb-farming-reviewer-blockers-fix.md`
- Reviewer pass handoff: `docs/agent-handoffs/active/20260515-23-herb-farming-reviewer-blockers-response.md`
- TechDesign spec: `docs/tech-design/herb-farming-system.md`

# Recommended QA Output

QA báo rõ:
- Pass/fail từng case nhóm "Must test".
- Nếu `MamNonReturned` luôn `true` dù nhiều lần thử → **blocker**, report ngay.
- Nếu herb không bị xóa sau sweep hoặc sweep chạy overlap → **blocker**, report ngay.
- Nếu thấy herb bị xóa hoặc item grant khi bag full / herb hết hạn → **blocker**.
- Các "Should test" nếu fail thì phân loại blocker/risk rõ.
</content>
</invoke>