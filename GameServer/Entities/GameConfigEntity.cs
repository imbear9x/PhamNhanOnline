using LinqToDB.Mapping;

namespace GameServer.Entities;

[Table("game_configs")]
public sealed class GameConfigEntity
{
    [Column("config_key"), PrimaryKey, NotNull] public string ConfigKey { get; set; } = string.Empty;
    [Column("config_value"), NotNull] public string ConfigValue { get; set; } = string.Empty;
    [Column("description")] public string? Description { get; set; }
    [Column("created_at"), NotNull] public DateTime CreatedAt { get; set; }
    [Column("updated_at"), NotNull] public DateTime UpdatedAt { get; set; }
}
