# 02. System Inventory

## Cách đọc

- `Done`: đã có flow server-client-db chạy được ở phase hiện tại.
- `Partial`: có code/runtime/schema nhưng chưa full UX hoặc chưa mở hết gameplay.
- `Prototype`: mới có placeholder/local-only/khung kỹ thuật.
- `Unknown`: chưa đủ bằng chứng từ code hiện có.

## Account / Auth

- Trạng thái: `Done`
- Server files: `GameServer/Services/AccountService.cs`, `GameServer/Services/AccountActionService.cs`, `GameServer/Network/Handlers/LoginHandler.cs`, `GameServer/Network/Handlers/RegisterHandler.cs`
- Client files: `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/Auth/Application/ClientAuthService.cs`, `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/Auth/Application/ClientLoginFlowService.cs`, `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/UI/Screens/Login/LoginScreenController.cs`
- DB tables: `accounts`, `account_credentials`, `account_security`
- Config: không có gameplay config riêng; phụ thuộc `dbConfig.json` để kết nối DB
- Luồng chính: login bằng password -> verify hash PBKDF2 -> update `last_login` -> trả account + credential + resume token
- Chỗ chưa rõ: Google/phone credential có service nhưng chưa thấy client flow sử dụng
- Source: `GameServer/Services/AccountService.cs`, `database/phamnhan_online.sql`

## Session Reconnect / Resume

- Trạng thái: `Done`
- Server files: `GameServer/Network/NetworkServer.cs`, `GameServer/Network/Handlers/ReconnectHandler.cs`
- Client files: `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/Auth/Application/ClientAuthService.cs`
- DB tables: none
- Config: `public.game_configs.network.reconnect_resume_window_seconds`
- Luồng chính: disconnect giữ session/resume token trong RAM trong một cửa sổ ngắn -> reconnect gửi token -> server resume ownership nếu token còn hợp lệ
- Chỗ chưa rõ: chưa thấy UI riêng cho reconnect thất bại ngoài popup/forced logout
- Source: `GameServer/Network/NetworkServer.cs`, `GameServer/Config/GameConfigKeys.cs`

## Character Creation / Selection

- Trạng thái: `Done`
- Server files: `GameServer/Services/CharacterService.cs`, `GameServer/Services/CharacterCreationActionService.cs`, `GameServer/Network/Handlers/CreateCharacterHandler.cs`, `GameServer/Network/Handlers/GetCharacterListHandler.cs`, `GameServer/Network/Handlers/GetCharacterDataHandler.cs`
- Client files: `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/Character/Application/ClientCharacterService.cs`, `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/Auth/Application/ClientLoginFlowService.cs`
- DB tables: `characters`, `character_base_stats`, `character_current_state`, `player_caves`, `player_garden_plots`, `player_skills`
- Config: `GameServer/Config/CharacterCreateConfig.json`, `public.game_configs.character.home_garden_plot_count`, `public.game_configs.character.starter_skill_id`
- Luồng chính: tạo nhân vật -> seed stat/current state/home cave/garden/starter skill -> load snapshot -> enter world
- Chỗ chưa rõ: server list character cho phép nhiều nhân vật, nhưng create flow hiện chặn account có hơn 1 nhân vật
- Source: `GameServer/Services/CharacterService.cs`, `GameServer/Config/CharacterCreateConfig.json`

## World Entry / Presence

- Trạng thái: `Done`
- Server files: `GameServer/Services/WorldEntryService.cs`, `GameServer/Runtime/CharacterRuntimeService.cs`, `GameServer/World/WorldInterestService.cs`, `GameServer/Network/Handlers/EnterWorldHandler.cs`
- Client files: `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/Character/Application/ClientCharacterService.cs`, `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Application/ClientWorldService.cs`
- DB tables: đọc `characters`, `character_base_stats`, `character_current_state`, `player_practice_sessions`, `player_notifications`
- Config: `gameTimeConfig.json`
- Luồng chính: attach player session -> settle cultivation/alchemy due -> join instance -> gửi `MapJoined` + `WorldRuntimeSnapshot` + spawn packet visible players + unread notifications
- Chỗ chưa rõ: chưa có explicit queue/login server selection nhiều shard ngoài `servers` table
- Source: `GameServer/Services/WorldEntryService.cs`, `GameServer/World/WorldInterestService.cs`

