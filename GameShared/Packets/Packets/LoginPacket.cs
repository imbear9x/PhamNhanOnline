using System.ComponentModel.DataAnnotations;
using GameShared.Attributes;
using GameShared.Messages;

namespace GameShared.Packets;

[Packet]
public partial class LoginPacket : IPacket
{
    [ValidationCode(MessageCode.UsernameWrong)]
    [Required]
    [StringLength(24, MinimumLength = 6)]
    public string? Username { get; set; }

    [ValidationCode(MessageCode.PasswordWrong)]
    [Required]
    [StringLength(32,MinimumLength =8)]
    public string? Password { get; set; }
}

[Packet]
public partial class LoginResultPacket : IPacket
{

    public bool? Success { get; set; }
    public MessageCode? Code { get; set; }
    public Guid? AccountId { get; set; }
    
}
