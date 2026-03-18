using LinqToDB.Mapping;

namespace GameServer.Entities;

[Table("player_equipments")]
public sealed class PlayerEquipmentEntity
{
    [Column("player_item_id"), PrimaryKey] public long PlayerItemId { get; set; }
    [Column("equipped_slot")] public int? EquippedSlot { get; set; }
    [Column("enhance_level"), NotNull] public int EnhanceLevel { get; set; }
    [Column("durability")] public int? Durability { get; set; }
    [Column("updated_at"), NotNull] public DateTime UpdatedAt { get; set; }
}
