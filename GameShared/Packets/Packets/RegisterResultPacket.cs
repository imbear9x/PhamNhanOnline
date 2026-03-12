using GameShared.Attributes;
using GameShared.Messages;

namespace GameShared.Packets;

[Packet]
public partial class RegisterResultPacket : IPacket
{
   

    public bool? Success { get; set; }
    public MessageCode? Code { get; set; }


}
