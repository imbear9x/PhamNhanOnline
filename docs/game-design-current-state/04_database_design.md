# 04. Database Design

## Ghi chú chung

- Tài liệu này mô tả schema hiện đang được server đọc/ghi từ tổ hợp `database/phamnhan_online.sql` và `database/initDatabase.sql`.
- `phamnhan_online.sql` chứa các bảng nền gốc như account/character/map/realm.
- `initDatabase.sql` chủ yếu là migration-style bootstrap mở rộng gameplay phase hiện tại: skill, inventory, alchemy, enemy, reward, config.
- Các cột dưới đây là `important columns`, không liệt kê toàn bộ constraint/index phụ.

Source: `database/phamnhan_online.sql`, `database/initDatabase.sql`, `GameServer/Entities/*.cs`

## Core account + character

### `accounts`

- Purpose: tài khoản gốc.
- Important columns: `id`, `created_at`, `last_login`, `status`.
- Relationships: 1-n tới `account_credentials`, 1-n tới `characters`.
- Runtime/config: runtime data.
- Ai ghi data: `AccountService`.
- Ai đọc data: `AccountService`, auth/login flow.
- Khi nào insert/update/delete: insert khi register hoặc login with Google; update `last_login` khi login; delete chưa thấy public flow.
- Validation liên quan: account phải tồn tại khi login/change credential.
- Rủi ro/chưa rõ: `status` chưa thấy gameplay/business rule sử dụng.
- Source: `database/phamnhan_online.sql`, `GameServer/Services/AccountService.cs`

### `account_credentials`

- Purpose: mapping provider credential tới account.
- Important columns: `id`, `account_id`, `provider`, `provider_user_id`, `password_hash`, `created_at`.
- Relationships: n-1 tới `accounts`.
- Runtime/config: runtime auth data.
- Ai ghi data: `AccountService`.
- Ai đọc data: `AccountService`.
- Khi nào insert/update/delete: insert khi register/link provider; update khi đổi password/credential; delete chưa thấy.
- Validation liên quan: unique theo provider + provider user id; verify password hash PBKDF2.
- Rủi ro/chưa rõ: không ghi token/secret ngoài hash, đây là đúng; flow Google/phone chưa thấy client dùng.
- Source: `database/phamnhan_online.sql`, `GameServer/Services/AccountService.cs`

### `account_security`

- Purpose: flags bảo mật account.
- Important columns: `account_id`, `email_verified`, `phone_verified`, `two_factor_enabled`.
- Relationships: 1-1 với `accounts`.
- Runtime/config: runtime account metadata.
- Ai ghi data: chưa thấy service phase hiện tại.
- Ai đọc data: chưa thấy.
- Khi nào insert/update/delete: Unknown / Need confirmation.
- Validation liên quan: chưa thấy.
- Rủi ro/chưa rõ: table tồn tại nhưng chưa active trong code phase hiện tại.
- Source: `database/phamnhan_online.sql`

### `servers`

- Purpose: danh sách game server/shard.
- Important columns: `id`, `name`, `status`.
- Relationships: `characters.server_id` tham chiếu logic-level.
- Runtime/config: config-ish runtime metadata.
- Ai ghi data: seed SQL.
- Ai đọc data: character creation currently stores `server_id`; chưa thấy shard selection flow đầy đủ.
- Khi nào insert/update/delete: seed/ops.
- Validation liên quan: `CreateCharacterPacket` gửi `serverId`.
- Rủi ro/chưa rõ: multi-server gameplay chưa hoàn thiện.
- Source: `database/phamnhan_online.sql`, `database/initDatabase.sql`, `GameServer/Services/CharacterService.cs`

### `characters`

- Purpose: nhân vật gốc.
- Important columns: `id`, `account_id`, `server_id`, `name`, `model_id`, cosmetic columns, `created_at`, `first_enter_world_at_utc`.
- Relationships: 1-1 với `character_base_stats`, `character_current_state`; 1-n với nhiều bảng runtime/player-owned.
- Runtime/config: runtime persistent.
- Ai ghi data: `CharacterService`.
- Ai đọc data: `CharacterService`, `WorldEntryService`, world/runtime attach.
- Khi nào insert/update/delete: insert khi create character; update first enter/cosmetics; delete chưa thấy.
- Validation liên quan: unique character name, mỗi account hiện chỉ tạo 1 character.
- Rủi ro/chưa rõ: list-character API gợi ý mở rộng nhiều character nhưng create flow chưa cho phép.
- Source: `database/phamnhan_online.sql`, `GameServer/Services/CharacterService.cs`

### `character_base_stats`

- Purpose: stat nền, progression persistent, realm/cultivation/potential.
- Important columns: `character_id`, `realm_id`, `cultivation`, `base_hp`, `base_mp`, `base_attack`, `base_move_speed`, `base_speed`, `base_sense`, `base_luck`, `base_stamina`, `unallocated_potential`, `active_martial_art_id`, `cultivation_progress`, `potential_reward_locked`.
- Relationships: n-1 tới `realm_templates`; logic-level tới `player_martial_arts`.
- Runtime/config: runtime persistent.
- Ai ghi data: `CharacterService`, `CharacterCultivationService`, `EquipmentActionService`, `EnemyRewardRuntimeService`.
- Ai đọc data: gần như toàn bộ progression/combat/inventory services.
- Khi nào insert/update/delete: insert lúc create character; update khi reward/tu luyện/đột phá/equip/allocate potential.
- Validation liên quan: realm existence, cultivation cap, potential allocation tiers.
- Rủi ro/chưa rõ: combat temporary buffs không persist ở đây, chỉ final persistent stats.
- Source: `database/phamnhan_online.sql`, `database/initDatabase.sql`, `GameServer/Services/CharacterService.cs`

### `character_current_state`

- Purpose: current HP/MP/stamina/map/zone/position/state flags.
- Important columns: `character_id`, `current_hp`, `current_mp`, `current_stamina`, `current_map_id`, `current_zone_index`, `current_pos_x`, `current_pos_y`, `is_expired`, `current_state`, `cultivation_started_at_utc`, `last_cultivation_rewarded_at_utc`, `last_saved_at`.
- Relationships: 1-1 với `characters`.
- Runtime/config: runtime persistent snapshot.
- Ai ghi data: `CharacterRuntimeSaveService`, `CharacterService`, `CharacterCombatDeathRecoveryService`.
- Ai đọc data: world entry, runtime attach, client snapshot responses.
- Khi nào insert/update/delete: insert lúc create character; update định kỳ/save-on-disconnect/important action.
- Validation liên quan: clamp resource theo base stats; casting state không persist, save service reset về idle.
- Rủi ro/chưa rõ: không lưu combat temporary statuses, by design.
- Source: `database/phamnhan_online.sql`, `GameServer/Runtime/CharacterRuntimeSaveService.cs`

