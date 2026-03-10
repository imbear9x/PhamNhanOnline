// NOTE: Mirror transport integration will be implemented later.
// This file is guarded to avoid build breaks when Mirror runtime APIs are unavailable.
#if MIRROR
using GameShared.Models;
using Mirror;

namespace GameShared.Messages;

public struct CharacterListRequest : NetworkMessage
{
    public Guid AccountId;
}

public struct CharacterListResponse : NetworkMessage
{
    public bool Success;
    public string Error;
    public CharacterModel[] Characters;
}

public struct CharacterCreateRequest : NetworkMessage
{
    public Guid AccountId;
    public string Name;
    public int ServerId;
    public int ModelId;
}

public struct CharacterCreateResponse : NetworkMessage
{
    public bool Success;
    public string Error;
    public CharacterModel Character;
    public CharacterStatsModel Stats;
}

public struct CharacterLoadRequest : NetworkMessage
{
    public Guid CharacterId;
}

public struct CharacterLoadResponse : NetworkMessage
{
    public bool Success;
    public string Error;
    public CharacterModel Character;
    public CharacterStatsModel Stats;
    public bool HasStats;
}

public struct CharacterUpdateRequest : NetworkMessage
{
    public Guid CharacterId;
    public string Name;
    public int ModelId;
    public int Gender;
    public int HairColor;
    public int EyeColor;
    public int FaceId;
}

public struct CharacterUpdateResponse : NetworkMessage
{
    public bool Success;
    public string Error;
    public CharacterModel Character;
}

#endif
