using GameShared.Models;
using GameServer.Runtime;
using GameServer.Time;
using GameServer.World;

namespace GameServer.DTO;

public static class NetworkModelMapper
{
    public static CharacterModel ToModel(this CharacterDto dto)
    {
        return new CharacterModel
        {
            CharacterId = dto.CharacterId,
            OwnerAccountId = dto.OwnerAccountId,
            WorldServerId = dto.WorldServerId,
            Name = dto.Name,
            Appearance = new CharacterAppearanceModel
            {
                ModelId = dto.Appearance.ModelId ?? 0,
                Gender = dto.Appearance.Gender ?? 0,
                HairColor = dto.Appearance.HairColor ?? 0,
                EyeColor = dto.Appearance.EyeColor ?? 0,
                FaceId = dto.Appearance.FaceId ?? 0
            },
            CreatedUnixMs = ToUnixMs(dto.CreatedUtc)
        };
    }

    public static CharacterBaseStatsModel ToModel(this CharacterBaseStatsDto dto)
    {
        return new CharacterBaseStatsModel
        {
            CharacterId = dto.CharacterId,
            RealmTemplateId = dto.RealmTemplateId ?? 0,
            Cultivation = dto.Cultivation ?? 0,
            BaseHp = dto.BaseHp ?? 0,
            BaseMp = dto.BaseMp ?? 0,
            BasePhysique = dto.BasePhysique ?? 0,
            BaseAttack = dto.BaseAttack ?? 0,
            BaseSpeed = dto.BaseSpeed ?? 0,
            BaseSpiritualSense = dto.BaseSpiritualSense ?? 0,
            BaseStamina = dto.BaseStamina ?? 0,
            LifespanBonus = dto.LifespanBonus ?? 0,
            BaseFortune = dto.BaseFortune ?? 0,
            BasePotential = dto.BasePotential ?? 0,
            UnallocatedPotential = dto.UnallocatedPotential ?? 0
        };
    }

    public static CharacterCurrentStateModel ToModel(this CharacterCurrentStateDto dto, GameTimeSnapshot gameTime)
    {
        return new CharacterCurrentStateModel
        {
            CharacterId = dto.CharacterId,
            CurrentHp = dto.CurrentHp,
            CurrentMp = dto.CurrentMp,
            CurrentStamina = dto.CurrentStamina,
            RemainingLifespan = CharacterLifespanRules.CalculateRemainingLifespanYears(dto.LifespanEndGameMinute, gameTime),
            CurrentMapId = dto.CurrentMapId,
            CurrentZoneIndex = dto.CurrentZoneIndex,
            CurrentPosX = dto.CurrentPosX,
            CurrentPosY = dto.CurrentPosY,
            IsDead = dto.IsDead,
            CurrentState = dto.CurrentState,
            CultivationStartedUnixMs = ToUnixMs(dto.CultivationStartedAtUtc),
            LastCultivationRewardedUnixMs = ToUnixMs(dto.LastCultivationRewardedAtUtc),
            LastSavedUnixMs = ToUnixMs(dto.LastSavedAt) ?? 0
        };
    }

    public static MapDefinitionModel ToModel(this MapDefinition definition)
    {
        return new MapDefinitionModel
        {
            MapId = definition.MapId,
            Name = definition.Name,
            MapType = (int)definition.Type,
            ClientMapKey = definition.ClientMapKey,
            AdjacentMapIds = definition.AdjacentMapIds.ToList(),
            Width = definition.Width,
            Height = definition.Height,
            CellSize = definition.CellSize,
            InterestRadius = definition.InterestRadius,
            DefaultSpawnX = definition.DefaultSpawnPosition.X,
            DefaultSpawnY = definition.DefaultSpawnPosition.Y,
            MaxPublicZoneCount = definition.MaxPublicZoneCount,
            MaxPlayersPerZone = definition.MaxPlayersPerZone,
            IsPrivatePerPlayer = definition.IsPrivatePerPlayer
        };
    }

    public static ObservedCharacterModel ToObservedCharacterModel(this PlayerSession player, GameTimeSnapshot gameTime)
    {
        var snapshot = player.RuntimeState.CaptureSnapshot();
        return new ObservedCharacterModel
        {
            Character = player.CharacterData.ToModel(),
            CurrentState = snapshot.CurrentState.ToModel(gameTime),
            MapId = player.MapId,
            ZoneIndex = player.ZoneIndex
        };
    }

    private static long? ToUnixMs(DateTime? dateTime)
    {
        if (!dateTime.HasValue)
            return null;

        var value = dateTime.Value;
        var utc = value.Kind == DateTimeKind.Utc
            ? value
            : DateTime.SpecifyKind(value, DateTimeKind.Utc);

        return new DateTimeOffset(utc).ToUnixTimeMilliseconds();
    }
}
