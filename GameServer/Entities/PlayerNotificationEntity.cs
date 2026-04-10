using LinqToDB.Mapping;

namespace GameServer.Entities;

[Table("player_notifications")]
public sealed class PlayerNotificationEntity
{
    [Column("id"), PrimaryKey, Identity] public long Id { get; set; }
    [Column("player_id"), NotNull] public Guid PlayerId { get; set; }
    [Column("notification_type"), NotNull] public int NotificationType { get; set; }
    [Column("source_type"), NotNull] public int SourceType { get; set; }
    [Column("source_id")] public long? SourceId { get; set; }
    [Column("title"), NotNull] public string Title { get; set; } = string.Empty;
    [Column("message"), NotNull] public string Message { get; set; } = string.Empty;
    [Column("display_item_template_id")] public int? DisplayItemTemplateId { get; set; }
    [Column("payload_json")] public string? PayloadJson { get; set; }
    [Column("created_at_utc"), NotNull] public DateTime CreatedAtUtc { get; set; }
    [Column("read_at_utc")] public DateTime? ReadAtUtc { get; set; }
}
