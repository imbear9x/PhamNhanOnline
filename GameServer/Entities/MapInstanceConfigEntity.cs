using LinqToDB.Mapping;

namespace GameServer.Entities;

[Table("map_instance_configs")]
public sealed class MapInstanceConfigEntity
{
    [Column("id"), PrimaryKey] public int Id { get; set; }
    [Column("code"), NotNull] public string Code { get; set; } = string.Empty;
    [Column("name"), NotNull] public string Name { get; set; } = string.Empty;
    [Column("map_template_id"), NotNull] public int MapTemplateId { get; set; }
    [Column("instance_mode"), NotNull] public int InstanceMode { get; set; }
    [Column("duration_seconds")] public int? DurationSeconds { get; set; }
    [Column("idle_destroy_seconds")] public int? IdleDestroySeconds { get; set; }
    [Column("completion_rule"), NotNull] public int CompletionRule { get; set; }
    [Column("complete_destroy_delay_seconds")] public int? CompleteDestroyDelaySeconds { get; set; }
    [Column("description")] public string? Description { get; set; }
    [Column("created_at"), NotNull] public DateTime CreatedAt { get; set; }
}
