using System;
using System.Threading;
using System.Threading.Tasks;
using LiteNetLib;
using PhamNhanOnline.Client.Core.Logging;
using PhamNhanOnline.Client.Network.Session;
using PhamNhanOnline.Client.Shared.Protocol;

namespace PhamNhanOnline.Client.Network.Transport
{
    public sealed class NullClientTransport : IClientTransport
    {
        public ClientConnectionState State { get; private set; } = ClientConnectionState.Disconnected;

        public event Action<ClientConnectionState> StateChanged;
        public event Action<ArraySegment<byte>> PayloadReceived;

        public async Task<ConnectionAttemptResult> ConnectAsync(ServerEndpoint endpoint, CancellationToken cancellationToken = default(CancellationToken))
        {
            SetState(ClientConnectionState.Connecting);
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();

            SetState(ClientConnectionState.Disconnected);
            var message = string.Format(
                "Transport placeholder only. Server endpoint '{0}' is configured, but gameplay transport is not wired yet.",
                endpoint);
            ClientLog.Warn(message);
            return ConnectionAttemptResult.Failed(message);
        }

        public Task DisconnectAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            SetState(ClientConnectionState.Disconnected);
            return Task.CompletedTask;
        }

        public void Tick()
        {
        }

        public void Send(ArraySegment<byte> payload, DeliveryMethod deliveryMethod)
        {
            ClientLog.Warn(string.Format("Dropped {0} bytes because no gameplay transport is wired yet.", payload.Count));
        }

        private void SetState(ClientConnectionState state)
        {
            if (State == state)
                return;

            State = state;
            var handler = StateChanged;
            if (handler != null)
                handler(State);
        }
    }
}
