using LinqToDB.Mapping;

namespace GameServer.Entities;

[Table("craft_recipes")]
public sealed class CraftRecipeEntity
{
    [Column("id"), PrimaryKey] public int Id { get; set; }
    [Column("code"), NotNull] public string Code { get; set; } = string.Empty;
    [Column("name"), NotNull] public string Name { get; set; } = string.Empty;
    [Column("result_item_template_id"), NotNull] public int ResultItemTemplateId { get; set; }
    [Column("result_quantity"), NotNull] public int ResultQuantity { get; set; }
    [Column("success_rate"), NotNull] public double SuccessRate { get; set; }
    [Column("mutation_rate"), NotNull] public double MutationRate { get; set; }
    [Column("mutation_rate_cap"), NotNull] public double MutationRateCap { get; set; }
    [Column("cost_currency_type")] public int? CostCurrencyType { get; set; }
    [Column("cost_currency_value"), NotNull] public long CostCurrencyValue { get; set; }
    [Column("description")] public string? Description { get; set; }
    [Column("created_at"), NotNull] public DateTime CreatedAt { get; set; }
}
