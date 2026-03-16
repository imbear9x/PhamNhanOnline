using GameServer.DTO;
using GameServer.Network.Interface;
using GameServer.Runtime;
using GameServer.Services;
using GameServer.Time;
using GameShared.Messages;
using GameShared.Packets;

namespace GameServer.Network.Handlers;

public sealed class GetCharacterDataHandler : IPacketHandler<GetCharacterDataPacket>
{
    private readonly CharacterService _characterService;
    private readonly CharacterLifecycleService _lifecycleService;
    private readonly CharacterCultivationService _cultivationService;
    private readonly INetworkSender _server;
    private readonly GameTimeService _gameTimeService;

    public GetCharacterDataHandler(
        CharacterService characterService,
        CharacterLifecycleService lifecycleService,
        CharacterCultivationService cultivationService,
        INetworkSender server,
        GameTimeService gameTimeService)
    {
        _characterService = characterService;
        _lifecycleService = lifecycleService;
        _cultivationService = cultivationService;
        _server = server;
        _gameTimeService = gameTimeService;
    }

    public async Task HandleAsync(ConnectionSession session, GetCharacterDataPacket packet)
    {
        try
        {
            var data = await _characterService.LoadCharacterSnapshotByAccountAsync(
                session.PlayerId,
                packet.CharacterId!.Value);

            if (data is null)
            {
                _server.Send(session.ConnectionId, new GetCharacterDataResultPacket
                {
                    Success = false,
                    Code = MessageCode.CharacterNotFound
                });
                return;
            }

            var cultivationSettlement = await _cultivationService.SettleSnapshotAsync(data);
            data = cultivationSettlement.Snapshot;
            data = await _lifecycleService.PrepareSnapshotForWorldEntryAsync(data);
            var isLifespanExpired = _lifecycleService.IsLifespanExpired(data.CurrentState);

            _server.Send(session.ConnectionId, new GetCharacterDataResultPacket
            {
                Success = true,
                Code = isLifespanExpired ? MessageCode.CharacterLifespanExpired : MessageCode.None,
                Character = data.Character.ToModel(),
                BaseStats = data.BaseStats?.ToModel(),
                CurrentState = data.CurrentState?.ToModel(_gameTimeService.GetCurrentSnapshot())
            });

            if (cultivationSettlement.RewardEvent is not null)
                _server.Send(session.ConnectionId, cultivationSettlement.RewardEvent.ToPacket());

            if (isLifespanExpired)
                _lifecycleService.NotifyLifespanExpired(session.ConnectionId, data.Character.CharacterId);
        }
        catch (Exception)
        {
            _server.Send(session.ConnectionId, new GetCharacterDataResultPacket
            {
                Success = false,
                Code = MessageCode.UnknownError
            });
            throw;
        }
    }
}
