# title
Reconnect runtime extraction

# scope
Session resume token issuance, reconnect packet handling, and server-side resume window behavior.

# source files
- `GameServer/Network/NetworkServer.cs`
- `GameServer/Network/Handlers/ReconnectHandler.cs`
- `GameServer/Network/Handlers/LoginHandler.cs`
- `GameServer/Config/GameConfigKeys.cs`
- `GameServer/Config/GameConfigValues.cs`
- `GameShared/Packets/Packets/CharacterPackets.cs`

# current runtime behavior
- `LoginHandler` issues a resume token through `INetworkSender.IssueResumeToken(...)` and includes it in the login result packet (`GameServer/Network/Handlers/LoginHandler.cs`).
- `NetworkServer` stores resume tickets keyed by token, with expiry controlled by configured resume window, and clears/revokes tokens when sessions close or resume succeeds (`GameServer/Network/NetworkServer.cs`).
- `ReconnectHandler` passes `packet.ResumeToken` to `TryResumeSession(...)`; on success it returns account id plus the same token, and on failure it returns the error code with empty account id (`GameServer/Network/Handlers/ReconnectHandler.cs`).
- Resume is account/session-level recovery; this handler itself does not reload world state or character snapshot directly (`GameServer/Network/Handlers/ReconnectHandler.cs`, `GameServer/Network/NetworkServer.cs`).

# validations / guards
- Empty/whitespace resume tokens are rejected in `NetworkServer.TryResumeSession(...)` (`GameServer/Network/NetworkServer.cs`).
- Expired or revoked tickets are removed and reported as failed resume attempts (`GameServer/Network/NetworkServer.cs`).
- Reconnect result is always explicit success/failure; handler does not throw domain-specific exceptions itself (`GameServer/Network/Handlers/ReconnectHandler.cs`).

# config/data dependencies
- Config key: `network.reconnect_resume_window_seconds` (`GameServer/Config/GameConfigKeys.cs`).
- In-memory resume ticket dictionaries inside `NetworkServer`; no persistent reconnect store is visible.

# client/server touch points
- Login response delivers `ResumeToken` for later reconnect use.
- `ReconnectPacket` / `ReconnectResultPacket` form the resume handshake surface.

# edge cases
- Server restart would drop in-memory resume tickets unless another layer persists them; no persistence is visible here.
- Success result returns account id, but downstream character/world restoration depends on later flow outside this handler.

# unclear or suspicious behavior
- Reconnect flow is documented at account/session level, but these files alone do not show whether character reattachment is immediate or deferred to later packets.
- Token lifecycle is centralized in `NetworkServer`; canonical docs should be careful not to overstate guarantees not shown in handlers.

# suggested canonical target docs
- `docs/systems/reconnect-and-session-resume-runtime.md`
