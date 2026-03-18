using LinqToDB.Mapping;

namespace GameServer.Entities;

[Table("equipment_template_stats")]
public sealed class EquipmentTemplateStatEntity
{
    [Column("id"), PrimaryKey, Identity] public int Id { get; set; }
    [Column("equipment_template_id"), NotNull] public int EquipmentTemplateId { get; set; }
    [Column("stat_type"), NotNull] public int StatType { get; set; }
    [Column("value"), NotNull] public decimal Value { get; set; }
    [Column("value_type"), NotNull] public int ValueType { get; set; }
}
