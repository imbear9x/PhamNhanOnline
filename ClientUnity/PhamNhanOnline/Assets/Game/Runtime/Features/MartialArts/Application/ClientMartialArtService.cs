using System;
using System.Threading.Tasks;
using GameShared.Messages;
using GameShared.Models;
using GameShared.Packets;
using PhamNhanOnline.Client.Features.Character.Application;
using PhamNhanOnline.Client.Network.Session;

namespace PhamNhanOnline.Client.Features.MartialArts.Application
{
    public sealed class ClientMartialArtService
    {
        private readonly ClientConnectionService connection;
        private readonly ClientCharacterState characterState;
        private readonly ClientMartialArtState martialArtState;

        private TaskCompletionSource<MartialArtListLoadResult> loadCompletionSource;
        private TaskCompletionSource<MartialArtSetActiveResult> setActiveCompletionSource;

        public ClientMartialArtService(
            ClientConnectionService connection,
            ClientCharacterState characterState,
            ClientMartialArtState martialArtState)
        {
            this.connection = connection;
            this.characterState = characterState;
            this.martialArtState = martialArtState;

            connection.Packets.Subscribe<GetOwnedMartialArtsResultPacket>(HandleGetOwnedMartialArtsResult);
            connection.Packets.Subscribe<SetActiveMartialArtResultPacket>(HandleSetActiveMartialArtResult);
            connection.Packets.Subscribe<UseItemResultPacket>(HandleUseItemResult);
            connection.Packets.Subscribe<UseMartialArtBookResultPacket>(HandleUseMartialArtBookResult);
            connection.StateChanged += HandleConnectionStateChanged;
        }

        public Task<MartialArtListLoadResult> LoadOwnedMartialArtsAsync(bool forceRefresh = false)
        {
            if (connection.State != ClientConnectionState.Connected)
            {
                return Task.FromResult(new MartialArtListLoadResult(
                    false,
                    null,
                    martialArtState.OwnedMartialArts,
                    martialArtState.CultivationPreview,
                    "Not connected to server.",
                    false));
            }

            if (!forceRefresh && martialArtState.HasLoadedMartialArts && !martialArtState.IsLoading)
            {
                return Task.FromResult(new MartialArtListLoadResult(
                    true,
                    martialArtState.LastResultCode ?? MessageCode.None,
                    martialArtState.OwnedMartialArts,
                    martialArtState.CultivationPreview,
                    "Martial arts loaded from cache.",
                    true));
            }

            if (loadCompletionSource != null && !loadCompletionSource.Task.IsCompleted)
                return loadCompletionSource.Task;

            loadCompletionSource = new TaskCompletionSource<MartialArtListLoadResult>();
            martialArtState.BeginLoading();
            connection.Send(new GetOwnedMartialArtsPacket());
            return loadCompletionSource.Task;
        }

        public Task<MartialArtSetActiveResult> SetActiveMartialArtAsync(int martialArtId)
        {
            if (connection.State != ClientConnectionState.Connected)
            {
                return Task.FromResult(new MartialArtSetActiveResult(
                    false,
                    null,
                    characterState.BaseStats,
                    martialArtState.CultivationPreview,
                    "Not connected to server."));
            }

            if (setActiveCompletionSource != null && !setActiveCompletionSource.Task.IsCompleted)
                return setActiveCompletionSource.Task;

            setActiveCompletionSource = new TaskCompletionSource<MartialArtSetActiveResult>();
            connection.Send(new SetActiveMartialArtPacket
            {
                MartialArtId = martialArtId
            });
            return setActiveCompletionSource.Task;
        }

        private void HandleGetOwnedMartialArtsResult(GetOwnedMartialArtsResultPacket packet)
        {
            var martialArts = packet.MartialArts != null ? packet.MartialArts.ToArray() : Array.Empty<PlayerMartialArtModel>();
            if (packet.Success == true)
            {
                martialArtState.ApplyOwnedMartialArts(
                    martialArts,
                    NormalizeActiveMartialArtId(packet.ActiveMartialArtId),
                    packet.CultivationPreview,
                    packet.Code ?? MessageCode.None,
                    string.Format("Loaded {0} martial art(s).", martialArts.Length));
            }
            else
            {
                martialArtState.ApplyFailure(
                    packet.Code,
                    string.Format("Failed to load martial arts: {0}", packet.Code ?? MessageCode.UnknownError));
            }

            CompletePending(new MartialArtListLoadResult(
                packet.Success == true,
                packet.Code,
                packet.Success == true ? martialArts : martialArtState.OwnedMartialArts,
                packet.Success == true ? packet.CultivationPreview : martialArtState.CultivationPreview,
                packet.Success == true
                    ? string.Format("Loaded {0} martial art(s).", martialArts.Length)
                    : string.Format("Failed to load martial arts: {0}", packet.Code ?? MessageCode.UnknownError),
                false));
        }

