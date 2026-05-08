# 08. Error And Block Cases

## Input sai / malformed

### Invalid packet fields

- error source: packet validators / annotation validation
- error message/code: `ValidationFailed` hoặc code chuyên biệt như `CharacterNameInvalid`, `MapIdInvalid`
- user-facing hay internal: user-facing qua result packet
- log ở đâu: không phải lúc nào cũng log; phần lớn trả packet fail
- hiện tại có recover không: có, user có thể gửi lại request hợp lệ
- Source: `GameServer/Network/Validations/*.cs`

### Invalid character name

- error source: `CreateCharacterPacketValidator`, `CharacterService`
- error message/code: `CharacterNameInvalid`
- user-facing hay internal: user-facing
- log ở đâu: none explicit
- hiện tại có recover không: có, đổi tên hợp lệ
- Source: `GameServer/Network/Validations/CreateCharacterPacketValidator.cs`, `GameServer/Services/CharacterService.cs`

### Invalid reconnect token

- error source: `NetworkServer.TryResumeSession`
- error message/code: `ReconnectTokenInvalid`, `ReconnectSessionExpired`, `AccountLoggedInElsewhere`
- user-facing hay internal: user-facing
- log ở đâu: network/session log khi relevant
- hiện tại có recover không: login lại
- Source: `GameServer/Network/NetworkServer.cs`

## State không hợp lệ

### Character actions restricted

- error source: `CharacterActionRestrictionMiddleware`
- error message/code: `CharacterActionsRestricted`
- user-facing hay internal: user-facing
- log ở đâu: server log `Restricted character action rejected`
- hiện tại có recover không: recover bằng return home / re-enter / state transition phù hợp
- Source: `GameServer/Network/Middleware/CharacterActionRestrictionMiddleware.cs`

### Character not in world for world actions

- error source: handlers như pickup/travel/map zones/alchemy status
- error message/code: `CharacterMustEnterWorld`, `CharacterNotInWorldInstance`
- user-facing hay internal: user-facing
- log ở đâu: tùy handler; không phải lúc nào cũng log
- hiện tại có recover không: enter world lại
- Source: `GameServer/Network/Handlers/PickupGroundRewardHandler.cs`, `GetAlchemyPracticeStatusHandler.cs`

### Wrong character state for action

- error source: `WorldInteractionGate`, `CharacterCultivationService`, `PracticeService`
- error message/code: state-block codes, cultivation/practice fail codes
- user-facing hay internal: user-facing
- log ở đâu: limited
- hiện tại có recover không: dừng state hiện tại hoặc chờ trạng thái kết thúc
- Source: `GameServer/Runtime/WorldInteractionGate.cs`, `GameServer/Runtime/CharacterCultivationService.cs`, `GameServer/Services/PracticeService.cs`

### Character not combat dead but requests return home

- error source: `ReturnHomeAfterCombatDeathHandler`
- error message/code: `CharacterNotCombatDead`
- user-facing hay internal: user-facing
- log ở đâu: none explicit
- hiện tại có recover không: no-op, player tiếp tục chơi
- Source: `GameServer/Network/Handlers/ReturnHomeAfterCombatDeathHandler.cs`

## Thiếu resource / requirement

### Not enough inventory quantity

- error source: `ItemService`, `AlchemyService`
- error message/code: invalid operation or alchemy `FailureReason`
- user-facing hay internal: user-facing once mapped in result/status text
- log ở đâu: client warn hoặc server exception handling
- hiện tại có recover không: có, kiếm thêm item
- Source: `GameServer/Services/ItemService.cs`, `GameServer/Services/AlchemyService.cs`

### Not enough mandatory alchemy inputs

- error source: `AlchemyService.ValidateCraftPillAsync`
- error message/code: `"Khong du nguyen lieu bat buoc de luyen dan."`
- user-facing hay internal: user-facing
- log ở đâu: result packet/status text
- hiện tại có recover không: có, thêm nguyên liệu
- Source: `GameServer/Services/AlchemyService.cs`

### Skill slot empty / skill blocked

- error source: `SkillService`, `AttackEnemyHandler`
- error message/code: `SkillLoadoutSlotEmpty`, `SkillLoadoutBlocked`, `SkillNotLearned`
- user-facing hay internal: user-facing
- log ở đâu: client warning/status
- hiện tại có recover không: có, đổi loadout hoặc equip đúng nguồn skill
- Source: `GameServer/Services/SkillService.cs`, `GameServer/Network/Handlers/AttackEnemyHandler.cs`

### Breakthrough requirement not met

- error source: `CharacterCultivationService`
- error message/code: breakthrough-related code
- user-facing hay internal: user-facing
- log ở đâu: none explicit
- hiện tại có recover không: tiếp tục cultivation tới cap hoặc fail/retry
- Source: `GameServer/Runtime/CharacterCultivationService.cs`

