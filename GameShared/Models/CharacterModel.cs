using GameShared.Attributes;

namespace GameShared.Models;

[PacketModel]
public struct CharacterAppearanceModel
{
    public int ModelId;
    public int Gender;
    public int HairColor;
    public int EyeColor;
    public int FaceId;
}

[PacketModel]
public struct CharacterModel
{
    public Guid CharacterId;
    public Guid OwnerAccountId;
    public int WorldServerId;
    public string Name;
    public CharacterAppearanceModel Appearance;
    public long? CreatedUnixMs;
}
