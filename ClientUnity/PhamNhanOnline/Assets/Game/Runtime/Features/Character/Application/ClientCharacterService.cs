using System;
using System.Threading.Tasks;
using GameShared.Messages;
using GameShared.Models;
using GameShared.Packets;
using PhamNhanOnline.Client.Network.Session;

namespace PhamNhanOnline.Client.Features.Character.Application
{
    public sealed class ClientCharacterService
    {
        private readonly ClientConnectionService connection;
        private readonly ClientCharacterState characterState;

        private TaskCompletionSource<CharacterListLoadResult> characterListCompletionSource;
        private TaskCompletionSource<CharacterDataLoadResult> characterDataCompletionSource;
        private TaskCompletionSource<CharacterCreateResult> characterCreateCompletionSource;
        private TaskCompletionSource<CharacterEnterWorldResult> characterEnterWorldCompletionSource;

        public ClientCharacterService(ClientConnectionService connection, ClientCharacterState characterState)
        {
            this.connection = connection;
            this.characterState = characterState;

            connection.Packets.Subscribe<GetCharacterListResultPacket>(HandleCharacterListResult);
            connection.Packets.Subscribe<GetCharacterDataResultPacket>(HandleCharacterDataResult);
            connection.Packets.Subscribe<CreateCharacterResultPacket>(HandleCreateCharacterResult);
            connection.Packets.Subscribe<EnterWorldResultPacket>(HandleEnterWorldResult);
            connection.Packets.Subscribe<CharacterBaseStatsChangedPacket>(HandleCharacterBaseStatsChanged);
            connection.Packets.Subscribe<CharacterCurrentStateChangedPacket>(HandleCharacterCurrentStateChanged);
            connection.StateChanged += HandleConnectionStateChanged;
        }

        public Task<CharacterListLoadResult> LoadCharacterListAsync()
        {
            if (connection.State != ClientConnectionState.Connected)
                return Task.FromResult(new CharacterListLoadResult(false, null, Array.Empty<CharacterModel>(), "Not connected to server."));

            characterListCompletionSource = new TaskCompletionSource<CharacterListLoadResult>();
            connection.Send(new GetCharacterListPacket());
            return characterListCompletionSource.Task;
        }

        public Task<CharacterDataLoadResult> LoadCharacterDataAsync(Guid characterId)
        {
            if (connection.State != ClientConnectionState.Connected)
                return Task.FromResult(new CharacterDataLoadResult(false, null, null, null, null, "Not connected to server."));

            characterDataCompletionSource = new TaskCompletionSource<CharacterDataLoadResult>();
            characterState.SelectCharacter(characterId);
            connection.Send(new GetCharacterDataPacket
            {
                CharacterId = characterId
            });
            return characterDataCompletionSource.Task;
        }

        public Task<CharacterCreateResult> CreateCharacterAsync(string name, int serverId, int modelId)
        {
            if (connection.State != ClientConnectionState.Connected)
                return Task.FromResult(new CharacterCreateResult(false, null, null, null, null, "Not connected to server."));

            characterCreateCompletionSource = new TaskCompletionSource<CharacterCreateResult>();
            connection.Send(new CreateCharacterPacket
            {
                Name = name,
                ServerId = serverId,
                ModelId = modelId
            });
            return characterCreateCompletionSource.Task;
        }

        public Task<CharacterEnterWorldResult> EnterWorldAsync(Guid characterId)
        {
            if (connection.State != ClientConnectionState.Connected)
                return Task.FromResult(new CharacterEnterWorldResult(false, null, null, null, null, "Not connected to server."));

            characterEnterWorldCompletionSource = new TaskCompletionSource<CharacterEnterWorldResult>();
            characterState.SelectCharacter(characterId);
            connection.Send(new EnterWorldPacket
            {
                CharacterId = characterId
            });
            return characterEnterWorldCompletionSource.Task;
        }

        private void HandleCharacterListResult(GetCharacterListResultPacket packet)
        {
            var characters = packet.Characters != null ? packet.Characters.ToArray() : Array.Empty<CharacterModel>();
            if (packet.Success == true)
                characterState.ApplyCharacterList(characters);

            CompletePending(ref characterListCompletionSource, new CharacterListLoadResult(
                packet.Success == true,
                packet.Code,
                characters,
                packet.Success == true
                    ? string.Format("Loaded {0} character(s).", characters.Length)
                    : string.Format("Failed to load character list: {0}", packet.Code ?? MessageCode.UnknownError)));
        }