### `breakthrough_attempts`

- Purpose: lịch sử đột phá.
- Important columns: `id`, `character_id`, `realm_id`, `success_rate`, `result`, `created_at`.
- Relationships: n-1 tới `characters`, logical tới `realm_templates`.
- Runtime/config: runtime history.
- Ai ghi data: `CharacterCultivationService`.
- Ai đọc data: hiện chủ yếu ghi audit/history, chưa thấy client query.
- Khi nào insert/update/delete: insert mỗi lần breakthrough attempt.
- Validation liên quan: attempt chỉ xảy ra khi cap/fullfill runtime rule.
- Rủi ro/chưa rõ: chưa có public UI/history view.
- Source: `database/phamnhan_online.sql`, `GameServer/Runtime/CharacterCultivationService.cs`

### `breakthrough_conditions`

- Purpose: cấu hình điều kiện/phụ trợ breakthrough.
- Important columns: `id`, `realm_id`, `condition_type`, `target_id`, `success_bonus`, `max_stack`.
- Relationships: n-1 tới `realm_templates`.
- Runtime/config: config data.
- Ai ghi data: seed/admin.
- Ai đọc data: repository tồn tại nhưng chưa thấy runtime service dùng.
- Khi nào insert/update/delete: config only.
- Validation liên quan: Unknown / Need confirmation.
- Rủi ro/chưa rõ: hiện là config orphan, cần xác nhận có bị bỏ dở hay phase sau.
- Source: `database/phamnhan_online.sql`, `GameServer/Repositories/BreakthroughConditionRepository.cs`

## Time + global config

### `game_time_state`

- Purpose: state đồng hồ game và maintenance interval persisted.
- Important columns: `id`, `anchor_utc`, `anchor_game_minute`, `game_minutes_per_real_minute`, `days_per_game_year`, `runtime_save_interval_seconds`, `derived_state_refresh_interval_seconds`, `updated_at`.
- Relationships: standalone singleton row.
- Runtime/config: config state persisted.
- Ai ghi data: `GameTimeService`.
- Ai đọc data: `GameTimeService`, runtime maintenance, world model conversion.
- Khi nào insert/update/delete: create if missing at boot; update by CLI sync/apply config.
- Validation liên quan: all interval/scales must be positive.
- Rủi ro/chưa rõ: only one primary row assumed.
- Source: `database/phamnhan_online.sql`, `GameServer/Time/GameTimeService.cs`

### `game_configs`

- Purpose: typed gameplay config key-value store.
- Important columns: `config_key`, `config_value`, `description`, `created_at`, `updated_at`.
- Relationships: standalone keyed table.
- Runtime/config: config data.
- Ai ghi data: SQL seed, admin DB edits.
- Ai đọc data: `ServiceCollectionExtensions.ConfigBuilders`.
- Khi nào insert/update/delete: seed in `initDatabase.sql`; server loads at startup.
- Validation liên quan: parsed into `GameConfigValues`; missing keys fall back to code defaults.
- Rủi ro/chưa rõ: not hot-reloaded; two code keys currently not seeded.
- Source: `database/initDatabase.sql`, `GameServer/Config/GameConfigKeys.cs`, `GameServer/Extensions/ServiceCollectionExtensions.ConfigBuilders.cs`

### `game_random_tables`

- Purpose: definition header cho weighted random tables.
- Important columns: `id`, `table_id`, `mode`, `luck_enabled`, `luck_bonus_parts_per_million_per_luck_point`, `luck_max_bonus_parts_per_million`, `none_entry_id`.
- Relationships: 1-n tới `game_random_entries`, `game_random_luck_tags`.
- Runtime/config: config data.
- Ai ghi data: seed/admin.
- Ai đọc data: `GameRandomService`, enemy reward runtime.
- Khi nào insert/update/delete: config only.
- Validation liên quan: random table id phải tồn tại khi reward rule dùng.
- Rủi ro/chưa rõ: invalid entry id chỉ log khi reward roll runtime.
- Source: `database/initDatabase.sql`, `GameServer/Runtime/EnemyRewardRuntimeService.cs`

### `game_random_entries`

- Purpose: weighted entries cho random table.
- Important columns: `id`, `game_random_table_id`, `entry_id`, `chance_parts_per_million`, `is_none`, `order_index`.
- Relationships: n-1 tới `game_random_tables`; 1-n tới `game_random_entry_tags`.
- Runtime/config: config data.
- Ai ghi data: seed/admin.
- Ai đọc data: `GameRandomService`.
- Khi nào insert/update/delete: config only.
- Validation liên quan: `entry_id` phải map được sang item reward hoặc none.
- Rủi ro/chưa rõ: unsupported reward entry chỉ bị log khi runtime.
- Source: `database/initDatabase.sql`, `GameServer/Runtime/EnemyRewardRuntimeService.cs`

### `game_random_entry_tags`

- Purpose: tagging entry cho filter/grouping random logic.
- Important columns: `id`, `game_random_entry_id`, `tag`.
- Relationships: n-1 tới `game_random_entries`.
- Runtime/config: config data.
- Ai ghi data: seed/admin.
- Ai đọc data: game random subsystem.
- Khi nào insert/update/delete: config only.
- Validation liên quan: Unknown / Need confirmation.
- Rủi ro/chưa rõ: chưa thấy gameplay dùng tag ngoài random infra.
- Source: `database/initDatabase.sql`

### `game_random_luck_tags`

- Purpose: tag cho rules luck bonus.
- Important columns: `id`, `game_random_table_id`, `tag`.
- Relationships: n-1 tới `game_random_tables`.
- Runtime/config: config data.
- Ai ghi data: seed/admin.
- Ai đọc data: game random subsystem.
- Khi nào insert/update/delete: config only.
- Validation liên quan: Unknown / Need confirmation.
- Rủi ro/chưa rõ: cần doc riêng nếu designer muốn dùng advanced luck behavior.
- Source: `database/initDatabase.sql`

## Realm + cultivation + potential

### `realm_templates`

