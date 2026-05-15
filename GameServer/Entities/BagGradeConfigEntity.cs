using LinqToDB.Mapping;

namespace GameServer.Entities;

[Table("bag_grade_configs")]
public sealed class BagGradeConfigEntity
{
    [Column("grade"), PrimaryKey, NotNull] public int Grade { get; set; }
    [Column("slot_count"), NotNull] public int SlotCount { get; set; }
    [Column("upgrade_cost_linh_thach"), NotNull] public long UpgradeCostLinhThach { get; set; }
    [Column("display_name"), NotNull] public string DisplayName { get; set; } = string.Empty;
}