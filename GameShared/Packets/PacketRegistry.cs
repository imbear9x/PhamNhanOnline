namespace GameShared.Packets;

public static class PacketRegistry
{
    public static bool TryGetId(IPacket packet, out int id) => PacketGeneratedRegistry.TryGetId(packet, out id);

    public static IPacket? Create(int id) => PacketGeneratedRegistry.Create(id);
}
