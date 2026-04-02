using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text.Json;
using GameServer.Config;
using GameServer.Diagnostics;
using GameServer.Network.Interface;
using GameServer.Runtime;
using GameServer.World;
using GameShared.Diagnostics;
using GameShared.Logging;
using GameShared.Messages;
using GameShared.Packets;
using LiteNetLib;

namespace GameServer.Network;

public sealed class NetworkServer : INetEventListener, INetworkSender
{
    private const string AccountLoggedInElsewhereMessage = "Tai khoan da duoc dang nhap tren thiet bi khac.";
    private const string ReconnectHoldReasonExpired = "resume window expired";
    private const string ReconnectHoldReasonFreshLogin = "fresh login replaced held session";
    private readonly NetManager _netManager;
    private readonly PacketDispatcher _dispatcher;
    private readonly WorldManager _worldManager;
    private readonly CharacterRuntimeSaveService _runtimeSaveService;
    private readonly CharacterCombatDeathRecoveryService _deathRecoveryService;
    private readonly ServerMetricsService _metrics;
    private readonly TimeSpan _resumeWindow;
    private readonly ConcurrentDictionary<int, ConnectionSession> _sessions = new();
    private readonly ConcurrentDictionary<string, ResumeTicket> _resumeTickets = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, RevokedResumeTicket> _revokedResumeTokens = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<Guid, string> _accountTokens = new();
    private readonly CancellationTokenSource _shutdownCts = new();
    public NetworkServer(
        PacketDispatcher dispatcher,
        WorldManager worldManager,
        CharacterRuntimeSaveService runtimeSaveService,
        CharacterCombatDeathRecoveryService deathRecoveryService,
        GameConfigValues gameConfig,
        ServerMetricsService metrics)
    {
        _dispatcher = dispatcher;
        _worldManager = worldManager;
        _runtimeSaveService = runtimeSaveService;
        _deathRecoveryService = deathRecoveryService;
        _resumeWindow = gameConfig.ResumeWindow;
        _metrics = metrics;
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
        _shutdownCts.Cancel();

        var sessions = _sessions.Values.ToList();
        foreach (var session in sessions)
        {
            session.StopInboundProcessing();
        }

        WaitForInboundProcessors(sessions);

        _runtimeSaveService.SaveDirtyPlayersAsync().GetAwaiter().GetResult();
        _netManager.Stop();
        _sessions.Clear();
        _resumeTickets.Clear();
        _revokedResumeTokens.Clear();
        _accountTokens.Clear();
    }

    public void PollEvents()
    {
        if (_shutdownCts.IsCancellationRequested)
            return;

        PurgeExpiredResumeTickets();
        _netManager.PollEvents();
    }

    public void Send(int connectionId, IPacket packet)
    {
        if (!_sessions.TryGetValue(connectionId, out var session))
            return;

        var profile = PacketTransportPolicy.Resolve(packet);
        var data = PacketSerializer.Serialize(packet);
        _metrics.RecordOutboundPacketSent(packet.GetType().Name, data.Length);
        session.Peer.Send(data, profile.DeliveryMethod);
    }

