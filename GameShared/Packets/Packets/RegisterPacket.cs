using System.ComponentModel.DataAnnotations;
using GameShared.Attributes;
using GameShared.Messages;

namespace GameShared.Packets;
[Packet]
public partial class RegisterPacket : IPacket
{
    [ValidationCode(MessageCode.UsernameWrong)]
    [Required]
    [StringLength(32, MinimumLength = 3)]
    public string? Username { get; set; }

    [ValidationCode(MessageCode.PasswordWrong)]
    [Required]
    [StringLength(128, MinimumLength = 8)]
    public string? Password { get; set; }

    [ValidationCode(MessageCode.EmailWrong)]
    [Required]
    [StringLength(254)]
    [EmailAddress]
    public string? Email { get; set; }


    
}