## Cheat / suspicious / authoritative clamp

### Suspicious movement overshoot

- error source: `GameLoop.LogSuspiciousMovementIfNeeded`
- error message/code: không trả code; log diagnostic only
- user-facing hay internal: internal
- log ở đâu: server log `[PositionSync] clamp movement ...`
- hiện tại có recover không: server vẫn clamp movement hợp lệ
- Source: `GameServer/Runtime/GameLoop.cs`

### Out-of-range interaction/combat

- error source: `WorldInteractionGate`, `AttackEnemyHandler`, `TravelToMapHandler`, `PickupGroundRewardHandler`
- error message/code: interaction/range-related codes
- user-facing hay internal: user-facing
- log ở đâu: not always explicit
- hiện tại có recover không: move closer and retry
- Source: `GameServer/Runtime/WorldInteractionGate.cs`

### Duplicate login on another device

- error source: `NetworkServer.DisconnectDuplicateSessions`
- error message/code: `AccountLoggedInElsewhere`, message `"Tai khoan da duoc dang nhap tren thiet bi khac."`
- user-facing hay internal: user-facing
- log ở đâu: server info log
- hiện tại có recover không: old session bị kick, user login lại
- Source: `GameServer/Network/NetworkServer.cs`

## DB / network failures

### Inbound packet deserialize exception

- error source: `NetworkServer.OnNetworkReceive`
- error message/code: none to user
- user-facing hay internal: internal
- log ở đâu: packet incident capture + server log
- hiện tại có recover không: packet dropped; connection stays
- Source: `GameServer/Network/NetworkServer.cs`

### Unhandled packet processing exception

- error source: `NetworkServer.DispatchWithIncidentCaptureAsync`
- error message/code: none standardized to user
- user-facing hay internal: internal first
- log ở đâu: packet incident capture + server error log
- hiện tại có recover không: request fails; server keeps running
- Source: `GameServer/Network/NetworkServer.cs`

### Periodic runtime save / settlement failure

- error source: `RuntimeMaintenanceService`
- error message/code: none to user directly
- user-facing hay internal: internal
- log ở đâu: server error log
- hiện tại có recover không: next tick/next interval may retry
- Source: `GameServer/Runtime/RuntimeMaintenanceService.cs`

## Config missing / schema mismatch

### Missing item/skill definition referenced by runtime

- error source: item/skill/enemy/map definition catalogs or sync services
- error message/code: usually `InvalidOperationException`
- user-facing hay internal: often internal first, may surface as unknown failure
- log ở đâu: server exception log
- hiện tại có recover không: requires data fix
- Source: `GameServer/Services/SkillService.cs`, `GameServer/Services/ItemService.cs`, `GameServer/World/MapCatalog.cs`

### Missing seeded `game_configs` keys

- error source: config bootstrap mismatch
- error message/code: not immediate error if code default exists
- user-facing hay internal: internal/config debt
- log ở đâu: not necessarily logged
- hiện tại có recover không: add missing rows or restart with fixed config
- Source: `GameServer/Config/GameConfigKeys.cs`, `database/initDatabase.sql`

### `initDatabase.sql` not full one-shot schema

- error source: DB setup process
- error message/code: setup-time failure if base tables absent
- user-facing hay internal: internal/devops
- log ở đâu: DB execution error
- hiện tại có recover không: run base schema first
- Source: `database/phamnhan_online.sql`, `database/initDatabase.sql`

## Client display / UX failure cases

### Connection lost popup and recovery

- error source: `WorldConnectionRecoveryController`, `LoginScreenController`
- error message/code: popup text like `Mat ket noi`
- user-facing hay internal: user-facing
- log ở đâu: client logs for fallback issues
- hiện tại có recover không: reconnect flow or forced logout confirm
- Source: `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/UI/World/WorldConnectionRecoveryController.cs`, `UI/Screens/Login/LoginScreenController.cs`

### Missing serialized references in scene/prefab

- error source: many controllers validate required refs
- error message/code: `ClientLog.Error(...)` or throw `InvalidOperationException`
- user-facing hay internal: internal/dev content wiring
- log ở đâu: Unity console/client logs
- hiện tại có recover không: usually no until prefab/scene fixed
- Source: `WorldSceneController.cs`, `PersistentWorldUIController.cs`, `LocalCharacterActionController.cs`

### Placeholder UI without server flow

- error source: quest/guild/smithing/talisman tabs
- error message/code: placeholder text, unsupported panel state
- user-facing hay internal: user-facing
- log ở đâu: none or optional client warnings
- hiện tại có recover không: no, feature not implemented
- Source: `WorldMenuController.cs`, `WorldCraftingPanelController.cs`
