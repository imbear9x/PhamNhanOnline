using System;
using System.Threading;
using System.Threading.Tasks;
using LiteNetLib;
using PhamNhanOnline.Client.Core.Logging;
using PhamNhanOnline.Client.Network.Session;
using PhamNhanOnline.Client.Shared.Protocol;

namespace PhamNhanOnline.Client.Network.Transport
{
    public sealed class NullClientTransport : IClientTransport, IClientTransportDebugControl
    {
        public ClientConnectionState State { get; private set; } = ClientConnectionState.Disconnected;
        public bool IsDebugNetworkBlocked { get; private set; }
        public float DebugNetworkBlockRemainingSeconds { get; private set; }

        public event Action<ClientConnectionState> StateChanged;
        public event Action<ArraySegment<byte>> PayloadReceived
        {
            add { }
            remove { }
        }

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

        public void BlockNetwork(TimeSpan? duration = null)
        {
            IsDebugNetworkBlocked = true;
            DebugNetworkBlockRemainingSeconds = duration.HasValue && duration.Value > TimeSpan.Zero
                ? (float)duration.Value.TotalSeconds
                : 0f;
        }

        public void UnblockNetwork()
        {
            IsDebugNetworkBlocked = false;
            DebugNetworkBlockRemainingSeconds = 0f;
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
