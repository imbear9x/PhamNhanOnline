using GameShared.Attributes;

namespace GameShared.Packets;
[Packet]
public partial class RegisterPacket : IPacket
{
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? Email { get; set; }


    
}

