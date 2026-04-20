using GameServer.DTO;
using GameServer.Exceptions;
using GameServer.Network.Interface;
using GameServer.Runtime;
using GameServer.Services;
using GameServer.Time;
using GameServer.Config;
using GameShared.Messages;
using GameShared.Packets;

namespace GameServer.Network.Handlers;

public sealed class UnequipInventoryItemHandler : IPacketHandler<UnequipInventoryItemPacket>
{
    private readonly EquipmentActionService _equipmentActionService;
    private readonly GameConfigValues _gameConfig;
    private readonly SkillRuntimeNotifier _skillNotifier;
    private readonly GameTimeService _gameTimeService;
    private readonly INetworkSender _network;

    public UnequipInventoryItemHandler(
        EquipmentActionService equipmentActionService,
        GameConfigValues gameConfig,
        SkillRuntimeNotifier skillNotifier,
        GameTimeService gameTimeService,
        INetworkSender network)
    {
        _equipmentActionService = equipmentActionService;
        _gameConfig = gameConfig;
        _skillNotifier = skillNotifier;
        _gameTimeService = gameTimeService;
        _network = network;
    }

    public async Task HandleAsync(ConnectionSession session, UnequipInventoryItemPacket packet)
    {
        if (session.Player is null)
        {
            _network.Send(session.ConnectionId, new UnequipInventoryItemResultPacket
            {
                Success = false,
                Code = MessageCode.CharacterMustEnterWorld
            });
            return;
        }

        var slotIndex = packet.Slot!.Value;
        if (slotIndex <= 0)
        {
            _network.Send(session.ConnectionId, new UnequipInventoryItemResultPacket
            {
                Success = false,
                Code = MessageCode.EquipmentSlotInvalid
            });
            return;
        }

        try
        {
            var result = await _equipmentActionService.UnequipAsync(session.Player, slotIndex);

            _network.Send(session.ConnectionId, new UnequipInventoryItemResultPacket
            {
                Success = true,
                Code = MessageCode.None,
                EquipmentSlotCount = _gameConfig.CharacterEquipmentSlotCount,
                Items = result.Items.Select(x => x.ToModel()).ToList(),
                BaseStats = result.RuntimeSnapshot.BaseStats.ToModel(),
                CurrentState = result.RuntimeSnapshot.CurrentState.ToModel(session.Player.CharacterData, result.RuntimeSnapshot.BaseStats, _gameTimeService.GetCurrentSnapshot())
            });

            if (result.ChangedSkillSnapshot.HasValue)
                _skillNotifier.NotifyOwnedSkillsChanged(session.Player, result.ChangedSkillSnapshot.Value);
        }
        catch (GameException ex)
        {
            _network.Send(session.ConnectionId, new UnequipInventoryItemResultPacket
            {
                Success = false,
                Code = ex.Code
            });
        }
    }
}
