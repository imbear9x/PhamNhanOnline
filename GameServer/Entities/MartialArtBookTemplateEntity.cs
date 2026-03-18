using LinqToDB.Mapping;

namespace GameServer.Entities;

[Table("martial_art_book_templates")]
public sealed class MartialArtBookTemplateEntity
{
    [Column("item_template_id"), PrimaryKey] public int ItemTemplateId { get; set; }
    [Column("martial_art_id"), NotNull] public int MartialArtId { get; set; }
}
