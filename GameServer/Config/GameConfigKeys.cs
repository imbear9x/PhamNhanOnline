namespace GameServer.Config;

public static class GameConfigKeys
{
    public const string NetworkReconnectResumeWindowSeconds = "network.reconnect_resume_window_seconds";
    public const string WorldPortalValidationBufferServerUnits = "world.portal_validation_buffer_server_units";
    public const string CharacterPositionSyncGraceServerUnits = "character.position_sync_grace_server_units";
    public const string CharacterPositionSyncMaxElapsedSeconds = "character.position_sync_max_elapsed_seconds";
    public const string CharacterPositionSyncMaxSpeedMultiplier = "character.position_sync_max_speed_multiplier";
    public const string CharacterPositionSyncCatchupMultiplier = "character.position_sync_catchup_multiplier";
    public const string CharacterPositionSyncCatchupMaxSeconds = "character.position_sync_catchup_max_seconds";
    public const string CombatSkillRangeGraceBufferUnits = "combat.skill_range_grace_buffer_units";
    public const string CombatDeathReturnHomeRecoveryRatio = "combat_death.return_home_recovery_ratio";
    public const string ItemDropPlayerOwnershipSeconds = "item_drop.player_drop_ownership_seconds";
    public const string ItemDropPlayerFreeForAllSeconds = "item_drop.player_drop_free_for_all_seconds";
    public const string ItemDropEnemyDefaultOwnershipSeconds = "item_drop.enemy_drop_default_ownership_seconds";
    public const string ItemDropEnemyDefaultFreeForAllSeconds = "item_drop.enemy_drop_default_free_for_all_seconds";
    public const string ItemDropGroundSpawnOffsetServerUnits = "item_drop.ground_spawn_offset_server_units";
    public const string GroundRewardPickupRadiusServerUnits = "ground_reward.pickup_radius_server_units";
    public const string WorldEmptyPublicInstanceLifetimeSeconds = "world.empty_public_instance_lifetime_seconds";
    public const string CultivationPotentialPerCultivationPoint = "cultivation.potential_per_cultivation_point";
    public const string CultivationSettlementIntervalSeconds = "cultivation.settlement_interval_seconds";
    public const string AlchemyPracticeCancelRefundProgressThreshold = "alchemy.practice_cancel_refund_progress_threshold";
    public const string CharacterHomeGardenPlotCount = "character.home_garden_plot_count";
    public const string CharacterEquipmentSlotCount = "character.equipment_slot_count";
    public const string CharacterStarterSkillId = "character.starter_skill_id";
    public const string SkillMaxLoadoutSlotCount = "skill.max_loadout_slot_count";
}

