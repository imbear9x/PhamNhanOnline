using LinqToDB.Mapping;

namespace GameServer.Entities;

[Table("player_items")]
public sealed class PlayerItemEntity
{
    [Column("id"), PrimaryKey, Identity] public long Id { get; set; }
    [Column("player_id"), NotNull] public Guid PlayerId { get; set; }
    [Column("item_template_id"), NotNull] public int ItemTemplateId { get; set; }
    [Column("quantity"), NotNull] public int Quantity { get; set; }
    [Column("is_bound"), NotNull] public bool IsBound { get; set; }
    [Column("acquired_at"), NotNull] public DateTime AcquiredAt { get; set; }
    [Column("expire_at")] public DateTime? ExpireAt { get; set; }
    [Column("updated_at"), NotNull] public DateTime UpdatedAt { get; set; }
}
