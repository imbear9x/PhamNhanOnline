using LinqToDB.Mapping;

namespace GameServer.Entities;

[Table("herb_templates")]
public sealed class HerbTemplateEntity
{
    [Column("id"), PrimaryKey] public int Id { get; set; }
    [Column("code"), NotNull] public string Code { get; set; } = string.Empty;
    [Column("name"), NotNull] public string Name { get; set; } = string.Empty;
    [Column("seed_item_template_id"), NotNull] public int SeedItemTemplateId { get; set; }
    [Column("replant_item_template_id")] public int? ReplantItemTemplateId { get; set; }
    [Column("description")] public string? Description { get; set; }
    [Column("created_at"), NotNull] public DateTime CreatedAt { get; set; }
}
