using GameServer.DTO;
using GameServer.Network.Interface;
using GameServer.Runtime;
using GameServer.Services;
using GameServer.Time;
using GameServer.World;
using GameShared.Messages;
using GameShared.Packets;

namespace GameServer.Network.Handlers;

public sealed class GetCharacterDataHandler : IPacketHandler<GetCharacterDataPacket>
{
    private readonly CharacterService _characterService;
    private readonly CharacterRuntimeService _runtimeService;
    private readonly CharacterLifecycleService _lifecycleService;
    private readonly WorldInterestService _interestService;
    private readonly INetworkSender _server;
    private readonly GameTimeService _gameTimeService;

    public GetCharacterDataHandler(
        CharacterService characterService,
        CharacterRuntimeService runtimeService,
        CharacterLifecycleService lifecycleService,
        WorldInterestService interestService,
        INetworkSender server,
        GameTimeService gameTimeService)
    {
        _characterService = characterService;
        _runtimeService = runtimeService;
        _lifecycleService = lifecycleService;
        _interestService = interestService;
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

            data = await _lifecycleService.PrepareSnapshotForWorldEntryAsync(data);
            var isLifespanExpired = _lifecycleService.IsLifespanExpired(data.CurrentState);

            session.SelectedCharacterId = data.Character.CharacterId;
            var player = _runtimeService.AttachPlayerSession(session, data);
            _interestService.EnsurePlayerInWorld(player);
            if (isLifespanExpired)
            {
                player.SetCharacterActionsRestricted(true);
                session.AreCharacterActionsRestricted = true;
            }

            var runtimeSnapshot = player.RuntimeState.CaptureSnapshot();

            _server.Send(session.ConnectionId, new GetCharacterDataResultPacket
            {
                Success = true,
                Code = isLifespanExpired ? MessageCode.CharacterLifespanExpired : MessageCode.None,
                Character = player.CharacterData.ToModel(),
                BaseStats = runtimeSnapshot.BaseStats.ToModel(),
                CurrentState = runtimeSnapshot.CurrentState.ToModel(_gameTimeService.GetCurrentSnapshot())
            });

            _interestService.PublishWorldSnapshot(player);

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
