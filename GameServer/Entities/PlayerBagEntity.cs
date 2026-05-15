using LinqToDB.Mapping;

namespace GameServer.Entities;

[Table("player_bags")]
public sealed class PlayerBagEntity
{
    [Column("player_id"), PrimaryKey, NotNull] public Guid PlayerId { get; set; }
    [Column("grade"), NotNull] public int Grade { get; set; }
    [Column("updated_at"), NotNull] public DateTime UpdatedAt { get; set; }
}