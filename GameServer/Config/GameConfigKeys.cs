namespace GameServer.Config;

public static class GameConfigKeys
{
    public const string NetworkReconnectResumeWindowSeconds = "network.reconnect_resume_window_seconds";
    public const string WorldPortalValidationBufferServerUnits = "world.portal_validation_buffer_server_units";
    public const string CombatSkillRangeGraceBufferUnits = "combat.skill_range_grace_buffer_units";
    public const string CombatDeathReturnHomeRecoveryRatio = "combat_death.return_home_recovery_ratio";
    public const string ItemDropPlayerOwnershipSeconds = "item_drop.player_drop_ownership_seconds";
    public const string ItemDropPlayerFreeForAllSeconds = "item_drop.player_drop_free_for_all_seconds";
    public const string ItemDropEnemyDefaultOwnershipSeconds = "item_drop.enemy_drop_default_ownership_seconds";
    public const string ItemDropEnemyDefaultFreeForAllSeconds = "item_drop.enemy_drop_default_free_for_all_seconds";
    public const string ItemDropGroundSpawnOffsetServerUnits = "item_drop.ground_spawn_offset_server_units";
    public const string WorldEmptyPublicInstanceLifetimeSeconds = "world.empty_public_instance_lifetime_seconds";
    public const string CultivationPotentialPerCultivationPoint = "cultivation.potential_per_cultivation_point";
    public const string CultivationSettlementIntervalSeconds = "cultivation.settlement_interval_seconds";
    public const string CharacterHomeGardenPlotCount = "character.home_garden_plot_count";
    public const string CharacterStarterBasicSkillId = "character.starter_basic_skill_id";
    public const string CharacterStarterBasicSkillSlotIndex = "character.starter_basic_skill_slot_index";
    public const string SkillMaxLoadoutSlotCount = "skill.max_loadout_slot_count";
}

