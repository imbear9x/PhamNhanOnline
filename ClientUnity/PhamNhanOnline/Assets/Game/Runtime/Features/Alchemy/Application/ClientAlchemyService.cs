using System;
using System.Linq;
using System.Threading.Tasks;
using GameShared.Messages;
using GameShared.Models;
using GameShared.Packets;
using PhamNhanOnline.Client.Features.Inventory.Application;
using PhamNhanOnline.Client.Network.Session;

namespace PhamNhanOnline.Client.Features.Alchemy.Application
{
    public sealed class ClientAlchemyService
    {
        private readonly ClientConnectionService connection;
        private readonly ClientInventoryState inventoryState;
        private readonly ClientAlchemyState alchemyState;

        private TaskCompletionSource<AlchemyRecipeListLoadResult> loadRecipesCompletionSource;
        private TaskCompletionSource<AlchemyRecipeDetailLoadResult> loadDetailCompletionSource;
        private TaskCompletionSource<AlchemyCraftPreviewResult> previewCompletionSource;
        private TaskCompletionSource<AlchemyCraftExecuteResult> craftCompletionSource;
        private TaskCompletionSource<AlchemyPracticeStatusLoadResult> practiceStatusCompletionSource;
        private TaskCompletionSource<PracticeSessionActionResult> pausePracticeCompletionSource;
        private TaskCompletionSource<PracticeSessionActionResult> resumePracticeCompletionSource;
        private TaskCompletionSource<PracticeSessionActionResult> cancelPracticeCompletionSource;
        private TaskCompletionSource<PracticeSessionActionResult> acknowledgePracticeCompletionSource;

        public ClientAlchemyService(
            ClientConnectionService connection,
            ClientInventoryState inventoryState,
            ClientAlchemyState alchemyState)
        {
            this.connection = connection;
            this.inventoryState = inventoryState;
            this.alchemyState = alchemyState;

            connection.Packets.Subscribe<GetLearnedPillRecipesResultPacket>(HandleGetLearnedRecipesResult);
            connection.Packets.Subscribe<GetPillRecipeDetailResultPacket>(HandleGetRecipeDetailResult);
            connection.Packets.Subscribe<PreviewCraftPillResultPacket>(HandlePreviewCraftResult);
            connection.Packets.Subscribe<CraftPillResultPacket>(HandleCraftPillResult);
            connection.Packets.Subscribe<GetAlchemyPracticeStatusResultPacket>(HandleGetPracticeStatusResult);
            connection.Packets.Subscribe<PausePracticeResultPacket>(HandlePausePracticeResult);
            connection.Packets.Subscribe<ResumePracticeResultPacket>(HandleResumePracticeResult);
            connection.Packets.Subscribe<CancelPracticeResultPacket>(HandleCancelPracticeResult);
            connection.Packets.Subscribe<AcknowledgePracticeResultResultPacket>(HandleAcknowledgePracticeResult);
            connection.Packets.Subscribe<PracticeCompletedPacket>(HandlePracticeCompleted);
            connection.StateChanged += HandleConnectionStateChanged;
        }

        public Task<AlchemyRecipeListLoadResult> LoadLearnedRecipesAsync(bool forceRefresh = false)
        {
            if (connection.State != ClientConnectionState.Connected)
            {
                return Task.FromResult(new AlchemyRecipeListLoadResult(
                    false,
                    null,
                    alchemyState.Recipes,
                    "Not connected to server.",
                    false));
            }

            if (!forceRefresh && alchemyState.HasLoadedRecipes && !alchemyState.IsLoadingRecipes)
            {
                return Task.FromResult(new AlchemyRecipeListLoadResult(
                    true,
                    alchemyState.LastResultCode ?? MessageCode.None,
                    alchemyState.Recipes,
                    "Pill recipes loaded from cache.",
                    true));
            }

            if (loadRecipesCompletionSource != null && !loadRecipesCompletionSource.Task.IsCompleted)
                return loadRecipesCompletionSource.Task;

            loadRecipesCompletionSource = new TaskCompletionSource<AlchemyRecipeListLoadResult>();
            alchemyState.BeginLoadingRecipes();
            connection.Send(new GetLearnedPillRecipesPacket());
            return loadRecipesCompletionSource.Task;
        }

