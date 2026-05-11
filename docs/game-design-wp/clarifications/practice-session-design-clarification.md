# Practice Session Design Clarification

_Evidence base: `docs/qa/legacy-domain-coverage-audit.md` (domain: practice sessions, status: `partial`) and `docs/implementation/extractions/practice-sessions-runtime-extraction.md`._

---

## What the code confirms exists

From `docs/implementation/extractions/practice-sessions-runtime-extraction.md`:

- A **generic practice session state machine** exists in `GameServer/Services/PracticeService.cs`. It is not alchemy-specific at the service layer.
- State machine behavior:
  - Sessions are tracked by `StartedAtUtc`, `LastResumedAtUtc`, `AccumulatedActiveSeconds`, `TotalDurationSeconds`.
  - Progress is server-calculated (not client-trusted timer).
  - Sessions expose: progress %, remaining duration, cancel threshold, can-pause flag, can-cancel flag, and optional type-specific payload (parsed from JSON).
  - Pause, resume, cancel, acknowledge-result are all supported operations.
- **Private home restriction**: certain practice operations require the player to be in their private home map instance.
- **Session blocking**: active practice sets character runtime state to `Practicing`, which blocks other actions.
- **Alchemy-specific payload**: the generic model includes optional alchemy-specific rate-segment data parsed from stored JSON.
- **Client-visible packet surface found**: `GetAlchemyPracticeStatus`, `PausePractice`, `ResumePractice`, `CancelPractice`, `AcknowledgePracticeResult` — all are live handler paths.

**Code source files:** `GameServer/Services/PracticeService.cs`, `GameServer/Network/Handlers/GetAlchemyPracticeStatusHandler.cs`, `GameServer/Network/Handlers/PausePracticeHandler.cs`, `GameServer/Network/Handlers/ResumePracticeHandler.cs`, `GameServer/Network/Handlers/CancelPracticeHandler.cs`, `GameServer/Network/Handlers/AcknowledgePracticeResultHandler.cs`, `GameServer/Services/AlchemyPracticeService.cs`.

---

## The taxonomy question

The audit notes: _"The generic practice state machine exists, but the visible surfaced use case is mainly alchemy; broader intended practice taxonomy is not fully evident."_

The code has a **generic service** but only **alchemy-specific handlers** surfaced in this batch. This creates ambiguity: is practice session a shared system intended for multiple activity types, or is it currently alchemy-only with a generalized internal structure?

---

## Open questions (needs design answer)

### Question 1 — Intended scope of the practice session system

- Is the practice session system designed to be a **shared backbone** for multiple activity types (alchemy, cultivation, crafting, training, etc.)?
- Or is it currently **alchemy-only** and the generic structure is an implementation choice rather than a design signal of upcoming practice types?
- **Needed answer:** list of activity types that are intended to use the practice session system, including which are in current scope vs future scope.

### Question 2 — Practice types taxonomy

If practice sessions are multi-type:

- What are the intended **activity categories**? Examples to confirm or reject:
  - Alchemy pill-crafting (confirmed from code)
  - Cultivation (separate service exists — does it share practice sessions, or use a separate mechanism?)
  - Skill training / martial arts training
  - Crafting of non-pill items
  - Other time-based player activities
- For each type: does it share the same pause/resume/cancel/acknowledge flow, or do some types have different lifecycle rules?

### Question 3 — Private home restriction scope

- The code requires players to be in their private home map for certain practice operations. Is this:
  a. Required for **all** practice types (all activities must happen in home cave)?
  b. Required only for **alchemy** (thematic: alchemist works at home)?
  c. Configurable per practice type?
- **Needed answer:** which activity types require private home presence, and what is the design rationale?

### Question 4 — Concurrency and session blocking

- Can a player have **multiple practice sessions running at once** (e.g., herb growing + alchemy at the same time)?
- The code shows `Practicing` as a single blocking state — does this mean only one practice session is allowed at a time?
- If multi-session is intended in the future, does the system need to change, or is single-session-at-a-time an intentional design constraint?

### Question 5 — Cancel semantics and refunds

- When a player cancels a practice session, what do they get back?
  - Full input refund?
  - Partial refund based on progress (cancel threshold config)?
  - No refund?
- Is the cancel refund rule the same for all practice types, or type-specific?
- The code has a `cancel threshold` field in the session model — what is the intended player-facing meaning of this threshold? (e.g., "cannot cancel after 50% progress" or "partial refund only if cancelled before 30%"?)

### Question 6 — Acknowledge result flow

- After a practice session completes, the player must explicitly acknowledge the result before the state clears.
- Is this mandatory for all practice types, or only some?
- What is the intended UX for this acknowledge step? (Pop-up dialog, reward screen, auto-accept on next login?)
- What happens if a player logs out before acknowledging? Does the result persist and re-prompt on next login?

---

## Acceptable current behavior (what can be stated now)

- Generic practice session state machine exists and is functional for alchemy.
- Session timing is server-authoritative (not client-trusted).
- Pause/resume/cancel/acknowledge lifecycle is fully wired for the alchemy surface.
- Private home restriction is enforced for practice operations.
- Active practice session blocks other character actions via `Practicing` state.
- Cancel provides a configurable partial-refund path.
- Session model includes alchemy-specific rate-segment payload; other practice types could use different payloads in the same generic model structure.

---

## What must be resolved before canonicalization

1. **Define the practice taxonomy** (Question 1 and 2) — is this alchemy-only or shared? List confirmed types.
2. **Confirm private home scope** (Question 3) — which types require home presence?
3. **Confirm concurrency model** (Question 4) — single or multi-session?
4. **Define cancel semantics** (Question 5) — are refund rules type-specific or universal?
5. **Confirm acknowledge UX** (Question 6) — especially offline/resume behavior.

---

## Canonicalization recommendation

- Canonicalize the generic practice session lifecycle (pause/resume/cancel/acknowledge, timing model, blocking state) as a **shared system doc** once the taxonomy question is answered.
- Do **not** create a single alchemy-only doc and call it "practice sessions" if the intent is multi-type — that will need to be re-opened immediately.
- If only alchemy is confirmed for current scope: canonicalize as alchemy-practice, and note the generic service layer as an implementation detail open for future practice types.
- The private home restriction and single-session concurrency rule should be called out explicitly in the canonical doc once confirmed.
