# title
Notifications runtime extraction

# scope
Unread notification push, online delivery, deduplicated unread creation, and player acknowledgement flow.

# source files
- `GameServer/Services/PlayerNotificationService.cs`
- `GameServer/Network/Handlers/AcknowledgePlayerNotificationHandler.cs`
- `GameServer/DTO/PlayerNotificationModelBuilder.cs`
- `GameServer/Services/AlchemyPracticeService.cs`
- `GameShared/Packets/Packets/CharacterPackets.cs`

# current runtime behavior
- `PlayerNotificationService.PushUnreadAsync` loads unread notifications for the online player session and sends each one immediately as `PlayerNotificationReceivedPacket` (`GameServer/Services/PlayerNotificationService.cs`).
- `PushToOnlinePlayerAsync` fetches a specific notification by id and only sends it if it still belongs to the player and remains unread (`GameServer/Services/PlayerNotificationService.cs`).
- `CreateAsync` always inserts a new notification row, optionally pushing it to the online player right away (`GameServer/Services/PlayerNotificationService.cs`).
- `EnsureUnreadAsync` checks for the latest unread notification with the same `(player, notification type, source type)` tuple; if one exists it reuses/pushes that row instead of inserting a duplicate unread row (`GameServer/Services/PlayerNotificationService.cs`).
- `AcknowledgeAsync` marks a notification as read if it belongs to the requesting player's character and has not already been acknowledged (`GameServer/Services/PlayerNotificationService.cs`).
- Alchemy practice completion is one visible producer path that sends notification packets/results into this subsystem (`GameServer/Services/AlchemyPracticeService.cs`).

# validations / guards
- Push methods no-op when the player/session is not online (`GameServer/Services/PlayerNotificationService.cs`).
- Acknowledge rejects non-world sessions, missing/non-positive ids, nonexistent notifications, and wrong-owner notifications (`GameServer/Services/PlayerNotificationService.cs`).
- Already-read notifications are not re-marked, but acknowledgement still succeeds with the existing id (`GameServer/Services/PlayerNotificationService.cs`).

# config/data dependencies
- Notification repository tables and notification model builder.
- World manager online-player lookup for push-if-online behavior.
- Producer systems such as alchemy practice completion.

# client/server touch points
- Server pushes `PlayerNotificationReceivedPacket` asynchronously.
- Client acknowledges through `AcknowledgePlayerNotificationPacket`; handler returns `AcknowledgePlayerNotificationResultPacket`.

# edge cases
- `EnsureUnreadAsync` deduplicates only against the latest unread by type/source, not against arbitrary payload equality.
- Unread push iterates notification-by-notification; no batch packet is visible.

# unclear or suspicious behavior
- Notification lifecycle beyond unread/read is minimal in these files; no archival, pagination, or delete behavior is visible.
- Deduplication key excludes richer payload identity, so semantically different notifications of the same type/source could collapse if emitted while one is still unread.

# suggested canonical target docs
- `docs/systems/player-notification-runtime.md`