        public Task<AlchemyRecipeDetailLoadResult> LoadRecipeDetailAsync(int recipeId, bool forceRefresh = false)
        {
            if (connection.State != ClientConnectionState.Connected)
            {
                return Task.FromResult(new AlchemyRecipeDetailLoadResult(
                    false,
                    null,
                    null,
                    string.Empty,
                    "Not connected to server.",
                    false));
            }

            PillRecipeDetailModel cachedDetail;
            if (!forceRefresh && alchemyState.TryGetRecipeDetail(recipeId, out cachedDetail))
            {
                return Task.FromResult(new AlchemyRecipeDetailLoadResult(
                    true,
                    alchemyState.LastResultCode ?? MessageCode.None,
                    cachedDetail,
                    string.Empty,
                    "Pill recipe detail loaded from cache.",
                    true));
            }

            if (loadDetailCompletionSource != null && !loadDetailCompletionSource.Task.IsCompleted)
                return loadDetailCompletionSource.Task;

            loadDetailCompletionSource = new TaskCompletionSource<AlchemyRecipeDetailLoadResult>();
            connection.Send(new GetPillRecipeDetailPacket
            {
                PillRecipeTemplateId = recipeId
            });
            return loadDetailCompletionSource.Task;
        }

        public Task<AlchemyCraftPreviewResult> PreviewCraftAsync(
            int recipeId,
            long[] selectedPlayerItemIds = null,
            int[] selectedOptionalInputIds = null)
        {
            if (connection.State != ClientConnectionState.Connected)
            {
                return Task.FromResult(new AlchemyCraftPreviewResult(
                    false,
                    null,
                    null,
                    string.Empty,
                    "Not connected to server."));
            }

            if (previewCompletionSource != null && !previewCompletionSource.Task.IsCompleted)
                return previewCompletionSource.Task;

            previewCompletionSource = new TaskCompletionSource<AlchemyCraftPreviewResult>();
            connection.Send(new PreviewCraftPillPacket
            {
                PillRecipeTemplateId = recipeId,
                SelectedPlayerItemIds = selectedPlayerItemIds != null ? selectedPlayerItemIds.ToList() : null,
                SelectedOptionalInputIds = selectedOptionalInputIds != null ? selectedOptionalInputIds.ToList() : null
            });
            return previewCompletionSource.Task;
        }

        public Task<AlchemyCraftExecuteResult> CraftPillAsync(
            int recipeId,
            long[] selectedPlayerItemIds = null,
            int[] selectedOptionalInputIds = null)
        {
            if (connection.State != ClientConnectionState.Connected)
            {
                return Task.FromResult(new AlchemyCraftExecuteResult(
                    false,
                    null,
                    null,
                    null,
                    inventoryState.Items,
                    Array.Empty<AlchemyConsumedItemModel>(),
                    string.Empty,
                    "Not connected to server."));
            }

            if (craftCompletionSource != null && !craftCompletionSource.Task.IsCompleted)
                return craftCompletionSource.Task;

            craftCompletionSource = new TaskCompletionSource<AlchemyCraftExecuteResult>();
            connection.Send(new CraftPillPacket
            {
                PillRecipeTemplateId = recipeId,
                SelectedPlayerItemIds = selectedPlayerItemIds != null ? selectedPlayerItemIds.ToList() : null,
                SelectedOptionalInputIds = selectedOptionalInputIds != null ? selectedOptionalInputIds.ToList() : null
            });
            return craftCompletionSource.Task;
        }

        public Task<AlchemyPracticeStatusLoadResult> LoadPracticeStatusAsync()
        {
            if (connection.State != ClientConnectionState.Connected)
            {
                return Task.FromResult(new AlchemyPracticeStatusLoadResult(
                    false,
                    null,
                    alchemyState.LastPracticeStatus,
                    "Not connected to server."));
            }

            if (practiceStatusCompletionSource != null && !practiceStatusCompletionSource.Task.IsCompleted)
                return practiceStatusCompletionSource.Task;

            practiceStatusCompletionSource = new TaskCompletionSource<AlchemyPracticeStatusLoadResult>();
            connection.Send(new GetAlchemyPracticeStatusPacket());
            return practiceStatusCompletionSource.Task;
        }

        public Task<PracticeSessionActionResult> PausePracticeAsync(long? practiceSessionId)
        {
            return SendPracticeActionAsync(
                ref pausePracticeCompletionSource,
                new PausePracticePacket { PracticeSessionId = practiceSessionId },
                "Not connected to server.");
        }

        public Task<PracticeSessionActionResult> ResumePracticeAsync(long? practiceSessionId)
        {
            return SendPracticeActionAsync(
                ref resumePracticeCompletionSource,
                new ResumePracticePacket { PracticeSessionId = practiceSessionId },
                "Not connected to server.");
        }

