# Dev Handoff — Death Penalty Runtime: Captive Scoped DI + Sync-over-Async DB Write

- Owner: dev
- Created by: reviewer
- Status: Done
- Severity: Required Fix
- Source review: row #40 `death-penalty-reviewer`
- Related feature/handoff: `docs/agent-handoffs/active/20260516-40-death-penalty-reviewer.md`
- Related files:
  - `GameServer/Runtime/CharacterRuntimeService.cs`
  - `GameServer/Services/DeathPenaltyService.cs`
  - `GameServer/Extensions/ServiceCollectionExtensions.cs`

---

## Blocker 1 — Captive Scoped service bị inject vào Singleton

### Problem

`CharacterRuntimeService` được đăng ký là **Singleton**:
```csharp
services.AddSingleton<CharacterRuntimeService>();
```

Constructor của nó hiện inject `DeathPenaltyService` trực tiếp:
```csharp
public CharacterRuntimeService(
    ...
    DeathPenaltyService deathPenaltyService) // <-- Scoped
```

`DeathPenaltyService` được đăng ký là **Scoped**:
```csharp
services.AddScoped<DeathPenaltyService>();
```

`DeathPenaltyService` phụ thuộc vào `CharacterService` → dùng Linq2DB `DataConnection` (scoped/per-request). Khi Singleton capture Scoped qua constructor, DI container .NET sẽ:
- Throw `InvalidOperationException` nếu scope validation bật.
- Giữ mãi một `DeathPenaltyService` instance (cùng DbConnection đã disposed) nếu không bật — dẫn đến lỗi DB không xác định hoặc race condition.

### Why It Matters

- Mọi lần character chết kể từ lần đầu `CharacterRuntimeService` được tạo, `ApplyOnCombatDeathAsync` sẽ dùng DbConnection của scope đã chết.
- Đây là class bug captive dependency đã từng bị fail trong các review trước (`HerbExpiryBackgroundService` inject scoped từ singleton).
- Build pass vì .NET DI không validate scope theo default với `IHost`, nhưng runtime sẽ fail.

### Evidence

- `GameServer/Extensions/ServiceCollectionExtensions.cs`:
  - line: `services.AddSingleton<CharacterRuntimeService>();`
  - line: `services.AddScoped<DeathPenaltyService>();`
- `GameServer/Runtime/CharacterRuntimeService.cs`: constructor inject `DeathPenaltyService _deathPenaltyService` trực tiếp.

### Required Change

Refactor `CharacterRuntimeService` để không capture `DeathPenaltyService` qua constructor. Dùng một trong hai hướng:

**Option A (recommended):** Inject `IServiceScopeFactory` vào `CharacterRuntimeService`. Khi cần gọi `ApplyOnCombatDeathAsync`, tạo scope tạm:
```csharp
using var scope = _scopeFactory.CreateScope();
var deathPenaltyService = scope.ServiceProvider.GetRequiredService<DeathPenaltyService>();
await deathPenaltyService.ApplyOnCombatDeathAsync(player, ct);
```

**Option B:** Đăng ký `DeathPenaltyService` là Singleton (nếu tất cả dependencies của nó cũng có thể là Singleton an toàn). Kiểm tra `CharacterService` — nếu nó dùng DbContext/DataConnection scoped thì Option A là bắt buộc.

---

## Blocker 2 — `GetAwaiter().GetResult()` trong sync game-loop path

### Problem

```csharp
// CharacterRuntimeService.cs
private void NotifyDeathTransitionIfNeeded(...)
{
    ...
    _deathPenaltyService.ApplyOnCombatDeathAsync(player).GetAwaiter().GetResult(); // <-- blocking
    _notifier.NotifyStateTransition(player, CharacterStateTransitionReasons.CombatDead);
}
```

