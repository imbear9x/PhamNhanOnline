using LinqToDB.Mapping;

namespace GameServer.Entities;

[Table("item_templates")]
public sealed class ItemTemplateEntity
{
    [Column("id"), PrimaryKey] public int Id { get; set; }
    [Column("code"), NotNull] public string Code { get; set; } = string.Empty;
    [Column("name"), NotNull] public string Name { get; set; } = string.Empty;
    [Column("item_type"), NotNull] public int ItemType { get; set; }
    [Column("rarity"), NotNull] public int Rarity { get; set; }
    [Column("max_stack"), NotNull] public int MaxStack { get; set; }
    [Column("is_tradeable"), NotNull] public bool IsTradeable { get; set; }
    [Column("is_droppable"), NotNull] public bool IsDroppable { get; set; }
    [Column("is_destroyable"), NotNull] public bool IsDestroyable { get; set; }
    [Column("icon")] public string? Icon { get; set; }
    [Column("background_icon")] public string? BackgroundIcon { get; set; }
    [Column("description")] public string? Description { get; set; }
    [Column("description_template")] public string? DescriptionTemplate { get; set; }
    [Column("created_at")] public DateTime? CreatedAt { get; set; }
}
