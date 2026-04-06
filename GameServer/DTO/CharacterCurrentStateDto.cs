using GameServer.Entities;

namespace GameServer.DTO;

public sealed record CharacterCurrentStateDto(
    Guid CharacterId,
    int CurrentHp,
    int CurrentMp,
    int CurrentStamina,
    long LifespanEndGameMinute,
    int? CurrentMapId,
    int CurrentZoneIndex,
    float CurrentPosX,
    float CurrentPosY,
    bool IsExpired,
    int CurrentState,
    DateTime? CultivationStartedAtUtc,
    DateTime? LastCultivationRewardedAtUtc,
    DateTime LastSavedAt)
{
    public static CharacterCurrentStateDto FromEntity(CharacterCurrentState entity) =>
        new(
            entity.CharacterId,
            entity.CurrentHp,
            entity.CurrentMp,
            entity.CurrentStamina,
            entity.LifespanEndGameMinute,
            entity.CurrentMapId,
            entity.CurrentZoneIndex,
            entity.CurrentPosX,
            entity.CurrentPosY,
            entity.IsExpired,
            entity.CurrentState,
            entity.CultivationStartedAtUtc,
            entity.LastCultivationRewardedAtUtc,
            entity.LastSavedAt);
}
