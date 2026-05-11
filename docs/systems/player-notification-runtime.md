---
title: Player notification runtime
doc_type: system
status: verified
owner: knowledge-manager
last_verified: 2026-05-12
source_of_truth:
  - docs/implementation/extractions/notifications-runtime-extraction.md
related_code:
  - GameServer/Services/PlayerNotificationService.cs
  - GameServer/Network/Handlers/AcknowledgePlayerNotificationHandler.cs
  - GameServer/DTO/PlayerNotificationModelBuilder.cs
---

# Player notification runtime

## Runtime behavior

- `PushUnreadAsync` loads unread notifications for an online player and sends each as `PlayerNotificationReceivedPacket`.
- `PushToOnlinePlayerAsync` sends a specific notification only if it belongs to the player and remains unread.
- `CreateAsync` always inserts a new row and can push immediately.
- `EnsureUnreadAsync` reuses the latest unread notification with the same `(player, notification type, source type)` tuple instead of inserting a duplicate unread row.
- `AcknowledgeAsync` marks an unread owned notification as read.

## Client/server surface

- Server push: `PlayerNotificationReceivedPacket`.
- Client acknowledgement: `AcknowledgePlayerNotificationPacket` / result packet.

## Limits

- No archival, pagination, or delete behavior was evidenced.
- Deduplication excludes richer payload identity, so different notifications of same type/source can collapse while one remains unread.

## Verification

Supported by `docs/implementation/extractions/notifications-runtime-extraction.md`.
