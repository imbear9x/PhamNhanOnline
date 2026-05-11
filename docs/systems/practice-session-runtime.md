---
title: Practice session runtime
doc_type: system
status: verified
owner: knowledge-manager
last_verified: 2026-05-12
source_of_truth:
  - docs/implementation/extractions/practice-sessions-runtime-extraction.md
related_code:
  - GameServer/Services/PracticeService.cs
  - GameServer/Services/AlchemyPracticeService.cs
  - GameServer/Network/Handlers/PausePracticeHandler.cs
  - GameServer/Network/Handlers/ResumePracticeHandler.cs
  - GameServer/Network/Handlers/CancelPracticeHandler.cs
  - GameServer/Network/Handlers/AcknowledgePracticeResultHandler.cs
---

# Practice session runtime

## Runtime behavior

- `CharacterRuntimeStateCodes.Practicing` is the blocking online-state marker for active practice.
- Private-home validation requires the player to be in their private home map instance and not expired/restricted.
- Progress derives from server timestamps and accumulated active seconds, not client timers.
- Session models expose progress, remaining duration, cancel threshold, can-pause/can-cancel flags, and optional alchemy rate-segment summaries.
- Snapshot/runtime alignment can clear stale practicing state when no blocking session exists.

## Client/server surface

- Alchemy status is exposed through `GetAlchemyPracticeStatusPacket`.
- Generic controls: pause, resume, cancel, acknowledge result.
- Cancel returns refreshed inventory after successful cancellation.

## Limits

- The subsystem is generic, but visible status retrieval is currently alchemy-specific.
- Pause lock reuses `PracticeCancelLocked` result code.

## Verification

Supported by `docs/implementation/extractions/practice-sessions-runtime-extraction.md`.
