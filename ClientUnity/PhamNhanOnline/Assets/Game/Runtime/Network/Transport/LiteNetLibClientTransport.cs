using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using LiteNetLib;
using PhamNhanOnline.Client.Core.Logging;
using PhamNhanOnline.Client.Network.Session;
using PhamNhanOnline.Client.Shared.Protocol;

namespace PhamNhanOnline.Client.Network.Transport
{
    public sealed class LiteNetLibClientTransport : IClientTransport, IClientTransportDebugControl, INetEventListener
    {
        private static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(5);
        private readonly object sync = new object();

        private NetManager netManager;
        private NetPeer peer;
        private TaskCompletionSource<ConnectionAttemptResult> connectCompletionSource;
        private bool debugNetworkBlocked;
        private DateTime? debugNetworkBlockUntilUtc;

        public ClientConnectionState State { get; private set; } = ClientConnectionState.Disconnected;
        public bool IsDebugNetworkBlocked
        {
            get
            {
                lock (sync)
                {
                    RefreshDebugBlockStateNoLock();
                    return debugNetworkBlocked;
                }
            }
        }

        public float DebugNetworkBlockRemainingSeconds
        {
            get
            {
                lock (sync)
                {
                    RefreshDebugBlockStateNoLock();
                    if (!debugNetworkBlocked || !debugNetworkBlockUntilUtc.HasValue)
                        return 0f;

                    return Math.Max(0f, (float)(debugNetworkBlockUntilUtc.Value - DateTime.UtcNow).TotalSeconds);
                }
            }
        }

        public event Action<ClientConnectionState> StateChanged;
        public event Action<ArraySegment<byte>> PayloadReceived;

        public Task<ConnectionAttemptResult> ConnectAsync(ServerEndpoint endpoint, CancellationToken cancellationToken = default(CancellationToken))
        {
            lock (sync)
            {
                if (State == ClientConnectionState.Connected && peer != null)
                    return Task.FromResult(ConnectionAttemptResult.Succeeded(string.Format("Already connected to {0}.", endpoint)));

                if (State == ClientConnectionState.Connecting && connectCompletionSource != null)
                    return connectCompletionSource.Task;

                RefreshDebugBlockStateNoLock();
                if (debugNetworkBlocked)
                    return Task.FromResult(ConnectionAttemptResult.Failed("Debug network block is active."));

                CleanupTransport();

                try
                {
                    netManager = new NetManager(this)
                    {
                        AutoRecycle = true,
                        UnconnectedMessagesEnabled = false
                    };

                    if (!netManager.Start())
                    {
                        SetState(ClientConnectionState.Disconnected);
                        return Task.FromResult(ConnectionAttemptResult.Failed("LiteNetLib client failed to start."));
                    }

                    connectCompletionSource = new TaskCompletionSource<ConnectionAttemptResult>();
                    if (cancellationToken.CanBeCanceled)
                    {
                        cancellationToken.Register(() =>
                        {
                            TryCompleteConnect(ConnectionAttemptResult.Failed("Connection cancelled."));
                            DisconnectInternal();
                        });
                    }

                    SetState(ClientConnectionState.Connecting);
                    netManager.Connect(endpoint.Host, endpoint.Port, string.Empty);
                    ClientLog.Info(string.Format("LiteNetLib connect requested to {0}.", endpoint));
                    StartConnectTimeout(endpoint, connectCompletionSource);
                    return connectCompletionSource.Task;
                }
                catch (Exception ex)
                {
                    CleanupTransport();
                    SetState(ClientConnectionState.Disconnected);
                    return Task.FromResult(ConnectionAttemptResult.Failed(string.Format("Transport connect failed: {0}", ex.Message)));
                }
            }
        }

        public Task DisconnectAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            DisconnectInternal();
            return Task.CompletedTask;
        }

        public void Tick()
        {
            NetManager manager = null;
            var shouldForceDisconnect = false;

            lock (sync)
            {
                RefreshDebugBlockStateNoLock();
                shouldForceDisconnect = debugNetworkBlocked &&
                                        (netManager != null || peer != null || State == ClientConnectionState.Connected || State == ClientConnectionState.Connecting);

                if (!shouldForceDisconnect)
                    manager = netManager;
            }

            if (shouldForceDisconnect)
            {
                DisconnectInternal();
                return;
            }

            if (manager != null)
                manager.PollEvents();
        }

        public void Send(ArraySegment<byte> payload, DeliveryMethod deliveryMethod)
        {
            if (IsDebugNetworkBlocked)
            {
                ClientLog.Warn(string.Format("Dropped {0} outbound bytes because debug network block is active.", payload.Count));
                return;
            }

            if (peer == null || State != ClientConnectionState.Connected)
            {
                ClientLog.Warn(string.Format("Dropped {0} outbound bytes because no server peer is connected yet.", payload.Count));
                return;
            }

            peer.Send(ToArray(payload), deliveryMethod);
        }