- Purpose: config các realm/cảnh giới.
- Important columns: `id`, `name`, `max_cultivation`, `base_breakthrough_rate`, `lifespan`, `absorption_multiplier`.
- Relationships: 1-n tới `character_base_stats`, `breakthrough_conditions`.
- Runtime/config: config data.
- Ai ghi data: migrations/seeds.
- Ai đọc data: `CharacterService`, `CharacterCultivationService`, `EnemyRewardRuntimeService`.
- Khi nào insert/update/delete: config only.
- Validation liên quan: breakthrough requires next realm exists.
- Rủi ro/chưa rõ: delete/reorder realm ids sẽ ảnh hưởng direct lookup `realm.Id + 1`.
- Source: `database/phamnhan_online.sql`, `database/initDatabase.sql`, `GameServer/Runtime/CharacterCultivationService.cs`

### `spiritual_energy_templates`

- Purpose: multipliers linh khí theo zone slot.
- Important columns: `id`, `code`, `name`, `lk_per_minute`.
- Relationships: 1-n tới `map_zone_slots`.
- Runtime/config: config data.
- Ai ghi data: seed/admin.
- Ai đọc data: `MapCatalog`.
- Khi nào insert/update/delete: config only.
- Validation liên quan: map zone slot phải reference id tồn tại.
- Rủi ro/chưa rõ: naming `lk_per_minute` cần thống nhất với domain term spiritual energy.
- Source: `database/initDatabase.sql`, `GameServer/World/MapCatalog.cs`

### `potential_stat_upgrade_tiers`

- Purpose: cost/gain tiers cho allocate potential.
- Important columns: `target_stat`, `tier_index`, `max_upgrade_count`, `potential_cost_per_upgrade`, `stat_gain_per_upgrade`, `is_enabled`.
- Relationships: logical to `character_base_stats`.
- Runtime/config: config data.
- Ai ghi data: seed/admin.
- Ai đọc data: `PotentialStatCatalog`.
- Khi nào insert/update/delete: config only.
- Validation liên quan: target stat enum phải hợp lệ; allocate flow build preview từ tiers này.
- Rủi ro/chưa rõ: nếu tier gaps sai dữ liệu, preview/upgrades sẽ cho kết quả khó đoán.
- Source: `database/initDatabase.sql`, `GameServer/Runtime/PotentialStatCatalog.cs`

## World / map / instance

### `map_templates`

- Purpose: config map master data.
- Important columns: `id`, `name`, `map_type`, `client_map_key`, `spiritual_energy`, `width`, `height`, `cell_size`, `max_public_zone_count`, `max_players_per_zone`, `supports_cave_placement`, `default_spawn_x/y`, `is_private_per_player`.
- Relationships: 1-n tới `map_zone_slots`, `map_spawn_points`, `map_portals`, `map_enemy_spawn_groups`, `map_instance_configs`; n-n logical qua `map_template_adjacent_maps`.
- Runtime/config: config data.
- Ai ghi data: seed/admin.
- Ai đọc data: `MapCatalog`, client map bootstrap via packets.
- Khi nào insert/update/delete: config only.
- Validation liên quan: at least one Home map must exist.
- Rủi ro/chưa rõ: deleting map breaks many linked tables.
- Source: `database/phamnhan_online.sql`, `database/initDatabase.sql`, `GameServer/World/MapCatalog.cs`

### `map_template_adjacent_maps`

- Purpose: adjacency metadata giữa map.
- Important columns: `map_template_id`, `adjacent_map_template_id`.
- Relationships: n-n self-reference of `map_templates`.
- Runtime/config: config data.
- Ai ghi data: seed/admin.
- Ai đọc data: `MapCatalog`.
- Khi nào insert/update/delete: config only.
- Validation liên quan: both maps should exist.
- Rủi ro/chưa rõ: actual portal travel now also builds adjacency implicitly from portal config.
- Source: `database/phamnhan_online.sql`, `GameServer/World/MapCatalog.cs`

### `map_zone_slots`

- Purpose: zone-specific spiritual energy config for public maps.
- Important columns: `id`, `map_template_id`, `zone_index`, `spiritual_energy_template_id`.
- Relationships: n-1 tới `map_templates`, `spiritual_energy_templates`.
- Runtime/config: config data.
- Ai ghi data: seed/admin.
- Ai đọc data: `MapCatalog`, zone query response.
- Khi nào insert/update/delete: config only.
- Validation liên quan: zone index unique per map.
- Rủi ro/chưa rõ: private maps ignore table.
- Source: `database/initDatabase.sql`, `GameServer/World/MapCatalog.cs`

### `map_spawn_points`

- Purpose: named spawn points inside maps.
- Important columns: `id`, `map_template_id`, `code`, `name`, `spawn_category`, `pos_x`, `pos_y`, `facing_degrees`.
- Relationships: n-1 tới `map_templates`; target of `map_portals`.
- Runtime/config: config data.
- Ai ghi data: seed/admin.
- Ai đọc data: `MapCatalog`, portal travel.
- Khi nào insert/update/delete: config only.
- Validation liên quan: portal target spawn must exist on target map.
- Rủi ro/chưa rõ: if default spawn and portal spawn diverge badly, client can appear in invalid authored area.
- Source: `database/initDatabase.sql`, `GameServer/World/MapCatalog.cs`

### `map_portals`

- Purpose: server-authoritative portal definitions.
- Important columns: `id`, `source_map_template_id`, `code`, `source_x/y`, `interaction_radius`, `interaction_mode`, `target_map_template_id`, `target_spawn_point_id`, `is_enabled`.
- Relationships: n-1 tới source/target `map_templates`, target `map_spawn_points`.
- Runtime/config: config data.
- Ai ghi data: seed/admin.
- Ai đọc data: `MapCatalog`, `TravelToMapHandler`.
- Khi nào insert/update/delete: config only.
- Validation liên quan: positive radius, valid interaction mode, target map/spawn exists.
- Rủi ro/chưa rõ: current client also has local fake home portals not represented here.
- Source: `database/initDatabase.sql`, `GameServer/Network/Handlers/TravelToMapHandler.cs`

### `map_instance_configs`

- Purpose: special per-map instance behavior for solo timed/farm maps.
- Important columns: `id`, `code`, `map_template_id`, `instance_mode`, `duration_seconds`, `idle_destroy_seconds`, `completion_rule`, `complete_destroy_delay_seconds`.
- Relationships: n-1 tới `map_templates`.
- Runtime/config: config data.
- Ai ghi data: seed/admin.
- Ai đọc data: `EnemyDefinitionCatalog`, `MapManager`.
- Khi nào insert/update/delete: config only.
- Validation liên quan: used when map has configured instance mode.
- Rủi ro/chưa rõ: completion rules need more content-level docs.
- Source: `database/initDatabase.sql`, `GameServer/World/MapManager.cs`

