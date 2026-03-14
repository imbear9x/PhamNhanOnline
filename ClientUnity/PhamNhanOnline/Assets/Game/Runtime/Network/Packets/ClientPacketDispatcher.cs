using System;
using System.Collections.Generic;
using GameShared.Packets;
using PhamNhanOnline.Client.Core.Logging;

namespace PhamNhanOnline.Client.Network.Packets
{
    public sealed class ClientPacketDispatcher
    {
        private readonly object sync = new object();
        private readonly Dictionary<Type, List<Delegate>> handlers = new Dictionary<Type, List<Delegate>>();

        public IDisposable Subscribe<TPacket>(Action<TPacket> handler)
            where TPacket : class, IPacket
        {
            if (handler == null)
                throw new ArgumentNullException("handler");

            var packetType = typeof(TPacket);
            lock (sync)
            {
                List<Delegate> packetHandlers;
                if (!handlers.TryGetValue(packetType, out packetHandlers))
                {
                    packetHandlers = new List<Delegate>();
                    handlers[packetType] = packetHandlers;
                }

                packetHandlers.Add(handler);
            }

            return new PacketSubscription(this, packetType, handler);
        }

        public void Dispatch(IPacket packet)
        {
            if (packet == null)
                return;

            Delegate[] invocationList;
            var packetType = packet.GetType();

            lock (sync)
            {
                List<Delegate> packetHandlers;
                if (!handlers.TryGetValue(packetType, out packetHandlers) || packetHandlers.Count == 0)
                    return;

                invocationList = packetHandlers.ToArray();
            }

            for (var i = 0; i < invocationList.Length; i++)
            {
                try
                {
                    invocationList[i].DynamicInvoke(packet);
                }
                catch (Exception ex)
                {
                    ClientLog.Error(string.Format("Packet handler failed for {0}: {1}", packetType.Name, ex));
                }
            }
        }

        private void Unsubscribe(Type packetType, Delegate handler)
        {
            lock (sync)
            {
                List<Delegate> packetHandlers;
                if (!handlers.TryGetValue(packetType, out packetHandlers))
                    return;

                packetHandlers.Remove(handler);
                if (packetHandlers.Count == 0)
                    handlers.Remove(packetType);
            }
        }

        private sealed class PacketSubscription : IDisposable
        {
            private readonly ClientPacketDispatcher dispatcher;
            private readonly Type packetType;
            private readonly Delegate handler;
            private bool disposed;

            public PacketSubscription(ClientPacketDispatcher dispatcher, Type packetType, Delegate handler)
            {
                this.dispatcher = dispatcher;
                this.packetType = packetType;
                this.handler = handler;
            }

            public void Dispose()
            {
                if (disposed)
                    return;

                disposed = true;
                dispatcher.Unsubscribe(packetType, handler);
            }
        }
    }
}
