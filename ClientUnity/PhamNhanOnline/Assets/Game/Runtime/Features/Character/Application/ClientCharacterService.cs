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
        private TaskCompletionSource<CharacterStartCultivationResult> startCultivationCompletionSource;
        private TaskCompletionSource<CharacterStopCultivationResult> stopCultivationCompletionSource;
        private TaskCompletionSource<CharacterBreakthroughResult> breakthroughCompletionSource;
        private TaskCompletionSource<CharacterAllocatePotentialResult> allocatePotentialCompletionSource;

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
            connection.Packets.Subscribe<StartCultivationResultPacket>(HandleStartCultivationResult);
            connection.Packets.Subscribe<StopCultivationResultPacket>(HandleStopCultivationResult);
            connection.Packets.Subscribe<BreakthroughResultPacket>(HandleBreakthroughResult);
            connection.Packets.Subscribe<AllocatePotentialResultPacket>(HandleAllocatePotentialResult);
            connection.Packets.Subscribe<CultivationRewardsGrantedPacket>(HandleCultivationRewardsGranted);
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

        public Task<CharacterStartCultivationResult> StartCultivationAsync()
        {
            if (connection.State != ClientConnectionState.Connected)
                return Task.FromResult(new CharacterStartCultivationResult(false, null, null, "Not connected to server."));

            startCultivationCompletionSource = new TaskCompletionSource<CharacterStartCultivationResult>();
            connection.Send(new StartCultivationPacket());
            return startCultivationCompletionSource.Task;
        }

        public Task<CharacterStopCultivationResult> StopCultivationAsync()
        {
            if (connection.State != ClientConnectionState.Connected)
                return Task.FromResult(new CharacterStopCultivationResult(false, null, null, "Not connected to server."));

            stopCultivationCompletionSource = new TaskCompletionSource<CharacterStopCultivationResult>();
            connection.Send(new StopCultivationPacket());
            return stopCultivationCompletionSource.Task;
        }

        public Task<CharacterBreakthroughResult> BreakthroughAsync()
        {
            if (connection.State != ClientConnectionState.Connected)
                return Task.FromResult(new CharacterBreakthroughResult(false, null, null, null, "Not connected to server."));

            breakthroughCompletionSource = new TaskCompletionSource<CharacterBreakthroughResult>();
            connection.Send(new BreakthroughPacket());
            return breakthroughCompletionSource.Task;
        }

        public Task<CharacterAllocatePotentialResult> AllocatePotentialAsync(PotentialAllocationTarget target)
        {
            if (connection.State != ClientConnectionState.Connected)
                return Task.FromResult(new CharacterAllocatePotentialResult(false, null, null, null, "Not connected to server."));

            allocatePotentialCompletionSource = new TaskCompletionSource<CharacterAllocatePotentialResult>();
            connection.Send(new AllocatePotentialPacket
            {
                TargetStat = (int)target
            });
            return allocatePotentialCompletionSource.Task;
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

        private void HandleStartCultivationResult(StartCultivationResultPacket packet)
        {
            if (packet.CurrentState.HasValue)
                characterState.ApplyCurrentState(packet.CurrentState);

            CompletePending(ref startCultivationCompletionSource, new CharacterStartCultivationResult(
                packet.Success == true,
                packet.Code,
                packet.CurrentState,
                packet.Success == true
                    ? "Cultivation started."
                    : string.Format("Failed to start cultivation: {0}", packet.Code ?? MessageCode.UnknownError)));
        }

        private void HandleStopCultivationResult(StopCultivationResultPacket packet)
        {
            if (packet.CurrentState.HasValue)
                characterState.ApplyCurrentState(packet.CurrentState);

            CompletePending(ref stopCultivationCompletionSource, new CharacterStopCultivationResult(
                packet.Success == true,
                packet.Code,
                packet.CurrentState,
                packet.Success == true
                    ? "Cultivation stopped."
                    : string.Format("Failed to stop cultivation: {0}", packet.Code ?? MessageCode.UnknownError)));
        }

        private void HandleBreakthroughResult(BreakthroughResultPacket packet)
        {
            if (packet.BaseStats.HasValue)
                characterState.ApplyBaseStats(packet.BaseStats);
            if (packet.CurrentState.HasValue)
                characterState.ApplyCurrentState(packet.CurrentState);

            CompletePending(ref breakthroughCompletionSource, new CharacterBreakthroughResult(
                packet.Success == true,
                packet.Code,
                packet.BaseStats,
                packet.CurrentState,
                packet.Success == true
                    ? "Breakthrough succeeded."
                    : string.Format("Failed to breakthrough: {0}", packet.Code ?? MessageCode.UnknownError)));
        }

        private void HandleAllocatePotentialResult(AllocatePotentialResultPacket packet)
        {
            if (packet.BaseStats.HasValue)
                characterState.ApplyBaseStats(packet.BaseStats);
            if (packet.CurrentState.HasValue)
                characterState.ApplyCurrentState(packet.CurrentState);

            CompletePending(ref allocatePotentialCompletionSource, new CharacterAllocatePotentialResult(
                packet.Success == true,
                packet.Code,
                packet.BaseStats,
                packet.CurrentState,
                packet.Success == true
                    ? "Potential allocated."
                    : string.Format("Failed to allocate potential: {0}", packet.Code ?? MessageCode.UnknownError)));
        }

        private void HandleCultivationRewardsGranted(CultivationRewardsGrantedPacket packet)
        {
            characterState.ApplyCultivationReward(new CultivationRewardNotice(
                packet.CharacterId,
                packet.CultivationGranted ?? 0,
                packet.UnallocatedPotentialGranted ?? 0,
                packet.ReachedRealmCap == true,
                packet.IsOfflineSettlement == true,
                packet.RewardedFromUnixMs,
                packet.RewardedToUnixMs));
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
            CompletePending(ref startCultivationCompletionSource, new CharacterStartCultivationResult(false, null, null, "Connection closed."));
            CompletePending(ref stopCultivationCompletionSource, new CharacterStopCultivationResult(false, null, null, "Connection closed."));
            CompletePending(ref breakthroughCompletionSource, new CharacterBreakthroughResult(false, null, null, null, "Connection closed."));
            CompletePending(ref allocatePotentialCompletionSource, new CharacterAllocatePotentialResult(false, null, null, null, "Connection closed."));
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

        private static void CompletePending(ref TaskCompletionSource<CharacterStartCultivationResult> completionSource, CharacterStartCultivationResult result)
        {
            var pending = completionSource;
            completionSource = null;
            if (pending != null)
                pending.TrySetResult(result);
        }

        private static void CompletePending(ref TaskCompletionSource<CharacterStopCultivationResult> completionSource, CharacterStopCultivationResult result)
        {
            var pending = completionSource;
            completionSource = null;
            if (pending != null)
                pending.TrySetResult(result);
        }

        private static void CompletePending(ref TaskCompletionSource<CharacterBreakthroughResult> completionSource, CharacterBreakthroughResult result)
        {
            var pending = completionSource;
            completionSource = null;
            if (pending != null)
                pending.TrySetResult(result);
        }

        private static void CompletePending(ref TaskCompletionSource<CharacterAllocatePotentialResult> completionSource, CharacterAllocatePotentialResult result)
        {
            var pending = completionSource;
            completionSource = null;
            if (pending != null)
                pending.TrySetResult(result);
        }
    }
}