## Movement Sync

- Trạng thái: `Done`
- Server files: `GameServer/Network/Handlers/CharacterPositionSyncHandler.cs`, `GameServer/Runtime/GameLoop.cs`
- Client files: `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/WorldLocalMovementSyncController.cs`, `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/Character/Presentation/LocalCharacterActionController.cs`
- DB tables: `character_current_state`
- Config: `character.position_sync_*`
- Luồng chính: client gửi vị trí hiện tại dạng intent/desired target -> server validate finite/range/state -> game loop tiến vị trí theo move speed authoritative
- Chỗ chưa rõ: chưa thấy pathfinding/server collision phức tạp; movement hiện là straight-line clamp
- Source: `GameServer/Runtime/GameLoop.cs`, `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/WorldLocalMovementSyncController.cs`

## Map Travel / Portal

- Trạng thái: `Done`
- Server files: `GameServer/Network/Handlers/TravelToMapHandler.cs`, `GameServer/World/MapCatalog.cs`, `GameServer/Runtime/WorldInteractionGate.cs`
- Client files: `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Application/ClientWorldTravelService.cs`, `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/WorldTargetActionController.cs`, `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/WorldPortalPresenter.cs`
- DB tables: `map_templates`, `map_spawn_points`, `map_portals`, `map_template_adjacent_maps`
- Config: `world.portal_validation_buffer_server_units`
- Luồng chính: client tới gần portal -> request use portal -> server resolve portal thật từ DB -> validate range/state/instance -> đổi map/spawn point -> republish snapshot
- Chỗ chưa rõ: legacy `TargetMapId` travel vẫn tồn tại nhưng portal path là flow chính
- Source: `GameServer/Network/Handlers/TravelToMapHandler.cs`, `GameServer/World/MapCatalog.cs`

## Zone Switching

- Trạng thái: `Done`
- Server files: `GameServer/Network/Handlers/GetMapZonesHandler.cs`, `GameServer/Network/Handlers/SwitchMapZoneHandler.cs`, `GameServer/World/MapManager.cs`
- Client files: `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/UI/World/WorldMapZonePanelController.cs`, `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Application/ClientWorldTravelService.cs`
- DB tables: `map_templates`, `map_zone_slots`
- Config: `world.empty_public_instance_lifetime_seconds`
- Luồng chính: query zone occupancy -> switch zone nếu map public hỗ trợ và slot còn chỗ -> server cập nhật zone/index/position và republish snapshot
- Chỗ chưa rõ: chưa có reservation/queue nếu zone full
- Source: `GameServer/Network/Handlers/GetMapZonesHandler.cs`, `GameServer/Network/Handlers/SwitchMapZoneHandler.cs`

## Combat / Skill Execution

- Trạng thái: `Done`
- Server files: `GameServer/Network/Handlers/AttackEnemyHandler.cs`, `GameServer/Runtime/SkillExecutionService.cs`, `GameServer/Runtime/WorldRuntimeSettlementService.cs`, `GameServer/Services/SkillService.cs`
- Client files: `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/Combat/Application/ClientCombatService.cs`, `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/UI/Hud/WorldCombatHudController.cs`, `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/WorldTargetActionController.Execution.cs`
- DB tables: `skills`, `skill_effects`, `player_skills`, `player_skill_loadouts`, `player_skill_grant_sources`
- Config: `combat.skill_range_grace_buffer_units`, `skill.max_loadout_slot_count`
- Luồng chính: resolve equipped skill from slot -> validate state/range/target/cooldown -> enqueue cast -> server settles cast release and impact in game loop -> broadcast started/resolved packets
- Chỗ chưa rõ: AoE/all-map target types chưa support; special-case skills phase sau
- Source: `GameServer/Network/Handlers/AttackEnemyHandler.cs`, `GameServer/Runtime/SkillExecutionService.cs`

