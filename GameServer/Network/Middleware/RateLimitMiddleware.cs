using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameServer.Network.Interface;
using GameShared.Packets;

namespace GameServer.Network.Middleware
{
    public class RateLimitMiddleware : IPacketMiddleware
    {
        private readonly Dictionary<int, DateTime> _lastPacketTime = new();

        public async Task InvokeAsync(ConnectionSession session, IPacket packet, Func<Task> next)
        {
            if (_lastPacketTime.TryGetValue(session.ConnectionId, out var last))
            {
                if ((DateTime.UtcNow - last).TotalMilliseconds < 50)
                    return;
            }

            _lastPacketTime[session.ConnectionId] = DateTime.UtcNow;

            await next();
        }
    }
}
