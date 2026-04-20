using GameServer.DTO;
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
    private readonly GameDb _db;
    private readonly EquipmentService _equipmentService;
    private readonly GameConfigValues _gameConfig;
    private readonly SkillService _skillService;
    private readonly SkillRuntimeNotifier _skillNotifier;
    private readonly CharacterFinalStatService _characterFinalStatService;
    private readonly ItemService _itemService;
    private readonly GameTimeService _gameTimeService;
    private readonly INetworkSender _network;

    public UnequipInventoryItemHandler(
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

        bool changed;
        CharacterRuntimeSnapshot runtimeSnapshot;
        IReadOnlyList<InventoryItemView> items;
        OwnedSkillsSnapshotDto? changedSkillSnapshot = null;
        await using (var tx = await _db.BeginTransactionAsync())
        {
            changed = await _equipmentService.UnequipItemAsync(session.Player.CharacterData.CharacterId, slotIndex);
            if (!changed)
            {
                await tx.RollbackAsync();
                _network.Send(session.ConnectionId, new UnequipInventoryItemResultPacket
                {
                    Success = false,
                    Code = MessageCode.EquipmentSlotEmpty
                });
                return;
            }

            var skillSync = await _skillService.SyncEquipmentGrantedSkillsAsync(session.Player.CharacterData.CharacterId);
            if (skillSync.Changed)
                changedSkillSnapshot = skillSync.Snapshot;

            runtimeSnapshot = await _characterFinalStatService.ApplyAuthoritativeFinalStatsAsync(session.Player);
            items = await _itemService.GetInventoryAsync(session.Player.CharacterData.CharacterId);
            await tx.CommitAsync();
        }

        _network.Send(session.ConnectionId, new UnequipInventoryItemResultPacket
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
}
