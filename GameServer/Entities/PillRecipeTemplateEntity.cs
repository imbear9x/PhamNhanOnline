using LinqToDB.Mapping;

namespace GameServer.Entities;

[Table("pill_recipe_templates")]
public sealed class PillRecipeTemplateEntity
{
    [Column("id"), PrimaryKey] public int Id { get; set; }
    [Column("code"), NotNull] public string Code { get; set; } = string.Empty;
    [Column("name"), NotNull] public string Name { get; set; } = string.Empty;
    [Column("recipe_book_item_template_id"), NotNull] public int RecipeBookItemTemplateId { get; set; }
    [Column("result_pill_item_template_id"), NotNull] public int ResultPillItemTemplateId { get; set; }
    [Column("description")] public string? Description { get; set; }
    [Column("craft_duration_seconds"), NotNull] public long CraftDurationSeconds { get; set; }
    [Column("base_success_rate"), NotNull] public double BaseSuccessRate { get; set; }
    [Column("success_rate_cap")] public double? SuccessRateCap { get; set; }
    [Column("mutation_rate"), NotNull] public double MutationRate { get; set; }
    [Column("mutation_rate_cap"), NotNull] public double MutationRateCap { get; set; }
    [Column("created_at"), NotNull] public DateTime CreatedAt { get; set; }
}
