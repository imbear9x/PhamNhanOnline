using LinqToDB.Mapping;

namespace GameServer.Entities;

[Table("map_portals")]
public sealed class MapPortalEntity
{
    [Column("id"), PrimaryKey, Identity] public int Id { get; set; }
    [Column("source_map_template_id"), NotNull] public int SourceMapTemplateId { get; set; }
    [Column("code"), NotNull] public string Code { get; set; } = string.Empty;
    [Column("name"), NotNull] public string Name { get; set; } = string.Empty;
    [Column("source_x"), NotNull] public float SourceX { get; set; }
    [Column("source_y"), NotNull] public float SourceY { get; set; }
    [Column("interaction_radius"), NotNull] public float InteractionRadius { get; set; }
    [Column("interaction_mode"), NotNull] public int InteractionMode { get; set; }
    [Column("target_map_template_id"), NotNull] public int TargetMapTemplateId { get; set; }
    [Column("target_spawn_point_id"), NotNull] public int TargetSpawnPointId { get; set; }
    [Column("is_enabled"), NotNull] public bool IsEnabled { get; set; }
    [Column("order_index"), NotNull] public int OrderIndex { get; set; }
    [Column("description")] public string? Description { get; set; }
    [Column("created_at"), NotNull] public DateTime CreatedAt { get; set; }
}
