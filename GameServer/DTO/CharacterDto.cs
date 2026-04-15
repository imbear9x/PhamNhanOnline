using GameServer.Entities;

namespace GameServer.DTO;

public sealed record CharacterDto(
    Guid CharacterId,
    Guid OwnerAccountId,
    int WorldServerId,
    string Name,
    CharacterAppearanceDto Appearance,
    DateTime? FirstEnterWorldAtUtc,
    DateTime? CreatedUtc)
{
    public static CharacterDto FromEntity(Character entity) =>
        new(
            entity.Id,
            entity.AccountId,
            entity.ServerId,
            entity.Name,
            new CharacterAppearanceDto(entity.ModelId, entity.Gender, entity.HairColor, entity.EyeColor, entity.FaceId),
            entity.FirstEnterWorldAtUtc,
            entity.CreatedAt);
}

public sealed record CharacterAppearanceDto(
    int? ModelId,
    int? Gender,
    int? HairColor,
    int? EyeColor,
    int? FaceId);

