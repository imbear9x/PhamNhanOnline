namespace GameServer.Network.Packets;

public sealed class LoginResultPacket : IPacket
{
    public PacketType PacketType => PacketType.LoginResult;

    public bool Success   { get; set; }
    public string Error   { get; set; } = string.Empty;
    public Guid AccountId { get; set; }
}

