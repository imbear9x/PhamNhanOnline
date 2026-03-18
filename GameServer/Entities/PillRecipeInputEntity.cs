using LinqToDB.Mapping;

namespace GameServer.Entities;

[Table("pill_recipe_inputs")]
public sealed class PillRecipeInputEntity
{
    [Column("id"), PrimaryKey, Identity] public int Id { get; set; }
    [Column("pill_recipe_template_id"), NotNull] public int PillRecipeTemplateId { get; set; }
    [Column("required_item_template_id"), NotNull] public int RequiredItemTemplateId { get; set; }
    [Column("required_quantity"), NotNull] public int RequiredQuantity { get; set; }
    [Column("consume_mode"), NotNull] public int ConsumeMode { get; set; }
    [Column("is_optional"), NotNull] public bool IsOptional { get; set; }
    [Column("success_rate_bonus"), NotNull] public double SuccessRateBonus { get; set; }
    [Column("mutation_bonus_rate"), NotNull] public double MutationBonusRate { get; set; }
    [Column("required_herb_maturity"), NotNull] public int RequiredHerbMaturity { get; set; }
}

