using LinqToDB.Mapping;

namespace GameServer.Entities;

[Table("equipment_templates")]
public sealed class EquipmentTemplateEntity
{
    [Column("item_template_id"), PrimaryKey] public int ItemTemplateId { get; set; }
    [Column("slot_type"), NotNull] public int SlotType { get; set; }
    [Column("equipment_type"), NotNull] public int EquipmentType { get; set; }
    [Column("level_requirement"), NotNull] public int LevelRequirement { get; set; }
}