        public void OnPeerConnected(NetPeer connectedPeer)
        {
            if (IsDebugNetworkBlocked)
            {
                connectedPeer.Disconnect();
                return;
            }

            peer = connectedPeer;
            SetState(ClientConnectionState.Connected);
            TryCompleteConnect(ConnectionAttemptResult.Succeeded(string.Format("Connected to {0}:{1}.", connectedPeer.Address, connectedPeer.Port)));
        }

        public void OnPeerDisconnected(NetPeer disconnectedPeer, DisconnectInfo disconnectInfo)
        {
            peer = null;
            SetState(ClientConnectionState.Disconnected);

            if (connectCompletionSource != null)
                TryCompleteConnect(ConnectionAttemptResult.Failed(string.Format("Disconnected before ready: {0}", disconnectInfo.Reason)));

            ClientLog.Warn(string.Format("Disconnected from server: {0}.", disconnectInfo.Reason));
        }

        public void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
        {
            ClientLog.Error(string.Format("Network error to {0}: {1}", endPoint, socketError));
            TryCompleteConnect(ConnectionAttemptResult.Failed(string.Format("Network error: {0}", socketError)));
        }

        public void OnNetworkReceive(NetPeer remotePeer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
        {
            var data = reader.GetRemainingBytes();
            reader.Recycle();

            var handler = PayloadReceived;
            if (handler != null)
                handler(new ArraySegment<byte>(data));
        }

        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
        {
            reader.Recycle();
        }

        public void OnNetworkLatencyUpdate(NetPeer remotePeer, int latency)
        {
        }

        public void OnConnectionRequest(ConnectionRequest request)
        {
            request.Reject();
        }

        public void BlockNetwork(TimeSpan? duration = null)
        {
            lock (sync)
            {
                debugNetworkBlocked = true;
                debugNetworkBlockUntilUtc = duration.HasValue && duration.Value > TimeSpan.Zero
                    ? DateTime.UtcNow.Add(duration.Value)
                    : (DateTime?)null;
            }

            ClientLog.Warn(duration.HasValue && duration.Value > TimeSpan.Zero
                ? string.Format("Debug network block enabled for {0:0.0}s.", duration.Value.TotalSeconds)
                : "Debug network block enabled.");
        }

        public void UnblockNetwork()
        {
            var wasBlocked = false;
            lock (sync)
            {
                RefreshDebugBlockStateNoLock();
                wasBlocked = debugNetworkBlocked;
                debugNetworkBlocked = false;
                debugNetworkBlockUntilUtc = null;
            }

            if (wasBlocked)
                ClientLog.Info("Debug network block cleared.");
        }

        private void DisconnectInternal()
        {
            CleanupTransport();
            SetState(ClientConnectionState.Disconnected);
        }

        private void CleanupTransport()
        {
            var localPeer = peer;
            peer = null;

            if (localPeer != null)
            {
                try
                {
                    localPeer.Disconnect();
                }
                catch
                {
                }
            }

            var manager = netManager;
            netManager = null;
            if (manager != null)
            {
                try
                {
                    manager.Stop();
                }
                catch
                {
                }
            }

            connectCompletionSource = null;
        }

        private void SetState(ClientConnectionState state)
        {
            if (State == state)
                return;

            State = state;
            var handler = StateChanged;
            if (handler != null)
                handler(state);
        }

        private void TryCompleteConnect(ConnectionAttemptResult result)
        {
            var pending = connectCompletionSource;
            connectCompletionSource = null;
            if (pending != null)
                pending.TrySetResult(result);
        }

        private void RefreshDebugBlockStateNoLock()
        {
            if (!debugNetworkBlocked || !debugNetworkBlockUntilUtc.HasValue)
                return;

            if (DateTime.UtcNow < debugNetworkBlockUntilUtc.Value)
                return;

            debugNetworkBlocked = false;
            debugNetworkBlockUntilUtc = null;
            ClientLog.Info("Debug network block expired.");
        }

        private void StartConnectTimeout(ServerEndpoint endpoint, TaskCompletionSource<ConnectionAttemptResult> pendingConnect)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(ConnectTimeout).ConfigureAwait(false);
                }
                catch
                {
                    return;
                }

                lock (sync)
                {
                    if (!ReferenceEquals(connectCompletionSource, pendingConnect) ||
                        State != ClientConnectionState.Connecting)
                    {
                        return;
                    }

                    ClientLog.Warn(string.Format("Connection attempt to {0} timed out after {1:0.0}s.", endpoint, ConnectTimeout.TotalSeconds));
                    TryCompleteConnect(ConnectionAttemptResult.Failed("Không thể kết nối tới server."));
                    DisconnectInternal();
                }
            });
        }

        private static byte[] ToArray(ArraySegment<byte> payload)
        {
            if (payload.Array == null)
                return Array.Empty<byte>();

            if (payload.Offset == 0 && payload.Count == payload.Array.Length)
                return payload.Array;

            var bytes = new byte[payload.Count];
            Buffer.BlockCopy(payload.Array, payload.Offset, bytes, 0, payload.Count);
            return bytes;
        }
    }
}
