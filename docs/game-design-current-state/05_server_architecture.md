# 05. Server Architecture

## Entry points

- Process bootstrap nằm ở `GameServer/Program.cs`.
- Startup tạo DI container, load config JSON + DB-backed config snapshot, start:
  - `NetworkServer`
  - `GameLoop`
  - `RuntimeMaintenanceService`
  - `ServerMetricsLoggerService`
- Program còn có CLI modes như `sync-game-time-config` và `preview-random-table`.

Source: `GameServer/Program.cs`

## Main layers

### Network / transport

- `NetworkServer` dùng `LiteNetLib` UDP.
- Mỗi peer có `ConnectionSession`, inbound packet queue và processor task riêng.
- `PacketDispatcher` route packet tới handler theo DI.
- Packet exception/deserialization exception được capture vào incident log.

Source: `GameServer/Network/NetworkServer.cs`

### Middleware

- `AuthMiddleware`: chặn packet có `[RequireAuth]` nếu session chưa authenticated.
- `RateLimitMiddleware`: rate limit packet realtime theo `PacketTransportPolicy`.
- `PacketValidationMiddleware`: chạy validator cụ thể hoặc annotation-based validation.
- `CharacterActionRestrictionMiddleware`: chặn action khi character expired/combat dead/restricted, trừ vài packet cho recovery/entry.

Source: `GameServer/Network/Middleware/*.cs`

### Handlers

- Handlers tương đối thin.
- Chúng làm 3 việc chính:
  - đọc packet/session
  - gọi service/runtime layer
  - đóng gói result packet trả client
- Ví dụ: `AttackEnemyHandler`, `TravelToMapHandler`, `UseItemHandler`, `CraftPillHandler`.

Source: `GameServer/Network/Handlers/*.cs`

### Services

- Service layer chứa nghiệp vụ persistent/domain-oriented:
  - account/character
  - inventory/equipment/item use
  - martial art/skill
  - alchemy/practice/notification
  - herb/farming partial
- Nhiều service dùng repository + `GameDb` transaction.

Source: `GameServer/Extensions/ServiceCollectionExtensions.cs`, `GameServer/Services/*.cs`

### Runtime / world

- Runtime layer chứa state sống trong RAM và tick liên tục:
  - player runtime snapshot
  - map instance
  - enemy runtime
  - combat statuses
  - desired movement target
  - ground rewards
  - pending skill executions
- `WorldRuntimeSettlementService` là hub settle runtime events của một instance.
- `GameLoop` tick 50ms.
- `RuntimeMaintenanceService` tick 50ms cho save/refresh/cultivation/alchemy settlement cleanup.

Source: `GameServer/Runtime/GameLoop.cs`, `GameServer/Runtime/RuntimeMaintenanceService.cs`, `GameServer/Runtime/WorldRuntimeSettlementService.cs`

## Domain models

- DB entities: `GameServer/Entities/*`
- DTO snapshots for runtime/service boundary: `GameServer/DTO/*`
- Shared packet models sent to client: `GameShared/Models/*`
- Runtime records/definitions:
  - `MapDefinition`, `MapPortalDefinition`
  - `SkillDefinition`, `SkillEffectDefinition`
  - `ItemDefinition`
  - `RealmTemplate`
  - `MapEnemySpawnGroupDefinition`

Source: `GameServer/Entities/*.cs`, `GameServer/Runtime/*.cs`, `GameShared/Models/*.cs`

## Runtime state

### Online player state

- `WorldManager` giữ `OnlinePlayers`.
- Mỗi `PlayerSession` có:
  - runtime base/current state
  - connection ownership
  - current map/instance/zone
  - desired movement target
  - visible character set
  - combat statuses
  - pending casts

Source: `GameServer/World/WorldManager.cs`, `GameServer/World/PlayerSession.cs`

### Map instance state

- `MapManager` giữ dictionary `mapId -> instanceId -> MapInstance`.
- Public map zone dùng `zoneIndex` như instance id.
- Private home/solo instance dùng generated instance id.
- `MapInstance` giữ:
  - players in instance
  - monsters
  - ground rewards
  - pending runtime event queues
  - expiration/completion metadata

Source: `GameServer/World/MapManager.cs`, `GameServer/World/MapInstance*.cs`

### Config state

- JSON bootstrap:
  - `gameTimeConfig.json`
  - `CharacterCreateConfig.json`
  - `dbConfig.json`
- DB-backed config:
  - `game_time_state`
  - `game_configs`
- Definition catalogs load eagerly from DB on startup:
  - map
  - item
  - combat
  - alchemy
  - enemy
  - potential tiers

