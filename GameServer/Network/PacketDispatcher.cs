using GameServer.Network.Packets;

namespace GameServer.Network;

public sealed class PacketDispatcher
{
    private readonly Handlers.RegisterHandler _registerHandler;
    private readonly Handlers.LoginHandler _loginHandler;

    public PacketDispatcher(Handlers.RegisterHandler registerHandler, Handlers.LoginHandler loginHandler)
    {
        _registerHandler = registerHandler;
        _loginHandler = loginHandler;
    }

    public void Dispatch(ConnectionSession session, IPacket packet)
    {
        switch (packet.PacketType)
        {
            case PacketType.Register:
                _ = _registerHandler.HandleAsync(session, (RegisterPacket)packet);
                break;

            case PacketType.Login:
                _ = _loginHandler.HandleAsync(session, (LoginPacket)packet);
                break;

            default:
                // Ignore unsupported packets for now.
                break;
        }
    }
}

