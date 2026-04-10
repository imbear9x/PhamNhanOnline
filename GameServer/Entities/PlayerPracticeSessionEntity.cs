using LinqToDB.Mapping;

namespace GameServer.Entities;

[Table("player_practice_sessions")]
public sealed class PlayerPracticeSessionEntity
{
    [Column("id"), PrimaryKey, Identity] public long Id { get; set; }
    [Column("player_id"), NotNull] public Guid PlayerId { get; set; }
    [Column("practice_type"), NotNull] public int PracticeType { get; set; }
    [Column("practice_state"), NotNull] public int PracticeState { get; set; }
    [Column("definition_id"), NotNull] public int DefinitionId { get; set; }
    [Column("current_map_id"), NotNull] public int CurrentMapId { get; set; }
    [Column("title"), NotNull] public string Title { get; set; } = string.Empty;
    [Column("total_duration_seconds"), NotNull] public long TotalDurationSeconds { get; set; }
    [Column("accumulated_active_seconds"), NotNull] public long AccumulatedActiveSeconds { get; set; }
    [Column("cancel_locked_progress"), NotNull] public double CancelLockedProgress { get; set; }
    [Column("request_payload_json")] public string? RequestPayloadJson { get; set; }
    [Column("result_payload_json")] public string? ResultPayloadJson { get; set; }
    [Column("started_at_utc"), NotNull] public DateTime StartedAtUtc { get; set; }
    [Column("last_resumed_at_utc")] public DateTime? LastResumedAtUtc { get; set; }
    [Column("paused_at_utc")] public DateTime? PausedAtUtc { get; set; }
    [Column("completed_at_utc")] public DateTime? CompletedAtUtc { get; set; }
    [Column("result_acknowledged_at_utc")] public DateTime? ResultAcknowledgedAtUtc { get; set; }
    [Column("updated_at_utc"), NotNull] public DateTime UpdatedAtUtc { get; set; }
    [Column("created_at_utc"), NotNull] public DateTime CreatedAtUtc { get; set; }
}