Source: `GameServer/Extensions/ServiceCollectionExtensions.ConfigBuilders.cs`, `GameServer/World/MapCatalog.cs`, `GameServer/Runtime/*Catalog.cs`

## Persistence flow

- Most CRUD uses repository classes over `GameDb`.
- Inventory mutations wrap in `PlayerInventoryTransactionService`, which acquires PostgreSQL advisory lock per player.
- Online runtime state is not persisted every tick immediately.
- Dirty flags are flushed by `CharacterRuntimeSaveService`:
  - periodic save
  - disconnect
  - explicit flush on key transitions
- Save service resets persisted `Casting` state to `Idle` intentionally.

Source: `GameServer/Services/PlayerInventoryTransactionService.cs`, `GameServer/Runtime/CharacterRuntimeSaveService.cs`

## Validation

- Validation split across layers:
  - packet shape validation in middleware
  - domain validation inside services
  - world state validation inside runtime gate/services
- `WorldInteractionGate` is the most important action gate for combat/pickup/portal-like interactions.
- It can settle runtime before action, re-check state/target/range, and return unified failure codes.

Source: `GameServer/Network/Validations/*.cs`, `GameServer/Runtime/WorldInteractionGate.cs`

## Error handling

- Domain/business failures usually surface as `GameException(MessageCode)`.
- Handlers convert them into result packets.
- Transport/runtime exceptions are logged and packet incident records are captured.
- Some flows also return human-readable `FailureReason`, especially alchemy craft preview/action.

Source: `GameServer/Services/*.cs`, `GameServer/Network/NetworkServer.cs`, `GameServer/Network/Handlers/CraftPillHandler.cs`

## Logging / metrics

- Uses `GameShared.Logging.Logger`.
- Metrics recorded for:
  - outbound/inbound packets
  - tick duration/overrun
  - dropped inbound packets
  - processing exceptions
- Movement clamp suspicion is logged when client desired movement is too aggressive.

Source: `GameServer/Network/NetworkServer.cs`, `GameServer/Runtime/GameLoop.cs`, `GameServer/Diagnostics/*`

## Cache / Redis

- Không thấy Redis hoặc external cache trong code hiện tại.
- Primary state stores là:
  - process RAM for online/runtime/combat
  - PostgreSQL for persistent state/config

Source: repo search under `GameServer`

## Server authoritative vs client presentation

### Logic server quyết định

- account auth success/fail
- character ownership
- world membership and instance selection
- actual authoritative map/zone/position
- portal validity and spawn point
- combat target/range/state/cooldown
- damage/heal/shield/stun/stat modifier application
- enemy AI/spawn/death/reward
- inventory ownership/location/quantity
- equip/unequip final stat recalculation
- cultivation settle/breakthrough/potential spending
- alchemy input validation, practice session completion, reward grant

Source: `GameServer/Services/WorldEntryService.cs`, `GameServer/Runtime/GameLoop.cs`, `GameServer/Runtime/SkillExecutionService.cs`, `GameServer/Services/ItemService.cs`, `GameServer/Runtime/CharacterCultivationService.cs`

### Logic client chủ yếu hiển thị

- world scene visuals and movement presentation
- target selection/pinning
- button/panel state
- portal/world interaction UX
- skill VFX/timeline presentation
- status popups and notifications

Source: `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/*.cs`, `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/UI/**/*.cs`

## Client trust risks

- Movement: client vẫn gửi position-derived intent; server đã harden thành desired target clamp, nhưng chưa thấy collision/path validation phức tạp.
- Local pseudo-portals: `LocalFixPortalPresenter` mở panel local qua target `Npc` giả; đây không phải security issue lớn, nhưng là UX path không đi qua server world config.
- Inventory UI có nhiều placeholder/use branches phía client; fortunately authoritative item use vẫn ở server.
- Client auto-select character đầu tiên; nếu sau này hỗ trợ nhiều character thì UX/business rule cần rõ hơn.

Source: `GameServer/Network/Handlers/CharacterPositionSyncHandler.cs`, `GameServer/Runtime/GameLoop.cs`, `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/LocalFixPortalPresenter.cs`, `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/Auth/Application/ClientLoginFlowService.cs`

## Anti-cheat / suspicious action handling

- Không thấy dedicated anti-cheat module/service.
- Hardening hiện tại nằm ở:
  - auth/session ownership
  - rate limit middleware
  - packet validation
  - range/state gate
  - server-side movement clamp
  - server-side inventory transaction lock
  - server-side authoritative skill/loot validation

Source: `GameServer/Network/Middleware/*.cs`, `GameServer/Runtime/WorldInteractionGate.cs`, `GameServer/Services/PlayerInventoryTransactionService.cs`
