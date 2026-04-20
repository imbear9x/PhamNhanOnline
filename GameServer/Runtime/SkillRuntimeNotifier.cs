using GameServer.DTO;
using GameServer.Network.Interface;
using GameServer.Services;
using GameShared.Packets;
using GameServer.World;

namespace GameServer.Runtime;

public sealed class SkillRuntimeNotifier
{
    private readonly INetworkSender _network;

    public SkillRuntimeNotifier(INetworkSender network)
    {
        _network = network;
    }

    public void NotifyOwnedSkillsChanged(PlayerSession player, OwnedSkillsSnapshotDto snapshot)
    {
        _network.Send(player.ConnectionId, new OwnedSkillsChangedPacket
        {
            MaxLoadoutSlotCount = snapshot.MaxLoadoutSlotCount,
            Skills = snapshot.Skills.Select(static x => x.ToModel()).ToList(),
            LoadoutSlots = snapshot.LoadoutSlots.Select(static x => x.ToModel()).ToList()
        });
    }
}
