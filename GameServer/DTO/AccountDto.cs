using GameServer.Entities;

namespace GameServer.DTO;

public sealed record AccountDto(
    Guid AccountId,
    DateTime? CreatedUtc,
    DateTime? LastLoginUtc,
    int? StatusCode)
{
    public static AccountDto FromEntity(Account entity) =>
        new(entity.Id, entity.CreatedAt, entity.LastLogin, entity.Status);
}