`NotifyDeathTransitionIfNeeded` là sync method được gọi từ game-loop tick (via `UpdateState` và các overload). `ApplyOnCombatDeathAsync` thực hiện **2 DB writes** (`UpdateCharacterCurrentStateAsync` và `UpdateCharacterBaseStatsAsync` / `MarkPendingPermanentDeletionAsync`). Blocking async DB write trên game-loop thread:
- Có thể deadlock nếu sync context hoặc task scheduler của server không phải threadpool thuần.
- Starve game-loop tick nếu DB chậm.
- Là class bug reviewer đã fail trước đó (herb farming sweep blocking `.GetAwaiter().GetResult()` trong hot tick).

### Why It Matters

Kể cả sau khi fix Blocker 1 (scope issue), call site này vẫn là nguồn gốc deadlock/starvation. Hai blocker phải fix cùng nhau.

### Evidence

- `GameServer/Runtime/CharacterRuntimeService.cs` dòng 218:
  ```csharp
  _deathPenaltyService.ApplyOnCombatDeathAsync(player).GetAwaiter().GetResult();
  ```
- `NotifyDeathTransitionIfNeeded` là `private void` (sync).
- Caller: `UpdateState`, `UpdateStateFromCombat`, `NotifyTick` — tất cả là sync hoặc fire-and-forget từ game loop.

### Required Change

Refactor `NotifyDeathTransitionIfNeeded` thành async, hoặc fire-and-forget theo pattern an toàn đã có trong codebase. Pattern đúng là tách death penalty mutation ra khỏi hot sync tick:

**Option A (recommended):** Đổi `NotifyDeathTransitionIfNeeded` thành `private async Task NotifyDeathTransitionIfNeededAsync(...)`, và propagate async lên caller. Nếu caller là fire-and-forget từ loop, dùng pattern tương tự `HerbExpiryBackgroundService` hoặc async event queue.

**Option B:** Queue death event ra ngoài tick và process async trong background task riêng (safer nếu game loop phải giữ sync).

---

## Acceptance Criteria

- [ ] `CharacterRuntimeService` không còn inject `DeathPenaltyService` trực tiếp vào constructor Singleton.
- [ ] `DeathPenaltyService` được resolve qua scope (hoặc đổi lifecycle thích hợp) mỗi khi gọi.
- [ ] `ApplyOnCombatDeathAsync` không được gọi bằng `.GetAwaiter().GetResult()` trong sync game-loop path.
- [ ] Death penalty vẫn được apply khi character transition sang `CombatDead`: clear Skill statuses + persist tribulation/lifespan mutation đúng theo realm.
- [ ] Build pass, 0 error.

## Verification Scope

- Kiểm tra DI registration: `CharacterRuntimeService` không còn bị validate error nếu bật `ValidateScopes = true`.
- Focused build: `dotnet build GameServer/GameServer.csproj -v minimal`.
- Code audit: không còn `.GetAwaiter().GetResult()` trong `NotifyDeathTransitionIfNeeded` hoặc bất kỳ path nào gọi `ApplyOnCombatDeathAsync`.

## Out Of Scope

- Không yêu cầu fix tribulation trigger runtime (defer đã được TD cho phép tại #39).
- Không yêu cầu thay đổi logic death penalty (chỉ fix cách gọi, không đổi behavior).
- Không yêu cầu refactor toàn bộ game loop sang async.
- Không yêu cầu thêm unit test cho death penalty trong handoff này.

## Notes

- Risk `PurgeAllItemsForPlayerAsync` nested `ExecuteAsync` (re-entrant `pg_advisory_xact_lock`) không phải blocker nhưng reviewer đã ghi nhận. Dev không cần fix trong handoff này.
- Risk `player.UpdateCharacter(PendingPermanentDeletion = false)` trước lifespan check là fragile nhưng không phải blocker (character pending-delete không thể vào combat path).
- Tất cả phần còn lại của slice (#40) đã pass: source tagging, pending-delete gates, schema, mapper, confirm-delete handler, purge graph order.
