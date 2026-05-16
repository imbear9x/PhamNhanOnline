namespace GameServer.Config;

public sealed class GameConfigValues
{
    public int NetworkReconnectResumeWindowSeconds { get; init; } = 3;
    public float WorldPortalValidationBufferServerUnits { get; init; } = 4f;
    public float CharacterPositionSyncGraceServerUnits { get; init; } = 45f;
    public float CharacterPositionSyncMaxElapsedSeconds { get; init; } = 1.5f;
    public float CharacterPositionSyncMaxSpeedMultiplier { get; init; } = 1.25f;
    public float CharacterPositionSyncCatchupMultiplier { get; init; } = 1.3f;
    public float CharacterPositionSyncCatchupMaxSeconds { get; init; } = 0.75f;
    public float CombatSkillRangeGraceBufferUnits { get; init; } = 12f;
    public double CombatDeathReturnHomeRecoveryRatio { get; init; } = 0.80d;
    public int DeathLifespanPenaltySeconds { get; init; } = 86400;
    public int DeathTribulationPenaltySeconds { get; init; } = 86400;
    public int ItemDropPlayerOwnershipSeconds { get; init; } = 10;
    public int ItemDropPlayerFreeForAllSeconds { get; init; } = 50;
    public int ItemDropEnemyDefaultOwnershipSeconds { get; init; } = 30;
    public int ItemDropEnemyDefaultFreeForAllSeconds { get; init; } = 30;
    public float ItemDropGroundSpawnOffsetServerUnits { get; init; } = 30f;
    public float GroundRewardPickupRadiusServerUnits { get; init; } = 120f;
    public int WorldEmptyPublicInstanceLifetimeSeconds { get; init; } = 120;
    public int CultivationPotentialPerCultivationPoint { get; init; } = 1;
    public int CultivationSettlementIntervalSeconds { get; init; } = 300;
    public double AlchemyPracticeCancelRefundProgressThreshold { get; init; } = 0.30d;
    public int CharacterHomeGardenPlotCount { get; init; } = 8;
    public int CharacterEquipmentSlotCount { get; init; } = 4;
    public int CharacterStarterSkillId { get; init; } = 0;
    public int SkillMaxLoadoutSlotCount { get; init; } = 5;
    public string InventoryBagUpgradeCurrencyCode { get; init; } = "currency.spirit_stone_small";
    public int HerbInventoryExpirySeconds { get; init; } = 604800;
    public int HerbExpirySweepIntervalSeconds { get; init; } = 60;

    public TimeSpan ResumeWindow => TimeSpan.FromSeconds(Math.Max(0, NetworkReconnectResumeWindowSeconds));
    public TimeSpan CultivationSettlementInterval => TimeSpan.FromSeconds(Math.Max(1, CultivationSettlementIntervalSeconds));
    public TimeSpan WorldEmptyPublicInstanceLifetime => TimeSpan.FromSeconds(Math.Max(1, WorldEmptyPublicInstanceLifetimeSeconds));
    public TimeSpan HerbInventoryExpiry => TimeSpan.FromSeconds(Math.Max(1, HerbInventoryExpirySeconds));
    public TimeSpan HerbExpirySweepInterval => TimeSpan.FromSeconds(Math.Max(1, HerbExpirySweepIntervalSeconds));
}

