using GameShared.Packets;
using System.Collections.Concurrent;
using System.Reflection;

public static class PacketSerializer
{
    private static readonly ConcurrentDictionary<Type, PacketAccessor> Accessors = new();

    public static byte[] Serialize(IPacket packet)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        if (!PacketRegistry.TryGetId(packet, out var id))
        {
            throw new InvalidOperationException($"Packet type '{packet.GetType().FullName}' is not registered");
        }

        writer.Write(id);
        var accessor = GetAccessor(packet.GetType());
        accessor.Serialize(packet, writer);

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

        var accessor = GetAccessor(packet.GetType());
        accessor.Deserialize(packet, reader);
        return packet;
    }

    private static PacketAccessor GetAccessor(Type packetType)
    {
        return Accessors.GetOrAdd(packetType, static type =>
        {
            var serializeMethod = type.GetMethod(
                "Serialize",
                BindingFlags.Instance | BindingFlags.Public,
                binder: null,
                types: new[] { typeof(BinaryWriter) },
                modifiers: null);
            var deserializeMethod = type.GetMethod(
                "Deserialize",
                BindingFlags.Instance | BindingFlags.Public,
                binder: null,
                types: new[] { typeof(BinaryReader) },
                modifiers: null);

            if (serializeMethod is null || deserializeMethod is null)
            {
                throw new InvalidOperationException(
                    $"Packet type '{type.FullName}' must declare public Serialize(BinaryWriter) and Deserialize(BinaryReader) methods.");
            }

            return new PacketAccessor(serializeMethod, deserializeMethod);
        });
    }

    private sealed class PacketAccessor
    {
        private readonly MethodInfo _serializeMethod;
        private readonly MethodInfo _deserializeMethod;

        public PacketAccessor(MethodInfo serializeMethod, MethodInfo deserializeMethod)
        {
            _serializeMethod = serializeMethod;
            _deserializeMethod = deserializeMethod;
        }

        public void Serialize(IPacket packet, BinaryWriter writer)
        {
            _serializeMethod.Invoke(packet, new object?[] { writer });
        }

        public void Deserialize(IPacket packet, BinaryReader reader)
        {
            _deserializeMethod.Invoke(packet, new object?[] { reader });
        }
    }
}
