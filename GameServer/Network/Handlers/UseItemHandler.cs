using GameServer.DTO;
using GameServer.Exceptions;
using GameServer.Network.Interface;
using GameServer.Services;
using GameServer.Time;
using GameShared.Messages;
using GameShared.Packets;

namespace GameServer.Network.Handlers;

public sealed class UseItemHandler : IPacketHandler<UseItemPacket>
{
    private readonly ItemUseService _itemUseService;
    private readonly GameTimeService _gameTimeService;
    private readonly INetworkSender _network;

    public UseItemHandler(
        ItemUseService itemUseService,
        GameTimeService gameTimeService,
        INetworkSender network)
    {
        _itemUseService = itemUseService;
        _gameTimeService = gameTimeService;
        _network = network;
    }

    public async Task HandleAsync(ConnectionSession session, UseItemPacket packet)
    {
        if (session.Player is null)
        {
            _network.Send(session.ConnectionId, new UseItemResultPacket
            {
                Success = false,
                Code = MessageCode.CharacterMustEnterWorld,
                PlayerItemId = packet.PlayerItemId,
                RequestedQuantity = packet.Quantity,
                AppliedQuantity = 0
            });
            return;
        }

        try
        {
            var result = await _itemUseService.UseAsync(
                session.Player,
                packet.PlayerItemId!.Value,
                packet.Quantity!.Value);

            _network.Send(session.ConnectionId, new UseItemResultPacket
            {
                Success = true,
                Code = MessageCode.None,
                PlayerItemId = packet.PlayerItemId,
                RequestedQuantity = result.RequestedQuantity,
                AppliedQuantity = result.AppliedQuantity,
                Items = result.Items.Select(x => x.ToModel()).ToList(),
                BaseStats = result.BaseStats?.ToModel(),
                CurrentState = result.CurrentState is null || result.BaseStats is null
                    ? null
                    : result.CurrentState.ToModel(session.Player.CharacterData, result.BaseStats, _gameTimeService.GetCurrentSnapshot()),
                LearnedMartialArt = result.LearnedMartialArt?.ToModel(),
                CultivationPreview = result.CultivationPreview?.ToModel(),
                CooldownMs = result.CooldownMs,
                CooldownEndsUnixMs = ToUnixMs(result.CooldownEndsAtUtc)
            });
        }
        catch (GameException ex)
        {
            _network.Send(session.ConnectionId, new UseItemResultPacket
            {
                Success = false,
                Code = ex.Code,
                PlayerItemId = packet.PlayerItemId,
                RequestedQuantity = packet.Quantity,
                AppliedQuantity = 0
            });
        }
    }

    private static long? ToUnixMs(DateTime? dateTime)
    {
        if (!dateTime.HasValue)
            return null;

        var utc = dateTime.Value.Kind == DateTimeKind.Utc
            ? dateTime.Value
            : DateTime.SpecifyKind(dateTime.Value, DateTimeKind.Utc);
        return new DateTimeOffset(utc).ToUnixTimeMilliseconds();
    }
}