## Enemy Spawn / AI / Runtime Instance

- Trạng thái: `Done`
- Server files: `GameServer/World/MapManager.cs`, `GameServer/World/MapInstance*.cs`, `GameServer/Runtime/EnemyDefinitionCatalog.cs`, `GameServer/Runtime/WorldRuntimeSettlementService.cs`
- Client files: `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Application/ClientWorldService.cs`, `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/WorldEnemiesPresenter.cs`
- DB tables: `enemy_templates`, `enemy_template_skills`, `enemy_reward_rules`, `map_enemy_spawn_groups`, `map_enemy_spawn_entries`, `map_instance_configs`
- Config: out-of-combat restore config nằm trong table enemy/map instance
- Luồng chính: map instance load spawn group definitions -> runtime tick spawn/patrol/combat/death -> publish spawn/hp/move/despawn
- Chỗ chưa rõ: boss/objective completion rule mới ở runtime foundation, cần content design thêm
- Source: `GameServer/World/MapManager.cs`, `GameServer/Runtime/WorldRuntimeSettlementService.cs`, `docs/architecture-and-roadmap/ENEMY_BOSS_INSTANCE_FLOW_DRAFT.md`

## Death Recovery

- Trạng thái: `Done`
- Server files: `GameServer/Runtime/CharacterCombatDeathRecoveryService.cs`, `GameServer/Network/Handlers/ReturnHomeAfterCombatDeathHandler.cs`
- Client files: `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/Character/Application/ClientCombatDeathRecoveryService.cs`, `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/UI/World/WorldCombatDeathController.cs`
- DB tables: `character_current_state`
- Config: `combat_death.return_home_recovery_ratio`
- Luồng chính: khi combat dead, client chỉ được bấm hồi về home; nếu disconnect lúc combat dead thì server cũng tự recover về home
- Chỗ chưa rõ: chưa thấy corpse/penalty/repair loop ngoài hồi HP/MP theo ratio
- Source: `GameServer/Runtime/CharacterCombatDeathRecoveryService.cs`, `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/UI/World/WorldCombatDeathController.cs`

## Loot / Ground Reward

- Trạng thái: `Done`
- Server files: `GameServer/Runtime/EnemyRewardRuntimeService.cs`, `GameServer/Runtime/GroundItemRuntimeService.cs`, `GameServer/Network/Handlers/PickupGroundRewardHandler.cs`, `GameServer/Network/Handlers/DropInventoryItemHandler.cs`
- Client files: `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Application/ClientGroundRewardService.cs`, `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/WorldGroundRewardPresenter.cs`
- DB tables: `player_items`, `enemy_reward_rules`, `game_random_*`
- Config: `item_drop.*`, `ground_reward.pickup_radius_server_units`
- Luồng chính: quái chết -> reward roll/random table -> direct grant hoặc ground reward -> claim/pickup -> inventory merge
- Chỗ chưa rõ: currency/reward entry type ngoài item mới partial
- Source: `GameServer/Runtime/EnemyRewardRuntimeService.cs`, `GameServer/Network/Handlers/PickupGroundRewardHandler.cs`

## Inventory

- Trạng thái: `Done`
- Server files: `GameServer/Services/ItemService.cs`, `GameServer/Services/PlayerInventoryTransactionService.cs`, `GameServer/Network/Handlers/GetInventoryHandler.cs`
- Client files: `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/Inventory/Application/ClientInventoryService.cs`, `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/UI/World/WorldInventoryPanelController.cs`
- DB tables: `player_items`, `player_equipments`, `player_equipment_stat_bonuses`, `player_soils`
- Config: `character.equipment_slot_count`
- Luồng chính: load inventory view từ item definitions + equipment rows -> client popup use/drop/unequip -> server mutation trong transaction lock per player
- Chỗ chưa rõ: chưa có weight/capacity/slot limit inventory thường
- Source: `GameServer/Services/ItemService.cs`, `GameServer/Services/PlayerInventoryTransactionService.cs`

