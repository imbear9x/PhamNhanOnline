---
title: Reviewer — Death Penalty Runtime DI/Async Required Fix Response
doc_type: handoff
status: Done
owner: reviewer
source_agent: dev
last_updated: 2026-05-16
source_design_doc: docs/game-design-wp/requirements/death-penalty.md
source_tech_design_doc: docs/tech-design/death-penalty.md
expected_output: review
queue_id: 42
feature_key: death-penalty
handoff_type: reviewer
source_handoff: docs/agent-handoffs/active/20260516-41-death-penalty-runtime-di-async-fix.md
response_to: docs/agent-handoffs/active/20260516-41-death-penalty-runtime-di-async-fix.md
iteration: 1
---

# Tóm tắt fix

Đã xử lý 2 blocker reviewer của `#41`:

1. **Captive Scoped DI**
   - `CharacterRuntimeService` không còn inject `DeathPenaltyService` trực tiếp qua constructor nữa.
   - thay bằng `IServiceScopeFactory`
   - khi cần apply death penalty sẽ tạo `CreateAsyncScope()` và resolve `DeathPenaltyService` trong scope mới

2. **Sync-over-async trong game-loop path**
   - đã bỏ `_deathPenaltyService.ApplyOnCombatDeathAsync(player).GetAwaiter().GetResult();`
   - `NotifyDeathTransitionIfNeeded(...)` giờ queue sang async helper `ApplyDeathPenaltyAndNotifyAsync(...)`
   - helper này tự resolve scoped service, await async path, rồi mới notify transition
   - có `try/catch` + `Logger.Error(...)` để tránh unobserved failure làm crash runtime path

# Files touched

- `GameServer/Runtime/CharacterRuntimeService.cs`
- `docs/agent-handoffs/QUEUE.md`
- `docs/agent-handoffs/active/20260516-41-death-penalty-runtime-di-async-fix.md`

# Cụ thể implementation

## CharacterRuntimeService
- thêm `using Microsoft.Extensions.DependencyInjection;`
- thay field:
  - từ `DeathPenaltyService _deathPenaltyService`
  - thành `IServiceScopeFactory _scopeFactory`
- đổi constructor tương ứng
- trong `NotifyDeathTransitionIfNeeded(...)`:
  - bỏ blocking `.GetAwaiter().GetResult()`
  - thay bằng `_ = ApplyDeathPenaltyAndNotifyAsync(player);`
- thêm helper private async:
  - `await using var scope = _scopeFactory.CreateAsyncScope();`
  - `var deathPenaltyService = scope.ServiceProvider.GetRequiredService<DeathPenaltyService>();`
  - `await deathPenaltyService.ApplyOnCombatDeathAsync(player);`
  - `_notifier.NotifyStateTransition(player, CharacterStateTransitionReasons.CombatDead);`
  - catch/log exception nếu fail

# Build / verification

Đã chạy:
- `dotnet build GameServer/GameServer.csproj -v minimal`

Kết quả:
- **Build succeeded**
- `0 Error(s)`
- còn `1 Warning(s)` cũ:
  - `CS8032 Humanizer.Analyzers.NamespaceMigrationAnalyzer`
  - thiếu `System.Collections.Immutable, Version=9.0.0.0`

# Reviewer focus

Xin reviewer kiểm lại các điểm sau:
1. `CharacterRuntimeService` singleton không còn captive scoped dependency
2. không còn `.GetAwaiter().GetResult()` ở death-penalty path
3. death penalty vẫn chỉ chạy khi transition lần đầu sang `CombatDead`
4. async helper không tạo regression về thứ tự notify quan trọng cho client/runtime

# Known risks / notes

- Fix này giữ hướng **không lan async qua toàn bộ game loop**, nhằm giảm rủi ro phạm vi.
- `_ = ApplyDeathPenaltyAndNotifyAsync(player);` là fire-and-forget có bắt lỗi nội bộ; reviewer nên đánh giá tiếp về ordering/runtime semantics nếu cần harden hơn.
- Handoff này không đổi logic gameplay của death penalty; chỉ sửa DI lifetime và cách gọi async.
