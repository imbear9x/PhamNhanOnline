namespace GameServer.Network.Packets;

public enum PacketType : byte
{
    Unknown        = 0,
    Register       = 1,
    RegisterResult = 2,
    Login          = 3,
    LoginResult    = 4
}

