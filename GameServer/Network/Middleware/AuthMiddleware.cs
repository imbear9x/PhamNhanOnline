using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameServer.Network.Interface;
using GameShared.Attributes;
using GameShared.Logging;
using GameShared.Packets;

namespace GameServer.Network.Middleware
{
    public class AuthMiddleware :IPacketMiddleware
    {
        static readonly HashSet<Type> _authPackets;
        static AuthMiddleware()
        {
            _authPackets = AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => typeof(IPacket).IsAssignableFrom(t))
                .Where(t => t.IsDefined(typeof(RequireAuthAttribute), true))
                .ToHashSet();
        }
        public async Task InvokeAsync(ConnectionSession session, IPacket packet, Func<Task> next)
        {
            if (_authPackets.Contains(packet.GetType()) && !session.IsAuthenticated)
            {
                Logger.Error($"Unauthorized packet: {packet.GetType().Name} (ConnectionId={session.ConnectionId})");
                return;
            }

            await next();
        }
    }
}