        public Task<PracticeSessionActionResult> CancelPracticeAsync(long? practiceSessionId)
        {
            return SendPracticeActionAsync(
                ref cancelPracticeCompletionSource,
                new CancelPracticePacket { PracticeSessionId = practiceSessionId },
                "Not connected to server.");
        }

        public Task<PracticeSessionActionResult> AcknowledgePracticeResultAsync(long? practiceSessionId)
        {
            return SendPracticeActionAsync(
                ref acknowledgePracticeCompletionSource,
                new AcknowledgePracticeResultPacket { PracticeSessionId = practiceSessionId },
                "Not connected to server.");
        }

        private Task<PracticeSessionActionResult> SendPracticeActionAsync<TPacket>(
            ref TaskCompletionSource<PracticeSessionActionResult> completionSource,
            TPacket packet,
            string disconnectedMessage)
            where TPacket : class, IPacket
        {
            if (connection.State != ClientConnectionState.Connected)
            {
                return Task.FromResult(new PracticeSessionActionResult(
                    false,
                    null,
                    null,
                    disconnectedMessage));
            }

            if (completionSource != null && !completionSource.Task.IsCompleted)
                return completionSource.Task;

            completionSource = new TaskCompletionSource<PracticeSessionActionResult>();
            connection.Send(packet);
            return completionSource.Task;
        }

        private void HandleGetLearnedRecipesResult(GetLearnedPillRecipesResultPacket packet)
        {
            var recipes = packet.Recipes != null ? packet.Recipes.ToArray() : Array.Empty<LearnedPillRecipeModel>();
            if (packet.Success == true)
            {
                alchemyState.ApplyRecipes(
                    recipes,
                    packet.Code ?? MessageCode.None,
                    string.Format("Loaded {0} pill recipe(s).", recipes.Length));
            }
            else
            {
                alchemyState.ApplyFailure(
                    packet.Code,
                    string.Format("Failed to load pill recipes: {0}", packet.Code ?? MessageCode.UnknownError));
            }

            CompletePending(new AlchemyRecipeListLoadResult(
                packet.Success == true,
                packet.Code,
                packet.Success == true ? recipes : alchemyState.Recipes,
                packet.Success == true
                    ? string.Format("Loaded {0} pill recipe(s).", recipes.Length)
                    : string.Format("Failed to load pill recipes: {0}", packet.Code ?? MessageCode.UnknownError),
                false));
        }

        private void HandleGetRecipeDetailResult(GetPillRecipeDetailResultPacket packet)
        {
            if (packet.Success == true && packet.Recipe.HasValue)
            {
                alchemyState.ApplyRecipeDetail(
                    packet.Recipe.Value,
                    packet.Code ?? MessageCode.None,
                    "Pill recipe detail loaded.");
            }
            else
            {
                alchemyState.ApplyFailure(
                    packet.Code,
                    string.Format("Failed to load pill recipe detail: {0}", packet.Code ?? MessageCode.UnknownError));
            }

            CompletePending(ref loadDetailCompletionSource, new AlchemyRecipeDetailLoadResult(
                packet.Success == true && packet.Recipe.HasValue,
                packet.Code,
                packet.Recipe,
                packet.FailureReason ?? string.Empty,
                packet.Success == true
                    ? "Pill recipe detail loaded."
                    : string.Format("Failed to load pill recipe detail: {0}", packet.Code ?? MessageCode.UnknownError),
                false));
        }

        private void HandlePreviewCraftResult(PreviewCraftPillResultPacket packet)
        {
            if (packet.Success == true)
            {
                alchemyState.ApplyPreview(
                    packet.Preview,
                    packet.Code,
                    packet.Preview.HasValue && packet.Preview.Value.CanCraft
                        ? "Alchemy preview ready."
                        : packet.FailureReason ?? "Alchemy preview indicates crafting is not ready.");
            }
            else
            {
                alchemyState.ApplyFailure(
                    packet.Code,
                    string.Format("Failed to preview crafting: {0}", packet.Code ?? MessageCode.UnknownError));
            }

            CompletePending(ref previewCompletionSource, new AlchemyCraftPreviewResult(
                packet.Success == true,
                packet.Code,
                packet.Preview,
                packet.FailureReason ?? string.Empty,
                packet.Success == true
                    ? (packet.Preview.HasValue && packet.Preview.Value.CanCraft
                        ? "Alchemy preview ready."
                        : packet.FailureReason ?? "Alchemy preview indicates crafting is not ready.")
                    : string.Format("Failed to preview crafting: {0}", packet.Code ?? MessageCode.UnknownError)));
        }

