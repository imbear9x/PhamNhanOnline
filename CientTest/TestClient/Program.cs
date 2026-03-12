using GameShared.Diagnostics;
using GameShared.Logging;
using GameShared.Messages;
using GameShared.Packets;
using LiteNetLib;
using System.Text.Json;

class AuthClientListener : INetEventListener
{
    private const string TestUsername = "khoivu";
    private const string InitialPassword = "t##AAAAadmin";
    private const string TargetPassword = "hihi@admin";

    private readonly object _sync = new();
    private bool _loginFinished;
    private string _currentKnownPassword = InitialPassword;
    private string? _pendingChangeNewPassword;
    private bool _needsSecondChangeToTarget;

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
            Username = TestUsername,
            Password = InitialPassword,
            Email = "khoivu@example.com"
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

                    // If account already exists, continue login to run password-change flow.
                    if (registerResult.Success is true || registerResult.Code == MessageCode.LoginAlreadyExists)
                    {
                        SendLogin(peer, _currentKnownPassword);
                    }
                    else
                    {
                        MarkLoginFinished();
                    }
                    break;
                }
                case LoginResultPacket loginResult:
                {
                    Logger.Info($"Login result: Success={loginResult.Success}, Code={loginResult.Code}, AccountId={loginResult.AccountId}");

                    if (loginResult.Success is true)
                    {
                        if (string.Equals(_currentKnownPassword, InitialPassword, StringComparison.Ordinal))
                        {
                            SendChangePassword(peer, InitialPassword, TargetPassword);
                        }
                        else
                        {
                            // Account may already be on target password from previous run. Change back once,
                            // then apply requested final target password so the full flow still executes.
                            _needsSecondChangeToTarget = true;
                            SendChangePassword(peer, TargetPassword, InitialPassword);
                        }
                    }
                    else if (loginResult.Code == MessageCode.InvalidCredentials &&
                             string.Equals(_currentKnownPassword, InitialPassword, StringComparison.Ordinal))
                    {
                        Logger.Info("Initial password login failed. Retrying with target password.");
                        _currentKnownPassword = TargetPassword;
                        SendLogin(peer, _currentKnownPassword);
                    }
                    else
                    {
                        MarkLoginFinished();
                    }
                    break;
                }
                case ChangePasswordResultPacket changeResult:
                {
                    Logger.Info($"Change password result: Success={changeResult.Success}, Code={changeResult.Code}");

                    if (changeResult.Success is true)
                    {
                        if (!string.IsNullOrWhiteSpace(_pendingChangeNewPassword))
                            _currentKnownPassword = _pendingChangeNewPassword;

                        if (_needsSecondChangeToTarget)
                        {
                            _needsSecondChangeToTarget = false;
                            SendChangePassword(peer, InitialPassword, TargetPassword);
                            break;
                        }
                    }

                    MarkLoginFinished();
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

            MarkLoginFinished();
        }
    }

    private static void SendPacket(NetPeer peer, IPacket packet)
    {
        var data = PacketSerializer.Serialize(packet);
        peer.Send(data, DeliveryMethod.ReliableOrdered);
        Logger.Info($"Sent {packet.GetType().Name} (bytes={data.Length})");
    }

    private void SendLogin(NetPeer peer, string password)
    {
        var loginPacket = new LoginPacket
        {
            Username = TestUsername,
            Password = password
        };
        SendPacket(peer, loginPacket);
    }

    private void SendChangePassword(NetPeer peer, string oldPassword, string newPassword)
    {
        _pendingChangeNewPassword = newPassword;

        var changePacket = new ChangePasswordPacket
        {
            Username = TestUsername,
            Password = oldPassword,
            NewPassword = newPassword
        };
        SendPacket(peer, changePacket);
    }

    public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        Logger.Info($"Disconnected from server. Reason: {disconnectInfo.Reason}");
        MarkLoginFinished();
    }

    public void OnNetworkError(System.Net.IPEndPoint endPoint, System.Net.Sockets.SocketError socketError)
    {
        Logger.Error($"Network error: {socketError}");
        MarkLoginFinished();
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

    private void MarkLoginFinished()
    {
        lock (_sync)
        {
            _loginFinished = true;
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

        if (GetBoolArg(args, "--reconnectMode=", false))
        {
            ReconnectServer.Run(args);
            return;
        }

        const string serverAddress = "127.0.0.1";
        const int serverPort = 7777;
        var timeoutSeconds = Math.Max(5, GetIntArg(args, "--timeoutSec=", 20));
        var waitForInput = GetBoolArg(args, "--waitForInput=", false);

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
        var startedAt = DateTime.UtcNow;

        while (!authListener.LoginFinished &&
               (DateTime.UtcNow - startedAt).TotalSeconds < timeoutSeconds)
        {
            netManager.PollEvents();
            Thread.Sleep(15);
        }

        if (!authListener.LoginFinished)
        {
            Logger.Error($"Client flow timed out after {timeoutSeconds}s without finishing login/register.");
        }

        netManager.Stop();

        Logger.Info("Finished client run.");

        if (waitForInput)
            Console.ReadLine();
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
