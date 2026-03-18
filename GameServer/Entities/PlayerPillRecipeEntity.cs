using LinqToDB.Mapping;

namespace GameServer.Entities;

[Table("player_pill_recipes")]
public sealed class PlayerPillRecipeEntity
{
    [Column("id"), PrimaryKey, Identity] public long Id { get; set; }
    [Column("player_id"), NotNull] public Guid PlayerId { get; set; }
    [Column("pill_recipe_template_id"), NotNull] public int PillRecipeTemplateId { get; set; }
    [Column("learned_at"), NotNull] public DateTime LearnedAt { get; set; }
    [Column("total_craft_count"), NotNull] public int TotalCraftCount { get; set; }
    [Column("current_success_rate_bonus"), NotNull] public double CurrentSuccessRateBonus { get; set; }
    [Column("updated_at"), NotNull] public DateTime UpdatedAt { get; set; }
}

