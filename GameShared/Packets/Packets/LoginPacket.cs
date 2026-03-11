

using GameShared.Attributes;

namespace GameShared.Packets;

[Packet]
public partial class LoginPacket : IPacket
{
   
    public string? Username { get; set; }
    public string? Password { get; set; }
    
}
