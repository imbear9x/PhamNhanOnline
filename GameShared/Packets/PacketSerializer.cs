using GameShared.Packets;

public static class PacketSerializer
{
    public static byte[] Serialize(IPacket packet)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        if (!PacketRegistry.TryGetId(packet, out var id))
        {
            throw new InvalidOperationException($"Packet type '{packet.GetType().FullName}' is not registered");
        }

        writer.Write(id);
        ((dynamic)packet).Serialize(writer);

        return ms.ToArray();
    }

    public static IPacket? Deserialize(byte[] data)
    {
        using var ms = new MemoryStream(data);
        using var reader = new BinaryReader(ms);

        var id = reader.ReadInt32();
        var packet = PacketRegistry.Create(id);
        if (packet is null)
        {
            return null;
        }

        ((dynamic)packet).Deserialize(reader);
        return packet;
    }
}
