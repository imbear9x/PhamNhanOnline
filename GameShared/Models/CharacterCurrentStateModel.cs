using GameShared.Attributes;

namespace GameShared.Models;

[PacketModel]
public struct CharacterCurrentStateModel
{
    public Guid CharacterId;
    public int CurrentHp;
    public int CurrentMp;
    public int CurrentStamina;
    public int RemainingLifespan;
    public int? CurrentMapId;
    public int CurrentZoneIndex;
    public float CurrentPosX;
    public float CurrentPosY;
    public bool IsDead;
    public int CurrentState;
    public long LastSavedUnixMs;
}
