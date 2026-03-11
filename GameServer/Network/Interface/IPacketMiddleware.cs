
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameShared.Packets;

namespace GameServer.Network.Interface
{
    public interface IPacketMiddleware
    {
        Task InvokeAsync(ConnectionSession session, IPacket packet, Func<Task> next);
    }
}
