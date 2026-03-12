using GameShared.Logging;
using GameShared.Messages;
using GameShared.Packets;
using LiteNetLib;

internal sealed class ReconnectServer : INetEventListener
{
    private readonly NetManager _netManager;
    private readonly string _host;
    private readonly int _port;
    private readonly string _username;
    private readonly string _password;
    private readonly int _forceDisconnectAfterSec;
    private readonly object _sync = new();

    private NetPeer? _peer;
    private string? _resumeToken;
    private bool _isAuthenticated;
    private bool _reconnectPending;
    private DateTime _nextReconnectAtUtc;
    private int _reconnectAttempt;
    private bool _forceDisconnectTriggered;
    private DateTime _startedAtUtc;

    public static void Run(string[] args)
    {
        var host = GetStringArg(args, "--host=") ?? "127.0.0.1";
        var port = GetIntArg(args, "--port=", 7777);
        var username = GetStringArg(args, "--username=") ?? "testuser";
        var password = GetStringArg(args, "--password=") ?? "TestPass";
        var runSeconds = Math.Max(10, GetIntArg(args, "--runSec=", 60));
        var forceDisconnectAfterSec = Math.Max(0, GetIntArg(args, "--forceDisconnectAfterSec=", 10));

        var reconnectServer = new ReconnectServer(
            host,
            port,
            username,
            password,
            forceDisconnectAfterSec);

        reconnectServer.Run(TimeSpan.FromSeconds(runSeconds));
    }

    private ReconnectServer(
        string host,
        int port,
        string username,
        string password,
        int forceDisconnectAfterSec)
    {
        _host = host;
        _port = port;
        _username = username;
        _password = password;
        _forceDisconnectAfterSec = forceDisconnectAfterSec;

        _netManager = new NetManager(this)
        {
            AutoRecycle = true
        };
    }

    public void Run(TimeSpan runDuration)
    {
        if (!_netManager.Start())
        {
            Logger.Error("ReconnectServer: failed to start LiteNetLib client.");
            return;
        }

        _startedAtUtc = DateTime.UtcNow;
        Logger.Info($"ReconnectServer: started. Target={_host}:{_port}, RunDuration={runDuration.TotalSeconds:0}s.");
        ConnectNow();

        while ((DateTime.UtcNow - _startedAtUtc) < runDuration)
        {
            _netManager.PollEvents();
            TickReconnect();
            TickForceDisconnect();
            Thread.Sleep(15);
        }

        _netManager.Stop();
        Logger.Info("ReconnectServer: finished.");
    }

    public void OnPeerConnected(NetPeer peer)
    {
        lock (_sync)
        {
            _peer = peer;
            _reconnectAttempt = 0;
            _reconnectPending = false;
        }

        Logger.Info("ReconnectServer: connected.");

        if (!string.IsNullOrWhiteSpace(_resumeToken))
        {
            SendPacket(peer, new ReconnectPacket { ResumeToken = _resumeToken });
            Logger.Info("ReconnectServer: sent ReconnectPacket.");
            return;
        }

        SendPacket(peer, new LoginPacket
        {
            Username = _username,
            Password = _password
        });
        Logger.Info("ReconnectServer: sent LoginPacket.");
    }

    public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        lock (_sync)
        {
            _peer = null;
            _isAuthenticated = false;
        }

