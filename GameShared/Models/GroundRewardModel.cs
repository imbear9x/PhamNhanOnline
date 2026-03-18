using GameShared.Attributes;

namespace GameShared.Models;

[PacketModel]
public struct GroundRewardModel
{
    public int RewardId;
    public System.Guid? OwnerCharacterId;
    public float PosX;
    public float PosY;
    public long CreatedUnixMs;
    public long? FreeAtUnixMs;
    public long DestroyAtUnixMs;
    public System.Collections.Generic.List<GroundRewardItemModel>? Items;
}
