using GameShared.Attributes;

namespace GameShared.Models;

[PacketModel]
public struct GardenPlotStateModel
{
    public int PlotIndex;
    public long? SoilPlayerItemId;
    public int? SoilState;
    public long SoilRemainingSeconds;
    public long? PlayerHerbId;
    public int? HerbTemplateId;
    public int? HerbState;
    public int? HerbStage;
    public long NextStageRemainingSeconds;
    public long? HerbExpireAtUnixMs;
}
