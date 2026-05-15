# Dev Handoff — Herb Farming: Fix 2 Blocker (Sweep Blocking Sync + Mầm Non 100%)

- Owner: dev
- Created by: reviewer
- Status: Done
- Severity: Required Fix
- Source review: Row #21 `herb-farming-system-reviewer`
- Related feature/handoff: `docs/agent-handoffs/active/20260515-21-herb-farming-system-reviewer.md`
- Queue ID: 22
- Feature Key: herb-farming-system
- Handoff Type: required-fix
- Source Handoff: `docs/agent-handoffs/active/20260515-21-herb-farming-system-reviewer.md`
- Response To: `docs/agent-handoffs/active/20260515-21-herb-farming-system-reviewer.md`
- Iteration: 4
- Related files:
  - `GameServer/Runtime/HerbExpiryBackgroundService.cs`
  - `GameServer/Services/HerbService.cs`

---

## Blocker 1 — `SweepExpiredHerbsIfDue` blocking sync với `.GetAwaiter().GetResult()` per-row trong maintenance hot loop

### Problem

`HerbExpiryBackgroundService.SweepExpiredHerbsIfDue` là `void` sync, nhưng bên trong dùng `.GetAwaiter().GetResult()` để gọi async repository:

```csharp
var expired = repository.ListExpiredInventoryHerbsAsync(utcNow, cancellationToken)
    .GetAwaiter().GetResult();

foreach (var herb in expired)
    repository.DeleteAsync(herb.Id, cancellationToken)
        .GetAwaiter().GetResult(); // ← per-row blocking!
```

`RuntimeMaintenanceService.Run()` chạy trên dedicated background thread với tick interval 50ms. `SweepExpiredHerbsIfDue` được gọi đồng bộ trong mỗi tick. Nếu DB chậm hoặc có nhiều herb hết hạn, sweep block toàn bộ maintenance loop (cultivation settlement, alchemy settlement, world cleanup) trong khi chờ DB. Per-row delete loop làm tình trạng tệ hơn tuyến tính theo số herb hết hạn.

### Why It Matters

- Tick overrun → maintenance pipeline bị delay: cultivation settlement, alchemy settlement, metric recording.
- N herb hết hạn = N lần block tuần tự → không thể scale.
- `.GetAwaiter().GetResult()` trên thread không có SynchronizationContext là "an toàn" về deadlock nhưng vẫn là pattern sai về architecture và hiệu năng.

### Evidence

- `GameServer/Runtime/HerbExpiryBackgroundService.cs` line 30, 32
- `GameServer/Runtime/RuntimeMaintenanceService.cs` line 187: `_herbExpiryBackgroundService.SweepExpiredHerbsIfDue(cancellationToken)` gọi sync trong `UpdateMaintenance(token)` — method này không async.

### Required Change

**Option A (recommended):** Đổi `SweepExpiredHerbsIfDue` sang `async Task SweepExpiredHerbsIfDueAsync(...)`. Gọi `await` từ maintenance loop (cần đổi `Run()` hoặc `UpdateMaintenance` sang async, hoặc tách sweep ra khỏi hot tick loop bằng cách spawn `Task` riêng có guard flag). Thêm batch delete thay vì per-row loop.

**Option B (minimal):** Giữ sync nhưng chỉ chạy sweep ngoài hot tick loop — ví dụ spawn `Task.Run(async () => await SweepAsync(...))` có `bool _sweepInProgress` guard để không overlap.

Batch delete có thể dùng `WHERE id = ANY(...)` hoặc `WHERE state = InInventory AND expire_at <= now` trực tiếp trong SQL thay vì load rồi delete từng row.

### Acceptance Criteria

- [ ] Không dùng `.GetAwaiter().GetResult()` trong hot maintenance tick loop.
- [ ] Sweep không block maintenance tick khi DB chậm.
- [ ] Sweep không chạy concurrent với chính nó (có guard hoặc `IHostedService` background loop riêng).
- [ ] Log `[HerbExpirySweep] Deleted {n} expired herb(s)` vẫn còn.
- [ ] Build pass, 0 error.

### Verification Scope

- Code review path `SweepExpiredHerbsIfDue` → `RuntimeMaintenanceService`.
- Build pass.
- Nếu đổi sang `IHostedService` loop: confirm service được register đúng.

### Out Of Scope

- Không cần thêm index mới cho `player_herbs.expire_at` trong lượt này (nhưng nên ghi nhận là cần sau).

