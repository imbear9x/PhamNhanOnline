using System;
using System.Collections.Concurrent;
using System.Reflection;
using GameShared.Attributes;
using GameShared.Packets;
using LiteNetLib;

namespace PhamNhanOnline.Client.Network.Packets
{
    public static class ClientPacketTransportPolicy
    {
        private static readonly ConcurrentDictionary<Type, DeliveryMethod> Cache = new ConcurrentDictionary<Type, DeliveryMethod>();

        public static DeliveryMethod Resolve(IPacket packet)
        {
            if (packet == null)
                throw new ArgumentNullException("packet");

            return Cache.GetOrAdd(packet.GetType(), CreateDeliveryMethod);
        }

        private static DeliveryMethod CreateDeliveryMethod(Type packetType)
        {
            var attribute = packetType.GetCustomAttribute<PacketTransportAttribute>(true);
            var mode = attribute != null ? attribute.Mode : PacketTransportMode.ReliableOrdered;

            switch (mode)
            {
                case PacketTransportMode.ReliableOrdered:
                    return DeliveryMethod.ReliableOrdered;
                case PacketTransportMode.ReliableSequenced:
                    return DeliveryMethod.ReliableSequenced;
                case PacketTransportMode.ReliableUnordered:
                    return DeliveryMethod.ReliableUnordered;
                case PacketTransportMode.Sequenced:
                    return DeliveryMethod.Sequenced;
                case PacketTransportMode.Unreliable:
                    return DeliveryMethod.Unreliable;
                case PacketTransportMode.UnreliableSequenced:
                    return DeliveryMethod.Sequenced;
                default:
                    return DeliveryMethod.ReliableOrdered;
            }
        }
    }
}
