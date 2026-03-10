namespace GameServer.Network.Packets;

public sealed class RegisterResultPacket : IPacket
{
    public PacketType PacketType => PacketType.RegisterResult;

    public bool   Success { get; set; }
    public string Error   { get; set; } = string.Empty;
}

