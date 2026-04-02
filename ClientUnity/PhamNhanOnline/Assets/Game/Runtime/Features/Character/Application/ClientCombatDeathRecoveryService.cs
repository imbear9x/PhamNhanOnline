using System.Threading.Tasks;
using GameShared.Messages;
using GameShared.Packets;
using PhamNhanOnline.Client.Network.Session;

namespace PhamNhanOnline.Client.Features.Character.Application
{
    public sealed class ClientCombatDeathRecoveryService
    {
        private readonly ClientConnectionService connection;
        private readonly ClientCharacterState characterState;
        private TaskCompletionSource<CombatDeathReturnHomeResult> returnHomeCompletionSource;

        public ClientCombatDeathRecoveryService(
            ClientConnectionService connection,
            ClientCharacterState characterState)
        {
            this.connection = connection;
            this.characterState = characterState;

            connection.Packets.Subscribe<ReturnHomeAfterCombatDeathResultPacket>(HandleReturnHomeResult);
            connection.StateChanged += HandleConnectionStateChanged;
        }

        public Task<CombatDeathReturnHomeResult> ReturnHomeAsync()
        {
            if (connection.State != ClientConnectionState.Connected)
            {
                return Task.FromResult(new CombatDeathReturnHomeResult(
                    false,
                    null,
                    characterState.BaseStats,
                    characterState.CurrentState,
                    "Not connected to server."));
            }

            if (returnHomeCompletionSource != null && !returnHomeCompletionSource.Task.IsCompleted)
                return returnHomeCompletionSource.Task;

            returnHomeCompletionSource = new TaskCompletionSource<CombatDeathReturnHomeResult>();
            connection.Send(new ReturnHomeAfterCombatDeathPacket());
            return returnHomeCompletionSource.Task;
        }

        private void HandleReturnHomeResult(ReturnHomeAfterCombatDeathResultPacket packet)
        {
            if (packet.BaseStats.HasValue)
                characterState.ApplyBaseStats(packet.BaseStats);
            if (packet.CurrentState.HasValue)
                characterState.ApplyCurrentState(packet.CurrentState);

            CompletePending(new CombatDeathReturnHomeResult(
                packet.Success == true,
                packet.Code,
                packet.BaseStats,
                packet.CurrentState,
                packet.Success == true
                    ? "Returned home."
                    : string.Format("Failed to return home: {0}", packet.Code ?? MessageCode.UnknownError)));
        }

        private void HandleConnectionStateChanged(ClientConnectionState state)
        {
            if (state != ClientConnectionState.Disconnected)
                return;

            CompletePending(new CombatDeathReturnHomeResult(
                false,
                null,
                characterState.BaseStats,
                characterState.CurrentState,
                "Connection closed."));
        }

        private void CompletePending(CombatDeathReturnHomeResult result)
        {
            var pending = returnHomeCompletionSource;
            returnHomeCompletionSource = null;
            if (pending != null)
                pending.TrySetResult(result);
        }
    }
}
