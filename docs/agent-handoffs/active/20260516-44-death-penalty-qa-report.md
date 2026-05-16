---
title: QA Report — Death Penalty Blocker-Response Slice
doc_type: handoff
status: Done
owner: techdesign
source_agent: qa
last_updated: 2026-05-16
source_design_doc: docs/game-design-wp/requirements/death-penalty.md
source_tech_design_doc: docs/tech-design/death-penalty.md
expected_output: client-handoff-evaluation
queue_id: 44
feature_key: death-penalty
handoff_type: qa
source_handoff: docs/agent-handoffs/active/20260516-43-death-penalty-qa.md
response_to: docs/agent-handoffs/active/20260516-42-death-penalty-runtime-di-async-response.md
iteration: 1
---

# QA Result Summary

**Passed**

Trong phạm vi handoff #43, QA xác nhận slice blocker-response death penalty đã được implement đúng theo reviewer handoff và TechDesign authority:
- có persist foundation cho `pending_permanent_deletion` và `next_tribulation_at_utc`
- gate pending-delete hoạt động đúng ở `GetCharacterData`, `EnterWorld`, `ReturnHomeAfterCombatDeath`
- `GetCharacterList` vẫn giữ character pending-delete trong list
- flow `ConfirmPermanentCharacterDeletion` trả đúng mã lỗi/response path và hard-delete graph đã được gọi
- clear combat status theo source `Skill`, giữ boundary cho `Talisman`/`Formation`
- death packet ordering đúng risk đã accept: `CharacterCurrentStateChanged` đi trước, `CharacterStateTransition(CombatDead)` đi sau khi async death-penalty apply xong
- DI/runtime async fix của reviewer slice vẫn đứng vững: `CharacterRuntimeService` không còn capture scoped service và không còn `.GetAwaiter().GetResult()` trên hot path

# Tested Scope

Theo handoff #43, QA kiểm tra các nhóm sau:
1. Pending permanent deletion gates
2. Confirm permanent delete flow
3. Death penalty mutation cho realm 1–18 và realm 19+
4. Combat status source tagging + clear-on-death
5. Client packet ordering của death transition
6. Focused build regression
7. Guard duplicate death-apply theo reviewer accepted scope

# Source Handoffs / Specs Used

1. `docs/agent-handoffs/active/20260516-43-death-penalty-qa.md`
2. `docs/agent-handoffs/active/20260516-42-death-penalty-runtime-di-async-response.md`
3. `docs/agent-handoffs/active/20260516-41-death-penalty-runtime-di-async-fix.md`
4. `docs/agent-handoffs/active/20260516-39-death-penalty-dev-blockers-response.md`
5. `docs/agent-handoffs/active/20260516-38-death-penalty-dev-blockers-techdesign.md`
6. `docs/tech-design/death-penalty.md`

# Environment / Setup

- Workspace: `/home/khoivu/Project/PhamNhanOnline`
- Verification mode cho lượt này:
  - code-path inspection trên authoritative runtime/handler/service paths
  - focused build verification
- Build command:
  - `dotnet build GameServer/GameServer.csproj -v minimal`
- Build result:
  - **Build succeeded**
  - **0 Warning(s)**
  - **0 Error(s)**

Ghi chú:
- Lượt QA này không có runtime integration harness sẵn để phát packet thật hoặc seed DB test scenario end-to-end.
- Vì vậy evidence chính là code/runtime path authority + build pass.
- Những case dưới đây được đánh dấu pass ở mức implementation evidence, không phải live gameplay capture.

# Checklist Results

## A. Pending permanent deletion gates

### A1. `GetCharacterList`: character pending-delete vẫn phải hiện trong list
**Pass ở mức implementation evidence**

