using System;
using System.Threading;
using System.Threading.Tasks;
using GameShared.Packets;
using PhamNhanOnline.Client.Core.Logging;
using PhamNhanOnline.Client.Network.Packets;
using PhamNhanOnline.Client.Network.Transport;
using PhamNhanOnline.Client.Shared.Protocol;

namespace PhamNhanOnline.Client.Network.Session
{
    public sealed class ClientConnectionService
    {
        private readonly IClientTransport transport;
        private readonly ClientPacketDispatcher packetDispatcher;

        public ClientConnectionService(IClientTransport transport, ServerEndpoint endpoint, ClientPacketDispatcher packetDispatcher)
        {
            this.transport = transport;
            this.packetDispatcher = packetDispatcher;
            Endpoint = endpoint;
            this.transport.StateChanged += HandleTransportStateChanged;
            this.transport.PayloadReceived += HandlePayloadReceived;
        }

        public ServerEndpoint Endpoint { get; private set; }
        public ClientConnectionState State { get; private set; } = ClientConnectionState.Disconnected;
        public string LastStatusMessage { get; private set; } = "Not connected.";
        public ClientPacketDispatcher Packets { get { return packetDispatcher; } }

        public event Action<ClientConnectionState> StateChanged;

        public void UpdateEndpoint(ServerEndpoint endpoint)
        {
            Endpoint = endpoint;
        }

        public async Task<ConnectionAttemptResult> ConnectAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            LastStatusMessage = string.Format("Connecting to {0}...", Endpoint);
            ClientLog.Info(LastStatusMessage);

            var result = await transport.ConnectAsync(Endpoint, cancellationToken);
            LastStatusMessage = result.Message;
            return result;
        }

        public Task DisconnectAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            LastStatusMessage = "Disconnecting...";
            return transport.DisconnectAsync(cancellationToken);
        }

        public void Tick()
        {
            transport.Tick();
        }

        public void Send(IPacket packet)
        {
            if (packet == null)
                throw new ArgumentNullException("packet");

            var payload = PacketSerializer.Serialize(packet);
            var deliveryMethod = ClientPacketTransportPolicy.Resolve(packet);
            transport.Send(new ArraySegment<byte>(payload), deliveryMethod);
            ClientLog.Info(string.Format("Sent packet {0} ({1} bytes, {2}).", packet.GetType().Name, payload.Length, deliveryMethod));
        }

        private void HandleTransportStateChanged(ClientConnectionState state)
        {
            State = state;
            if (state == ClientConnectionState.Disconnected && string.IsNullOrWhiteSpace(LastStatusMessage))
                LastStatusMessage = "Disconnected.";

            var handler = StateChanged;
            if (handler != null)
                handler(state);
        }

        private void HandlePayloadReceived(ArraySegment<byte> payload)
        {
            var buffer = ToArray(payload);
            var packet = PacketSerializer.Deserialize(buffer);
            if (packet == null)
            {
                ClientLog.Warn(string.Format("Dropped inbound payload with {0} bytes because PacketSerializer returned null.", buffer.Length));
                return;
            }

            ClientLog.Info(string.Format("Received packet {0}.", packet.GetType().Name));
            packetDispatcher.Dispatch(packet);
        }

        private static byte[] ToArray(ArraySegment<byte> payload)
        {
            if (payload.Array == null)
                return Array.Empty<byte>();

            if (payload.Offset == 0 && payload.Count == payload.Array.Length)
                return payload.Array;

            var data = new byte[payload.Count];
            Buffer.BlockCopy(payload.Array, payload.Offset, data, 0, payload.Count);
            return data;
        }
    }
}
