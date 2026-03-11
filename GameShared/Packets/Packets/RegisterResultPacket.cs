using GameShared.Attributes;

namespace GameShared.Packets;

[Packet]
public partial class RegisterResultPacket : IPacket
{
   

    public bool? Success { get; set; }
    public string? Error { get; set; }


}

