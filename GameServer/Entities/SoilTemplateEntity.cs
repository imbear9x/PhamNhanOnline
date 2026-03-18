using LinqToDB.Mapping;

namespace GameServer.Entities;

[Table("soil_templates")]
public sealed class SoilTemplateEntity
{
    [Column("item_template_id"), PrimaryKey] public int ItemTemplateId { get; set; }
    [Column("growth_speed_rate"), NotNull] public decimal GrowthSpeedRate { get; set; }
    [Column("max_active_seconds"), NotNull] public long MaxActiveSeconds { get; set; }
    [Column("description")] public string? Description { get; set; }
}

