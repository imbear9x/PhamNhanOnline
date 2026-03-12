using GameShared.Models;

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

    public static CharacterStatsModel ToModel(this CharacterStatsDto dto)
    {
        return new CharacterStatsModel
        {
            CharacterId = dto.CharacterId,
            RealmTemplateId = dto.RealmTemplateId ?? 0,
            Cultivation = dto.Cultivation ?? 0,
            Health = dto.Health ?? 0,
            Mana = dto.Mana ?? 0,
            Physique = dto.Physique ?? 0,
            Attack = dto.Attack ?? 0,
            Speed = dto.Speed ?? 0,
            SpiritualSense = dto.SpiritualSense ?? 0,
            Fortune = dto.Fortune ?? 0,
            Potential = dto.Potential ?? 0
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
