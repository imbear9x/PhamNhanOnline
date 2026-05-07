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
        private const int PayloadPreviewBytes = 16;

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
        public bool LogPacketTraffic { get; set; }

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
            if (LogPacketTraffic)
                ClientLog.Info(string.Format("Sent packet {0} ({1} bytes, {2}).", packet.GetType().Name, payload.Length, deliveryMethod));
        }

        public bool SupportsDebugNetworkControl
        {
            get { return transport is IClientTransportDebugControl; }
        }

        public bool IsDebugNetworkBlocked
        {
            get
            {
                var debugControl = transport as IClientTransportDebugControl;
                return debugControl != null && debugControl.IsDebugNetworkBlocked;
            }
        }

        public float DebugNetworkBlockRemainingSeconds
        {
            get
            {
                var debugControl = transport as IClientTransportDebugControl;
                return debugControl != null ? debugControl.DebugNetworkBlockRemainingSeconds : 0f;
            }
        }

        public void BlockNetworkForDebug(TimeSpan? duration = null)
        {
            var debugControl = transport as IClientTransportDebugControl;
            if (debugControl == null)
                return;

            debugControl.BlockNetwork(duration);
        }

        public void UnblockNetworkForDebug()
        {
            var debugControl = transport as IClientTransportDebugControl;
            if (debugControl == null)
                return;

            debugControl.UnblockNetwork();
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
            IPacket packet;
            try
            {
                packet = PacketSerializer.Deserialize(buffer);
            }
            catch (Exception ex)
            {
                ClientLog.Error(
                    string.Format(
                        "Dropped malformed inbound payload ({0} bytes, head={1}). Deserialize failed: {2}: {3}",
                        buffer.Length,
                        FormatPayloadPreview(buffer),
                        ex.GetType().Name,
                        ex.Message),
                    persistToLogger: true);
                return;
            }

            if (packet == null)
            {
                ClientLog.Warn(
                    string.Format(
                        "Dropped inbound payload with {0} bytes because PacketSerializer returned null. head={1}",
                        buffer.Length,
                        FormatPayloadPreview(buffer)),
                    persistToLogger: true);
                return;
            }

            if (LogPacketTraffic)
                ClientLog.Info(string.Format("Received packet {0}.", packet.GetType().Name));
            packetDispatcher.Dispatch(packet);
        }

        private static string FormatPayloadPreview(byte[] buffer)
        {
            if (buffer == null || buffer.Length == 0)
                return "<empty>";

            var previewLength = Math.Min(buffer.Length, PayloadPreviewBytes);
            return BitConverter.ToString(buffer, 0, previewLength);
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
