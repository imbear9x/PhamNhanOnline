using LinqToDB.Mapping;
using System;

namespace GameServer.Entities;

[Table("spiritual_energy_templates")]
public sealed class SpiritualEnergyTemplateEntity
{
    [Column("id"           , IsPrimaryKey = true)] public int       Id          { get; set; }
    [Column("code"                              )] public string    Code        { get; set; } = string.Empty;
    [Column("name"                              )] public string    Name        { get; set; } = string.Empty;
    [Column("lk_per_minute"                     )] public decimal   LkPerMinute { get; set; }
    [Column("created_at"                        )] public DateTime? CreatedAt   { get; set; }
}
