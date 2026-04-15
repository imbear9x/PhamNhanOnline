using GameShared.Attributes;

namespace GameShared.Models;

[PacketModel]
public struct CharacterCurrentStateModel
{
    public Guid CharacterId;
    public int CurrentHp;
    public int CurrentMp;
    public int CurrentStamina;
    public long? LifespanEndUnixMs;
    public int? CurrentMapId;
    public int CurrentZoneIndex;
    public float CurrentPosX;
    public float CurrentPosY;
    public bool IsExpired;
    public int CurrentState;
    public long? CultivationStartedUnixMs;
    public long? LastCultivationRewardedUnixMs;
    public long LastSavedUnixMs;
}