## Martial arts + skills

### `martial_arts`

- Purpose: master definition of martial arts.
- Important columns: `id`, `code`, `name`, `icon`, `quality`, `category`, `description_template`, `qi_absorption_rate`, `max_stage`.
- Relationships: 1-n tới `martial_art_stages`, `martial_art_skills`, `martial_art_book_templates`, `player_martial_arts`.
- Runtime/config: config data.
- Ai ghi data: seed/admin.
- Ai đọc data: `MartialArtService`, `CharacterCultivationService`, description pipeline.
- Khi nào insert/update/delete: config only.
- Validation liên quan: active martial art must exist when cultivation starts.
- Rủi ro/chưa rõ: stage/skill content consistency must be curated manually.
- Source: `database/initDatabase.sql`, `GameServer/Runtime/CharacterCultivationService.cs`

### `martial_art_stages`

- Purpose: stage progression per martial art.
- Important columns: `id`, `martial_art_id`, `stage_level`, `exp_required`, `is_bottleneck`, `breakthrough_base_rate`, `breakthrough_exp_penalty`.
- Relationships: n-1 tới `martial_arts`; 1-n tới `martial_art_stage_stat_bonuses`.
- Runtime/config: config data.
- Ai ghi data: seed/admin.
- Ai đọc data: martial art progression service.
- Khi nào insert/update/delete: config only.
- Validation liên quan: progression math.
- Rủi ro/chưa rõ: martial art stage breakthrough may need clearer design docs.
- Source: `database/initDatabase.sql`

### `martial_art_stage_stat_bonuses`

- Purpose: stat bonuses granted by martial art stage.
- Important columns: `martial_art_stage_id`, `stat_type`, `value`, `value_type`.
- Relationships: n-1 tới `martial_art_stages`.
- Runtime/config: config data.
- Ai ghi data: seed/admin.
- Ai đọc data: progression/stat calculation.
- Khi nào insert/update/delete: config only.
- Validation liên quan: stat enum/value type must match code enums.
- Rủi ro/chưa rõ: no dedicated admin safety layer seen in runtime itself.
- Source: `database/initDatabase.sql`

### `skills`

- Purpose: master skill definitions.
- Important columns: `id`, `code`, `name`, `skill_group_code`, `skill_level`, `skill_type`, `skill_category`, `target_type`, `cast_range`, `cast_time_ms`, `travel_time_ms`, `cooldown_ms`, `description_template`.
- Relationships: 1-n tới `skill_effects`; linked from `player_skills`, `martial_art_skills`, `enemy_template_skills`, `equipment_template_skill_grants`.
- Runtime/config: config data.
- Ai ghi data: seed/admin.
- Ai đọc data: combat, skill loadout, UI description/presentation.
- Khi nào insert/update/delete: config only.
- Validation liên quan: player/equipment/enemy references must exist.
- Rủi ro/chưa rõ: canonical grouping depends on `skill_group_code` consistency.
- Source: `database/initDatabase.sql`, `GameServer/Services/SkillService.cs`

### `skill_effects`

- Purpose: data-driven skill effect list.
- Important columns: `skill_id`, `effect_type`, `order_index`, `formula_type`, `value_type`, `base_value`, `ratio_value`, `extra_value`, `chance_value`, `duration_ms`, `stat_type`, `resource_type`, `target_scope`, `trigger_timing`.
- Relationships: n-1 tới `skills`.
- Runtime/config: config data.
- Ai ghi data: seed/admin.
- Ai đọc data: `SkillExecutionService`.
- Khi nào insert/update/delete: config only.
- Validation liên quan: enum integrity is critical for combat runtime.
- Rủi ro/chưa rõ: invalid effect data can lead to silent no-op or unsupported paths.
- Source: `database/initDatabase.sql`, `GameServer/Runtime/SkillExecutionService.cs`

### `martial_art_skills`

- Purpose: unlock mapping từ martial art sang skill.
- Important columns: `martial_art_id`, `skill_id`, `unlock_stage`.
- Relationships: n-1 tới `martial_arts`, `skills`.
- Runtime/config: config data.
- Ai ghi data: seed/admin.
- Ai đọc data: martial art / skill ownership services.
- Khi nào insert/update/delete: config only.
- Validation liên quan: source skill must exist.
- Rủi ro/chưa rõ: phase hiện tại independent player skills reduced some earlier coupling.
- Source: `database/initDatabase.sql`, `GameServer/Runtime/CombatDefinitionCatalog.cs`

### `player_martial_arts`

- Purpose: martial arts đã học và progress của player.
- Important columns: `id`, `player_id`, `martial_art_id`, `current_stage`, `current_exp`, `created_at`, `updated_at`.
- Relationships: n-1 tới `characters`, `martial_arts`.
- Runtime/config: runtime persistent.
- Ai ghi data: martial art learn/use progression.
- Ai đọc data: martial art UI, cultivation preview.
- Khi nào insert/update/delete: insert khi học; update progress/active indirect; delete chưa thấy.
- Validation liên quan: duplicate learned martial art bị chặn.
- Rủi ro/chưa rõ: active martial art itself stored on `character_base_stats.active_martial_art_id`.
- Source: `database/initDatabase.sql`, `GameServer/Services/MartialArtService.cs`

### `player_skills`

- Purpose: owned skills của player với source metadata.
- Important columns: `id`, `player_id`, `skill_id`, `skill_group_code`, `source_type`, `source_player_item_id`, `source_martial_art_id`, `source_martial_art_skill_id`, `unlocked_at`, `is_active`.
- Relationships: n-1 tới `characters`, `skills`; 1-n tới `player_skill_loadouts`, `player_skill_grant_sources`.
- Runtime/config: runtime persistent.
- Ai ghi data: `CharacterService` starter skill, `SkillService`, item use/equipment sync.
- Ai đọc data: `SkillService`, combat loadout resolution.
- Khi nào insert/update/delete: insert when learn/grant; update canonical source/skill id; delete when equipment-only grant disappears and no other source remains.
- Validation liên quan: normalize canonical skill per `skill_group_code`.
- Rủi ro/chưa rõ: conflicting same-group same-level templates throw runtime exception.
- Source: `database/initDatabase.sql`, `GameServer/Services/SkillService.cs`

### `player_skill_loadouts`

