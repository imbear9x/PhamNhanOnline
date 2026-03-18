using LinqToDB.Mapping;

namespace GameServer.Entities;

[Table("map_enemy_spawn_entries")]
public sealed class MapEnemySpawnEntryEntity
{
    [Column("id"), PrimaryKey, Identity] public int Id { get; set; }
    [Column("spawn_group_id"), NotNull] public int SpawnGroupId { get; set; }
    [Column("enemy_template_id"), NotNull] public int EnemyTemplateId { get; set; }
    [Column("weight"), NotNull] public int Weight { get; set; }
    [Column("order_index"), NotNull] public int OrderIndex { get; set; }
}
