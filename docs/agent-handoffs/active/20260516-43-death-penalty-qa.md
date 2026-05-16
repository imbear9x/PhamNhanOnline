---
title: QA Handoff — Death Penalty Blocker-Response Slice
doc_type: handoff
status: Done
owner: qa
source_agent: reviewer
last_updated: 2026-05-16
source_design_doc: docs/game-design-wp/requirements/death-penalty.md
source_tech_design_doc: docs/tech-design/death-penalty.md
expected_output: verification
queue_id: 43
feature_key: death-penalty
handoff_type: qa
source_handoff: docs/agent-handoffs/active/20260516-42-death-penalty-runtime-di-async-response.md
response_to: docs/agent-handoffs/active/20260516-42-death-penalty-runtime-di-async-response.md
iteration: 1
---

# Reviewer Verdict

**Pass with risks**

Slice blocker-response (#39 → #42) đã pass review. 2 blockers kỹ thuật đã được fix:
1. Captive Scoped DI trong Singleton — đã fix bằng `IServiceScopeFactory`.
2. Sync-over-async DB write trong game-loop — đã fix bằng fire-and-forget async helper có catch/log.

# Scope của slice này

Slice này **KHÔNG** cover toàn bộ death penalty feature. Chỉ cover phần blocker-response từ #39:
- Persist `pending_permanent_deletion`
- Persist `next_tribulation_at_utc`
- Gate pending-delete tại `GetCharacterList` / `GetCharacterData` / `EnterWorld` / `ReturnHomeAfterCombatDeath`
- Confirm permanent delete packet + hard-delete flow
- Combat status source tagging (`CombatStatusSourceType`) + `ClearBySource(Skill)` on death
- Runtime death mutation (trừ thọ/tribulation, mark pending-delete nếu hết thọ)

**Chưa có trong slice này** (defer theo TD authority):
- Drop linh thạch khi chết
- Drop item khi chết
- Tribulation trigger runtime (chỉ persist timestamp, chưa khởi động battle)

# Accepted Risks

## Risk 1 — Fire-and-forget ordering: penalty DB write sau notify HP=0

Sequence thực tế:
1. Client nhận `CharacterCurrentStateChanged` (HP=0, state=CombatDead) — **ngay lập tức**
2. Server async: DB write trừ thọ nguyên / tribulation
3. Client nhận `CharacterStateTransition(CombatDead)` — **sau** DB write xong

Nếu DB write fail: server log error, client vẫn ở state `CombatDead` nhưng penalty chưa áp vào DB. Character vẫn có thể hồi sinh bình thường; chỉ mất penalty của lần chết đó.

QA chú ý: verify client không bị flicker/trạng thái lạ giữa 2 packet khi có network delay bình thường.

## Risk 2 — Tribulation trigger chưa nối

`next_tribulation_at_utc` được persist/cập nhật đúng khi realm 19+ chết. Nhưng chưa có runtime hook để trigger actual tribulation battle khi countdown về 0. Deferred theo TD authority.

# QA Test Scope

## Must test

### A. Pending permanent deletion gates

1. **Character có `pending_permanent_deletion = true`** (setup thủ công hoặc qua lifespan expire):
   - `GetCharacterList`: character vẫn hiện trong list → **expected: hiện**
   - `GetCharacterData`: trả fail + `CharacterPendingPermanentDeletion` + snapshot payload → **expected: fail code + snapshot**
   - `EnterWorld`: bị reject trước recovery → **expected: reject**
   - `ReturnHomeAfterCombatDeath`: bị reject → **expected: reject**

2. **Confirm permanent delete**:
   - Gửi `ConfirmPermanentCharacterDeletionPacket` với CharacterId đúng
   - Expected: tất cả data character bị xóa, trả `Success = true`
   - Gửi lần 2 với cùng CharacterId: Expected: fail `CharacterNotFound`

3. **Confirm delete khi character không pending**:
   - Expected: fail `CharacterPendingPermanentDeletion` (guard đúng)

### B. Death penalty mutation khi chết

4. **Realm 1–18 chết**:
   - Expected: `LifespanBonus` giảm `ceil(death.lifespan_penalty_seconds / 86400)` ngày
   - Expected: Skill buff bị clear, Talisman/Formation buff giữ nguyên

5. **Realm 19+ chết**:
   - Expected: `next_tribulation_at_utc` giảm `death.tribulation_penalty_seconds` giây (hoặc floor về `utcNow`)
   - Expected: Skill buff bị clear

6. **Realm 1–18 chết khi thọ nguyên về 0** (lifespan đã cạn):
   - Expected: `pending_permanent_deletion = true` được persist
   - Expected: character không thể `EnterWorld` / `ReturnHome` sau đó

### C. Combat status source tagging

7. **Skill shield / stun / stat modifier**:
   - Apply skill buff → chết → Expected: buff bị clear
   - Apply bùa (Talisman) buff → chết → Expected: buff giữ nguyên

### D. Client packet ordering (risk verification)

8. **Khi chết, verify thứ tự packet client nhận**:
   - `CharacterCurrentStateChanged` (HP=0) đến trước
   - `CharacterStateTransition(CombatDead)` đến sau
   - Client hiển thị đúng, không flicker state lạ

## Should test nếu có tool

9. **2 death events cạnh tranh trên cùng character** (concurrent kill spam):
   - Expected: penalty chỉ áp 1 lần (guard `wasCombatDead` trong `NotifyDeathTransitionIfNeeded`)

# Source Chain

- TD spec: `docs/agent-handoffs/active/20260515-22-death-penalty-techdesign.md`
- TD blocker clarification: `docs/agent-handoffs/active/20260516-38-death-penalty-dev-blockers-techdesign.md`
- Dev implementation: `docs/agent-handoffs/active/20260516-39-death-penalty-dev-blockers-response.md`
- Reviewer fail (DI/async): `docs/agent-handoffs/active/20260516-40-death-penalty-reviewer.md`
- Dev fix: `docs/agent-handoffs/active/20260516-41-death-penalty-runtime-di-async-fix.md`
- Reviewer pass: `docs/agent-handoffs/active/20260516-42-death-penalty-runtime-di-async-response.md`

# Recommended QA Output

QA báo rõ:
- Pass/fail từng case A–D
- Nếu penalty áp sai realm → **blocker**
- Nếu pending-delete gate bị bypass → **blocker**
- Nếu confirm-delete không xóa sạch data → **blocker**
- Nếu Talisman/Formation buff bị clear khi chết → **regression/blocker**
- Nếu client flicker state → phân loại risk/bug và mô tả rõ
