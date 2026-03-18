using LinqToDB.Mapping;

namespace GameServer.Entities;

[Table("player_caves")]
public sealed class PlayerCaveEntity
{
    [Column("id"), PrimaryKey, Identity] public long Id { get; set; }
    [Column("owner_character_id"), NotNull] public Guid OwnerCharacterId { get; set; }
    [Column("map_template_id"), NotNull] public int MapTemplateId { get; set; }
    [Column("zone_index"), NotNull] public int ZoneIndex { get; set; }
    [Column("is_home"), NotNull] public bool IsHome { get; set; }
    [Column("created_at"), NotNull] public DateTime CreatedAt { get; set; }
    [Column("updated_at"), NotNull] public DateTime UpdatedAt { get; set; }
}

