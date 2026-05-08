# 03. Feature Flows

## Feature: login

- Trigger: người chơi bấm nút connect ở Login scene
- Client sends: `LoginPacket(loginId, password)`
- Server receives: `LoginHandler`
- Server validates: packet validator, auth state chưa cần; `AccountService` normalize login id và verify PBKDF2 password hash
- Server changes state: `ConnectionSession.PlayerId`, `IsAuthenticated`, issue resume token
- Server saves DB: update `accounts.last_login`
- Server responds: `LoginResultPacket(success, code, accountId, resumeToken, ...)`
- Client updates UI: `ClientAuthService` complete task; `LoginScreenController` cập nhật status text
- Success case: client tiếp tục load character list
- Fail cases: invalid credentials, account missing, login id đã tồn tại nếu register
- Blocked cases: disconnected transport
- Important files: `GameServer/Network/Handlers/LoginHandler.cs`, `GameServer/Services/AccountService.cs`, `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/Auth/Application/ClientAuthService.cs`

## Feature: enter game

- Trigger: login thành công; client auto chọn character đầu tiên
- Client sends: `GetCharacterListPacket`, `GetCharacterDataPacket`, `EnterWorldPacket`
- Server receives: `GetCharacterListHandler`, `GetCharacterDataHandler`, `EnterWorldHandler`
- Server validates: auth middleware, packet validators, account owns character
- Server changes state: attach runtime player session, settle cultivation/alchemy due, ensure first enter time, ensure player in map instance
- Server saves DB: có thể update `characters.first_enter_world_at_utc`, flush due state later qua maintenance/save service
- Server responds: character snapshot packets, `EnterWorldResultPacket`, `MapJoinedPacket`, `WorldRuntimeSnapshotPacket`, unread notifications
- Client updates UI: `ClientCharacterService` apply character/base/current state, load inventory/martial arts/skills, load world scene
- Success case: vào map và thấy local player + enemy + reward snapshot
- Fail cases: character not found, actions restricted, world entry failure
- Blocked cases: session không authenticated hoặc character expired/combat dead chưa recover
- Important files: `GameServer/Services/WorldEntryService.cs`, `GameServer/World/WorldInterestService.cs`, `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/Auth/Application/ClientLoginFlowService.cs`

## Feature: move

- Trigger: local player di chuyển trong world
- Client sends: `CharacterPositionSyncPacket(CurrentPosX, CurrentPosY)` định kỳ theo local presenter
- Server receives: `CharacterPositionSyncHandler`
- Server validates: finite coordinates, player tồn tại, không defeated/cultivating/practicing/casting/stunned
- Server changes state: set/clear desired movement target trong runtime session
- Server saves DB: không save ngay; save định kỳ qua `CharacterRuntimeSaveService`
- Server responds: không trả packet sync trực tiếp cho request; game loop sau đó broadcast move qua `ObservedCharacterMovedPacket` và current state change nếu cần
- Client updates UI: local client vẫn tự move theo presentation; remote clients cập nhật observed position
- Success case: server kéo vị trí tới target theo move speed hợp lệ
- Fail cases: ignored packet khi state bị chặn hoặc map invalid
- Blocked cases: target ngoài clamp map, suspicious too-far intent bị clamp
- Important files: `GameServer/Network/Handlers/CharacterPositionSyncHandler.cs`, `GameServer/Runtime/GameLoop.cs`, `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/WorldLocalMovementSyncController.cs`

## Feature: attack / cast skill

- Trigger: player chọn target hostile và request primary action hoặc bấm nút skill
- Client sends: `AttackEnemyPacket(target, skillSlotIndex)`
- Server receives: `AttackEnemyHandler`
- Server validates: player in world, action gate pass, slot valid, skill equipped, cooldown pass, target type supported, target still alive, range pass
- Server changes state: enqueue pending skill execution vào `MapInstance`, start local cooldown/casting state
- Server saves DB: combat trạng thái chỉ ở RAM; current state có thể dirty khi vào casting
- Server responds: `AttackEnemyResultPacket`, sau đó broadcast `SkillCastStartedPacket` và `SkillImpactResolvedPacket`
- Client updates UI: set pending request/cooldown/local cast HUD; play skill presentation; update enemy/player HP
- Success case: skill cast và impact resolve trên runtime tick
- Fail cases: slot empty, skill blocked, target invalid, out of range, state blocked
- Blocked cases: AoE/all-map target types chưa support
- Important files: `GameServer/Network/Handlers/AttackEnemyHandler.cs`, `GameServer/Runtime/SkillExecutionService.cs`, `GameServer/Runtime/WorldRuntimeSettlementService.cs`, `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/Combat/Application/ClientCombatService.cs`

## Feature: receive damage