Evidence:
- `GameServer/Network/Handlers/GetCharacterListHandler.cs`
- Handler gọi trực tiếp `CharacterService.GetCharactersByAccountAsync(session.PlayerId)`
- `CharacterService.GetCharactersByAccountAsync(...)` chỉ list theo account, không filter `PendingPermanentDeletion`
- `CharacterDto` có field `PendingPermanentDeletion` và được map từ entity `characters.pending_permanent_deletion`

Expected:
- character pending-delete vẫn xuất hiện trong danh sách character selection

Actual:
- code path giữ nguyên list behavior, không filter pending-delete

### A2. `GetCharacterData`: pending-delete phải fail bằng `CharacterPendingPermanentDeletion` và vẫn gửi snapshot
**Pass ở mức implementation evidence**

Evidence:
- `GameServer/Network/Handlers/GetCharacterDataHandler.cs`
- Nếu `_characterService.IsPendingPermanentDeletion(data)`:
  - gửi `GetCharacterDataResultPacket`
  - `Success = false`
  - `Code = MessageCode.CharacterPendingPermanentDeletion`
  - vẫn đính kèm `Character`, `BaseStats`, `CurrentState`

Expected:
- fail code đúng + snapshot payload vẫn có

Actual:
- code path đúng expectation

### A3. `EnterWorld`: pending-delete phải bị reject trước recovery/world attach
**Pass ở mức implementation evidence**

Evidence:
- `GameServer/Services/WorldEntryService.cs`
- Sau `LoadCharacterSnapshotByAccountAsync(...)`, nếu `_characterService.IsPendingPermanentDeletion(data)` thì trả `WorldEntryActionResult.Failure(MessageCode.CharacterPendingPermanentDeletion)` ngay
- `EnterWorldHandler` sẽ trả `EnterWorldResultPacket { Success = false, Code = result.Code }`
- Gate xảy ra trước các bước `PrepareSnapshotForWorldEntryAsync`, `RecoverSnapshotToHomeAsync`, `AttachPlayerSession(...)`

Expected:
- reject sớm, không cho vào world và không vào recovery path

Actual:
- đúng expectation

### A4. `ReturnHomeAfterCombatDeath`: pending-delete phải bị reject
**Pass ở mức implementation evidence**

Evidence:
- `GameServer/Network/Handlers/ReturnHomeAfterCombatDeathHandler.cs`
- Nếu `player.CharacterData.PendingPermanentDeletion` thì `SendFailure(session, MessageCode.CharacterPendingPermanentDeletion)` và return

Expected:
- character đã pending-delete không được dùng flow hồi sinh về home

Actual:
- đúng expectation

## B. Confirm permanent delete

### B1. Character đang pending-delete: confirm delete phải hard-delete và trả `Success = true`
**Pass ở mức implementation evidence**

Evidence:
- `GameServer/Network/Handlers/ConfirmPermanentCharacterDeletionHandler.cs`
- gọi `PermanentCharacterDeletionService.ConfirmAsync(...)`
- `GameServer/Services/PermanentCharacterDeletionService.cs`
- Nếu snapshot tồn tại và đang pending-delete:
  - chạy trong `_inventoryTransactions.ExecuteAsync(characterId, ...)`
  - purge items
  - delete skill grant sources, skill loadouts, skills, martial arts, pill recipes, practice sessions, notifications, herbs, garden plots, caves, breakthrough attempts
  - delete current state, base stats, và row character
  - trả `PermanentCharacterDeletionResult.Succeeded(characterId)`

Expected:
- hard-delete graph được thực thi và result packet báo success

Actual:
- implementation đúng flow expected trong phạm vi code evidence

### B2. Confirm delete lần 2 với cùng `CharacterId`: phải fail `CharacterNotFound`
**Pass ở mức implementation evidence**

Evidence:
- `PermanentCharacterDeletionService.ConfirmAsync(...)`
- bước đầu tiên load snapshot theo account + character id; nếu không còn row character thì trả `PermanentCharacterDeletionResult.Failed(MessageCode.CharacterNotFound, characterId)`