## Equipment

- Trạng thái: `Done`
- Server files: `GameServer/Services/EquipmentService.cs`, `GameServer/Services/EquipmentActionService.cs`, `GameServer/Services/EquipmentStatService.cs`
- Client files: `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/UI/World/WorldCharacterEquipController.cs`, `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/UI/Inventory/CharacterEquipmentLoadoutView.cs`
- DB tables: `equipment_templates`, `equipment_template_stats`, `equipment_template_skill_grants`, `player_equipments`, `player_equipment_stat_bonuses`
- Config: `character.equipment_slot_count`
- Luồng chính: validate inventory ownership + slot -> equip/unequip -> sync granted skills + recalc final stats
- Chỗ chưa rõ: durability/enhance level tồn tại trong schema nhưng chưa thấy gameplay loop mở rộng
- Source: `GameServer/Services/EquipmentService.cs`, `GameServer/Services/EquipmentActionService.cs`

## Item Use

- Trạng thái: `Done`
- Server files: `GameServer/Services/ItemUseService.cs`, `GameServer/Network/Handlers/UseItemHandler.cs`
- Client files: `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/UI/World/WorldInventoryPanelController.ItemActions.cs`
- DB tables: `player_items`, `martial_art_book_templates`, `player_martial_arts`, `player_pill_recipes`, `pill_templates`, `pill_effects`
- Config: item semantics chủ yếu nằm trong DB definition tables
- Luồng chính: generic `UseItemPacket` cho equipment, martial art book, pill recipe book, consumable pill effect
- Chỗ chưa rõ: talisman/soil/herb item types chưa đi qua public flow phase hiện tại
- Source: `GameServer/Services/ItemUseService.cs`, `docs/reference-and-specs/ITEM_USE_FLOW_SPEC.md`

## Martial Arts

- Trạng thái: `Done`
- Server files: `GameServer/Services/MartialArtService.cs`, `GameServer/Services/MartialArtActionService.cs`
- Client files: `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/MartialArts/Application/ClientMartialArtService.cs`, `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/UI/World/WorldCultivationPanelController.cs`
- DB tables: `martial_arts`, `martial_art_stages`, `martial_art_stage_stat_bonuses`, `martial_art_skills`, `player_martial_arts`
- Config: progression nằm trong DB
- Luồng chính: player học martial art từ book -> có thể set active martial art -> active art quyết định cultivation preview/absorption
- Chỗ chưa rõ: switching cost/cooldown/chặn combat chưa thấy design riêng
- Source: `GameServer/Runtime/CharacterCultivationService.cs`, `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/MartialArts/Application/ClientMartialArtService.cs`

## Cultivation / Breakthrough

- Trạng thái: `Done`
- Server files: `GameServer/Runtime/CharacterCultivationService.cs`, `GameServer/Services/CultivationActionService.cs`, `GameServer/Network/Handlers/StartCultivationHandler.cs`, `StopCultivationHandler.cs`, `BreakthroughHandler.cs`
- Client files: `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/UI/World/WorldCultivationPanelController*.cs`, `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/Character/Application/ClientCharacterService.cs`
- DB tables: `character_base_stats`, `character_current_state`, `realm_templates`, `breakthrough_attempts`
- Config: `cultivation.*`, map spiritual energy tables, realm templates
- Luồng chính: chỉ tu luyện ở private home instance với active martial art -> settle theo thời gian -> đạt cap thì đột phá bằng roll chance -> thất bại có penalty/lock potential reward
- Chỗ chưa rõ: `breakthrough_conditions` table chưa thấy runtime dùng
- Source: `GameServer/Runtime/CharacterCultivationService.cs`, `GameServer/Repositories/BreakthroughConditionRepository.cs`

