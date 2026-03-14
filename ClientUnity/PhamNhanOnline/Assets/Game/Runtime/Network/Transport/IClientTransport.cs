using System;
using System.Threading;
using System.Threading.Tasks;
using LiteNetLib;
using PhamNhanOnline.Client.Network.Session;
using PhamNhanOnline.Client.Shared.Protocol;

namespace PhamNhanOnline.Client.Network.Transport
{
    public interface IClientTransport
    {
        ClientConnectionState State { get; }
        event Action<ClientConnectionState> StateChanged;
        event Action<ArraySegment<byte>> PayloadReceived;

        Task<ConnectionAttemptResult> ConnectAsync(ServerEndpoint endpoint, CancellationToken cancellationToken = default(CancellationToken));
        Task DisconnectAsync(CancellationToken cancellationToken = default(CancellationToken));
        void Tick();
        void Send(ArraySegment<byte> payload, DeliveryMethod deliveryMethod);
    }
}