        private void HandleSetActiveMartialArtResult(SetActiveMartialArtResultPacket packet)
        {
            if (packet.BaseStats.HasValue)
                characterState.ApplyBaseStats(packet.BaseStats);

            if (packet.Success == true)
            {
                var activeMartialArtId = packet.BaseStats.HasValue
                    ? NormalizeActiveMartialArtId(packet.BaseStats.Value.ActiveMartialArtId)
                    : martialArtState.ActiveMartialArtId;
                martialArtState.ApplyActiveMartialArt(
                    activeMartialArtId,
                    packet.CultivationPreview,
                    packet.Code ?? MessageCode.None,
                    "Active martial art updated.");
            }
            else
            {
                martialArtState.ApplyFailure(
                    packet.Code,
                    string.Format("Failed to set active martial art: {0}", packet.Code ?? MessageCode.UnknownError));
            }

            CompletePending(ref setActiveCompletionSource, new MartialArtSetActiveResult(
                packet.Success == true,
                packet.Code,
                packet.BaseStats,
                packet.CultivationPreview,
                packet.Success == true
                    ? "Active martial art updated."
                    : string.Format("Failed to set active martial art: {0}", packet.Code ?? MessageCode.UnknownError)));
        }

        private void HandleUseItemResult(UseItemResultPacket packet)
        {
            if (packet.BaseStats.HasValue)
                characterState.ApplyBaseStats(packet.BaseStats);

            if (packet.Success == true && packet.LearnedMartialArt.HasValue)
            {
                martialArtState.AppendLearnedMartialArt(
                    packet.LearnedMartialArt.Value,
                    packet.BaseStats.HasValue ? NormalizeActiveMartialArtId(packet.BaseStats.Value.ActiveMartialArtId) : martialArtState.ActiveMartialArtId,
                    packet.CultivationPreview,
                    packet.Code ?? MessageCode.None,
                    "Learned martial art.");
            }
        }

        private void HandleUseMartialArtBookResult(UseMartialArtBookResultPacket packet)
        {
            if (packet.BaseStats.HasValue)
                characterState.ApplyBaseStats(packet.BaseStats);

            if (packet.Success == true && packet.LearnedMartialArt.HasValue)
            {
                martialArtState.AppendLearnedMartialArt(
                    packet.LearnedMartialArt.Value,
                    packet.BaseStats.HasValue ? NormalizeActiveMartialArtId(packet.BaseStats.Value.ActiveMartialArtId) : martialArtState.ActiveMartialArtId,
                    packet.CultivationPreview,
                    packet.Code ?? MessageCode.None,
                    "Learned martial art.");
            }
        }

        private void HandleConnectionStateChanged(ClientConnectionState state)
        {
            if (state != ClientConnectionState.Disconnected)
                return;

            martialArtState.Clear();
            CompletePending(new MartialArtListLoadResult(
                false,
                null,
                Array.Empty<PlayerMartialArtModel>(),
                null,
                "Connection closed.",
                false));
            CompletePending(ref setActiveCompletionSource, new MartialArtSetActiveResult(
                false,
                null,
                null,
                null,
                "Connection closed."));
        }

        private void CompletePending(MartialArtListLoadResult result)
        {
            var pending = loadCompletionSource;
            loadCompletionSource = null;
            if (pending != null)
                pending.TrySetResult(result);
        }

        private static void CompletePending(ref TaskCompletionSource<MartialArtSetActiveResult> completionSource, MartialArtSetActiveResult result)
        {
            var pending = completionSource;
            completionSource = null;
            if (pending != null)
                pending.TrySetResult(result);
        }

        private static int? NormalizeActiveMartialArtId(int? activeMartialArtId)
        {
            return activeMartialArtId.HasValue && activeMartialArtId.Value > 0
                ? activeMartialArtId
                : null;
        }
    }
}
