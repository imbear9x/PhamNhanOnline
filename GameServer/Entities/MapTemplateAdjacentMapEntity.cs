using LinqToDB.Mapping;

namespace GameServer.Entities;

[Table("map_template_adjacent_maps")]
public sealed class MapTemplateAdjacentMapEntity
{
    [Column("map_template_id"          , IsPrimaryKey = true)] public int MapTemplateId         { get; set; }
    [Column("adjacent_map_template_id" , IsPrimaryKey = true)] public int AdjacentMapTemplateId { get; set; }
}
