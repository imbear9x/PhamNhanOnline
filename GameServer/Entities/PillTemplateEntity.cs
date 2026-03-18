using LinqToDB.Mapping;

namespace GameServer.Entities;

[Table("pill_templates")]
public sealed class PillTemplateEntity
{
    [Column("item_template_id"), PrimaryKey] public int ItemTemplateId { get; set; }
    [Column("pill_category"), NotNull] public int PillCategory { get; set; }
    [Column("usage_type"), NotNull] public int UsageType { get; set; }
    [Column("cooldown_ms")] public int? CooldownMs { get; set; }
}

