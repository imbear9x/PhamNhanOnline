using LinqToDB.Mapping;

namespace GameServer.Entities;

[Table("player_equipment_stat_bonuses")]
public sealed class PlayerEquipmentStatBonusEntity
{
    [Column("id"), PrimaryKey, Identity] public long Id { get; set; }
    [Column("player_item_id"), NotNull] public long PlayerItemId { get; set; }
    [Column("stat_type"), NotNull] public int StatType { get; set; }
    [Column("value"), NotNull] public decimal Value { get; set; }
    [Column("value_type"), NotNull] public int ValueType { get; set; }
    [Column("source_type"), NotNull] public int SourceType { get; set; }
    [Column("created_at"), NotNull] public DateTime CreatedAt { get; set; }
}
