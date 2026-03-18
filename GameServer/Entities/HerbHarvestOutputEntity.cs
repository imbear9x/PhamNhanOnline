using LinqToDB.Mapping;

namespace GameServer.Entities;

[Table("herb_harvest_outputs")]
public sealed class HerbHarvestOutputEntity
{
    [Column("id"), PrimaryKey, Identity] public int Id { get; set; }
    [Column("herb_template_id"), NotNull] public int HerbTemplateId { get; set; }
    [Column("required_stage"), NotNull] public int RequiredStage { get; set; }
    [Column("output_type"), NotNull] public int OutputType { get; set; }
    [Column("result_item_template_id"), NotNull] public int ResultItemTemplateId { get; set; }
    [Column("result_quantity"), NotNull] public int ResultQuantity { get; set; }
    [Column("output_chance"), NotNull] public double OutputChance { get; set; }
}

