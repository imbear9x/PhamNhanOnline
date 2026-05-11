---
title: Reconnect and session resume runtime
doc_type: system
status: verified
owner: knowledge-manager
last_verified: 2026-05-12
source_of_truth:
  - docs/implementation/extractions/reconnect-runtime-extraction.md
related_code:
  - GameServer/Network/NetworkServer.cs
  - GameServer/Network/Handlers/LoginHandler.cs
  - GameServer/Network/Handlers/ReconnectHandler.cs
  - GameServer/Config/GameConfigKeys.cs
---

# Reconnect and session resume runtime

## Scope

Canonical account/session-level reconnect behavior. This doc does not claim direct world or character reattachment beyond what the reconnect handler proves.

## Runtime behavior

- `LoginHandler` issues a resume token through `INetworkSender.IssueResumeToken(...)` and includes it in the login result.
- `NetworkServer` stores resume tickets in memory keyed by token. Expiry is controlled by `network.reconnect_resume_window_seconds`.
- Tickets are cleared/revoked when sessions close or when resume succeeds.
- `ReconnectHandler` calls `TryResumeSession(...)`; success returns account id plus the same token, failure returns an error with empty account id.
- Resume is account/session recovery. Character snapshot reload and world restoration are downstream flow, not directly performed by this handler.

## Guards and limits

- Empty/whitespace tokens fail.
- Expired or revoked tickets are removed and reported as failed attempts.
- Resume tickets are in-memory; server restart persistence was not evidenced.

## Verification

Supported by `docs/implementation/extractions/reconnect-runtime-extraction.md` and listed code paths.
