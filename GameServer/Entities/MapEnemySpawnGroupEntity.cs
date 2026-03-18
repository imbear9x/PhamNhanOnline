using LinqToDB.Mapping;

namespace GameServer.Entities;

[Table("map_enemy_spawn_groups")]
public sealed class MapEnemySpawnGroupEntity
{
    [Column("id"), PrimaryKey] public int Id { get; set; }
    [Column("code"), NotNull] public string Code { get; set; } = string.Empty;
    [Column("name"), NotNull] public string Name { get; set; } = string.Empty;
    [Column("map_template_id"), NotNull] public int MapTemplateId { get; set; }
    [Column("runtime_scope"), NotNull] public int RuntimeScope { get; set; }
    [Column("zone_index")] public int? ZoneIndex { get; set; }
    [Column("spawn_mode"), NotNull] public int SpawnMode { get; set; }
    [Column("is_boss_spawn"), NotNull] public bool IsBossSpawn { get; set; }
    [Column("max_alive"), NotNull] public int MaxAlive { get; set; }
    [Column("respawn_seconds"), NotNull] public int RespawnSeconds { get; set; }
    [Column("initial_spawn_delay_seconds"), NotNull] public int InitialSpawnDelaySeconds { get; set; }
    [Column("center_x"), NotNull] public float CenterX { get; set; }
    [Column("center_y"), NotNull] public float CenterY { get; set; }
    [Column("spawn_radius"), NotNull] public float SpawnRadius { get; set; }
    [Column("description")] public string? Description { get; set; }
    [Column("created_at"), NotNull] public DateTime CreatedAt { get; set; }
}
