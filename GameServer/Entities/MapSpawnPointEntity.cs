using LinqToDB.Mapping;

namespace GameServer.Entities;

[Table("map_spawn_points")]
public sealed class MapSpawnPointEntity
{
    [Column("id"), PrimaryKey, Identity] public int Id { get; set; }
    [Column("map_template_id"), NotNull] public int MapTemplateId { get; set; }
    [Column("code"), NotNull] public string Code { get; set; } = string.Empty;
    [Column("name"), NotNull] public string Name { get; set; } = string.Empty;
    [Column("spawn_category"), NotNull] public int SpawnCategory { get; set; }
    [Column("pos_x"), NotNull] public float PosX { get; set; }
    [Column("pos_y"), NotNull] public float PosY { get; set; }
    [Column("facing_degrees")] public float? FacingDegrees { get; set; }
    [Column("description")] public string? Description { get; set; }
    [Column("created_at"), NotNull] public DateTime CreatedAt { get; set; }
}
