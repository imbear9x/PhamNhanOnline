using GameShared.Packets;
using LiteNetLib;

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
        Console.WriteLine("Connected to server");

        var packet = new RegisterPacket
        {
            Username = "testuser",
            Password = "123456"
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
            Console.WriteLine($"Deserialize failed. ByteLength={bytes.Length}");
            return;
        }

        switch (packet)
        {
            case RegisterResultPacket registerResult:
            {
                Console.WriteLine("Register result:");
                Console.WriteLine($"Success = {registerResult.Success}");
                Console.WriteLine($"Error = {registerResult.Error}");

                var loginPacket = new LoginPacket
                {
                    Username = "testuser",
                    Password = "123456"
                };
                SendPacket(peer, loginPacket);
                break;
            }
            case LoginResultPacket loginResult:
            {
                Console.WriteLine("Login result:");
                Console.WriteLine($"Success = {loginResult.Success}");
                Console.WriteLine($"Error = {loginResult.Error}");
                Console.WriteLine($"AccountId = {loginResult.AccountId}");

                lock (_sync)
                {
                    _loginFinished = true;
                }
                break;
            }
            default:
            {
                Console.WriteLine($"Unhandled packet type: {packet.GetType().Name}");
                break;
            }
        }
    }

    private static void SendPacket(NetPeer peer, IPacket packet)
    {
        var data = PacketSerializer.Serialize(packet);
        peer.Send(data, DeliveryMethod.ReliableOrdered);
        Console.WriteLine($"Sent {packet.GetType().Name} (bytes={data.Length})");
    }

    public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        Console.WriteLine($"Disconnected from server. Reason: {disconnectInfo.Reason}");
    }

    public void OnNetworkError(System.Net.IPEndPoint endPoint, System.Net.Sockets.SocketError socketError)
    {
        Console.WriteLine($"Network error: {socketError}");
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
}

class Program
{
    static void Main(string[] args)
    {
        const string serverAddress = "127.0.0.1";
        const int serverPort = 7777;

        var authListener = new AuthClientListener();
        var netManager = new NetManager(authListener)
        {
            AutoRecycle = true
        };

        if (!netManager.Start())
        {
            Console.WriteLine("Failed to start LiteNetLib client.");
            return;
        }

        Console.WriteLine("Connecting to server...");
        netManager.Connect(serverAddress, serverPort, string.Empty);

        while (!authListener.LoginFinished)
        {
            netManager.PollEvents();
            Thread.Sleep(15);
        }

        Thread.Sleep(200);
        netManager.Stop();

        Console.WriteLine("Finished. Press any key to exit.");
        Console.ReadKey();
    }
}
