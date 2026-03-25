using GameShared.Messages;

namespace GameServer.DTO;

public sealed record CultivationPreviewDto(
    int ActiveMartialArtId,
    decimal QiAbsorptionRate,
    decimal SpiritualEnergyPerMinute,
    decimal RealmAbsorptionMultiplier,
    decimal EstimatedCultivationPerMinute,
    decimal EstimatedPotentialPerMinute,
    MessageCode BlockedReason);