## Potential Allocation

- Trạng thái: `Done`
- Server files: `GameServer/Runtime/PotentialStatCatalog.cs`, `GameServer/Network/Handlers/AllocatePotentialHandler.cs`
- Client files: `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/UI/World/WorldPotentialPanelController.cs`
- DB tables: `potential_stat_upgrade_tiers`, `character_base_stats`
- Config: tiering nằm trong DB
- Luồng chính: server build preview theo stat target/tier/cost/gain -> client bấm option -> server spend potential và tăng base stat counts
- Chỗ chưa rõ: reset/respec chưa có
- Source: `GameServer/Runtime/PotentialStatCatalog.cs`, `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/UI/World/WorldPotentialPanelController.cs`

## Skill Loadout / Skill Ownership

- Trạng thái: `Done`
- Server files: `GameServer/Services/SkillService.cs`, `GameServer/Network/Handlers/GetOwnedSkillsHandler.cs`, `SetSkillLoadoutSlotHandler.cs`, `SwapSkillLoadoutSlotsHandler.cs`
- Client files: `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/Skills/Application/ClientSkillService.cs`, `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/UI/World/WorldSkillPanelController.cs`
- DB tables: `player_skills`, `player_skill_loadouts`, `player_skill_grant_sources`, `equipment_template_skill_grants`
- Config: `skill.max_loadout_slot_count`
- Luồng chính: server normalize loadout, remove invalid rows, block equipment-granted skill nếu realm requirement chưa đủ
- Chỗ chưa rõ: starter/basic skill content id hiện vẫn config-driven
- Source: `GameServer/Services/SkillService.cs`

## Alchemy Crafting

- Trạng thái: `Done`
- Server files: `GameServer/Services/AlchemyCraftQueryService.cs`, `GameServer/Services/AlchemyCraftActionService.cs`, `GameServer/Services/AlchemyService.cs`
- Client files: `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/Alchemy/Application/ClientAlchemyService.cs`, `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/UI/World/WorldCraftingPanelController.cs`
- DB tables: `pill_recipe_templates`, `pill_recipe_inputs`, `player_pill_recipes`, `pill_recipe_mastery_stages`, `pill_templates`, `pill_effects`
- Config: recipe/rate/mastery chủ yếu trong DB
- Luồng chính: load learned recipes -> detail -> preview with selected inputs -> start craft -> consume items -> spawn practice session
- Chỗ chưa rõ: `required_herb_maturity` explicitly deferred to later phase
- Source: `GameServer/Services/AlchemyService.cs`, `GameServer/Services/AlchemyCraftActionService.cs`

## Practice Sessions / Notifications

- Trạng thái: `Done`
- Server files: `GameServer/Services/PracticeService.cs`, `GameServer/Services/AlchemyPracticeService.cs`, `GameServer/Services/PlayerNotificationService.cs`
- Client files: `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/Notifications/Application/ClientNotificationService.cs`, `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/UI/Common/NotificationInboxController.cs`
- DB tables: `player_practice_sessions`, `player_notifications`
- Config: `alchemy.practice_cancel_refund_progress_threshold` exists in code keys, seed row currently missing
- Luồng chính: practice session active/pause/resume/cancel -> completion payload -> inventory grant -> push notification -> client acknowledge
- Chỗ chưa rõ: code supports other `PracticeType` values but only alchemy end-to-end is visible
- Source: `GameServer/Services/PracticeService.cs`, `GameServer/Services/AlchemyPracticeService.cs`, `GameServer/Config/GameConfigKeys.cs`

## Home Cave / Garden / Herb Farming

