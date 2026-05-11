# title
Practice sessions runtime extraction

# scope
Generic practice-session state machine, private-home restriction, pause/resume/cancel/acknowledge flow, and current-state alignment.

# source files
- `GameServer/Services/PracticeService.cs`
- `GameServer/Network/Handlers/GetAlchemyPracticeStatusHandler.cs`
- `GameServer/Network/Handlers/PausePracticeHandler.cs`
- `GameServer/Network/Handlers/ResumePracticeHandler.cs`
- `GameServer/Network/Handlers/CancelPracticeHandler.cs`
- `GameServer/Network/Handlers/AcknowledgePracticeResultHandler.cs`
- `GameServer/Services/AlchemyPracticeService.cs`

# current runtime behavior
- `PracticeService` treats `CharacterRuntimeStateCodes.Practicing` as the blocking online-state marker for active practice sessions (`GameServer/Services/PracticeService.cs`).
- `TryValidatePrivateHome` requires the player to be in their private home map instance and not in expired/lifespan-restricted state before certain practice actions can proceed (`GameServer/Services/PracticeService.cs`).
- Session progress is derived from `StartedAtUtc`, `LastResumedAtUtc`, `AccumulatedActiveSeconds`, and `TotalDurationSeconds`; pause/resume/cancel logic uses calculated progress rather than client-trusted timers (`GameServer/Services/PracticeService.cs`).
- `BuildSessionModel` returns a normalized practice-session model including progress, remaining duration, cancel threshold, can-pause/can-cancel flags, and optional alchemy-specific rate-segment summaries parsed from stored payload JSON (`GameServer/Services/PracticeService.cs`).
- `AlignSnapshotStateAsync` and `SyncOnlinePlayerState` reconcile persisted/runtime character state with whether a blocking practice session is actually active (`GameServer/Services/PracticeService.cs`).
- Handlers for pause/resume/cancel/acknowledge call into `PracticeService` and return the updated session or success code; cancel additionally refreshes inventory afterwards (`GameServer/Network/Handlers/*.cs`).

# validations / guards
- Non-world sessions fail mutation requests via service-level result codes (`GameServer/Services/PracticeService.cs`).
- Pause requires an active session and rejects once progress reaches the pause-lock condition (`GameServer/Services/PracticeService.cs`).
- Private-home validation rejects public/non-home maps and expired/lifespan-restricted characters (`GameServer/Services/PracticeService.cs`).
- Session ownership is enforced when resolving mutable sessions from handlers (`GameServer/Services/PracticeService.cs`).

# config/data dependencies
- Practice session repository rows persist timing, payload JSON, and state.
- Game config contributes cancel refund threshold and related practice constants (`GameServer/Services/PracticeService.cs`).
- Alchemy practice payload/result schemas are embedded into generic session model building.

# client/server touch points
- `GetAlchemyPracticeStatusPacket` returns practice status through `AlchemyPracticeService`.
- Pause/resume/cancel/acknowledge handlers expose direct session-control packets.
- Cancel flow also sends refreshed inventory snapshot after successful cancellation.

# edge cases
- Session model can report completion-level progress even when state is not yet acknowledged complete.
- If stored snapshot state says practicing but no blocking session exists, alignment can force the state back to idle.
- JSON payload parse issues would affect model enrichment, but core session state still lives in repository rows.

# unclear or suspicious behavior
- This subsystem is generic, but visible handler surface is still alchemy-specific for status retrieval.
- Pause lock returns `PracticeCancelLocked`, which suggests error-code reuse rather than a dedicated pause-lock code.

# suggested canonical target docs
- `docs/systems/practice-session-runtime.md`
- `docs/alchemy/alchemy-practice-session-runtime.md`
