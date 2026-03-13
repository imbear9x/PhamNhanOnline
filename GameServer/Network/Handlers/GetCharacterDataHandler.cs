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
    private readonly CharacterRuntimeService _runtimeService;
    private readonly INetworkSender _server;
    private readonly GameTimeService _gameTimeService;

    public GetCharacterDataHandler(
        CharacterService characterService,
        CharacterRuntimeService runtimeService,
        INetworkSender server,
        GameTimeService gameTimeService)
    {
        _characterService = characterService;
        _runtimeService = runtimeService;
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

            session.SelectedCharacterId = data.Character.CharacterId;
            _runtimeService.AttachPlayerSession(session, data);

            _server.Send(session.ConnectionId, new GetCharacterDataResultPacket
            {
                Success = true,
                Code = MessageCode.None,
                Character = data.Character.ToModel(),
                BaseStats = data.BaseStats?.ToModel(),
                CurrentState = data.CurrentState?.ToModel(_gameTimeService.GetCurrentSnapshot())
            });
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