    public string IssueResumeToken(ConnectionSession session, Guid accountId)
    {
        DisconnectDuplicateSessions(session, accountId);
        ReleaseHeldPlayerIfAny(accountId, ReconnectHoldReasonFreshLogin);

        var token = CreateResumeToken();
        var ticket = new ResumeTicket(accountId, IsConnected: true, ExpiresAtUtc: DateTime.MaxValue);
        _resumeTickets[token] = ticket;

        _accountTokens.AddOrUpdate(
            accountId,
            addValueFactory: _ => token,
            updateValueFactory: (_, oldToken) =>
            {
                if (!string.Equals(oldToken, token, StringComparison.Ordinal))
                {
                    RevokeResumeToken(oldToken, MessageCode.AccountLoggedInElsewhere);
                }

                return token;
            });

        if (!string.IsNullOrWhiteSpace(session.ResumeToken) &&
            !string.Equals(session.ResumeToken, token, StringComparison.Ordinal))
        {
            _resumeTickets.TryRemove(session.ResumeToken, out ResumeTicket? _);
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

        if (_revokedResumeTokens.TryRemove(resumeToken, out var revokedTicket))
        {
            errorCode = revokedTicket.Code;
            return false;
        }

        if (!_resumeTickets.TryGetValue(resumeToken, out var ticket))
            return false;

        if (ticket.IsConnected)
            return false;

        if (ticket.ExpiresAtUtc < DateTime.UtcNow)
        {
            _resumeTickets.TryRemove(resumeToken, out ResumeTicket? _);
            _accountTokens.TryRemove(ticket.AccountId, out var _);
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

    public void OnPeerConnected(NetPeer peer)
    {
        if (_shutdownCts.IsCancellationRequested)
        {
            peer.Disconnect();
            return;
        }

        var session = new ConnectionSession(peer);
        session.InboundProcessorTask = Task.Run(() => ProcessInboundPacketsAsync(session, _shutdownCts.Token));
        _sessions[peer.Id] = session;
        _metrics.RemoveSession(session.ConnectionId);
    }

    public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        if (!_sessions.TryRemove(peer.Id, out var session))
            return;

        session.StopInboundProcessing();
        WaitForInboundProcessor(session);
        _metrics.RemoveSession(session.ConnectionId);

        if (_shutdownCts.IsCancellationRequested)
            return;

        if (session.IsAuthenticated &&
            _worldManager.IsOwnedByConnection(session.PlayerId, session.ConnectionId))
        {
            if (_deathRecoveryService.IsCombatDead(session.Player?.RuntimeState.CaptureSnapshot().CurrentState))
            {
                _deathRecoveryService.RecoverDisconnectedPlayerToHomeAsync(session.Player!).GetAwaiter().GetResult();
                _worldManager.RemovePlayer(session.PlayerId);
            }
            else
            {
                _runtimeSaveService.FlushPlayerAsync(session.PlayerId).GetAwaiter().GetResult();
                if (string.IsNullOrWhiteSpace(session.ResumeToken))
                {
                    _worldManager.RemovePlayer(session.PlayerId);
                }
                else
                {
                    _worldManager.TryMarkPlayerDisconnected(session.PlayerId);
                }
            }
        }
        else if (session.IsAuthenticated &&
                 _worldManager.TryGetPlayer(session.PlayerId, out var replacementOwner))
        {
            Logger.Info(
                $"Disconnected session no longer owns online player. AccountId={session.PlayerId}, " +
                $"DisconnectedConnectionId={session.ConnectionId}, ActiveConnectionId={replacementOwner.ConnectionId}");
        }

        if (!session.IsAuthenticated || string.IsNullOrWhiteSpace(session.ResumeToken))
            return;

        if (_resumeTickets.TryGetValue(session.ResumeToken, out var resumeTicket))
        {
            _resumeTickets[session.ResumeToken] = resumeTicket with
            {
                IsConnected = false,
                ExpiresAtUtc = DateTime.UtcNow + _resumeWindow
            };
        }
    }

    public void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
    {
        // Log if needed.
    }

    public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
    {
        if (_shutdownCts.IsCancellationRequested)
        {
            reader.Recycle();
            return;
        }

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

        if (!session.TryEnqueueInboundPacket(new InboundPacketEnvelope(packet, bytes, channelNumber, deliveryMethod)))
        {
            _metrics.RecordInboundPacketDropped(session.ConnectionId, session.PendingInboundPacketCount, packet.GetType().Name, bytes.Length);
            Logger.Error($"Inbound packet dropped: {packet.GetType().Name} (ConnectionId={session.ConnectionId})");
            return;
        }

        _metrics.RecordInboundPacketEnqueued(session.ConnectionId, session.PendingInboundPacketCount, packet.GetType().Name, bytes.Length);
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
        request.AcceptIfKey(string.Empty);
    }

    private async Task ProcessInboundPacketsAsync(ConnectionSession session, CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var envelope in session.ReadInboundPacketsAsync(cancellationToken))
            {
                var startedAt = Stopwatch.GetTimestamp();
                await DispatchWithIncidentCaptureAsync(
                    session,
                    envelope.Packet,
                    envelope.RawPayload,
                    envelope.ChannelNumber,
                    envelope.DeliveryMethod);
                var duration = Stopwatch.GetElapsedTime(startedAt);
                _metrics.RecordInboundPacketProcessed(session.ConnectionId, duration, session.PendingInboundPacketCount);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            _metrics.RecordInboundPacketProcessingException();
            Logger.Error(ex, $"Inbound processor crashed: ConnectionId={session.ConnectionId}");
        }
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
            _metrics.RecordInboundPacketProcessingException();
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

            _resumeTickets.TryRemove(kv.Key, out ResumeTicket? _);
            _accountTokens.TryRemove(kv.Value.AccountId, out var _);
            ReleaseHeldPlayerIfAny(kv.Value.AccountId, ReconnectHoldReasonExpired);
        }

        foreach (var kv in _revokedResumeTokens)
        {
            if (kv.Value.ExpiresAtUtc > now)
                continue;

            _revokedResumeTokens.TryRemove(kv.Key, out RevokedResumeTicket? _);
        }
    }

