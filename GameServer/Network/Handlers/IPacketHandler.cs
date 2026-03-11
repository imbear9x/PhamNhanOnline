using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Network.Handlers
{
    public interface IPacketHandler<TPacket>
    {
        Task HandleAsync(ConnectionSession session, TPacket packet);
    }
}