Expected:
- lần 2 sau khi đã xóa sẽ không còn snapshot và trả `CharacterNotFound`

Actual:
- code path đúng expectation

### B3. Confirm delete khi character không pending: phải fail `CharacterPendingPermanentDeletion`
**Pass ở mức implementation evidence**

Evidence:
- `PermanentCharacterDeletionService.ConfirmAsync(...)`
- nếu snapshot tồn tại nhưng `!_characterService.IsPendingPermanentDeletion(snapshot)` thì trả `PermanentCharacterDeletionResult.Failed(MessageCode.CharacterPendingPermanentDeletion, characterId)`

Expected:
- guard đúng, không cho xóa nhầm character chưa pending-delete

Actual:
- code path đúng expectation

## C. Death penalty mutation khi chết

### C1. Realm 1–18 chết: giảm lifespan theo `ceil(seconds/86400)`
**Pass ở mức implementation evidence**

Evidence:
- `GameServer/Services/DeathPenaltyService.cs`
- với `realmId < 19`:
  - `penaltyDays = (int)Math.Ceiling(Math.Max(0, _gameConfig.DeathLifespanPenaltySeconds) / 86400d)`
  - `updatedBaseStats.LifespanBonus = (baseStats.LifespanBonus ?? 0) - penaltyDays`
  - persist qua `UpdateCharacterBaseStatsAsync(updatedBaseStats, cancellationToken)`

Expected:
- thọ nguyên bonus giảm đúng công thức trần ngày

Actual:
- implementation đúng expectation

### C2. Realm 19+ chết: giảm `next_tribulation_at_utc`, floor tại `utcNow`
**Pass ở mức implementation evidence**

Evidence:
- `GameServer/Services/DeathPenaltyService.cs`
- với `realmId >= 19`:
  - lấy `currentState.NextTribulationAtUtc ?? utcNow`
  - trừ `DeathTribulationPenaltySeconds`
  - nếu nhỏ hơn `utcNow` thì clamp lên `utcNow`
  - persist qua `UpdateCharacterCurrentStateAsync(updatedState, cancellationToken)`
- `GameServer/Entities/CharacterCurrentState.cs` và `GameServer/DTO/CharacterCurrentStateDto.cs` đều đã có field `NextTribulationAtUtc`

Expected:
- countdown tribulation được persist đúng field authority đã chốt

Actual:
- implementation đúng expectation

### C3. Realm 1–18 chết khi lifespan về 0: phải mark `pending_permanent_deletion = true`
**Pass ở mức implementation evidence**

Evidence:
- `GameServer/Services/DeathPenaltyService.cs`
- sau khi persist `updatedBaseStats`, service tính:
  - `lifespanEndUtc = CharacterLifespanRules.ResolveLifespanEndUtc(...)`
  - nếu `IsExpired(lifespanEndUtc.Value, utcNow)` thì gọi `MarkPendingPermanentDeletionAsync(characterId, true, ...)`
  - cập nhật runtime character snapshot `PendingPermanentDeletion = true`

Expected:
- character cạn thọ nguyên do death penalty sẽ chuyển sang pending permanent deletion

Actual:
- code path đúng expectation

## D. Combat status source tagging

### D1. Skill buff/debuff phải bị clear khi chết
**Pass ở mức implementation evidence**

Evidence:
- `GameServer/Runtime/CombatStatusRuntime.cs`
  - enum `CombatStatusSourceType { Skill, Talisman, Formation, External }`
  - `ClearBySource(CombatStatusSourceType sourceType)` xóa shield/stat modifier/stun đúng theo source
- `GameServer/Services/DeathPenaltyService.cs`
  - gọi `player.CombatStatuses.ClearBySource(CombatStatusSourceType.Skill);`
- `GameServer/Runtime/SkillExecutionService.cs`
  - shield/stun/stat modifier từ skill đều được add với `CombatStatusSourceType.Skill`

