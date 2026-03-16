using LinqToDB.Mapping;
using System;

namespace GameServer.Entities;

[Table("map_templates")]
public sealed class MapTemplateEntity
{
    [Column("id"                 , IsPrimaryKey = true)] public int      Id                { get; set; }
    [Column("name"                                 )] public string   Name              { get; set; } = string.Empty;
    [Column("map_type"                             )] public int      MapType           { get; set; }
    [Column("client_map_key"                       )] public string   ClientMapKey      { get; set; } = string.Empty;
    [Column("width"                                )] public float    Width             { get; set; }
    [Column("height"                               )] public float    Height            { get; set; }
    [Column("cell_size"                            )] public float    CellSize          { get; set; }
    [Column("interest_radius"                      )] public float    InterestRadius    { get; set; }
    [Column("default_spawn_x"                      )] public float    DefaultSpawnX     { get; set; }
    [Column("default_spawn_y"                      )] public float    DefaultSpawnY     { get; set; }
    [Column("max_public_zone_count"                )] public int      MaxPublicZoneCount { get; set; }
    [Column("max_players_per_zone"                 )] public int      MaxPlayersPerZone { get; set; }
    [Column("supports_cave_placement"              )] public bool     SupportsCavePlacement { get; set; }
    [Column("is_private_per_player"                )] public bool     IsPrivatePerPlayer { get; set; }
    [Column("spiritual_energy"                     )] public decimal  SpiritualEnergy { get; set; }
    [Column("created_at"                           )] public DateTime? CreatedAt         { get; set; }
}