- Purpose: slot assignment for owned skills.
- Important columns: `id`, `player_id`, `slot_index`, `player_skill_id`.
- Relationships: n-1 tới `player_skills`.
- Runtime/config: runtime persistent.
- Ai ghi data: `SkillService`.
- Ai đọc data: `SkillService`, combat skill resolution.
- Khi nào insert/update/delete: insert/update when set/swap slot; delete when invalid or duplicate.
- Validation liên quan: slot range, blocked by realm/equipment source validity.
- Rủi ro/chưa rõ: invalid rows are auto-normalized out silently.
- Source: `database/initDatabase.sql`, `GameServer/Services/SkillService.cs`

### `player_skill_grant_sources`

- Purpose: canonical source history for granted skills.
- Important columns: `id`, `player_id`, `player_skill_id`, `source_type`, `granted_skill_id`, `source_player_item_id`, `source_equipment_template_id`.
- Relationships: n-1 tới `player_skills`.
- Runtime/config: runtime persistent.
- Ai ghi data: `SkillService`.
- Ai đọc data: `SkillService`.
- Khi nào insert/update/delete: sync when equipment grants appear/disappear or baseline non-equipment source is ensured.
- Validation liên quan: source skill ids must exist.
- Rủi ro/chưa rõ: table is core to equipment-granted skill reconciliation; if data drift occurs, loadout can be normalized away.
- Source: `database/initDatabase.sql`, `GameServer/Services/SkillService.cs`

## Items / equipment / inventory

### `item_templates`

- Purpose: master item definitions.
- Important columns: `id`, `code`, `name`, `item_type`, `rarity`, `max_stack`, `is_tradeable`, `is_droppable`, `is_destroyable`, `icon`, `background_icon`, `description_template`.
- Relationships: parent of many specialized tables.
- Runtime/config: config data.
- Ai ghi data: seed/admin.
- Ai đọc data: `ItemDefinitionCatalog`, inventory, item use, descriptions, rewards.
- Khi nào insert/update/delete: config only.
- Validation liên quan: item type-specific invariants checked by catalog/service.
- Rủi ro/chưa rõ: some item types exist without public gameplay flow yet.
- Source: `database/initDatabase.sql`, `GameServer/Runtime/ItemDefinitionCatalog.cs`

### `player_items`

- Purpose: all physical/stack item instances for player inventory or ground.
- Important columns: `id`, `player_id`, `item_template_id`, `location_type`, `quantity`, `is_bound`, `acquired_at`, `expire_at`, `updated_at`.
- Relationships: n-1 tới `item_templates`; 1-1 optional tới `player_equipments`, `player_soils`.
- Runtime/config: runtime persistent.
- Ai ghi data: `ItemService`, loot/craft/item use flows.
- Ai đọc data: inventory, alchemy validation, equipment, reward pickup/drop.
- Khi nào insert/update/delete: constant mutations on gameplay actions.
- Validation liên quan: ownership/location/expiry/quantity.
- Rủi ro/chưa rõ: ground items are stored in same table using `location_type = Ground`.
- Source: `database/initDatabase.sql`, `GameServer/Services/ItemService.cs`

### `equipment_templates`

- Purpose: item templates that are equipment.
- Important columns: `item_template_id`, `equipment_type`, `level_requirement`.
- Relationships: 1-1 tới `item_templates`; 1-n tới `equipment_template_stats`, `equipment_template_skill_grants`.
- Runtime/config: config data.
- Ai ghi data: seed/admin.
- Ai đọc data: equipment/item services.
- Khi nào insert/update/delete: config only.
- Validation liên quan: item template must exist and be equipment type.
- Rủi ro/chưa rõ: current runtime generic slot count is fixed, not strict per equipment subtype yet.
- Source: `database/initDatabase.sql`, `GameServer/Services/EquipmentService.cs`

### `equipment_template_stats`

- Purpose: stat bonuses from equipment templates.
- Important columns: `equipment_template_id`, `stat_type`, `value`, `value_type`.
- Relationships: n-1 tới `equipment_templates`.
- Runtime/config: config data.
- Ai ghi data: seed/admin.
- Ai đọc data: equipment stat calculation.
- Khi nào insert/update/delete: config only.
- Validation liên quan: stat enums must match runtime.
- Rủi ro/chưa rõ: no explicit per-slot validation in table itself.
- Source: `database/initDatabase.sql`

### `equipment_template_skill_grants`

- Purpose: skills granted by equipping an item.
- Important columns: `equipment_template_id`, `skill_id`, `required_realm_template_id`, `display_order`.
- Relationships: n-1 tới `equipment_templates`, `skills`.
- Runtime/config: config data.
- Ai ghi data: seed/admin.
- Ai đọc data: `SkillService`.
- Khi nào insert/update/delete: config only.
- Validation liên quan: realm requirement checked when assigning to loadout.
- Rủi ro/chưa rõ: if config references unknown skill, sync throws.
- Source: `database/initDatabase.sql`, `GameServer/Services/SkillService.cs`

### `player_equipments`

- Purpose: per-item equipment state.
- Important columns: `player_item_id`, `equipped_slot`, `enhance_level`, `durability`, `updated_at`.
- Relationships: 1-1 tới `player_items`.
- Runtime/config: runtime persistent.
- Ai ghi data: `EquipmentService`, `ItemService` ensure-record logic.
- Ai đọc data: inventory/equipment/stat/skill sync.
- Khi nào insert/update/delete: created when equipment item instance exists; update equip slot; delete when item deleted.
- Validation liên quan: equipped item cannot be removed/dropped.
- Rủi ro/chưa rõ: enhance/durability not yet gameplay-complete.
- Source: `database/initDatabase.sql`, `GameServer/Services/ItemService.cs`, `GameServer/Services/EquipmentService.cs`

### `player_equipment_stat_bonuses`

- Purpose: persistent per-item bonus rows.
- Important columns: `id`, `player_item_id`, `stat_type`, `value`, `value_type`, `source_type`.
- Relationships: n-1 tới `player_items`.
- Runtime/config: runtime persistent.
- Ai ghi data: equipment bonus/stat service.
- Ai đọc data: equipment stat calculation.
- Khi nào insert/update/delete: updated when item/bonus changes; delete when item deleted.
- Validation liên quan: source type must map to known bonus source.
- Rủi ro/chưa rõ: currently less visible on client.
- Source: `database/initDatabase.sql`, `GameServer/Services/ItemService.cs`

### `martial_art_book_templates`

