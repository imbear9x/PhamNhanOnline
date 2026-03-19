using GameShared.Attributes;

namespace GameShared.Models;

[PacketModel]
public struct ObservedCharacterModel
{
    public CharacterModel Character;
    public CharacterCurrentStateModel CurrentState;
    public int MaxHp;
    public int MaxMp;
    public int MapId;
    public int ZoneIndex;
}
