using LinqToDB.Mapping;

namespace GameServer.Entities;

[Table("martial_arts")]
public sealed class MartialArtEntity
{
    [Column("id"), PrimaryKey] public int Id { get; set; }
    [Column("code"), NotNull] public string Code { get; set; } = string.Empty;
    [Column("name"), NotNull] public string Name { get; set; } = string.Empty;
    [Column("icon")] public string? Icon { get; set; }
    [Column("quality"), NotNull] public int Quality { get; set; }
    [Column("category")] public string? Category { get; set; }
    [Column("description")] public string? Description { get; set; }
    [Column("qi_absorption_rate"), NotNull] public decimal QiAbsorptionRate { get; set; }
    [Column("max_stage"), NotNull] public int MaxStage { get; set; }
    [Column("created_at")] public DateTime? CreatedAt { get; set; }
}
