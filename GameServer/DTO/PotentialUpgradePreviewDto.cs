using GameShared.Models;

namespace GameServer.DTO;

public sealed record PotentialUpgradePreviewDto(
    PotentialAllocationTarget Target,
    int NextUpgradeCount,
    int TierIndex,
    int PotentialCost,
    decimal StatGain,
    bool IsAvailable,
    bool CanUpgrade);
