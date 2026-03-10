namespace GameShared.Models;

public struct CharacterAppearanceModel
{
    public int ModelId;
    public int Gender;
    public int HairColor;
    public int EyeColor;
    public int FaceId;
}

public struct CharacterModel
{
    public Guid CharacterId;
    public Guid OwnerAccountId;
    public int WorldServerId;
    public string Name;
    public CharacterAppearanceModel Appearance;
    public long? CreatedUnixMs;
}

