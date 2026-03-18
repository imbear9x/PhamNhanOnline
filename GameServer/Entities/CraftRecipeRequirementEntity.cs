using LinqToDB.Mapping;

namespace GameServer.Entities;

[Table("craft_recipe_requirements")]
public sealed class CraftRecipeRequirementEntity
{
    [Column("id"), PrimaryKey, Identity] public int Id { get; set; }
    [Column("craft_recipe_id"), NotNull] public int CraftRecipeId { get; set; }
    [Column("required_item_template_id"), NotNull] public int RequiredItemTemplateId { get; set; }
    [Column("required_quantity"), NotNull] public int RequiredQuantity { get; set; }
    [Column("consume_mode"), NotNull] public int ConsumeMode { get; set; }
    [Column("is_optional"), NotNull] public bool IsOptional { get; set; }
    [Column("mutation_bonus_rate"), NotNull] public double MutationBonusRate { get; set; }
}
