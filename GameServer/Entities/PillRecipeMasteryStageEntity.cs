using LinqToDB.Mapping;

namespace GameServer.Entities;

[Table("pill_recipe_mastery_stages")]
public sealed class PillRecipeMasteryStageEntity
{
    [Column("id"), PrimaryKey, Identity] public int Id { get; set; }
    [Column("pill_recipe_template_id"), NotNull] public int PillRecipeTemplateId { get; set; }
    [Column("required_total_craft_count"), NotNull] public int RequiredTotalCraftCount { get; set; }
    [Column("success_rate_bonus"), NotNull] public double SuccessRateBonus { get; set; }
}
