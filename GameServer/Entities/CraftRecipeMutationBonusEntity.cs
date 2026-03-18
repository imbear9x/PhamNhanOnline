using LinqToDB.Mapping;

namespace GameServer.Entities;

[Table("craft_recipe_mutation_bonuses")]
public sealed class CraftRecipeMutationBonusEntity
{
    [Column("id"), PrimaryKey, Identity] public int Id { get; set; }
    [Column("craft_recipe_id"), NotNull] public int CraftRecipeId { get; set; }
    [Column("stat_type"), NotNull] public int StatType { get; set; }
    [Column("value"), NotNull] public decimal Value { get; set; }
    [Column("value_type"), NotNull] public int ValueType { get; set; }
}