- Trigger: skill effect `Damage` resolve trên player hoặc enemy
- Client sends: none trực tiếp, đây là server runtime effect
- Server receives: internal runtime event từ `SkillExecutionService`
- Server validates: caster/target vẫn resolve được trong cùng instance
- Server changes state: giảm HP, absorb shield nếu có, mark combat dead nếu HP về 0, clear movement target nếu defeated
- Server saves DB: player current state dirty; enemy state chỉ ở RAM instance
- Server responds: `ObservedCharacterCurrentStateChangedPacket`, `EnemyHpChangedPacket`, `SkillImpactResolvedPacket`, optional `CharacterStateTransitionPacket`
- Client updates UI: stat bar, popup damage, death popup nếu local player combat dead
- Success case: HP giảm đúng, observers thấy đổi
- Fail cases: target despawn/disconnected trước impact
- Blocked cases: none; effect simply không apply nếu target invalid lúc resolve
- Important files: `GameServer/Runtime/CharacterRuntimeService.cs`, `GameServer/Runtime/SkillExecutionService.cs`, `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/UI/Hud/WorldCombatValuePopupController.cs`

## Feature: monster death

- Trigger: enemy HP về 0 do combat impact
- Client sends: none
- Server receives: internal `EnemyDeathRuntimeEvent`
- Server validates: contribution snapshots và reward rules theo template
- Server changes state: enqueue despawn/reward/progression
- Server saves DB: có thể flush player base/current state nếu được cộng cultivation/potential hoặc direct items
- Server responds: enemy despawn packet, ground reward spawn packet, inventory changes nếu direct grant
- Client updates UI: enemy biến mất, reward hiện ra, inventory có thể reload sau pickup/direct result noti
- Success case: kill -> reward event hoàn tất
- Fail cases: random table entry invalid, reward item definition missing
- Blocked cases: none cho death itself; reward target set có thể rỗng nếu không ai đủ điều kiện
- Important files: `GameServer/World/MapInstance.Combat.cs`, `GameServer/Runtime/EnemyRewardRuntimeService.cs`

## Feature: drop loot from inventory

- Trigger: player chọn `Vut ra` từ inventory popup
- Client sends: `DropInventoryItemPacket(playerItemId, quantity)`
- Server receives: `DropInventoryItemHandler`
- Server validates: item belongs to inventory, droppable, not equipped, quantity valid, player in world instance
- Server changes state: move item/stack split sang `LocationType.Ground`, spawn `GroundRewardEntity`
- Server saves DB: update/delete `player_items`, maybe split stack rows
- Server responds: `DropInventoryItemResultPacket`, then world ground reward spawn packets
- Client updates UI: reload inventory, ground reward appears in world
- Success case: item đổi từ inventory sang ground
- Fail cases: item not droppable, quantity invalid, not in world
- Blocked cases: equipped item or inserted soil cannot leave inventory
- Important files: `GameServer/Network/Handlers/DropInventoryItemHandler.cs`, `GameServer/Services/ItemService.cs`, `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/Inventory/Application/ClientInventoryService.cs`

## Feature: pickup item

- Trigger: player tới gần ground reward và request interaction
- Client sends: `PickupGroundRewardPacket(rewardId)`
- Server receives: `PickupGroundRewardHandler`
- Server validates: player entered world, reward exists in instance, action gate pass, within pickup radius, claim succeeds, inventory transaction succeeds
- Server changes state: ground item -> inventory merge, ground reward claim completed and despawned
- Server saves DB: update/delete/create `player_items`
- Server responds: `PickupGroundRewardResultPacket(granted items)` + ground reward despawn
- Client updates UI: clear selected target, force reload inventory, remove reward visual
- Success case: inventory nhận item và reward biến mất
- Fail cases: ownership/range invalid, claim conflict, inventory move failure
- Blocked cases: player state blocked by gate
- Important files: `GameServer/Network/Handlers/PickupGroundRewardHandler.cs`, `GameServer/Services/ItemService.cs`, `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Application/ClientGroundRewardService.cs`

## Feature: inventory change

- Trigger: enter world, item use, equip, unequip, drop, pickup, direct reward, craft consume/result
- Client sends: tùy action; đọc inventory qua `GetInventoryPacket`
- Server receives: `GetInventoryHandler` hoặc action-specific handlers
- Server validates: ownership, location, quantity, expiry, equip/soil restrictions
- Server changes state: mutate `player_items` and linked records
- Server saves DB: luôn qua repository + thường nằm trong `PlayerInventoryTransactionService`
- Server responds: `GetInventoryResultPacket` hoặc action result packet có `Items`
- Client updates UI: `ClientInventoryState` replace item list, world inventory panel refresh
- Success case: client state đồng bộ với authoritative inventory
- Fail cases: item invalid, not owned, unsupported use
- Blocked cases: stale local popup, disconnected
- Important files: `GameServer/Services/ItemService.cs`, `GameServer/Services/PlayerInventoryTransactionService.cs`, `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/Inventory/Application/ClientInventoryService.cs`

## Feature: equip item

