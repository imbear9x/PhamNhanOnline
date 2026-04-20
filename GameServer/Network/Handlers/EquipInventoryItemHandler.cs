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

public sealed class EquipInventoryItemHandler : IPacketHandler<EquipInventoryItemPacket>
{
    private readonly GameDb _db;
    private readonly EquipmentService _equipmentService;
    private readonly GameConfigValues _gameConfig;
    private readonly SkillService _skillService;
    private readonly SkillRuntimeNotifier _skillNotifier;
    private readonly CharacterFinalStatService _characterFinalStatService;
    private readonly ItemService _itemService;
    private readonly GameTimeService _gameTimeService;
    private readonly INetworkSender _network;

    public EquipInventoryItemHandler(
        GameDb db,
        EquipmentService equipmentService,
        GameConfigValues gameConfig,
        SkillService skillService,
        SkillRuntimeNotifier skillNotifier,
        CharacterFinalStatService characterFinalStatService,
        ItemService itemService,
        GameTimeService gameTimeService,
        INetworkSender network)
    {
        _db = db;
        _equipmentService = equipmentService;
        _gameConfig = gameConfig;
        _skillService = skillService;
        _skillNotifier = skillNotifier;
        _characterFinalStatService = characterFinalStatService;
        _itemService = itemService;
        _gameTimeService = gameTimeService;
        _network = network;
    }

    public async Task HandleAsync(ConnectionSession session, EquipInventoryItemPacket packet)
    {
        if (session.Player is null)
        {
            _network.Send(session.ConnectionId, new EquipInventoryItemResultPacket
            {
                Success = false,
                Code = MessageCode.CharacterMustEnterWorld
            });
            return;
        }

        try
        {
            var slotIndex = packet.Slot!.Value;
            if (slotIndex <= 0)
            {
                _network.Send(session.ConnectionId, new EquipInventoryItemResultPacket
                {
                    Success = false,
                    Code = MessageCode.EquipmentSlotInvalid
                });
                return;
            }

            OwnedSkillsSnapshotDto? changedSkillSnapshot = null;
            CharacterRuntimeSnapshot runtimeSnapshot;
            IReadOnlyList<InventoryItemView> items;
            await using (var tx = await _db.BeginTransactionAsync())
            {
                await _equipmentService.EquipItemAsync(session.Player.CharacterData.CharacterId, packet.PlayerItemId!.Value, slotIndex);
                var skillSync = await _skillService.SyncEquipmentGrantedSkillsAsync(session.Player.CharacterData.CharacterId);
                if (skillSync.Changed)
                    changedSkillSnapshot = skillSync.Snapshot;

                runtimeSnapshot = await _characterFinalStatService.ApplyAuthoritativeFinalStatsAsync(session.Player);
                items = await _itemService.GetInventoryAsync(session.Player.CharacterData.CharacterId);
                await tx.CommitAsync();
            }

            _network.Send(session.ConnectionId, new EquipInventoryItemResultPacket
            {
                Success = true,
                Code = MessageCode.None,
                EquipmentSlotCount = _gameConfig.CharacterEquipmentSlotCount,
                Items = items.Select(x => x.ToModel()).ToList(),
                BaseStats = runtimeSnapshot.BaseStats.ToModel(),
                CurrentState = runtimeSnapshot.CurrentState.ToModel(session.Player.CharacterData, runtimeSnapshot.BaseStats, _gameTimeService.GetCurrentSnapshot())
            });

            if (changedSkillSnapshot.HasValue)
                _skillNotifier.NotifyOwnedSkillsChanged(session.Player, changedSkillSnapshot.Value);
        }
        catch (GameException ex)
        {
            _network.Send(session.ConnectionId, new EquipInventoryItemResultPacket
            {
                Success = false,
                Code = ex.Code
            });
        }
    }
}
