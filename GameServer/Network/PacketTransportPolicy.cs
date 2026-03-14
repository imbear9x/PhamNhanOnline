using System.Collections.Concurrent;
using System.Reflection;
using GameShared.Attributes;
using GameShared.Packets;
using LiteNetLib;

namespace GameServer.Network;

public static class PacketTransportPolicy
{
    private static readonly ConcurrentDictionary<Type, PacketTransportProfile> Cache = new();

    public static PacketTransportProfile Resolve(IPacket packet) => Resolve(packet.GetType());

    public static PacketTransportProfile Resolve(Type packetType) =>
        Cache.GetOrAdd(packetType, CreateProfile);

    private static PacketTransportProfile CreateProfile(Type packetType)
    {
        var transport = packetType.GetCustomAttribute<PacketTransportAttribute>(inherit: true);
        var mode = transport?.Mode ?? PacketTransportMode.ReliableOrdered;
        var trafficClass = transport?.TrafficClass ?? PacketTrafficClass.Business;
        var minIntervalMs = Math.Max(0, transport?.MinIntervalMs ?? 0);

        return new PacketTransportProfile(
            ToDeliveryMethod(mode),
            trafficClass,
            minIntervalMs);
    }

    private static DeliveryMethod ToDeliveryMethod(PacketTransportMode mode) =>
        mode switch
        {
            PacketTransportMode.ReliableOrdered => DeliveryMethod.ReliableOrdered,
            PacketTransportMode.ReliableSequenced => DeliveryMethod.ReliableSequenced,
            PacketTransportMode.ReliableUnordered => DeliveryMethod.ReliableUnordered,
            PacketTransportMode.Sequenced => DeliveryMethod.Sequenced,
            PacketTransportMode.Unreliable => DeliveryMethod.Unreliable,
            PacketTransportMode.UnreliableSequenced => DeliveryMethod.Sequenced,
            _ => DeliveryMethod.ReliableOrdered
        };
}

public readonly record struct PacketTransportProfile(
    DeliveryMethod DeliveryMethod,
    PacketTrafficClass TrafficClass,
    int MinIntervalMs);
