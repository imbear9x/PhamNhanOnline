using GameServer.Entities;

namespace GameServer.DTO;

public sealed record CharacterCurrentStateDto(
    Guid CharacterId,
    int CurrentHp,
    int CurrentMp,
    int CurrentStamina,
    long LifespanEndGameMinute,
    int? CurrentMapId,
    float CurrentPosX,
    float CurrentPosY,
    bool IsDead,
    int CurrentState,
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
            entity.CurrentPosX,
            entity.CurrentPosY,
            entity.IsDead,
            entity.CurrentState,
            entity.LastSavedAt);
}
