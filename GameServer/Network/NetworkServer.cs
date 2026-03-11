using System.Collections.Concurrent;
using System.Net;
using GameServer.Network.Interface;
using GameShared.Packets;
using LiteNetLib;

namespace GameServer.Network;

public sealed class NetworkServer : INetEventListener, INetworkSender
{
    private readonly NetManager _netManager;
    private readonly PacketDispatcher _dispatcher;
    private readonly ConcurrentDictionary<int, ConnectionSession> _sessions = new();

    public NetworkServer(PacketDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
        _netManager = new NetManager(this)
        {
            AutoRecycle = true
        };
    }

    public void Start()
    {
        if (!_netManager.Start(7777))
            throw new InvalidOperationException("Failed to start UDP server on port 7777.");
    }

    public void Stop()
    {
        _netManager.Stop();
        _sessions.Clear();
    }

    public void PollEvents()
    {
        _netManager.PollEvents();
    }

    public void Send(int connectionId, IPacket packet)
    {
        if (!_sessions.TryGetValue(connectionId, out var session))
            return;

        var data = PacketSerializer.Serialize(packet);
        session.Peer.Send(data, DeliveryMethod.ReliableOrdered);
    }

    // INetEventListener

    public void OnPeerConnected(NetPeer peer)
    {
        var session = new ConnectionSession(peer);
        _sessions[peer.Id] = session;
    }

    public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        _sessions.TryRemove(peer.Id, out _);
    }

    public void OnNetworkError(IPEndPoint endPoint, System.Net.Sockets.SocketError socketError)
    {
        // Log if needed.
    }

    public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
    {
        if (!_sessions.TryGetValue(peer.Id, out var session))
        {
            reader.Recycle();
            return;
        }

        var bytes = reader.GetRemainingBytes();
        reader.Recycle();

        var packet = PacketSerializer.Deserialize(bytes);
        if (packet is null)
            return;

        _ = _dispatcher.DispatchAsync(session, packet);
    }

    public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
    {
        // Ignore unconnected messages for now.
        reader.Recycle();
    }

    public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
    {
        // Optional latency tracking.
    }

    public void OnConnectionRequest(ConnectionRequest request)
    {
        // Accept all for now; in production you may want to check keys or limits.
        request.AcceptIfKey(string.Empty);
    }
}