        private void HandleCharacterDataResult(GetCharacterDataResultPacket packet)
        {
            if (packet.Success == true && packet.Character.HasValue)
                characterState.ApplyCharacterData(packet.Character.Value, packet.BaseStats, packet.CurrentState);

            CompletePending(ref characterDataCompletionSource, new CharacterDataLoadResult(
                packet.Success == true,
                packet.Code,
                packet.Character,
                packet.BaseStats,
                packet.CurrentState,
                packet.Success == true
                    ? "Character data loaded."
                    : string.Format("Failed to load character data: {0}", packet.Code ?? MessageCode.UnknownError)));
        }

        private void HandleCreateCharacterResult(CreateCharacterResultPacket packet)
        {
            if (packet.Success == true && packet.Character.HasValue)
                characterState.AppendCharacter(packet.Character.Value);

            CompletePending(ref characterCreateCompletionSource, new CharacterCreateResult(
                packet.Success == true,
                packet.Code,
                packet.Character,
                packet.BaseStats,
                packet.CurrentState,
                packet.Success == true
                    ? "Character created successfully."
                    : string.Format("Failed to create character: {0}", packet.Code ?? MessageCode.UnknownError)));
        }

        private void HandleEnterWorldResult(EnterWorldResultPacket packet)
        {
            if (packet.Success == true && packet.Character.HasValue)
                characterState.ApplyCharacterData(packet.Character.Value, packet.BaseStats, packet.CurrentState);

            CompletePending(ref characterEnterWorldCompletionSource, new CharacterEnterWorldResult(
                packet.Success == true,
                packet.Code,
                packet.Character,
                packet.BaseStats,
                packet.CurrentState,
                packet.Success == true
                    ? "Entered world successfully."
                    : string.Format("Failed to enter world: {0}", packet.Code ?? MessageCode.UnknownError)));
        }

        private void HandleCharacterBaseStatsChanged(CharacterBaseStatsChangedPacket packet)
        {
            characterState.ApplyBaseStats(packet.BaseStats);
        }

        private void HandleCharacterCurrentStateChanged(CharacterCurrentStateChangedPacket packet)
        {
            characterState.ApplyCurrentState(packet.CurrentState);
        }

        private void HandleConnectionStateChanged(ClientConnectionState state)
        {
            if (state != ClientConnectionState.Disconnected)
                return;

            characterState.Clear();
            CompletePending(ref characterListCompletionSource, new CharacterListLoadResult(false, null, Array.Empty<CharacterModel>(), "Connection closed."));
            CompletePending(ref characterDataCompletionSource, new CharacterDataLoadResult(false, null, null, null, null, "Connection closed."));
            CompletePending(ref characterCreateCompletionSource, new CharacterCreateResult(false, null, null, null, null, "Connection closed."));
            CompletePending(ref characterEnterWorldCompletionSource, new CharacterEnterWorldResult(false, null, null, null, null, "Connection closed."));
        }

        private static void CompletePending(ref TaskCompletionSource<CharacterListLoadResult> completionSource, CharacterListLoadResult result)
        {
            var pending = completionSource;
            completionSource = null;
            if (pending != null)
                pending.TrySetResult(result);
        }

        private static void CompletePending(ref TaskCompletionSource<CharacterDataLoadResult> completionSource, CharacterDataLoadResult result)
        {
            var pending = completionSource;
            completionSource = null;
            if (pending != null)
                pending.TrySetResult(result);
        }

        private static void CompletePending(ref TaskCompletionSource<CharacterCreateResult> completionSource, CharacterCreateResult result)
        {
            var pending = completionSource;
            completionSource = null;
            if (pending != null)
                pending.TrySetResult(result);
        }

        private static void CompletePending(ref TaskCompletionSource<CharacterEnterWorldResult> completionSource, CharacterEnterWorldResult result)
        {
            var pending = completionSource;
            completionSource = null;
            if (pending != null)
                pending.TrySetResult(result);
        }
    }
}