- Trigger: player use equipment item hoặc explicit equip action
- Client sends: `EquipInventoryItemPacket(playerItemId, slotIndex)` hoặc generic `UseItemPacket` với equipment
- Server receives: `EquipInventoryItemHandler` hoặc `UseItemHandler`
- Server validates: item in inventory, equipment type valid, slot valid, target slot count valid
- Server changes state: set equipped slot, unequip item occupying same slot, sync equipment-granted skills, refresh final stats
- Server saves DB: update `player_equipments`, maybe `player_skills`/`player_skill_grant_sources`, persist base/current stats dirty later
- Server responds: updated items, base stats, current state, possibly owned skills snapshot
- Client updates UI: equipment panel/inventory/HUD stats refresh
- Success case: item equipped and stat/skill bonus active
- Fail cases: slot invalid, item not equipment, item missing
- Blocked cases: loadout rows with granted skill can be normalized out if requirement no longer met
- Important files: `GameServer/Services/EquipmentService.cs`, `GameServer/Services/EquipmentActionService.cs`, `GameServer/Services/SkillService.cs`

## Feature: quest accept / progress / complete

- Trigger: `Unknown / Need confirmation`
- Client sends: `Unknown / Need confirmation`
- Server receives: `Unknown / Need confirmation`
- Server validates: chưa thấy quest packet/domain flow hiện tại
- Server changes state: chưa thấy
- Server saves DB: chưa thấy quest table
- Server responds: chưa thấy
- Client updates UI: tab quest hiện placeholder text
- Success case: chưa implement
- Fail cases: chưa implement
- Blocked cases: feature thiếu server + DB + client flow
- Important files: `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/UI/World/WorldMenuController.cs`

## Feature: map change

- Trigger: dùng portal hoặc switch zone
- Client sends: `TravelToMapPacket` hoặc `SwitchMapZonePacket`
- Server receives: `TravelToMapHandler` hoặc `SwitchMapZoneHandler`
- Server validates: map/portal exists, current instance valid, target zone valid, action gate pass
- Server changes state: update map id / zone / position / instance membership / visibility
- Server saves DB: current state dirty, flush theo maintenance/save
- Server responds: travel result + new `MapJoinedPacket` + world snapshot
- Client updates UI: unload/rebuild map visual, respawn entities, refresh world state
- Success case: player xuất hiện ở map/zone mới
- Fail cases: invalid portal/map/zone, zone full, state blocked
- Blocked cases: private maps không hỗ trợ zone selection
- Important files: `GameServer/Network/Handlers/TravelToMapHandler.cs`, `GameServer/Network/Handlers/SwitchMapZoneHandler.cs`, `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Application/ClientWorldTravelService.cs`

## Feature: cultivation

- Trigger: player bấm start cultivation trong cultivation panel
- Client sends: `StartCultivationPacket`
- Server receives: `StartCultivationHandler`
- Server validates: entered world, ở private home instance, có active martial art, không practicing/casting/stunned/expired
- Server changes state: current state -> `Cultivating`, set timestamps
- Server saves DB: current state dirty, cultivation settle theo maintenance interval
- Server responds: `StartCultivationResultPacket(current state)`
- Client updates UI: panel khóa/mở nút phù hợp, reward popups nếu online settlement
- Success case: player vào trạng thái tu luyện
- Fail cases: thiếu active martial art, sai map/runtime state, lifespan expired
- Blocked cases: close panel khi đang cultivating cũng bị khóa ở client
- Important files: `GameServer/Runtime/CharacterCultivationService.cs`, `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/UI/World/WorldCultivationPanelController.cs`

## Feature: breakthrough

- Trigger: player bấm breakthrough
- Client sends: `BreakthroughPacket`
- Server receives: `BreakthroughHandler`
- Server validates: settle cultivation trước, đạt realm cap, có next realm, state hợp lệ
- Server changes state: roll chance, ghi `breakthrough_attempts`, success thì tăng realm, fail thì giảm cultivation/progress theo config data
- Server saves DB: update `character_base_stats`, insert `breakthrough_attempts`
- Server responds: `BreakthroughResultPacket(base/current state)`
- Client updates UI: refresh realm/progress/status text
- Success case: lên realm mới
- Fail cases: chưa tới cap, không có next realm, roll fail
- Blocked cases: feature conditions table chưa được dùng
- Important files: `GameServer/Runtime/CharacterCultivationService.cs`, `GameServer/Network/Handlers/BreakthroughHandler.cs`

## Feature: admin config reload / game time sync

- Trigger: vận hành server chạy command mode
- Client sends: none
- Server receives: CLI args `sync-game-time-config` hoặc startup bootstrap
- Server validates: game time config values positive
- Server changes state: update `game_time_state`
- Server saves DB: `game_time_state`
- Server responds: console output
- Client updates UI: none trực tiếp
- Success case: config sync thành công
- Fail cases: invalid config
- Blocked cases: gameplay `game_configs` khác hiện chưa hot-reload; cần restart server để có hiệu lực
- Important files: `GameServer/Program.cs`, `GameServer/Time/GameTimeService.cs`, `docs/reference-and-specs/GAME_CONFIGS.md`
