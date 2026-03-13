using GameServer.Entities;

namespace GameServer.DTO;

public sealed record CharacterBaseStatsDto(
    Guid CharacterId,
    int? RealmTemplateId,
    long? Cultivation,
    int? BaseHp,
    int? BaseMp,
    int? BasePhysique,
    int? BaseAttack,
    int? BaseSpeed,
    int? BaseSpiritualSense,
    double? BaseFortune,
    int? BasePotential)
{
    public static CharacterBaseStatsDto FromEntity(CharacterBaseStat entity) =>
        new(
            entity.CharacterId,
            entity.RealmId,
            entity.Cultivation,
            entity.BaseHp,
            entity.BaseMp,
            entity.BasePhysique,
            entity.BaseAttack,
            entity.BaseSpeed,
            entity.BaseSpiritualSense,
            entity.BaseFortune,
            entity.BasePotential);
}
