using GameShared.Diagnostics;
using GameShared.Logging;
using GameShared.Packets;
using LiteNetLib;

internal static class PacketReplayTool
{
    public static bool TryRun(string[] args)
    {
        var replayPath = ResolveReplayPath(args);
        if (replayPath is null)
            return false;

        var host = GetStringArg(args, "--host=") ?? "127.0.0.1";
        var port = GetIntArg(args, "--port=", 7777);

        Replay(replayPath, host, port);
        return true;
    }

    private static void Replay(string replayPath, string host, int port)
    {
        if (!File.Exists(replayPath))
        {
            Logger.Error($"Replay file not found: {replayPath}");
            return;
        }

        var raw = File.ReadAllText(replayPath);
        if (!PacketIncidentCapture.TryParse(raw, out var incident) || incident is null)
        {
            Logger.Error("Replay file is not a valid PacketIncidentRecord.");
            return;
        }

        if (string.IsNullOrWhiteSpace(incident.PacketPayloadBase64))
        {
            Logger.Error("Replay file has empty PacketPayloadBase64.");
            return;
        }

        byte[] payload;
        try
        {
            payload = Convert.FromBase64String(incident.PacketPayloadBase64);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "PacketPayloadBase64 is invalid.");
            return;
        }

        var method = ParseDeliveryMethod(incident.DeliveryMethod);
        var listener = new ReplayClientListener(payload, method, incident.PacketType);
        var netManager = new NetManager(listener)
        {
            AutoRecycle = true
        };

        if (!netManager.Start())
        {
            Logger.Error("Failed to start replay client.");
            return;
        }

        Logger.Info($"Replay mode: connecting to {host}:{port} with packet {incident.PacketType}.");
        netManager.Connect(host, port, string.Empty);

        var started = DateTime.UtcNow;
        while (!listener.Completed && (DateTime.UtcNow - started).TotalSeconds < 15)
        {
            netManager.PollEvents();
            Thread.Sleep(15);
        }

        netManager.Stop();
        Logger.Info("Replay mode finished.");
    }

    private static DeliveryMethod ParseDeliveryMethod(string? raw)
    {
        if (Enum.TryParse<DeliveryMethod>(raw, ignoreCase: true, out var parsed))
            return parsed;

        return DeliveryMethod.ReliableOrdered;
    }

    private static string? ResolveReplayPath(string[] args)
    {
        var explicitPath = GetStringArg(args, "--replayFile=");
        if (!string.IsNullOrWhiteSpace(explicitPath))
            return explicitPath;

        foreach (var arg in args)
        {
            if (string.Equals(arg, "--replay", StringComparison.OrdinalIgnoreCase))
                return "dataPacket.json";
        }

        // Convenience mode: if dataPacket.json exists, auto-run replay.
        const string defaultPath = "dataPacket.json";
        return File.Exists(defaultPath) ? defaultPath : null;
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

            if (int.TryParse(arg[prefix.Length..], out var value))
                return value;
        }

        return defaultValue;
    }

    private sealed class ReplayClientListener : INetEventListener
    {
        private readonly byte[] _payload;
        private readonly DeliveryMethod _method;
        private readonly string _packetType;
        private readonly object _sync = new();
        private bool _completed;

        public bool Completed
        {
            get
            {
                lock (_sync)
                {
                    return _completed;
                }
            }
        }

        public ReplayClientListener(byte[] payload, DeliveryMethod method, string packetType)
        {
            _payload = payload;
            _method = method;
            _packetType = packetType;
        }

        public void OnPeerConnected(NetPeer peer)
        {
            peer.Send(_payload, _method);
            Logger.Info($"Replay packet sent. Type={_packetType}, Bytes={_payload.Length}, Delivery={_method}.");
        }

        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
        {
            var bytes = reader.GetRemainingBytes();
            reader.Recycle();

            var packet = PacketSerializer.Deserialize(bytes);
            if (packet is null)
            {
                Logger.Info($"Replay response: deserialize failed. ByteLength={bytes.Length}");
            }
            else
            {
                Logger.Info($"Replay response packet: {packet.GetType().Name}");
            }

            MarkCompleted();
        }

        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            Logger.Info($"Replay disconnected. Reason={disconnectInfo.Reason}");
            MarkCompleted();
        }

        public void OnNetworkError(System.Net.IPEndPoint endPoint, System.Net.Sockets.SocketError socketError)
        {
            Logger.Error($"Replay network error: {socketError}");
            MarkCompleted();
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

        private void MarkCompleted()
        {
            lock (_sync)
            {
                _completed = true;
            }
        }
    }
}
