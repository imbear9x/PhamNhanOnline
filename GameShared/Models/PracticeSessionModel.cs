using GameShared.Attributes;

namespace GameShared.Models;

[PacketModel]
public struct PracticeSessionModel
{
    public long PracticeSessionId;
    public int PracticeType;
    public int PracticeState;
    public int DefinitionId;
    public int RequestedCraftCount;
    public int BoostedCraftCount;
    public List<AlchemyCraftRateSegmentModel>? SuccessRateSegments;
    public string? Title;
    public long TotalDurationSeconds;
    public long AccumulatedActiveSeconds;
    public long RemainingDurationSeconds;
    public double Progress;
    public double CancelRefundProgressThreshold;
    public bool CanPause;
    public bool CanCancel;
    public bool IsPaused;
    public long? StartedUnixMs;
    public long? LastResumedUnixMs;
    public long? PausedUnixMs;
    public long? CompletedUnixMs;
}
