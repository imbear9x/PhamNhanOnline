using System.Numerics;
using GameServer.Runtime;

namespace GameServer.World;

public sealed class GroundRewardEntity
{
    public int Id { get; }
    public Guid? OwnerCharacterId { get; private set; }
    public Vector2 Position { get; }
    public IReadOnlyList<GroundRewardItem> Items { get; }
    public DateTime CreatedAtUtc { get; }
    public DateTime? FreeAtUtc { get; }
    public DateTime DestroyAtUtc { get; }
    public bool IsDestroyed { get; private set; }

    public GroundRewardEntity(
        int id,
        Guid? ownerCharacterId,
        Vector2 position,
        IReadOnlyList<GroundRewardItem> items,
        DateTime createdAtUtc,
        DateTime? freeAtUtc,
        DateTime destroyAtUtc)
    {
        Id = id;
        OwnerCharacterId = ownerCharacterId;
        Position = position;
        Items = items;
        CreatedAtUtc = createdAtUtc;
        FreeAtUtc = freeAtUtc;
        DestroyAtUtc = destroyAtUtc;
    }

    public void Update(DateTime utcNow)
    {
        if (IsDestroyed)
            return;

        if (OwnerCharacterId.HasValue && FreeAtUtc.HasValue && utcNow >= FreeAtUtc.Value)
            OwnerCharacterId = null;

        if (utcNow >= DestroyAtUtc)
            IsDestroyed = true;
    }
}
