---
title: Game configs phase 1
doc_type: config-contract
status: verified
owner: dev
code_status: code-verified
last_verified: 2026-05-11
source_of_truth:
  - docs/reference-and-specs/GAME_CONFIGS.md
  - GameServer/Config/GameConfigValues.cs
  - GameServer/Config/GameConfigKeys.cs
related_docs:
  - docs/rules/server-transaction-boundary.md
  - docs/rules/client-state-sync-runtime.md
tags:
  - config
  - server
  - contracts
  - phase1
---

# Summary

`public.game_configs` là bảng chứa gameplay/server config có thể tinh chỉnh thay cho hardcode trực tiếp trong `GameServer`.

**Graph links:** [[config-and-contract-map]] · [[server-runtime-architecture]] · [[client-state-sync-runtime]] · [[world-observer-and-movement-runtime]] · [[skill-combat-runtime]]

# Runtime contract

## Table purpose

Bảng `public.game_configs` chứa:

- `config_key`
- `config_value`
- `description`
- `created_at`
- `updated_at`

Server parse `config_value` từ chuỗi sang typed snapshot runtime.

## Loading model

Theo legacy doc và code hiện tại:

- server load config thành typed snapshot `GameConfigValues`
- snapshot được giữ trong runtime
- thay đổi DB không có hot reload tự động
- cần restart server để giá trị mới có hiệu lực

# Verified key set

Các key dưới đây đã được verify bởi `GameConfigKeys.cs` và `GameConfigValues.cs`.

| `config_key` | Property | Type | Default |
|---|---|---:|---:|
| `network.reconnect_resume_window_seconds` | `NetworkReconnectResumeWindowSeconds` | `int` | `3` |
| `world.portal_validation_buffer_server_units` | `WorldPortalValidationBufferServerUnits` | `float` | `4` |
| `character.position_sync_grace_server_units` | `CharacterPositionSyncGraceServerUnits` | `float` | `45` |
| `character.position_sync_max_elapsed_seconds` | `CharacterPositionSyncMaxElapsedSeconds` | `float` | `1.5` |
| `character.position_sync_max_speed_multiplier` | `CharacterPositionSyncMaxSpeedMultiplier` | `float` | `1.25` |
| `character.position_sync_catchup_multiplier` | `CharacterPositionSyncCatchupMultiplier` | `float` | `1.3` |
| `character.position_sync_catchup_max_seconds` | `CharacterPositionSyncCatchupMaxSeconds` | `float` | `0.75` |
| `combat.skill_range_grace_buffer_units` | `CombatSkillRangeGraceBufferUnits` | `float` | `12` |
| `combat_death.return_home_recovery_ratio` | `CombatDeathReturnHomeRecoveryRatio` | `double` | `0.80` |
| `item_drop.player_drop_ownership_seconds` | `ItemDropPlayerOwnershipSeconds` | `int` | `10` |
| `item_drop.player_drop_free_for_all_seconds` | `ItemDropPlayerFreeForAllSeconds` | `int` | `50` |
| `item_drop.enemy_drop_default_ownership_seconds` | `ItemDropEnemyDefaultOwnershipSeconds` | `int` | `30` |
| `item_drop.enemy_drop_default_free_for_all_seconds` | `ItemDropEnemyDefaultFreeForAllSeconds` | `int` | `30` |
| `item_drop.ground_spawn_offset_server_units` | `ItemDropGroundSpawnOffsetServerUnits` | `float` | `30` |
| `ground_reward.pickup_radius_server_units` | `GroundRewardPickupRadiusServerUnits` | `float` | `120` |
| `world.empty_public_instance_lifetime_seconds` | `WorldEmptyPublicInstanceLifetimeSeconds` | `int` | `120` |
| `cultivation.potential_per_cultivation_point` | `CultivationPotentialPerCultivationPoint` | `int` | `1` |
| `cultivation.settlement_interval_seconds` | `CultivationSettlementIntervalSeconds` | `int` | `300` |
| `alchemy.practice_cancel_refund_progress_threshold` | `AlchemyPracticeCancelRefundProgressThreshold` | `double` | `0.30` |
| `character.home_garden_plot_count` | `CharacterHomeGardenPlotCount` | `int` | `8` |
| `character.equipment_slot_count` | `CharacterEquipmentSlotCount` | `int` | `4` |
| `character.starter_skill_id` | `CharacterStarterSkillId` | `int` | `0` |
| `skill.max_loadout_slot_count` | `SkillMaxLoadoutSlotCount` | `int` | `5` |

# Operational rule

Sau khi sửa `public.game_configs`:

- giá trị mới chưa tự áp dụng ngay
- phải restart server để runtime snapshot nạp lại

# Notes

Legacy doc `GAME_CONFIGS.md` vẫn hữu ích như bảng tham khảo rộng hơn, nhưng canonical contract cho second-brain bây giờ là file này.
