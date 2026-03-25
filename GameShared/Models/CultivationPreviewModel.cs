using GameShared.Attributes;
using GameShared.Messages;

namespace GameShared.Models;

[PacketModel]
public struct CultivationPreviewModel
{
    public int ActiveMartialArtId;
    public double QiAbsorptionRate;
    public double SpiritualEnergyPerMinute;
    public double RealmAbsorptionMultiplier;
    public double EstimatedCultivationPerMinute;
    public double EstimatedPotentialPerMinute;
    public MessageCode BlockedReason;
}
