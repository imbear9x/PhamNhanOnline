using System.ComponentModel.DataAnnotations;
using GameShared.Attributes;
using GameShared.Messages;
using GameShared.Models;

namespace GameShared.Packets;

[Packet]
[RequireAuth]
public partial class CreateCharacterPacket : IPacket
{
    [ValidationCode(MessageCode.CharacterNameInvalid)]
    [Required]
    [StringLength(20, MinimumLength = 3)]
    public string? Name { get; set; }

    [ValidationCode(MessageCode.CharacterServerInvalid)]
    [Required]
    [Range(1, int.MaxValue)]
    public int? ServerId { get; set; }

    [ValidationCode(MessageCode.CharacterModelInvalid)]
    [Required]
    [Range(1, int.MaxValue)]
    public int? ModelId { get; set; }
}

[Packet]
public partial class CreateCharacterResultPacket : IPacket
{
    public bool? Success { get; set; }
    public MessageCode? Code { get; set; }
    public CharacterModel? Character { get; set; }
    public CharacterBaseStatsModel? BaseStats { get; set; }
    public CharacterCurrentStateModel? CurrentState { get; set; }
}

[Packet]
[RequireAuth]
public partial class GetCharacterListPacket : IPacket
{
}

[Packet]
public partial class GetCharacterListResultPacket : IPacket
{
    public bool? Success { get; set; }
    public MessageCode? Code { get; set; }
    public List<CharacterModel>? Characters { get; set; }
}

[Packet]
[RequireAuth]
public partial class GetCharacterDataPacket : IPacket
{
    [ValidationCode(MessageCode.CharacterIdInvalid)]
    [Required]
    public Guid? CharacterId { get; set; }
}

[Packet]
public partial class GetCharacterDataResultPacket : IPacket
{
    public bool? Success { get; set; }
    public MessageCode? Code { get; set; }
    public CharacterModel? Character { get; set; }
    public CharacterBaseStatsModel? BaseStats { get; set; }
    public CharacterCurrentStateModel? CurrentState { get; set; }
}

[Packet]
public partial class CharacterBaseStatsChangedPacket : IPacket
{
    public CharacterBaseStatsModel? BaseStats { get; set; }
}

[Packet]
public partial class CharacterCurrentStateChangedPacket : IPacket
{
    public CharacterCurrentStateModel? CurrentState { get; set; }
}
