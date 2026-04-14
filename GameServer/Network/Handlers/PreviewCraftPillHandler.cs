using GameServer.DTO;
using GameServer.Exceptions;
using GameServer.Network.Interface;
using GameServer.Services;
using GameShared.Messages;
using GameShared.Packets;

namespace GameServer.Network.Handlers;

public sealed class PreviewCraftPillHandler : IPacketHandler<PreviewCraftPillPacket>
{
    private readonly PillRecipeService _pillRecipeService;
    private readonly AlchemyService _alchemyService;
    private readonly ItemService _itemService;
    private readonly AlchemyModelBuilder _modelBuilder;
    private readonly INetworkSender _network;

    public PreviewCraftPillHandler(
        PillRecipeService pillRecipeService,
        AlchemyService alchemyService,
        ItemService itemService,
        AlchemyModelBuilder modelBuilder,
        INetworkSender network)
    {
        _pillRecipeService = pillRecipeService;
        _alchemyService = alchemyService;
        _itemService = itemService;
        _modelBuilder = modelBuilder;
        _network = network;
    }

    public async Task HandleAsync(ConnectionSession session, PreviewCraftPillPacket packet)
    {
        if (session.Player is null)
        {
            _network.Send(session.ConnectionId, new PreviewCraftPillResultPacket
            {
                Success = false,
                Code = MessageCode.CharacterMustEnterWorld
            });
            return;
        }

        var playerId = session.Player.CharacterData.CharacterId;
        try
        {
            await _pillRecipeService.GetRecipeDetailAsync(playerId, packet.PillRecipeTemplateId!.Value);

            var validation = await _alchemyService.ValidateCraftPillAsync(
                playerId,
                packet.PillRecipeTemplateId.Value,
                packet.RequestedCraftCount ?? 1,
                packet.SelectedPlayerItemIds,
                packet.SelectedOptionalInputs);
            var inventory = await _itemService.GetInventoryAsync(playerId);
            var inventoryByPlayerItemId = inventory.ToDictionary(static item => item.PlayerItemId);

            _network.Send(session.ConnectionId, new PreviewCraftPillResultPacket
            {
                Success = true,
                Code = validation.Success ? MessageCode.None : MessageCode.AlchemyInputInvalid,
                FailureReason = validation.FailureReason,
                Preview = _modelBuilder.BuildCraftPreviewModel(
                    packet.PillRecipeTemplateId.Value,
                    validation,
                    inventoryByPlayerItemId)
            });
        }
        catch (GameException ex)
        {
            _network.Send(session.ConnectionId, new PreviewCraftPillResultPacket
            {
                Success = false,
                Code = ex.Code
            });
        }
    }
}