    private void DisconnectDuplicateSessions(ConnectionSession replacementSession, Guid accountId)
    {
        var duplicates = _sessions.Values
            .Where(existing =>
                existing.ConnectionId != replacementSession.ConnectionId &&
                existing.IsAuthenticated &&
                existing.PlayerId == accountId)
            .ToArray();

        foreach (var duplicate in duplicates)
        {
            if (_worldManager.IsOwnedByConnection(accountId, duplicate.ConnectionId))
            {
                Logger.Info(
                    $"Replacing active session for account {accountId}. " +
                    $"OldConnectionId={duplicate.ConnectionId}, NewConnectionId={replacementSession.ConnectionId}");
                _runtimeSaveService.FlushPlayerAsync(accountId).GetAwaiter().GetResult();
                _worldManager.RemovePlayer(accountId);
            }

            if (!string.IsNullOrWhiteSpace(duplicate.ResumeToken))
            {
                _resumeTickets.TryRemove(duplicate.ResumeToken, out ResumeTicket? _);
                _revokedResumeTokens[duplicate.ResumeToken] = new RevokedResumeTicket(
                    MessageCode.AccountLoggedInElsewhere,
                    DateTime.UtcNow + _resumeWindow);
            }

            Send(duplicate.ConnectionId, new SessionTerminationPacket
            {
                Code = MessageCode.AccountLoggedInElsewhere,
                Message = AccountLoggedInElsewhereMessage
            });
            duplicate.ResumeToken = null;
            duplicate.Peer.Disconnect();
        }
    }

    private void RevokeResumeToken(string resumeToken, MessageCode code)
    {
        if (string.IsNullOrWhiteSpace(resumeToken))
            return;

        _resumeTickets.TryRemove(resumeToken, out ResumeTicket? _);
        _revokedResumeTokens[resumeToken] = new RevokedResumeTicket(code, DateTime.UtcNow + _resumeWindow);
    }

    private void ReleaseHeldPlayerIfAny(Guid accountId, string reason)
    {
        if (!_worldManager.TryGetPlayer(accountId, out var player))
            return;

        var hasActiveSession = _sessions.Values.Any(existing =>
            existing.IsAuthenticated &&
            existing.PlayerId == accountId &&
            existing.Player is not null);
        if (hasActiveSession)
            return;

        Logger.Info(
            $"Releasing held runtime player for account {accountId} because {reason}. " +
            $"MapId={player.MapId}, InstanceId={player.InstanceId}, ZoneIndex={player.ZoneIndex}");
        _runtimeSaveService.FlushPlayerAsync(accountId).GetAwaiter().GetResult();
        _worldManager.RemovePlayer(accountId);
    }

    private static void WaitForInboundProcessors(IEnumerable<ConnectionSession> sessions)
    {
        foreach (var session in sessions)
        {
            WaitForInboundProcessor(session);
        }
    }

    private static void WaitForInboundProcessor(ConnectionSession session)
    {
        if (session.InboundProcessorTask is null)
            return;

        try
        {
            session.InboundProcessorTask.GetAwaiter().GetResult();
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception)
        {
            // The processor already logged its own failure.
        }
    }

    private sealed record ResumeTicket(Guid AccountId, bool IsConnected, DateTime ExpiresAtUtc);
    private sealed record RevokedResumeTicket(MessageCode Code, DateTime ExpiresAtUtc);
}