Expected:
- effect có source `Skill` bị clear khi combat death resolve

Actual:
- boundary source-tag + clear-by-source đúng expectation

### D2. Talisman/Formation buff phải được giữ nguyên
**Pass ở mức implementation evidence**

Evidence:
- `ClearBySource(...)` chỉ xóa item có `SourceType == Skill`
- enum source đã tách riêng `Talisman` và `Formation`
- không có logic clear-all trong `DeathPenaltyService`

Expected:
- effect source khác `Skill` không bị sweep nhầm

Actual:
- code path đúng expectation

## E. Client packet ordering / reviewer risk verification

### E1. `CharacterCurrentStateChanged(HP=0)` phải đến trước `CharacterStateTransition(CombatDead)`
**Pass ở mức implementation evidence**

Evidence:
- `GameServer/Runtime/CharacterRuntimeService.cs`
- trong `ApplyDamage(...)` / `ApplyResourceDelta(...)`:
  - cập nhật current state
  - gọi `_notifier.NotifyCurrentStateChanged(player, snapshot.CurrentState)` **ngay lập tức**
  - sau đó `NotifyDeathTransitionIfNeeded(...)`
- `NotifyDeathTransitionIfNeeded(...)` không notify transition trực tiếp nữa mà fire-and-forget `_ = ApplyDeathPenaltyAndNotifyAsync(player);`
- `ApplyDeathPenaltyAndNotifyAsync(...)`:
  - resolve scoped `DeathPenaltyService`
  - `await deathPenaltyService.ApplyOnCombatDeathAsync(player);`
  - chỉ sau đó mới `_notifier.NotifyStateTransition(player, CharacterStateTransitionReasons.CombatDead);`
- `GameServer/Runtime/CharacterRuntimeNotifier.cs` xác nhận `NotifyCurrentStateChanged(...)` và `NotifyStateTransition(...)` là 2 packet riêng biệt

Expected:
- packet HP/state snapshot đi trước, transition packet đi sau khi penalty persist xong

Actual:
- ordering đúng reviewer accepted risk

### E2. Không còn captive scoped DI và không còn sync-over-async trên hot path
**Pass ở mức implementation evidence**

Evidence:
- `GameServer/Runtime/CharacterRuntimeService.cs`
  - constructor inject `IServiceScopeFactory`, không còn `DeathPenaltyService`
  - `ApplyDeathPenaltyAndNotifyAsync(...)` tạo `CreateAsyncScope()` rồi resolve `DeathPenaltyService`
  - không còn `.GetAwaiter().GetResult()`
- `GameServer/Extensions/ServiceCollectionExtensions.cs`
  - `CharacterRuntimeService` vẫn là singleton, `DeathPenaltyService` resolve theo scope runtime call là phù hợp reviewer fix

Expected:
- không còn captive dependency và blocking DB call trong sync path

Actual:
- implementation đúng expectation

## F. Duplicate death-apply guard

### F1. Không apply penalty lặp lại khi đã ở `CombatDead`
**Pass ở mức implementation evidence**

Evidence:
- `CharacterRuntimeService.NotifyDeathTransitionIfNeeded(...)`
- guard:
  - `wasCombatDead = CharacterRuntimeStateCodes.IsCombatDead(previousState.CurrentState)`
  - return nếu `wasCombatDead || !isCombatDead || currentState.IsExpired`

Expected:
- chỉ transition non-dead -> combat-dead mới queue death penalty

Actual:
- đúng expectation

# Expected vs Actual

## Case A — pending-delete gates
- Expected: list vẫn hiện; data/world/recovery bị gate đúng code
- Actual: `GetCharacterList` không filter; `GetCharacterData`, `EnterWorld`, `ReturnHomeAfterCombatDeath` đều có gate rõ ràng
- Verdict: Pass

## Case B — confirm permanent delete
- Expected: pending-delete confirm xóa sạch graph; lần 2 trả `CharacterNotFound`; non-pending trả `CharacterPendingPermanentDeletion`
- Actual: service path match đúng 3 case trên
- Verdict: Pass