        private void HandleCraftPillResult(CraftPillResultPacket packet)
        {
            var inventoryItems = packet.Items != null ? packet.Items.ToArray() : inventoryState.Items;
            if (packet.Items != null)
            {
                inventoryState.ApplyInventory(
                    inventoryItems,
                    packet.Code,
                    packet.Success == true ? "Inventory updated after starting alchemy." : "Inventory refreshed after alchemy attempt.");
            }

            if (packet.Success == true)
            {
                alchemyState.ApplyCraftStarted(
                    packet.Recipe,
                    packet.Session,
                    packet.ConsumedItems != null ? packet.ConsumedItems.ToArray() : Array.Empty<AlchemyConsumedItemModel>(),
                    packet.Code,
                    "Alchemy practice started.");
            }
            else
            {
                alchemyState.ApplyFailure(
                    packet.Code,
                    packet.FailureReason ?? "Alchemy could not be started.");
            }

            CompletePending(ref craftCompletionSource, new AlchemyCraftExecuteResult(
                packet.Success == true,
                packet.Code,
                packet.Recipe,
                packet.Session,
                inventoryItems,
                packet.ConsumedItems != null ? packet.ConsumedItems.ToArray() : Array.Empty<AlchemyConsumedItemModel>(),
                packet.FailureReason ?? string.Empty,
                packet.Success == true
                    ? "Alchemy practice started."
                    : packet.FailureReason ?? string.Format("Alchemy failed: {0}", packet.Code ?? MessageCode.UnknownError)));
        }

        private void HandleGetPracticeStatusResult(GetAlchemyPracticeStatusResultPacket packet)
        {
            if (packet.Success == true)
            {
                alchemyState.ApplyPracticeStatus(
                    packet.Status,
                    packet.Code,
                    "Alchemy practice status loaded.");
            }
            else
            {
                alchemyState.ApplyFailure(
                    packet.Code,
                    string.Format("Failed to load alchemy practice status: {0}", packet.Code ?? MessageCode.UnknownError));
            }

            CompletePending(ref practiceStatusCompletionSource, new AlchemyPracticeStatusLoadResult(
                packet.Success == true,
                packet.Code,
                packet.Status,
                packet.Success == true
                    ? "Alchemy practice status loaded."
                    : string.Format("Failed to load alchemy practice status: {0}", packet.Code ?? MessageCode.UnknownError)));
        }

        private void HandlePausePracticeResult(PausePracticeResultPacket packet)
        {
            if (packet.Success == true)
            {
                alchemyState.ApplyPracticeSessionChange(packet.Session, packet.Code, "Alchemy practice paused.");
            }
            else
            {
                alchemyState.ApplyFailure(packet.Code, "Failed to pause alchemy practice.");
            }

            CompletePending(ref pausePracticeCompletionSource, new PracticeSessionActionResult(
                packet.Success == true,
                packet.Code,
                packet.Session,
                packet.Success == true ? "Alchemy practice paused." : "Failed to pause alchemy practice."));
        }

        private void HandleResumePracticeResult(ResumePracticeResultPacket packet)
        {
            if (packet.Success == true)
            {
                alchemyState.ApplyPracticeSessionChange(packet.Session, packet.Code, "Alchemy practice resumed.");
            }
            else
            {
                alchemyState.ApplyFailure(packet.Code, "Failed to resume alchemy practice.");
            }

            CompletePending(ref resumePracticeCompletionSource, new PracticeSessionActionResult(
                packet.Success == true,
                packet.Code,
                packet.Session,
                packet.Success == true ? "Alchemy practice resumed." : "Failed to resume alchemy practice."));
        }

        private void HandleCancelPracticeResult(CancelPracticeResultPacket packet)
        {
            if (packet.Success == true)
            {
                alchemyState.ApplyPracticeSessionChange(null, packet.Code, "Alchemy practice canceled.");
            }
            else
            {
                alchemyState.ApplyFailure(packet.Code, "Failed to cancel alchemy practice.");
            }

            CompletePending(ref cancelPracticeCompletionSource, new PracticeSessionActionResult(
                packet.Success == true,
                packet.Code,
                null,
                packet.Success == true ? "Alchemy practice canceled." : "Failed to cancel alchemy practice."));
        }

