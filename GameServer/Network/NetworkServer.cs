using System.Collections.Concurrent;
using System.Net;
using System.Security.Cryptography;
using GameServer.Network.Interface;
using GameShared.Diagnostics;
using GameShared.Logging;
using GameShared.Messages;
using GameShared.Packets;
using GameServer.Runtime;
using GameServer.World;
using LiteNetLib;
using System.Text.Json;

namespace GameServer.Network;

public sealed class NetworkServer : INetEventListener, INetworkSender
{
    private readonly NetManager _netManager;
    private readonly PacketDispatcher _dispatcher;
    private readonly WorldManager _worldManager;
    private readonly CharacterRuntimeSaveService _runtimeSaveService;
    private readonly ConcurrentDictionary<int, ConnectionSession> _sessions = new();
    private readonly ConcurrentDictionary<string, ResumeTicket> _resumeTickets = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<Guid, string> _accountTokens = new();
    private static readonly TimeSpan ResumeWindow = TimeSpan.FromMinutes(2);

    public NetworkServer(
        PacketDispatcher dispatcher,
        WorldManager worldManager,
        CharacterRuntimeSaveService runtimeSaveService)
    {
        _dispatcher = dispatcher;
        _worldManager = worldManager;
        _runtimeSaveService = runtimeSaveService;
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
        _runtimeSaveService.SaveDirtyPlayersAsync().GetAwaiter().GetResult();
        _netManager.Stop();
        _sessions.Clear();
        _resumeTickets.Clear();
        _accountTokens.Clear();
    }

    public void PollEvents()
    {
        PurgeExpiredResumeTickets();
        _netManager.PollEvents();
    }

    public void Send(int connectionId, IPacket packet)
    {
        if (!_sessions.TryGetValue(connectionId, out var session))
            return;

        var data = PacketSerializer.Serialize(packet);
        session.Peer.Send(data, DeliveryMethod.ReliableOrdered);
    }

    public string IssueResumeToken(ConnectionSession session, Guid accountId)
    {
        var token = CreateResumeToken();
        var ticket = new ResumeTicket(accountId, IsConnected: true, ExpiresAtUtc: DateTime.MaxValue);
        _resumeTickets[token] = ticket;

        _accountTokens.AddOrUpdate(
            accountId,
            addValueFactory: _ => token,
            updateValueFactory: (accountKey, oldToken) =>
            {
                if (!string.Equals(oldToken, token, StringComparison.Ordinal))
                    _resumeTickets.TryRemove(oldToken, out _);
                return token;
            });

        if (!string.IsNullOrWhiteSpace(session.ResumeToken) &&
            !string.Equals(session.ResumeToken, token, StringComparison.Ordinal))
        {
            _resumeTickets.TryRemove(session.ResumeToken, out _);
        }

        session.ResumeToken = token;
        return token;
    }

    public bool TryResumeSession(
        ConnectionSession session,
        string resumeToken,
        out Guid accountId,
        out MessageCode errorCode)
    {
        accountId = Guid.Empty;
        errorCode = MessageCode.ReconnectTokenInvalid;

        if (string.IsNullOrWhiteSpace(resumeToken))
            return false;

        PurgeExpiredResumeTickets();

        if (!_resumeTickets.TryGetValue(resumeToken, out var ticket))
            return false;

        if (ticket.IsConnected)
            return false;

        if (ticket.ExpiresAtUtc < DateTime.UtcNow)
        {
            _resumeTickets.TryRemove(resumeToken, out _);
            _accountTokens.TryRemove(ticket.AccountId, out _);
            errorCode = MessageCode.ReconnectSessionExpired;
            return false;
        }

        _resumeTickets[resumeToken] = ticket with
        {
            IsConnected = true,
            ExpiresAtUtc = DateTime.MaxValue
        };

        accountId = ticket.AccountId;
        session.PlayerId = ticket.AccountId;
        session.IsAuthenticated = true;
        session.ResumeToken = resumeToken;
        return true;
    }

    // INetEventListener

    public void OnPeerConnected(NetPeer peer)
    {
        var session = new ConnectionSession(peer);
        _sessions[peer.Id] = session;
    }

    public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        if (!_sessions.TryRemove(peer.Id, out var session))
            return;

        if (session.IsAuthenticated)
        {
            _runtimeSaveService.FlushPlayerAsync(session.PlayerId).GetAwaiter().GetResult();
            _worldManager.RemovePlayer(session.PlayerId);
        }

        if (!session.IsAuthenticated || string.IsNullOrWhiteSpace(session.ResumeToken))
            return;

        if (_resumeTickets.TryGetValue(session.ResumeToken, out var ticket))
        {
            _resumeTickets[session.ResumeToken] = ticket with
            {
                IsConnected = false,
                ExpiresAtUtc = DateTime.UtcNow + ResumeWindow
            };
        }
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

        _ = DispatchWithIncidentCaptureAsync(session, packet, bytes, channelNumber, deliveryMethod);
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

    private async Task DispatchWithIncidentCaptureAsync(
        ConnectionSession session,
        IPacket packet,
        byte[] rawPayload,
        byte channelNumber,
        DeliveryMethod deliveryMethod)
    {
        try
        {
            await _dispatcher.DispatchAsync(session, packet);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, $"Unhandled packet exception: {packet.GetType().Name} (ConnectionId={session.ConnectionId})");

            PacketIncidentCapture.Log(new PacketIncidentRecord
            {
                CapturedAtUtc = DateTime.UtcNow,
                Source = "Server",
                IncidentType = "ServerInboundPacketException",
                ConnectionId = session.ConnectionId,
                RemoteEndPoint = $"{session.Peer.Address}:{session.Peer.Port}",
                IsAuthenticated = session.IsAuthenticated,
                PlayerId = session.PlayerId == Guid.Empty ? null : session.PlayerId,
                ChannelNumber = channelNumber,
                DeliveryMethod = deliveryMethod.ToString(),
                PacketType = packet.GetType().FullName ?? packet.GetType().Name,
                PacketJson = TrySerializePacket(packet),
                PacketPayloadBase64 = Convert.ToBase64String(rawPayload),
                ExceptionType = ex.GetType().FullName ?? ex.GetType().Name,
                ExceptionMessage = ex.Message,
                ExceptionStackTrace = ex.StackTrace
            });
        }
    }

    private static string TrySerializePacket(IPacket packet)
    {
        try
        {
            return JsonSerializer.Serialize(packet, packet.GetType());
        }
        catch (Exception ex)
        {
            return $"<serialize_failed:{ex.GetType().Name}>";
        }
    }

    private static string CreateResumeToken()
    {
        Span<byte> bytes = stackalloc byte[16];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToHexString(bytes);
    }

    private void PurgeExpiredResumeTickets()
    {
        var now = DateTime.UtcNow;
        foreach (var kv in _resumeTickets)
        {
            if (kv.Value.IsConnected || kv.Value.ExpiresAtUtc > now)
                continue;

            _resumeTickets.TryRemove(kv.Key, out _);
            _accountTokens.TryRemove(kv.Value.AccountId, out _);
        }
    }

    private sealed record ResumeTicket(Guid AccountId, bool IsConnected, DateTime ExpiresAtUtc);
}
