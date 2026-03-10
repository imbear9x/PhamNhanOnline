namespace GameServer.Network.Packets;

public sealed class RegisterPacket : IPacket
{
    public PacketType PacketType => PacketType.Register;

    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Email    { get; set; } = string.Empty;
}

