using GameServer.DTO;
using GameServer.Network.Interface;
using GameServer.Services;
using GameShared.Messages;
using GameShared.Packets;

namespace GameServer.Network.Handlers;

public sealed class GetOwnedSkillsHandler : IPacketHandler<GetOwnedSkillsPacket>
{
    private readonly SkillService _skillService;
    private readonly INetworkSender _network;

    public GetOwnedSkillsHandler(
        SkillService skillService,
        INetworkSender network)
    {
        _skillService = skillService;
        _network = network;
    }

    public async Task HandleAsync(ConnectionSession session, GetOwnedSkillsPacket packet)
    {
        if (session.Player is null)
        {
            _network.Send(session.ConnectionId, new GetOwnedSkillsResultPacket
            {
                Success = false,
                Code = MessageCode.CharacterMustEnterWorld
            });
            return;
        }

        var snapshot = await _skillService.GetOwnedSkillsAsync(session.Player.CharacterData.CharacterId);
        _network.Send(session.ConnectionId, new GetOwnedSkillsResultPacket
        {
            Success = true,
            Code = MessageCode.None,
            MaxLoadoutSlotCount = snapshot.MaxLoadoutSlotCount,
            Skills = snapshot.Skills.Select(static x => x.ToModel()).ToList(),
            LoadoutSlots = snapshot.LoadoutSlots.Select(static x => x.ToModel()).ToList()
        });
    }
}