---

## Blocker 2 — Mầm non roll chance hardcode 100% (`CheckChance(1_000_000)`)

### Problem

Trong `HerbService.ExtractHerbAsync`:

```csharp
if (herbDefinition.ReplantItemTemplateId.HasValue)
{
    mamNonReturned = _randomService.CheckChance(1_000_000).Success; // ← 100% luôn!
    if (mamNonReturned)
        grants.Add(new ItemGrantRequest(herbDefinition.ReplantItemTemplateId.Value, 1, false, null));
}
```

`1_000_000` partsPerMillion = 100%. Mọi herb có `ReplantItemTemplateId` đều **luôn trả mầm non** khi extract, bất kể balance design. Player có thể plant → harvest → extract vô hạn lần để dupe mầm non không bao giờ hao.

### Why It Matters

- Gameplay-breaking exploit: mầm non infinite loop, không có consumption.
- `MamNonReturned = true` luôn → response misleading, client không thể phân biệt được trường hợp nào thực sự "may mắn".
- TechDesign spec ghi rõ *"Roll mầm non return chance"* — "roll" ngụ ý chance < 100% và lấy từ config/definition.

### Evidence

- `GameServer/Services/HerbService.cs` line 336: `_randomService.CheckChance(1_000_000).Success`
- Spec: `herb_templates.replant_item_template_id` và "Roll mầm non return chance — if pass, AddItemAsync for replant item template."

### Required Change

1. Thêm `ReplantChance` (hoặc tương đương) vào `HerbTemplateDefinition` / `herb_templates` config/seed.
2. Đọc chance từ definition và truyền vào `CheckChance(ToPartsPerMillion(herbDefinition.ReplantChance))`.
3. Nếu spec chốt rằng 100% là intentional design thì phải document rõ trong TechDesign spec và xóa `_randomService.CheckChance` (thay bằng `mamNonReturned = true` và comment giải thích). Nhưng reviewer đánh giá đây không phải intent theo spec hiện tại.

### Acceptance Criteria

- [ ] Mầm non roll dùng chance thực tế từ config/definition, không phải hardcode 100%.
- [ ] Hoặc: nếu 100% là intentional, TechDesign spec phải xác nhận và comment code phải giải thích rõ.
- [ ] Build pass, 0 error.

### Verification Scope

- Confirm `HerbTemplateDefinition` có field chance cho replant.
- Confirm seed data điền đúng chance (ví dụ 30% = 0.3 hoặc 300_000 PPM).
- Build pass.

### Out Of Scope

- Không sửa các field khác của `herb_templates`.

---

## Accepted Risks (không phải blocker, ghi nhận để QA/Dev biết)

### Risk 1 — Expiry delete ngoài transaction trong pre-lock path của `ExtractHerbAsync`
- `IsHerbExpired` early-exit: `DeleteAsync` xảy ra ngoài `_inventoryTransactions.ExecuteAsync`.
- Herb đã hết hạn nên không có item grant risk, chỉ là inconsistency atomicity.
- Không block push nhưng nên thống nhất: tốt hơn là để delete xảy ra bên trong lock/transaction.

### Risk 2 — `GetGardenPlotsHandler` N+1 write-in-read pattern
- `GetNextStageRemainingSecondsAsync` → `MaterializeHerbProgressAsync` → write DB.
- `GetGardenPlots` là write operation ẩn, không atomic, không trong inventory lock.
- Pre-existing behavior nhưng được expose thêm qua API mới.

### Risk 3 — `grants` list ngoài lock, reuse trong lock
- Nếu tương lai có retry, `grants` không được recompute.
- Hiện tại safe, là code smell.

---

## Retest Scope Sau Fix

Sau khi Dev sửa và gửi lại Reviewer:

1. `ExtractHerb` với herb có `ReplantItemTemplateId` + chance thực tế → xác nhận không phải 100%.
2. Expiry sweep chạy không block maintenance tick (có thể kiểm tra log overrun).
3. `ExtractHerb` herb hết hạn → `GardenHerbExpired`, không grant.
4. Build pass, 0 error.

## Source Chain

- Reviewer handoff: `docs/agent-handoffs/active/20260515-21-herb-farming-system-reviewer.md`
- Dev implementation handoff: `docs/agent-handoffs/active/20260514-herb-farming-system-dev.md`
- TechDesign spec: `docs/tech-design/herb-farming-system.md`
</content>
</invoke>