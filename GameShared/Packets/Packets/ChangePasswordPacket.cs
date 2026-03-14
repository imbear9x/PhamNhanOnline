using System.ComponentModel.DataAnnotations;
using GameShared.Attributes;
using GameShared.Messages;

namespace GameShared.Packets;

[Packet]
[RequireAuth]
[PacketTransport(PacketTransportMode.ReliableOrdered, MinIntervalMs = 1000)]
public partial class ChangePasswordPacket : IPacket
{
    [ValidationCode(MessageCode.PasswordWrong)]
    [Required]
    [StringLength(128, MinimumLength = 8)]
    public string? Password { get; set; }

    [ValidationCode(MessageCode.PasswordWrong)]
    [Required]
    [StringLength(128, MinimumLength = 8)]
    public string? NewPassword { get; set; }
}

[Packet]
[PacketTransport(PacketTransportMode.ReliableOrdered)]
public partial class ChangePasswordResultPacket : IPacket
{
    public bool? Success { get; set; }
    public MessageCode? Code { get; set; }
}
