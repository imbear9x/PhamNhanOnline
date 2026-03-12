using System.ComponentModel.DataAnnotations;
using GameShared.Attributes;
using GameShared.Messages;

namespace GameShared.Packets;

[Packet]
public partial class ReconnectPacket : IPacket
{
    [ValidationCode(MessageCode.ReconnectTokenInvalid)]
    [Required]
    [StringLength(128, MinimumLength = 16)]
    public string? ResumeToken { get; set; }
}

[Packet]
public partial class ReconnectResultPacket : IPacket
{
    public bool? Success { get; set; }
    public MessageCode? Code { get; set; }
    public Guid? AccountId { get; set; }
    public string? ResumeToken { get; set; }
}