        Logger.Info($"ReconnectServer: disconnected. Reason={disconnectInfo.Reason}.");
        ScheduleReconnect();
    }

    public void OnNetworkError(System.Net.IPEndPoint endPoint, System.Net.Sockets.SocketError socketError)
    {
        Logger.Error($"ReconnectServer: network error {socketError}.");
        ScheduleReconnect();
    }

    public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
    {
        var bytes = reader.GetRemainingBytes();
        reader.Recycle();

        var packet = PacketSerializer.Deserialize(bytes);
        if (packet is null)
        {
            Logger.Error($"ReconnectServer: deserialize failed. Bytes={bytes.Length}.");
            return;
        }

        switch (packet)
        {
            case LoginResultPacket loginResult:
            {
                Logger.Info(
                    $"ReconnectServer: LoginResult Success={loginResult.Success}, Code={loginResult.Code}, AccountId={loginResult.AccountId}.");

                if (loginResult.Success is true)
                {
                    lock (_sync)
                    {
                        _isAuthenticated = true;
                        _resumeToken = loginResult.ResumeToken;
                    }
                    Logger.Info($"ReconnectServer: resume token captured = {MaskToken(loginResult.ResumeToken)}.");
                }
                break;
            }
            case ReconnectResultPacket reconnectResult:
            {
                Logger.Info(
                    $"ReconnectServer: ReconnectResult Success={reconnectResult.Success}, Code={reconnectResult.Code}, AccountId={reconnectResult.AccountId}.");

                if (reconnectResult.Success is true)
                {
                    lock (_sync)
                    {
                        _isAuthenticated = true;
                        if (!string.IsNullOrWhiteSpace(reconnectResult.ResumeToken))
                            _resumeToken = reconnectResult.ResumeToken;
                    }
                    Logger.Info($"ReconnectServer: resume success with token {MaskToken(_resumeToken)}.");
                }
                else if (reconnectResult.Code is MessageCode.ReconnectSessionExpired or MessageCode.ReconnectTokenInvalid)
                {
                    lock (_sync)
                    {
                        _resumeToken = null;
                    }

                    SendPacket(peer, new LoginPacket
                    {
                        Username = _username,
                        Password = _password
                    });
                    Logger.Info("ReconnectServer: reconnect failed, fallback to LoginPacket.");
                }
                break;
            }
            default:
            {
                Logger.Info($"ReconnectServer: received unhandled packet {packet.GetType().Name}.");
                break;
            }
        }
    }

    public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
    {
    }

    public void OnNetworkReceiveUnconnected(System.Net.IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
    {
        reader.Recycle();
    }

    public void OnConnectionRequest(ConnectionRequest request)
    {
        request.Reject();
    }

    private void TickReconnect()
    {
        DateTime reconnectAtUtc;
        lock (_sync)
        {
            if (!_reconnectPending)
                return;

            reconnectAtUtc = _nextReconnectAtUtc;
        }

        if (DateTime.UtcNow < reconnectAtUtc)
            return;

        ConnectNow();
    }

    private void TickForceDisconnect()
    {
        if (_forceDisconnectAfterSec <= 0)
            return;

        lock (_sync)
        {
            if (_forceDisconnectTriggered || !_isAuthenticated || _peer is null)
                return;

            var elapsed = DateTime.UtcNow - _startedAtUtc;
            if (elapsed.TotalSeconds < _forceDisconnectAfterSec)
                return;

            _forceDisconnectTriggered = true;
            Logger.Info("ReconnectServer: force disconnect for reconnect test.");
            _peer.Disconnect();
        }
    }

    private void ConnectNow()
    {
        lock (_sync)
        {
            _reconnectPending = false;
        }

        Logger.Info($"ReconnectServer: connecting to {_host}:{_port}...");
        _netManager.Connect(_host, _port, string.Empty);
    }

    private void ScheduleReconnect()
    {
        lock (_sync)
        {
            if (_reconnectPending)
                return;

            _reconnectAttempt++;
            var delaySeconds = Math.Min(15, (int)Math.Pow(2, Math.Min(_reconnectAttempt, 4)));
            _nextReconnectAtUtc = DateTime.UtcNow.AddSeconds(delaySeconds);
            _reconnectPending = true;
            Logger.Info($"ReconnectServer: scheduled reconnect in {delaySeconds}s (attempt {_reconnectAttempt}).");
        }
    }

    private static void SendPacket(NetPeer peer, IPacket packet)
    {
        var data = PacketSerializer.Serialize(packet);
        peer.Send(data, DeliveryMethod.ReliableOrdered);
    }

    private static string MaskToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return "<null>";

        if (token.Length <= 8)
            return token;

        return $"{token[..4]}...{token[^4..]}";
    }

    private static string? GetStringArg(string[] args, string prefix)
    {
        foreach (var arg in args)
        {
            if (arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return arg[prefix.Length..];
        }

        return null;
    }

    private static int GetIntArg(string[] args, string prefix, int defaultValue)
    {
        foreach (var arg in args)
        {
            if (!arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                continue;

            if (int.TryParse(arg[prefix.Length..], out var parsed))
                return parsed;
        }

        return defaultValue;
    }
}
