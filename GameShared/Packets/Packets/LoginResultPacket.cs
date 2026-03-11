using GameShared.Attributes;

namespace GameShared.Packets;

[Packet]
public partial class LoginResultPacket : IPacket
{

    public bool? Success { get; set; }
    public string? Error { get; set; }
    public Guid? AccountId { get; set; }
    
}