- Purpose: item -> martial art mapping for books.
- Important columns: `item_template_id`, `martial_art_id`.
- Relationships: 1-1 with book item; n-1 to `martial_arts`.
- Runtime/config: config data.
- Ai ghi data: seed/admin.
- Ai đọc data: `ItemUseService`.
- Khi nào insert/update/delete: config only.
- Validation liên quan: book use requires mapping exists.
- Rủi ro/chưa rõ: duplicate/invalid map breaks item use flow.
- Source: `database/initDatabase.sql`, `GameServer/Services/ItemUseService.cs`

## Alchemy / recipes / farming

### `craft_recipes`

- Purpose: generic craft foundation.
- Important columns: `id`, `code`, `name`, `result_item_template_id`, `result_quantity`, `success_rate`, `mutation_rate`, costs.
- Relationships: 1-n tới `craft_recipe_requirements`, `craft_recipe_mutation_bonuses`.
- Runtime/config: config data.
- Ai ghi data: seed/admin.
- Ai đọc data: generic craft foundation; current alchemy flow mostly uses pill recipe tables instead.
- Khi nào insert/update/delete: config only.
- Validation liên quan: Unknown / Need confirmation.
- Rủi ro/chưa rõ: foundation exists but public gameplay currently centered on pill recipes.
- Source: `database/initDatabase.sql`

### `craft_recipe_requirements`

- Purpose: generic craft inputs.
- Important columns: `craft_recipe_id`, `required_item_template_id`, `required_quantity`, `consume_mode`, `is_optional`, `mutation_bonus_rate`.
- Relationships: n-1 tới `craft_recipes`.
- Runtime/config: config data.
- Ai ghi data: seed/admin.
- Ai đọc data: generic craft foundation.
- Khi nào insert/update/delete: config only.
- Validation liên quan: Unknown / Need confirmation.
- Rủi ro/chưa rõ: generic system may be intended for smithing/talisman later.
- Source: `database/initDatabase.sql`

### `craft_recipe_mutation_bonuses`

- Purpose: generic mutation outcome bonuses.
- Important columns: `craft_recipe_id`, `stat_type`, `value`, `value_type`.
- Relationships: n-1 tới `craft_recipes`.
- Runtime/config: config data.
- Ai ghi data: seed/admin.
- Ai đọc data: generic craft foundation.
- Khi nào insert/update/delete: config only.
- Validation liên quan: Unknown / Need confirmation.
- Rủi ro/chưa rõ: current alchemy practice flow does not yet expose mutation-branch reward design richly.
- Source: `database/initDatabase.sql`

### `player_caves`

- Purpose: player-owned cave/home record.
- Important columns: `id`, `owner_character_id`, `map_template_id`, `zone_index`, `is_home`.
- Relationships: n-1 tới `characters`, `map_templates`; 1-n tới `player_garden_plots`.
- Runtime/config: runtime persistent.
- Ai ghi data: `CharacterService`, `HerbService`.
- Ai đọc data: cultivation/home systems, herb service.
- Khi nào insert/update/delete: auto create on character creation/home ensure.
- Validation liên quan: owner must match player using cave.
- Rủi ro/chưa rõ: no public client cave management UI yet.
- Source: `database/initDatabase.sql`, `GameServer/Services/CharacterService.cs`, `GameServer/Services/HerbService.cs`

### `player_garden_plots`

- Purpose: plots inside cave.
- Important columns: `id`, `player_id`, `cave_id`, `plot_index`, `current_soil_player_item_id`, `current_player_herb_id`.
- Relationships: n-1 tới `player_caves`; logical links to `player_soils`, `player_herbs`.
- Runtime/config: runtime persistent.
- Ai ghi data: `CharacterService`, `HerbService`.
- Ai đọc data: `HerbService`.
- Khi nào insert/update/delete: seed when cave created; update when insert soil/plant/harvest.
- Validation liên quan: owned cave/plot checks.
- Rủi ro/chưa rõ: no client packet/UI yet.
- Source: `database/initDatabase.sql`, `GameServer/Services/HerbService.cs`

### `soil_templates`

- Purpose: metadata for soil items.
- Important columns: `item_template_id`, `growth_speed_rate`, `max_active_seconds`, `description`.
- Relationships: 1-1 tới `item_templates`.
- Runtime/config: config data.
- Ai ghi data: seed/admin.
- Ai đọc data: `AlchemyDefinitionCatalog`, `HerbService`.
- Khi nào insert/update/delete: config only.
- Validation liên quan: soil item must have max_stack = 1 per catalog rule.
- Rủi ro/chưa rõ: not player-facing yet.
- Source: `database/initDatabase.sql`, `GameServer/Runtime/ItemDefinitionCatalog.cs`

### `player_soils`

- Purpose: runtime state for a soil item instance.
- Important columns: `player_item_id`, `total_used_seconds`, `state`, `inserted_plot_id`, `updated_at`.
- Relationships: 1-1 tới `player_items`; logical link to `player_garden_plots`.
- Runtime/config: runtime persistent.
- Ai ghi data: `ItemService`, `HerbService`.
- Ai đọc data: inventory restrictions, alchemy validation, herb growth.
- Khi nào insert/update/delete: create when soil item created; update while inserted/depleted; delete when item deleted.
- Validation liên quan: inserted soil cannot leave inventory or be used as alchemy ingredient.
- Rủi ro/chưa rõ: client currently cannot manipulate directly.
- Source: `database/initDatabase.sql`, `GameServer/Services/ItemService.cs`, `GameServer/Services/HerbService.cs`

### `herb_templates`

- Purpose: herb master data.
- Important columns: `id`, `code`, `name`, `seed_item_template_id`, `replant_item_template_id`, `description`.
- Relationships: 1-n tới `herb_growth_stage_configs`, `herb_harvest_outputs`, `player_herbs`.
- Runtime/config: config data.
- Ai ghi data: seed/admin.
- Ai đọc data: `AlchemyDefinitionCatalog`, `HerbService`.
- Khi nào insert/update/delete: config only.
- Validation liên quan: seed/replant items must map to valid item templates.
- Rủi ro/chưa rõ: no completed player-facing flow yet.
- Source: `database/initDatabase.sql`, `GameServer/Runtime/AlchemyDefinitionCatalog.cs`

### `herb_growth_stage_configs`

