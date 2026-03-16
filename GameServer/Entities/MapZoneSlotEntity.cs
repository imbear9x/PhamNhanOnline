using LinqToDB.Mapping;

namespace GameServer.Entities;

[Table("map_zone_slots")]
public sealed class MapZoneSlotEntity
{
    [Column("id", IsPrimaryKey = true)] public int Id { get; set; }
    [Column("map_template_id")] public int MapTemplateId { get; set; }
    [Column("zone_index")] public int ZoneIndex { get; set; }
    [Column("spiritual_energy_template_id")] public int SpiritualEnergyTemplateId { get; set; }
    [Column("created_at")] public DateTime? CreatedAt { get; set; }
}
