---
title: Reviewer Response — Herb Farming Blocker Fixes
doc_type: handoff
status: Done
owner: reviewer
source_agent: dev
last_updated: 2026-05-15
source_design_doc: docs/game-design-wp/requirements/herb-farming-system.md
source_tech_design_doc: docs/tech-design/herb-farming-system.md
expected_output: review
queue_id: 23
feature_key: herb-farming-system
handoff_type: required-fix-response
source_handoff: docs/agent-handoffs/active/20260515-22-herb-farming-reviewer-blockers-fix.md
response_to: docs/agent-handoffs/active/20260515-22-herb-farming-reviewer-blockers-fix.md
supersedes: docs/agent-handoffs/active/20260515-21-herb-farming-system-reviewer.md
iteration: 5
---

# Goal

Reviewer re-check 2 required blockers đã nêu ở `#22`:
1. herb expiry sweep đang block hot maintenance tick bằng sync/per-row calls
2. mầm non extract chance đang hardcode 100%

# Fix Summary

## Blocker 1 — Expiry sweep không còn block hot tick

Đã refactor `GameServer/Runtime/HerbExpiryBackgroundService.cs` + `RuntimeMaintenanceService.cs`:

- Đổi entrypoint từ `SweepExpiredHerbsIfDue(...)` sang `ScheduleSweepIfDue(...)`
- `RuntimeMaintenanceService` chỉ **schedule** sweep, không chờ DB ngay trong tick loop
- Dùng `Interlocked.CompareExchange` guard `_sweepInProgress` để tránh sweep overlap
- Sweep thực tế chạy trong async method `SweepExpiredHerbsAsync(...)`
- Bỏ hoàn toàn `.GetAwaiter().GetResult()` khỏi herb expiry path
- Bỏ delete per-row; thay bằng batch delete repository method:
  - `PlayerHerbRepository.DeleteExpiredInventoryHerbsAsync(DateTime utcNow, ...)`
- Log giữ lại theo reviewer AC:
  - `"[HerbExpirySweep] Deleted {n} expired herb(s)"`

## Blocker 2 — Mầm non không còn hardcode 100%

Đã thêm chance config vào herb definition path:

- `GameServer/Entities/HerbTemplateEntity.cs`
  - thêm `ReplantReturnChance` map tới cột `replant_return_chance`
- `GameServer/Runtime/AlchemySystemTypes.cs`
  - `HerbTemplateDefinition` thêm field `double ReplantReturnChance`
- `GameServer/Runtime/AlchemyDefinitionCatalog.cs`
  - load `ReplantReturnChance` từ DB/entity vào runtime definition
- `GameServer/Services/HerbService.cs`
  - thay:
    - `CheckChance(1_000_000)`
  - bằng:
    - `CheckChance(ToPartsPerMillion(herbDefinition.ReplantReturnChance))`

## DB / seed update cho chance mới

Đã cập nhật `database/initDatabase.sql`:
- `herb_templates.replant_return_chance double precision NOT NULL DEFAULT 0.3`
- `ALTER TABLE public.herb_templates ADD COLUMN IF NOT EXISTS replant_return_chance double precision NOT NULL DEFAULT 0.3`

Giá trị default hiện đặt là `0.3` (30%) để tránh exploit 100% và bám đúng yêu cầu “fixed configured chance”.

# Files / Modules Touched

- `GameServer/Entities/HerbTemplateEntity.cs`
- `GameServer/Runtime/AlchemySystemTypes.cs`
- `GameServer/Runtime/AlchemyDefinitionCatalog.cs`
- `GameServer/Repositories/PlayerHerbRepository.cs`
- `GameServer/Runtime/HerbExpiryBackgroundService.cs`
- `GameServer/Runtime/RuntimeMaintenanceService.cs`
- `GameServer/Services/HerbService.cs`
- `database/initDatabase.sql`

# Build / Test Result

Focused build đã chạy:
- `dotnet build GameServer/GameServer.csproj -v minimal`
- Result: **pass**, `0 error`

Observed warning còn lại:
- `CS8032` từ `Humanizer.Analyzers.NamespaceMigrationAnalyzer`
- thiếu `System.Collections.Immutable, Version=9.0.0.0`
- warning môi trường/analyzer, không phải blocker logic của fix này

# DB / Schema / Seed Changes

Có thay đổi:
- thêm cột `herb_templates.replant_return_chance`
- thêm `ALTER TABLE ... ADD COLUMN IF NOT EXISTS replant_return_chance ...`
- default/init value hiện là `0.3`

Không đổi thêm packet schema.

# Runtime / Contract Impact

- Maintenance tick không còn chờ herb expiry DB delete trực tiếp ở hot path
- Expiry sweep không overlap với chính nó
- Extract herb có `ReplantItemTemplateId` nay roll theo chance config thực tế, không còn always-true
- `MamNonReturned` packet result nay phản ánh roll chance config này

# Verification Scope Completed By Dev

- đọc reviewer handoff `#22`
- audit lại `HerbExpiryBackgroundService`, `RuntimeMaintenanceService`, `HerbService`, herb definition path
- implement async non-blocking expiry scheduling + batch delete
- implement replant chance field end-to-end entity → runtime definition → service
- focused compile verification cho `GameServer`

# Retest Guidance For Reviewer

1. Confirm `RuntimeMaintenanceService` không còn gọi herb sweep bằng sync/blocking DB path trong tick.
2. Confirm herb expiry path không còn `.GetAwaiter().GetResult()`.
3. Confirm repository delete là batch, không phải per-row loop.
4. Confirm `ExtractHerbAsync` dùng `herbDefinition.ReplantReturnChance`, không hardcode `1_000_000`.
5. Confirm `database/initDatabase.sql` có schema/init cho `replant_return_chance`.
6. Build pass.

# Known Gaps / Notes

- Tôi chọn hướng tối thiểu để không phải chuyển cả maintenance loop sang async: schedule async sweep + non-overlap guard. Mục tiêu là bỏ block ở hot tick mà vẫn fit kiến trúc hiện tại.
- Default `0.3` là giá trị seed/config tạm hợp lý để thay thế 100%; nếu reviewer muốn giá trị khác theo balance data thực tế thì có thể follow up data-only sau.
- Các accepted risks reviewer nêu trước đó ngoài 2 blocker này chưa được mở rộng xử lý trong lượt này.

# Risks / Blockers

- Không còn blocker code nào trong phạm vi `#22` theo phía dev.
- Reviewer nên rà thêm việc queue-id trong repo hiện có dấu hiệu bị reuse ở file khác ngoài chain này; tôi dùng `#23` cho response mới để tránh va chạm canonical lifecycle key.
