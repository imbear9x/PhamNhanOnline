using GameServer.World;
using LiteNetLib;

namespace GameServer.Network;

public sealed class ConnectionSession
{
    public NetPeer Peer { get; }
    public int ConnectionId => Peer.Id;

    public Guid PlayerId { get; set; }
    public Guid SelectedCharacterId { get; set; }
    public string? ResumeToken { get; set; }
    public PlayerSession? Player { get; set; }
    public bool IsAuthenticated { get; set; }


    public ConnectionSession(NetPeer peer)
    {
        Peer = peer;
        PlayerId = Guid.Empty;
        SelectedCharacterId = Guid.Empty;
    }
}