- Purpose: timing thresholds for herb growth stages.
- Important columns: `herb_template_id`, `stage`, `required_growth_seconds`.
- Relationships: n-1 tới `herb_templates`.
- Runtime/config: config data.
- Ai ghi data: seed/admin.
- Ai đọc data: `AlchemyDefinitionCatalog`, `HerbService`.
- Khi nào insert/update/delete: config only.
- Validation liên quan: stage ordering matters.
- Rủi ro/chưa rõ: balance doc absent.
- Source: `database/initDatabase.sql`, `GameServer/Services/HerbService.cs`

### `player_herbs`

- Purpose: runtime planted/in-inventory herb entities.
- Important columns: `id`, `player_id`, `herb_template_id`, `current_stage`, `planted_at`, `accumulated_growth_seconds`, `state`, `current_plot_id`.
- Relationships: n-1 tới `characters`, `herb_templates`, `player_garden_plots`.
- Runtime/config: runtime persistent.
- Ai ghi data: `HerbService`.
- Ai đọc data: `HerbService`.
- Khi nào insert/update/delete: insert when plant; update growth/state; delete on harvest.
- Validation liên quan: ownership and plot state checks.
- Rủi ro/chưa rõ: no client packets yet.
- Source: `database/initDatabase.sql`, `GameServer/Services/HerbService.cs`

### `herb_harvest_outputs`

- Purpose: output items by herb stage.
- Important columns: `herb_template_id`, `required_stage`, `output_type`, `result_item_template_id`, `result_quantity`, `output_chance`.
- Relationships: n-1 tới `herb_templates`.
- Runtime/config: config data.
- Ai ghi data: seed/admin.
- Ai đọc data: `AlchemyDefinitionCatalog`, `HerbService`.
- Khi nào insert/update/delete: config only.
- Validation liên quan: harvest requires matching stage/output.
- Rủi ro/chưa rõ: if config missing, harvest throws runtime invalid operation.
- Source: `database/initDatabase.sql`, `GameServer/Services/HerbService.cs`

### `pill_templates`

- Purpose: pill item metadata.
- Important columns: `item_template_id`, `pill_category`, `usage_type`, `cooldown_ms`.
- Relationships: 1-1 tới `item_templates`; 1-n tới `pill_effects`.
- Runtime/config: config data.
- Ai ghi data: seed/admin.
- Ai đọc data: `ItemUseService`, alchemy/model builders.
- Khi nào insert/update/delete: config only.
- Validation liên quan: consumable use supported mainly for direct-use pills.
- Rủi ro/chưa rõ: more complex pill effects phase sau.
- Source: `database/initDatabase.sql`, `GameServer/Services/ItemUseService.cs`

### `pill_effects`

- Purpose: effects of pill use.
- Important columns: `pill_template_id`, `effect_type`, `order_index`, `value_type`, `base_value`, `ratio_value`, `duration_ms`, `stat_type`, `note`.
- Relationships: n-1 tới `pill_templates`.
- Runtime/config: config data.
- Ai ghi data: seed/admin.
- Ai đọc data: `ItemUseService`.
- Khi nào insert/update/delete: config only.
- Validation liên quan: unsupported effect types cause use failure.
- Rủi ro/chưa rõ: current phase supports only subset of consumable behaviors.
- Source: `database/initDatabase.sql`, `GameServer/Services/ItemUseService.cs`

### `pill_recipe_templates`

- Purpose: master alchemy recipe.
- Important columns: `id`, `code`, `name`, `recipe_book_item_template_id`, `result_pill_item_template_id`, `craft_duration_seconds`, `base_success_rate`, `success_rate_cap`, `mutation_rate`, `mutation_rate_cap`.
- Relationships: 1-n tới `pill_recipe_inputs`, `pill_recipe_mastery_stages`, `player_pill_recipes`.
- Runtime/config: config data.
- Ai ghi data: seed/admin.
- Ai đọc data: `AlchemyDefinitionCatalog`, `AlchemyService`, `PillRecipeService`.
- Khi nào insert/update/delete: config only.
- Validation liên quan: player must have learned recipe to craft.
- Rủi ro/chưa rõ: required herb maturity inputs postponed to later phase.
- Source: `database/initDatabase.sql`, `GameServer/Services/AlchemyService.cs`

### `pill_recipe_inputs`

- Purpose: required/optional inputs for pill recipe.
- Important columns: `pill_recipe_template_id`, `required_item_template_id`, `required_quantity`, `consume_mode`, `is_optional`, `success_rate_bonus`, `mutation_bonus_rate`, `required_herb_maturity`.
- Relationships: n-1 tới `pill_recipe_templates`.
- Runtime/config: config data.
- Ai ghi data: seed/admin.
- Ai đọc data: `AlchemyService`.
- Khi nào insert/update/delete: config only.
- Validation liên quan: optional selections normalized by input id; herb maturity currently rejected if any row uses it.
- Rủi ro/chưa rõ: table is richer than current public feature set.
- Source: `database/initDatabase.sql`, `GameServer/Services/AlchemyService.cs`

### `player_pill_recipes`

- Purpose: learned alchemy recipes + mastery progress.
- Important columns: `id`, `player_id`, `pill_recipe_template_id`, `learned_at`, `total_craft_count`, `current_success_rate_bonus`, `updated_at`.
- Relationships: n-1 tới `characters`, `pill_recipe_templates`.
- Runtime/config: runtime persistent.
- Ai ghi data: item use learns recipe; alchemy completion updates mastery.
- Ai đọc data: alchemy query/validation services.
- Khi nào insert/update/delete: insert on learning recipe; update after successful craft sessions.
- Validation liên quan: duplicate learn blocked.
- Rủi ro/chưa rõ: mastery bonus depends on staged thresholds; balance doc needed.
- Source: `database/initDatabase.sql`, `GameServer/Services/AlchemyPracticeService.cs`

### `pill_recipe_mastery_stages`

- Purpose: craft count thresholds for mastery bonus.
- Important columns: `pill_recipe_template_id`, `required_total_craft_count`, `success_rate_bonus`.
- Relationships: n-1 tới `pill_recipe_templates`.
- Runtime/config: config data.
- Ai ghi data: seed/admin.
- Ai đọc data: `AlchemyService`.
- Khi nào insert/update/delete: config only.
- Validation liên quan: ordered thresholds expected.
- Rủi ro/chưa rõ: no explicit admin guard against overlapping thresholds.
- Source: `database/initDatabase.sql`, `GameServer/Services/AlchemyService.cs`

### `player_practice_sessions`

