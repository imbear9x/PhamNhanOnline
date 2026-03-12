using GameShared.Diagnostics;
using GameShared.Logging;
using GameShared.Messages;
using GameShared.Packets;
using LiteNetLib;
using System.Text.Json;

class AuthClientListener : INetEventListener
{
    private readonly object _sync = new();
    private bool _loginFinished;

    public bool LoginFinished
    {
        get
        {
            lock (_sync)
            {
                return _loginFinished;
            }
        }
    }

    public void OnPeerConnected(NetPeer peer)
    {
        Logger.Info("Connected to server.");

        var packet = new RegisterPacket
        {
            Username = "testuser",
            Password = "Test@1234",
            // Email = "testuser@example.com"
        };

        SendPacket(peer, packet);
    }

    public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
    {
        var bytes = reader.GetRemainingBytes();
        reader.Recycle();

        var packet = PacketSerializer.Deserialize(bytes);
        if (packet is null)
        {
            Logger.Error($"Deserialize failed. ByteLength={bytes.Length}");
            return;
        }

        try
        {
            switch (packet)
            {
                case RegisterResultPacket registerResult:
                {
                    Logger.Info($"Register result: Success={registerResult.Success}, Code={registerResult.Code}");

                    // If account already exists, continue to login to complete auth flow test.
                    if (registerResult.Success is true || registerResult.Code == MessageCode.LoginAlreadyExists)
                    {
                        var loginPacket = new LoginPacket
                        {
                            Username = "testuser",
                            Password = "Test@1234"
                        };
                        SendPacket(peer, loginPacket);
                    }
                    else
                    {
                        lock (_sync)
                        {
                            _loginFinished = true;
                        }
                    }
                    break;
                }
                case LoginResultPacket loginResult:
                {
                    Logger.Info($"Login result: Success={loginResult.Success}, Code={loginResult.Code}, AccountId={loginResult.AccountId}");

                    lock (_sync)
                    {
                        _loginFinished = true;
                    }
                    break;
                }
                default:
                {
                    Logger.Info($"Unhandled packet type: {packet.GetType().Name}");
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, $"Client packet handler error: {packet.GetType().Name}");
            PacketIncidentCapture.Log(new PacketIncidentRecord
            {
                CapturedAtUtc = DateTime.UtcNow,
                Source = "Client",
                IncidentType = "ClientPacketHandleException",
                ConnectionId = peer.Id,
                RemoteEndPoint = $"{peer.Address}:{peer.Port}",
                ChannelNumber = channelNumber,
                DeliveryMethod = deliveryMethod.ToString(),
                PacketType = packet.GetType().FullName ?? packet.GetType().Name,
                PacketJson = TrySerializePacket(packet),
                PacketPayloadBase64 = Convert.ToBase64String(bytes),
                ExceptionType = ex.GetType().FullName ?? ex.GetType().Name,
                ExceptionMessage = ex.Message,
                ExceptionStackTrace = ex.StackTrace
            });

            lock (_sync)
            {
                _loginFinished = true;
            }
        }
    }

    private static void SendPacket(NetPeer peer, IPacket packet)
    {
        var data = PacketSerializer.Serialize(packet);
        peer.Send(data, DeliveryMethod.ReliableOrdered);
        Logger.Info($"Sent {packet.GetType().Name} (bytes={data.Length})");
    }

    public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        Logger.Info($"Disconnected from server. Reason: {disconnectInfo.Reason}");
    }

    public void OnNetworkError(System.Net.IPEndPoint endPoint, System.Net.Sockets.SocketError socketError)
    {
        Logger.Error($"Network error: {socketError}");
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
}

class Program
{
    static void Main(string[] args)
    {
        Logger.Configure(GetLogRootPath(args));
        PacketIncidentCapture.Configure(GetBoolArg(args, "--packetIncidentCapture=", true));
        Logger.Info($"Packet incident capture enabled: {PacketIncidentCapture.Enabled}");

        if (PacketReplayTool.TryRun(args))
            return;

        const string serverAddress = "127.0.0.1";
        const int serverPort = 7777;

        var authListener = new AuthClientListener();
        var netManager = new NetManager(authListener)
        {
            AutoRecycle = true
        };

        if (!netManager.Start())
        {
            Logger.Error("Failed to start LiteNetLib client.");
            return;
        }

        Logger.Info("Connecting to server...");
        netManager.Connect(serverAddress, serverPort, string.Empty);

        while (!authListener.LoginFinished)
        {
            netManager.PollEvents();
            Thread.Sleep(15);
        }

        Thread.Sleep(200);
        netManager.Stop();

        Logger.Info("Finished client run.");
    }

    private static string? GetLogRootPath(string[] args)
    {
        const string prefix = "--logRoot=";
        foreach (var arg in args)
        {
            if (arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return arg[prefix.Length..];
        }

        return null;
    }

    private static bool GetBoolArg(string[] args, string prefix, bool defaultValue)
    {
        foreach (var arg in args)
        {
            if (!arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                continue;

            var raw = arg[prefix.Length..];
            if (bool.TryParse(raw, out var parsed))
                return parsed;
        }

        return defaultValue;
    }
}