        private void HandleAcknowledgePracticeResult(AcknowledgePracticeResultResultPacket packet)
        {
            if (packet.Success == true)
            {
                alchemyState.ClearPendingPracticeResult(packet.Code, "Alchemy practice result acknowledged.");
            }
            else
            {
                alchemyState.ApplyFailure(packet.Code, "Failed to acknowledge alchemy practice result.");
            }

            CompletePending(ref acknowledgePracticeCompletionSource, new PracticeSessionActionResult(
                packet.Success == true,
                packet.Code,
                alchemyState.CurrentPracticeSession,
                packet.Success == true
                    ? "Alchemy practice result acknowledged."
                    : "Failed to acknowledge alchemy practice result."));
        }

        private void HandlePracticeCompleted(PracticeCompletedPacket packet)
        {
            if (!packet.Session.HasValue || packet.Session.Value.PracticeType != 2)
                return;

            alchemyState.ApplyPracticeSessionChange(
                null,
                MessageCode.None,
                packet.Result.HasValue
                    ? (packet.Result.Value.Success
                        ? "Alchemy practice completed successfully."
                        : "Alchemy practice completed with failure.")
                    : "Alchemy practice completed.");
        }

        private void HandleConnectionStateChanged(ClientConnectionState state)
        {
            if (state != ClientConnectionState.Disconnected)
                return;

            alchemyState.Clear();
            CompletePending(new AlchemyRecipeListLoadResult(
                false,
                null,
                Array.Empty<LearnedPillRecipeModel>(),
                "Connection closed.",
                false));
            CompletePending(ref loadDetailCompletionSource, new AlchemyRecipeDetailLoadResult(
                false,
                null,
                null,
                string.Empty,
                "Connection closed.",
                false));
            CompletePending(ref previewCompletionSource, new AlchemyCraftPreviewResult(
                false,
                null,
                null,
                string.Empty,
                "Connection closed."));
            CompletePending(ref craftCompletionSource, new AlchemyCraftExecuteResult(
                false,
                null,
                null,
                null,
                Array.Empty<InventoryItemModel>(),
                Array.Empty<AlchemyConsumedItemModel>(),
                string.Empty,
                "Connection closed."));
            CompletePending(ref practiceStatusCompletionSource, new AlchemyPracticeStatusLoadResult(
                false,
                null,
                null,
                "Connection closed."));
            CompletePending(ref pausePracticeCompletionSource, new PracticeSessionActionResult(false, null, null, "Connection closed."));
            CompletePending(ref resumePracticeCompletionSource, new PracticeSessionActionResult(false, null, null, "Connection closed."));
            CompletePending(ref cancelPracticeCompletionSource, new PracticeSessionActionResult(false, null, null, "Connection closed."));
            CompletePending(ref acknowledgePracticeCompletionSource, new PracticeSessionActionResult(false, null, null, "Connection closed."));
        }

        private void CompletePending(AlchemyRecipeListLoadResult result)
        {
            var pending = loadRecipesCompletionSource;
            loadRecipesCompletionSource = null;
            if (pending != null)
                pending.TrySetResult(result);
        }

        private static void CompletePending(ref TaskCompletionSource<AlchemyRecipeDetailLoadResult> completionSource, AlchemyRecipeDetailLoadResult result)
        {
            var pending = completionSource;
            completionSource = null;
            if (pending != null)
                pending.TrySetResult(result);
        }

        private static void CompletePending(ref TaskCompletionSource<AlchemyCraftPreviewResult> completionSource, AlchemyCraftPreviewResult result)
        {
            var pending = completionSource;
            completionSource = null;
            if (pending != null)
                pending.TrySetResult(result);
        }

        private static void CompletePending(ref TaskCompletionSource<AlchemyCraftExecuteResult> completionSource, AlchemyCraftExecuteResult result)
        {
            var pending = completionSource;
            completionSource = null;
            if (pending != null)
                pending.TrySetResult(result);
        }

        private static void CompletePending(ref TaskCompletionSource<AlchemyPracticeStatusLoadResult> completionSource, AlchemyPracticeStatusLoadResult result)
        {
            var pending = completionSource;
            completionSource = null;
            if (pending != null)
                pending.TrySetResult(result);
        }

        private static void CompletePending(ref TaskCompletionSource<PracticeSessionActionResult> completionSource, PracticeSessionActionResult result)
        {
            var pending = completionSource;
            completionSource = null;
            if (pending != null)
                pending.TrySetResult(result);
        }
    }
}
