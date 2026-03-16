using GameServer.Entities;

namespace GameServer.DTO;

public sealed record CharacterBaseStatsDto(
    Guid CharacterId,
    int? RealmTemplateId,
    int? RealmLifespan,
    long? Cultivation,
    int? BaseHp,
    int? BaseMp,
    int? BasePhysique,
    int? BaseAttack,
    int? BaseSpeed,
    int? BaseSpiritualSense,
    int? BaseStamina,
    int? LifespanBonus,
    double? BaseFortune,
    int? BasePotential,
    int? UnallocatedPotential)
{
    public static CharacterBaseStatsDto FromEntity(CharacterBaseStat entity, int? realmLifespan = null) =>
        new(
            entity.CharacterId,
            entity.RealmId,
            realmLifespan,
            entity.Cultivation,
            entity.BaseHp,
            entity.BaseMp,
            entity.BasePhysique,
            entity.BaseAttack,
            entity.BaseSpeed,
            entity.BaseSpiritualSense,
            entity.BaseStamina,
            entity.LifespanBonus,
            entity.BaseFortune,
            entity.BasePotential,
            entity.UnallocatedPotential);
}
