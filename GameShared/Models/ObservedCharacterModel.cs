using GameShared.Attributes;

namespace GameShared.Models;

[PacketModel]
public struct ObservedCharacterModel
{
    public CharacterModel Character;
    public CharacterCurrentStateModel CurrentState;
    public int MapId;
    public int InstanceId;
}