- Trạng thái: `Partial`
- Server files: `GameServer/Services/HerbService.cs`, `GameServer/Runtime/AlchemyDefinitionCatalog.cs`, `GameServer/Services/CharacterService.cs`
- Client files: chỉ thấy item presentation types; chưa thấy UI flow/packet handlers gameplay
- DB tables: `player_caves`, `player_garden_plots`, `soil_templates`, `player_soils`, `herb_templates`, `herb_growth_stage_configs`, `player_herbs`, `herb_harvest_outputs`
- Config: `character.home_garden_plot_count`
- Luồng chính: character creation seed home cave + plots; server có service cho insert soil/plant/move/harvest herb
- Chỗ chưa rõ: chưa có public packet handlers/client screens để dùng hệ này
- Source: `GameServer/Services/HerbService.cs`, `GameServer/Services/CharacterService.cs`

## Local Home Station Portals

- Trạng thái: `Prototype`
- Server files: none trực tiếp; chủ yếu client-local interaction
- Client files: `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/LocalFixPortalPresenter.cs`, `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/UI/World/WorldUIController.cs`
- DB tables: none
- Config: scene prefab setup
- Luồng chính: target local pseudo-NPC portal in home -> open cultivation/alchemy/smithing/talisman panel locally
- Chỗ chưa rõ: đây không phải portal server travel; chỉ là world interaction UI shortcut
- Source: `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/Features/World/Presentation/LocalFixPortalPresenter.cs`

## Smithing / Talisman

- Trạng thái: `Prototype`
- Server files: enums/types tồn tại (`PracticeType`, `ItemType`) nhưng chưa thấy completed packet flow
- Client files: `WorldCraftingPanelController.cs`, `WorldUIController.cs`, `LocalFixPortalPresenter.cs`
- DB tables: chưa thấy recipe/content tables riêng cho smithing/talisman
- Config: local panel title placeholder
- Luồng chính: mở panel được, nhưng panel hiển thị placeholder text
- Chỗ chưa rõ: server-side design model còn thiếu
- Source: `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/UI/World/WorldCraftingPanelController.cs`, `GameServer/Runtime/PracticeSystemTypes.cs`

## Quest / Guild

- Trạng thái: `Prototype`
- Server files: chưa thấy packet/service/domain flow
- Client files: `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/UI/World/WorldMenuController.cs`
- DB tables: chưa thấy quest/guild tables
- Config: none
- Luồng chính: menu tab placeholder text thôi
- Chỗ chưa rõ: gameplay spec chưa được nối
- Source: `ClientUnity/PhamNhanOnline/Assets/Game/Runtime/UI/World/WorldMenuController.cs`

## Admin Config / Game Time

- Trạng thái: `Partial`
- Server files: `GameServer/Time/GameTimeService.cs`, `GameServer/Program.cs`, `GameServer/Extensions/ServiceCollectionExtensions.ConfigBuilders.cs`
- Client files: none trực tiếp
- DB tables: `game_time_state`, `game_configs`
- Config: `gameTimeConfig.json`, `public.game_configs`
- Luồng chính: bootstrap JSON -> persist/load DB state -> maintenance dùng interval -> CLI command sync game time config
- Chỗ chưa rõ: `game_configs` load một lần khi server start; chưa có hot-reload gameplay config service
- Source: `GameServer/Time/GameTimeService.cs`, `GameServer/Program.cs`, `docs/reference-and-specs/GAME_CONFIGS.md`

## Logging / Diagnostics / Packet Incident Capture

- Trạng thái: `Done`
- Server files: `GameServer/Network/NetworkServer.cs`, `GameServer/Diagnostics/*`, `GameServer/Runtime/GameLoop.cs`
- Client files: nhiều `ClientLog.*`, `WorldConnectionDebugController.cs`, `WorldTravelDebugController.cs`
- DB tables: none
- Config: runtime metrics and incident capture are code-driven
- Luồng chính: log packet exception, movement clamp suspicion, maintenance tick metrics, client debug HUD
- Chỗ chưa rõ: chưa thấy external log aggregation
- Source: `GameServer/Network/NetworkServer.cs`, `GameServer/Runtime/GameLoop.cs`
