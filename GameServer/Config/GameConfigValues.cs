namespace GameServer.Config;

public sealed class GameConfigValues
{
    public int NetworkReconnectResumeWindowSeconds { get; init; } = 3;
    public float WorldPortalValidationBufferServerUnits { get; init; } = 4f;
    public float CombatSkillRangeGraceBufferUnits { get; init; } = 12f;
    public double CombatDeathReturnHomeRecoveryRatio { get; init; } = 0.80d;
    public int ItemDropPlayerOwnershipSeconds { get; init; } = 10;
    public int ItemDropPlayerFreeForAllSeconds { get; init; } = 50;
    public int ItemDropEnemyDefaultOwnershipSeconds { get; init; } = 30;
    public int ItemDropEnemyDefaultFreeForAllSeconds { get; init; } = 30;
    public float ItemDropGroundSpawnOffsetServerUnits { get; init; } = 30f;
    public int WorldEmptyPublicInstanceLifetimeSeconds { get; init; } = 120;
    public int CultivationPotentialPerCultivationPoint { get; init; } = 1;
    public int CultivationSettlementIntervalSeconds { get; init; } = 300;
    public int CharacterHomeGardenPlotCount { get; init; } = 8;
    public int CharacterStarterBasicSkillId { get; init; } = 0;
    public int CharacterStarterBasicSkillSlotIndex { get; init; } = 1;
    public int SkillMaxLoadoutSlotCount { get; init; } = 5;

    public TimeSpan ResumeWindow => TimeSpan.FromSeconds(Math.Max(0, NetworkReconnectResumeWindowSeconds));
    public TimeSpan CultivationSettlementInterval => TimeSpan.FromSeconds(Math.Max(1, CultivationSettlementIntervalSeconds));
    public TimeSpan WorldEmptyPublicInstanceLifetime => TimeSpan.FromSeconds(Math.Max(1, WorldEmptyPublicInstanceLifetimeSeconds));
}

