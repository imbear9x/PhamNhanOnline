using System.ComponentModel.DataAnnotations;
using GameShared.Attributes;
using GameShared.Messages;

namespace GameShared.Packets;

[Packet]
public partial class LoginPacket : IPacket
{
    [ValidationCode(MessageCode.UsernameWrong)]
    [Required]
    [StringLength(32, MinimumLength = 3)]
    public string? Username { get; set; }

    [ValidationCode(MessageCode.PasswordWrong)]
    [Required]
    public string? Password { get; set; }
}
