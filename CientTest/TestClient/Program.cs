using System;
using System.Threading;
using LiteNetLib;
using LiteNetLib.Utils;

enum PacketType : byte
{
    Register = 1,
    RegisterResult = 2,
    Login = 3,
    LoginResult = 4
}

class AuthClientListener : INetEventListener
{
    private NetPeer? _serverPeer;

    private readonly object _sync = new();
    private bool _registerResultReceived;
    private bool _loginResultReceived;

    public bool LoginFinished
    {
        get
        {
            lock (_sync)
            {
                return _loginResultReceived;
            }
        }
    }

    public AuthClientListener()
    {
    }

    public void OnPeerConnected(NetPeer peer)
    {
        Console.WriteLine("Connected to server");
        _serverPeer = peer;

        // Send RegisterPacket
        var writer = new NetDataWriter();
        writer.Put((byte)PacketType.Register);
        writer.Put("testuser");          // Username
        writer.Put("123456");           // Password
        writer.Put("test@test.com");    // Email

        peer.Send(writer, DeliveryMethod.ReliableOrdered);
    }

    public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
    {
        var packetType = (PacketType)reader.GetByte();

        switch (packetType)
        {
            case PacketType.RegisterResult:
            {
                bool success = reader.GetBool();
                Console.WriteLine("Register result:");
                Console.WriteLine($"Success = {success}");

                lock (_sync)
                {
                    _registerResultReceived = true;
                }

                // After register result, send LoginPacket
                var writer = new NetDataWriter();
                writer.Put((byte)PacketType.Login);
                writer.Put("testuser");  // Username
                writer.Put("123456");    // Password
                peer.Send(writer, DeliveryMethod.ReliableOrdered);

                break;
            }
            case PacketType.LoginResult:
            {
                bool success = reader.GetBool();
                long accountId = reader.GetLong();

                Console.WriteLine("Login result:");
                Console.WriteLine($"Success = {success}");
                Console.WriteLine($"AccountId = {accountId}");

                lock (_sync)
                {
                    _loginResultReceived = true;
                }

                break;
            }
            default:
            {
                // Unknown or unhandled packet type; just ignore remaining data
                break;
            }
        }

        reader.Recycle();
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
        // Not needed for this simple test client
    }

    public void OnNetworkReceiveUnconnected(System.Net.IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
    {
        // Not used in this simple client
        reader.Recycle();
    }

    public void OnConnectionRequest(ConnectionRequest request)
    {
        // This is a client; we do not accept incoming connections
        request.Reject();
    }
}

class Program
{
    static void Main(string[] args)
    {
        const string serverAddress = "127.0.0.1";
        const int serverPort = 7777;
        const string connectionKey = "game";

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
        netManager.Connect(serverAddress, serverPort, connectionKey);

        // Main loop
        while (!authListener.LoginFinished)
        {
            netManager.PollEvents();
            Thread.Sleep(15);
        }

        // Give some time for any final packets, then stop
        Thread.Sleep(200);
        netManager.Stop();

        Console.WriteLine("Finished. Press any key to exit.");
        Console.ReadKey();
    }
}
