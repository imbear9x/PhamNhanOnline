namespace GameServer.Network.Packets;

public sealed class LoginPacket : IPacket
{
    public PacketType PacketType => PacketType.Login;

    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