## Case C — realm death mutation
- Expected: realm 1–18 trừ lifespan days; realm 19+ trừ `next_tribulation_at_utc`; lifespan cạn thì mark pending-delete
- Actual: `DeathPenaltyService` triển khai đúng theo công thức/scope reviewer handoff mô tả
- Verdict: Pass

## Case D — buff clear semantics
- Expected: clear skill-origin statuses, giữ talisman/formation
- Actual: source-tag runtime + `ClearBySource(Skill)` đáp ứng đúng boundary
- Verdict: Pass

## Case E — packet ordering / async fix
- Expected: `CharacterCurrentStateChanged` trước, `CharacterStateTransition(CombatDead)` sau; không còn captive scoped DI / `.GetAwaiter().GetResult()`
- Actual: current runtime path đúng expectation
- Verdict: Pass

# Concrete Evidence Pointers

- `GameServer/Runtime/CharacterRuntimeService.cs`
- `GameServer/Services/DeathPenaltyService.cs`
- `GameServer/Runtime/CombatStatusRuntime.cs`
- `GameServer/Runtime/SkillExecutionService.cs`
- `GameServer/Network/Handlers/GetCharacterListHandler.cs`
- `GameServer/Network/Handlers/GetCharacterDataHandler.cs`
- `GameServer/Network/Handlers/ReturnHomeAfterCombatDeathHandler.cs`
- `GameServer/Network/Handlers/ConfirmPermanentCharacterDeletionHandler.cs`
- `GameServer/Services/PermanentCharacterDeletionService.cs`
- `GameServer/Services/WorldEntryService.cs`
- `GameServer/Entities/Character.cs`
- `GameServer/Entities/CharacterCurrentState.cs`
- `GameServer/DTO/CharacterDto.cs`
- `GameServer/DTO/CharacterCurrentStateDto.cs`
- Build log from `dotnet build GameServer/GameServer.csproj -v minimal`

# Known Limits / Residual Risks

1. QA lượt này xác minh bằng implementation evidence + build pass; chưa có live runtime packet capture hay DB before/after snapshot cho từng case.
2. Reviewer accepted risk vẫn giữ nguyên authority:
   - nếu async penalty DB write fail sau khi client đã nhận `CharacterCurrentStateChanged(HP=0)`, server chỉ log lỗi; penalty DB mutation có thể không persist cho lần chết đó.
   - đây là risk đã được reviewer chấp nhận ở #42, không phải fail mới của QA.
3. Tribulation actual battle trigger vẫn chưa nối runtime; chỉ persist `next_tribulation_at_utc`. Đây là defer hợp lệ theo TD authority, ngoài scope slice QA này.

# Result Summary

**Passed**

# Next Owner

**techdesign**

# Recommended Next Action

TechDesign đánh giá client impact và tạo handoff `dev-client` nếu Unity client cần implement hoặc đổi behavior. Không route thẳng sang `release`.

Nếu muốn tăng độ chắc chắn sau handoff này, nên bổ sung integration test hoặc packet-level repro cho:
1. realm 1–18 chết và pending-delete khi lifespan cạn
2. realm 19+ chết với nhiều giá trị `next_tribulation_at_utc`
3. confirm-delete end-to-end và verify DB graph đã sạch
4. packet ordering khi có network delay / DB chậm

# Retest Scope

Nếu sau này có thay đổi tiếp ở death penalty runtime, QA nên retest tối thiểu:
1. pending-delete gates (`GetCharacterData`, `EnterWorld`, `ReturnHomeAfterCombatDeath`)
2. confirm permanent deletion graph cleanup
3. realm split logic (1–18 vs 19+)
4. `ClearBySource(Skill)` không sweep nhầm `Talisman` / `Formation`
5. death transition packet ordering + async error handling
