using GameServer.Entities;

namespace GameServer.DTO;

public sealed record CharacterStatsDto(
    Guid CharacterId,
    int? RealmTemplateId,
    long? Cultivation,
    int? Health,
    int? Mana,
    int? Physique,
    int? Attack,
    int? Speed,
    int? SpiritualSense,
    double? Fortune,
    int? Potential)
{
    public static CharacterStatsDto FromEntity(CharacterStat entity) =>
        new(
            entity.CharacterId,
            entity.RealmId,
            entity.Cultivation,
            entity.Hp,
            entity.Mp,
            entity.Physique,
            entity.Attack,
            entity.Speed,
            entity.SpiritualSense,
            entity.Fortune,
            entity.Potential);
}

