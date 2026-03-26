using System;
using System.Threading.Tasks;
using GameShared.Messages;
using GameShared.Models;
using GameShared.Packets;
using PhamNhanOnline.Client.Network.Session;

namespace PhamNhanOnline.Client.Features.Skills.Application
{
    public sealed class ClientSkillService
    {
        private readonly ClientConnectionService connection;
        private readonly ClientSkillState skillState;

        private TaskCompletionSource<SkillListLoadResult> loadCompletionSource;
        private TaskCompletionSource<SkillLoadoutSetResult> setLoadoutCompletionSource;

        public ClientSkillService(
            ClientConnectionService connection,
            ClientSkillState skillState)
        {
            this.connection = connection;
            this.skillState = skillState;

            connection.Packets.Subscribe<GetOwnedSkillsResultPacket>(HandleGetOwnedSkillsResult);
            connection.Packets.Subscribe<SetSkillLoadoutSlotResultPacket>(HandleSetSkillLoadoutSlotResult);
            connection.StateChanged += HandleConnectionStateChanged;
        }

        public Task<SkillListLoadResult> LoadOwnedSkillsAsync(bool forceRefresh = false)
        {
            if (connection.State != ClientConnectionState.Connected)
            {
                return Task.FromResult(new SkillListLoadResult(
                    false,
                    null,
                    skillState.MaxLoadoutSlotCount,
                    skillState.Skills,
                    skillState.LoadoutSlots,
                    "Not connected to server.",
                    false));
            }

            if (!forceRefresh && skillState.HasLoadedSkills && !skillState.IsLoading)
            {
                return Task.FromResult(new SkillListLoadResult(
                    true,
                    skillState.LastResultCode ?? MessageCode.None,
                    skillState.MaxLoadoutSlotCount,
                    skillState.Skills,
                    skillState.LoadoutSlots,
                    "Skills loaded from cache.",
                    true));
            }

            if (loadCompletionSource != null && !loadCompletionSource.Task.IsCompleted)
                return loadCompletionSource.Task;

            loadCompletionSource = new TaskCompletionSource<SkillListLoadResult>();
            skillState.BeginLoading();
            connection.Send(new GetOwnedSkillsPacket());
            return loadCompletionSource.Task;
        }

        public Task<SkillLoadoutSetResult> SetSkillLoadoutSlotAsync(int slotIndex, long playerSkillId)
        {
            if (connection.State != ClientConnectionState.Connected)
            {
                return Task.FromResult(new SkillLoadoutSetResult(
                    false,
                    null,
                    skillState.MaxLoadoutSlotCount,
                    skillState.Skills,
                    skillState.LoadoutSlots,
                    "Not connected to server."));
            }

            if (setLoadoutCompletionSource != null && !setLoadoutCompletionSource.Task.IsCompleted)
                return setLoadoutCompletionSource.Task;

            setLoadoutCompletionSource = new TaskCompletionSource<SkillLoadoutSetResult>();
            connection.Send(new SetSkillLoadoutSlotPacket
            {
                SlotIndex = slotIndex,
                PlayerSkillId = playerSkillId
            });
            return setLoadoutCompletionSource.Task;
        }

        private void HandleGetOwnedSkillsResult(GetOwnedSkillsResultPacket packet)
        {
            var skills = packet.Skills != null ? packet.Skills.ToArray() : Array.Empty<PlayerSkillModel>();
            var loadoutSlots = packet.LoadoutSlots != null ? packet.LoadoutSlots.ToArray() : Array.Empty<SkillLoadoutSlotModel>();
            var maxLoadoutSlotCount = Math.Max(0, packet.MaxLoadoutSlotCount ?? 0);

            if (packet.Success == true)
            {
                skillState.ApplySnapshot(
                    maxLoadoutSlotCount,
                    skills,
                    loadoutSlots,
                    packet.Code ?? MessageCode.None,
                    string.Format("Loaded {0} skill(s).", skills.Length));
            }
            else
            {
                skillState.ApplyFailure(
                    packet.Code,
                    string.Format("Failed to load skills: {0}", packet.Code ?? MessageCode.UnknownError));
            }

            CompletePending(new SkillListLoadResult(
                packet.Success == true,
                packet.Code,
                packet.Success == true ? maxLoadoutSlotCount : skillState.MaxLoadoutSlotCount,
                packet.Success == true ? skills : skillState.Skills,
                packet.Success == true ? loadoutSlots : skillState.LoadoutSlots,
                packet.Success == true
                    ? string.Format("Loaded {0} skill(s).", skills.Length)
                    : string.Format("Failed to load skills: {0}", packet.Code ?? MessageCode.UnknownError),
                false));
        }

        private void HandleSetSkillLoadoutSlotResult(SetSkillLoadoutSlotResultPacket packet)
        {
            var skills = packet.Skills != null ? packet.Skills.ToArray() : Array.Empty<PlayerSkillModel>();
            var loadoutSlots = packet.LoadoutSlots != null ? packet.LoadoutSlots.ToArray() : Array.Empty<SkillLoadoutSlotModel>();
            var maxLoadoutSlotCount = Math.Max(0, packet.MaxLoadoutSlotCount ?? 0);

            if (packet.Success == true)
            {
                skillState.ApplySnapshot(
                    maxLoadoutSlotCount,
                    skills,
                    loadoutSlots,
                    packet.Code ?? MessageCode.None,
                    "Skill loadout updated.");
            }
            else
            {
                skillState.ApplyFailure(
                    packet.Code,
                    string.Format("Failed to update skill loadout: {0}", packet.Code ?? MessageCode.UnknownError));
            }

            CompletePending(ref setLoadoutCompletionSource, new SkillLoadoutSetResult(
                packet.Success == true,
                packet.Code,
                packet.Success == true ? maxLoadoutSlotCount : skillState.MaxLoadoutSlotCount,
                packet.Success == true ? skills : skillState.Skills,
                packet.Success == true ? loadoutSlots : skillState.LoadoutSlots,
                packet.Success == true
                    ? "Skill loadout updated."
                    : string.Format("Failed to update skill loadout: {0}", packet.Code ?? MessageCode.UnknownError)));
        }

        private void HandleConnectionStateChanged(ClientConnectionState state)
        {
            if (state != ClientConnectionState.Disconnected)
                return;

            skillState.Clear();
            CompletePending(new SkillListLoadResult(
                false,
                null,
                0,
                Array.Empty<PlayerSkillModel>(),
                Array.Empty<SkillLoadoutSlotModel>(),
                "Connection closed.",
                false));
            CompletePending(ref setLoadoutCompletionSource, new SkillLoadoutSetResult(
                false,
                null,
                0,
                Array.Empty<PlayerSkillModel>(),
                Array.Empty<SkillLoadoutSlotModel>(),
                "Connection closed."));
        }

        private void CompletePending(SkillListLoadResult result)
        {
            var pending = loadCompletionSource;
            loadCompletionSource = null;
            if (pending != null)
                pending.TrySetResult(result);
        }

        private static void CompletePending(ref TaskCompletionSource<SkillLoadoutSetResult> completionSource, SkillLoadoutSetResult result)
        {
            var pending = completionSource;
            completionSource = null;
            if (pending != null)
                pending.TrySetResult(result);
        }
    }
}
