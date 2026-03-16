using GameShared.Packets;
using PhamNhanOnline.Client.Core.Application;
using PhamNhanOnline.Client.Core.Logging;
using PhamNhanOnline.Client.Features.Character.Presentation;
using UnityEngine;

namespace PhamNhanOnline.Client.Features.World.Presentation
{
    public sealed class WorldLocalMovementSyncController : MonoBehaviour
    {
        [SerializeField] private WorldLocalPlayerPresenter localPlayerPresenter;
        [SerializeField] private WorldMapPresenter worldMapPresenter;
        [SerializeField] private WorldLocalMovementSyncConfig syncConfig;

        private Vector2 lastObservedServerPosition;
        private Vector2 lastSentServerPosition;
        private bool hasObservedPosition;
        private bool hasSentPosition;
        private bool wasMovingLastFrame;
        private float timeSinceLastSend;
        private bool warnedMissingMapping;
        private bool lastSentFacingLeft;
        private LocalCharacterActionController.MovementSyncPhase lastSentMovementPhase;
        private bool hasLastSentPresentationState;
        private WorldLocalMovementSyncConfig runtimeFallbackConfig;

        private WorldLocalMovementSyncConfig ActiveConfig
        {
            get
            {
                if (syncConfig != null)
                    return syncConfig;

                if (runtimeFallbackConfig == null)
                    runtimeFallbackConfig = WorldLocalMovementSyncConfig.CreateRuntimeDefaults();

                return runtimeFallbackConfig;
            }
        }

        private void Start()
        {
            if (!ClientRuntime.IsInitialized)
                return;

            ClientRuntime.World.MapChanged += HandleMapChanged;
            ResetSyncState();
        }

        private void OnDestroy()
        {
            if (!ClientRuntime.IsInitialized)
                return;

            ClientRuntime.World.MapChanged -= HandleMapChanged;
        }

        private void Update()
        {
            if (!ClientRuntime.IsInitialized)
                return;

            if (ClientRuntime.Connection.State != PhamNhanOnline.Client.Network.Session.ClientConnectionState.Connected)
                return;

            if (localPlayerPresenter == null || worldMapPresenter == null)
                return;

            var playerTransform = localPlayerPresenter.CurrentPlayerTransform;
            if (playerTransform == null)
                return;

            Vector2 serverPosition;
            if (!worldMapPresenter.TryMapWorldPositionToServer(playerTransform.position, out serverPosition))
            {
                if (!warnedMissingMapping)
                {
                    ClientLog.Warn("WorldLocalMovementSyncController could not map local world position to server coordinates.");
                    warnedMissingMapping = true;
                }

                return;
            }

            warnedMissingMapping = false;
            timeSinceLastSend += Time.deltaTime;
            var activeConfig = ActiveConfig;

            var movedSinceObserve = hasObservedPosition
                ? Vector2.Distance(lastObservedServerPosition, serverPosition)
                : 0f;
            var isMoving = movedSinceObserve >= activeConfig.MovingDetectionThreshold;
            lastObservedServerPosition = serverPosition;
            hasObservedPosition = true;

            var localActionController = localPlayerPresenter.CurrentLocalActionController;
            var currentFacingLeft = localActionController != null ? localActionController.IsFacingLeft : false;
            var currentMovementPhase = localActionController != null
                ? localActionController.CurrentMovementSyncPhase
                : LocalCharacterActionController.MovementSyncPhase.Grounded;

            if (!hasSentPosition)
            {
                SendPosition(serverPosition, currentFacingLeft, currentMovementPhase);
                wasMovingLastFrame = isMoving;
                return;
            }

            var distanceSinceLastSend = Vector2.Distance(lastSentServerPosition, serverPosition);
            var startedMoving = isMoving && !wasMovingLastFrame;
            var stoppedMoving = !isMoving && wasMovingLastFrame;
            var facingChanged = !hasLastSentPresentationState || currentFacingLeft != lastSentFacingLeft;
            var phaseChanged = !hasLastSentPresentationState || currentMovementPhase != lastSentMovementPhase;
            var shouldSyncStateChange = startedMoving || stoppedMoving || facingChanged || phaseChanged;

            if (shouldSyncStateChange && timeSinceLastSend >= activeConfig.ImmediateStateChangeSyncIntervalSeconds)
            {
                if (startedMoving || facingChanged || phaseChanged || distanceSinceLastSend >= activeConfig.FinalStopSyncThreshold)
                {
                    SendPosition(serverPosition, currentFacingLeft, currentMovementPhase);
                    wasMovingLastFrame = isMoving;
                    return;
                }
            }

            if (timeSinceLastSend >= activeConfig.MinSyncIntervalSeconds && distanceSinceLastSend >= activeConfig.SyncDistanceThreshold)
            {
                SendPosition(serverPosition, currentFacingLeft, currentMovementPhase);
                wasMovingLastFrame = isMoving;
                return;
            }

            if (isMoving && timeSinceLastSend >= activeConfig.MaxSyncIntervalSeconds)
            {
                SendPosition(serverPosition, currentFacingLeft, currentMovementPhase);
                wasMovingLastFrame = true;
                return;
            }

            if (stoppedMoving
                && timeSinceLastSend >= activeConfig.MinSyncIntervalSeconds
                && distanceSinceLastSend >= activeConfig.FinalStopSyncThreshold)
            {
                SendPosition(serverPosition, currentFacingLeft, currentMovementPhase);
            }

            wasMovingLastFrame = isMoving;
        }

        private void HandleMapChanged()
        {
            ResetSyncState();
        }

        private void ResetSyncState()
        {
            hasObservedPosition = false;
            hasSentPosition = false;
            wasMovingLastFrame = false;
            timeSinceLastSend = 0f;
            warnedMissingMapping = false;
            hasLastSentPresentationState = false;
            lastObservedServerPosition = Vector2.zero;
            lastSentServerPosition = Vector2.zero;
            lastSentFacingLeft = false;
            lastSentMovementPhase = LocalCharacterActionController.MovementSyncPhase.Grounded;
        }

        private void SendPosition(
            Vector2 serverPosition,
            bool currentFacingLeft,
            LocalCharacterActionController.MovementSyncPhase currentMovementPhase)
        {
            ClientRuntime.Connection.Send(new CharacterPositionSyncPacket
            {
                CurrentPosX = serverPosition.x,
                CurrentPosY = serverPosition.y
            });

            lastSentServerPosition = serverPosition;
            lastSentFacingLeft = currentFacingLeft;
            lastSentMovementPhase = currentMovementPhase;
            hasLastSentPresentationState = true;
            hasSentPosition = true;
            timeSinceLastSend = 0f;
        }
    }
}