- Purpose: generic async practice session state.
- Important columns: `id`, `player_id`, `practice_type`, `practice_state`, `definition_id`, `current_map_id`, `title`, `total_duration_seconds`, `accumulated_active_seconds`, `cancel_locked_progress`, `request_payload_json`, `result_payload_json`, lifecycle timestamps.
- Relationships: n-1 tới `characters`; logical to definition table by `practice_type`.
- Runtime/config: runtime persistent.
- Ai ghi data: `AlchemyCraftActionService`, `PracticeService`, `AlchemyPracticeService`.
- Ai đọc data: practice/alchemy status restore, notifications, client resume panel.
- Khi nào insert/update/delete: insert on craft start; update pause/resume/cancel/complete/ack; delete not currently normal path.
- Validation liên quan: world/private home/state restrictions, refund threshold, type-specific payload.
- Rủi ro/chưa rõ: supports more practice types than current gameplay uses.
- Source: `database/initDatabase.sql`, `GameServer/Services/PracticeService.cs`

## Enemy / reward

### `enemy_templates`

- Purpose: master enemy config.
- Important columns: `id`, `code`, `name`, `kind`, `ai_behavior`, `max_hp`, `base_attack`, `base_move_speed`, patrol/detection/combat radii, out-of-combat restore flags, reward totals.
- Relationships: 1-n tới `enemy_template_skills`, `enemy_reward_rules`, `map_enemy_spawn_entries`.
- Runtime/config: config data.
- Ai ghi data: seed/admin.
- Ai đọc data: `EnemyDefinitionCatalog`, map runtime.
- Khi nào insert/update/delete: config only.
- Validation liên quan: reward rules and skill ids must exist.
- Rủi ro/chưa rõ: combat stat balance is highly config-sensitive.
- Source: `database/initDatabase.sql`, `GameServer/Runtime/EnemyDefinitionCatalog.cs`

### `enemy_template_skills`

- Purpose: skill list for enemy template.
- Important columns: `enemy_template_id`, `skill_id`, `order_index`.
- Relationships: n-1 tới `enemy_templates`, `skills`.
- Runtime/config: config data.
- Ai ghi data: seed/admin.
- Ai đọc data: enemy runtime combat.
- Khi nào insert/update/delete: config only.
- Validation liên quan: skill must exist.
- Rủi ro/chưa rõ: skill priority logic currently simple.
- Source: `database/initDatabase.sql`, `GameServer/Runtime/WorldRuntimeSettlementService.cs`

### `enemy_reward_rules`

- Purpose: reward distribution config per enemy.
- Important columns: `enemy_template_id`, `delivery_type`, `target_rule`, `game_random_table_id`, `roll_count`, `ownership_duration_seconds`, `free_for_all_duration_seconds`, `minimum_damage_parts_per_million`, `order_index`.
- Relationships: n-1 tới `enemy_templates`, `game_random_tables`.
- Runtime/config: config data.
- Ai ghi data: seed/admin.
- Ai đọc data: `EnemyRewardRuntimeService`.
- Khi nào insert/update/delete: config only.
- Validation liên quan: reward table must exist, delivery/target enums valid.
- Rủi ro/chưa rõ: direct currency/non-item reward entries not fully documented.
- Source: `database/initDatabase.sql`, `GameServer/Runtime/EnemyRewardRuntimeService.cs`

### `map_enemy_spawn_groups`

- Purpose: spawn group config per map/runtime/zone.
- Important columns: `id`, `code`, `name`, `map_template_id`, `runtime_scope`, `zone_index`, `spawn_mode`, `is_boss_spawn`, `max_alive`, `respawn_seconds`, `center_x/y`, `spawn_radius`, `patrol_radius`, `patrol_route_type`.
- Relationships: n-1 tới `map_templates`; 1-n tới `map_enemy_spawn_entries`.
- Runtime/config: config data.
- Ai ghi data: seed/admin.
- Ai đọc data: `EnemyDefinitionCatalog`, `MapManager`.
- Khi nào insert/update/delete: config only.
- Validation liên quan: runtime scope and zone selection must match target map behavior.
- Rủi ro/chưa rõ: objective/boss modes need extra content docs.
- Source: `database/initDatabase.sql`, `GameServer/World/MapManager.cs`

### `map_enemy_spawn_entries`

- Purpose: weighted enemy options inside spawn group.
- Important columns: `spawn_group_id`, `enemy_template_id`, `weight`, `order_index`.
- Relationships: n-1 tới `map_enemy_spawn_groups`, `enemy_templates`.
- Runtime/config: config data.
- Ai ghi data: seed/admin.
- Ai đọc data: enemy spawn runtime.
- Khi nào insert/update/delete: config only.
- Validation liên quan: enemy template must exist.
- Rủi ro/chưa rõ: balance tuning docs absent.
- Source: `database/initDatabase.sql`, `GameServer/Runtime/EnemyDefinitionCatalog.cs`

## Notifications

### `player_notifications`

- Purpose: unread/inbox notification records, currently used strongly by practice completion.
- Important columns: `id`, `player_id`, `notification_type`, `source_type`, `source_id`, `title`, `message`, `display_item_template_id`, `payload_json`, `created_at_utc`, `read_at_utc`.
- Relationships: n-1 tới `characters`; optional logical link to source entity by type/id.
- Runtime/config: runtime persistent.
- Ai ghi data: `AlchemyPracticeService`, `PlayerNotificationService`.
- Ai đọc data: `PlayerNotificationService`, client notification UI.
- Khi nào insert/update/delete: insert on notification creation; update `read_at_utc` when acknowledged; delete not normal path.
- Validation liên quan: owner must match when acknowledging.
- Rủi ro/chưa rõ: retention/cleanup policy not visible.
- Source: `database/initDatabase.sql`, `GameServer/Services/PlayerNotificationService.cs`

## Schema gaps / inconsistencies

- `initDatabase.sql` assumes nền schema account/character/map/realm đã tồn tại; không phải one-shot full bootstrap file.
- `GameConfigKeys` có 2 key chưa được seed vào `game_configs`: `item_drop.ground_spawn_offset_server_units`, `alchemy.practice_cancel_refund_progress_threshold`.
- `breakthrough_conditions` tồn tại nhưng chưa thấy runtime sử dụng.
- `HerbService` và các bảng farming đã khá đầy đủ, nhưng packet/client flow chưa lộ ra, nên đây là subsystem partial.

Source: `database/initDatabase.sql`, `database/phamnhan_online.sql`, `GameServer/Config/GameConfigKeys.cs`, `GameServer/Services/HerbService.cs`, `GameServer/Repositories/BreakthroughConditionRepository.cs`
