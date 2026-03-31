using System.Collections.Generic;
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
        [SerializeField] private WorldSceneReadinessService readinessService;
        [SerializeField] private WorldLocalMovementSyncConfig syncConfig;

        private Vector2 lastSentServerPosition;
        private bool hasSentPosition;
        private bool wasMovingLastFrame;
        private float timeSinceLastSend;
        private bool warnedMissingMapping;
        private bool lastSentFacingLeft;
        private LocalCharacterActionController.MovementSyncPhase lastSentMovementPhase;
        private bool hasLastSentPresentationState;
        private WorldLocalMovementSyncConfig runtimeFallbackConfig;
        private readonly List<MovementObservationSample> movementObservationSamples = new();
        private float movementObservationClock;
        private bool runtimeEventsBound;

        private readonly struct MovementObservationSample
        {
            public MovementObservationSample(float time, Vector2 position)
            {
                Time = time;
                Position = position;
            }

            public float Time { get; }

            public Vector2 Position { get; }
        }

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

            AutoWireReferences();
            TryBindRuntimeEvents();
            ResetSyncState();
        }

        private void OnEnable()
        {
            AutoWireReferences();
            TryBindRuntimeEvents();
        }

        private void OnDestroy()
        {
            UnbindRuntimeEvents();
        }

        private void Update()
        {
            if (!ClientRuntime.IsInitialized)
                return;

            if (ClientRuntime.Connection.State != PhamNhanOnline.Client.Network.Session.ClientConnectionState.Connected)
                return;

            if (readinessService != null && !readinessService.IsReady(WorldSceneReadyKey.LocalPlayer))
                return;

            if (localPlayerPresenter == null || worldMapPresenter == null)
                return;

            var currentState = ClientRuntime.Character.CurrentState;
            if (currentState.HasValue && currentState.Value.CurrentState == CultivatingStateCode)
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
            movementObservationClock += Time.deltaTime;
            var activeConfig = ActiveConfig;

            var movementDistanceWithinWindow = ObserveMovementDistanceWithinWindow(
                serverPosition,
                Mathf.Max(0.01f, activeConfig.MovingDetectionWindowSeconds));
            var isMoving = movementDistanceWithinWindow >= activeConfig.MovingDetectionThresholdMapUnits;

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
                if (startedMoving || facingChanged || phaseChanged || distanceSinceLastSend >= activeConfig.FinalStopSyncThresholdMapUnits)
                {
                    SendPosition(serverPosition, currentFacingLeft, currentMovementPhase);
                    wasMovingLastFrame = isMoving;
                    return;
                }
            }

            if (timeSinceLastSend >= activeConfig.MinSyncIntervalSeconds && distanceSinceLastSend >= activeConfig.SyncDistanceThresholdMapUnits)
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
                && distanceSinceLastSend >= activeConfig.FinalStopSyncThresholdMapUnits)
            {
                SendPosition(serverPosition, currentFacingLeft, currentMovementPhase);
            }

            wasMovingLastFrame = isMoving;
        }

        public bool TryForceSyncCurrentPosition()
        {
            if (!ClientRuntime.IsInitialized)
                return false;

            if (ClientRuntime.Connection.State != PhamNhanOnline.Client.Network.Session.ClientConnectionState.Connected)
                return false;

            if (readinessService != null && !readinessService.IsReady(WorldSceneReadyKey.LocalPlayer))
                return false;

            if (localPlayerPresenter == null || worldMapPresenter == null)
                return false;

            var playerTransform = localPlayerPresenter.CurrentPlayerTransform;
            if (playerTransform == null)
                return false;

            Vector2 serverPosition;
            if (!worldMapPresenter.TryMapWorldPositionToServer(playerTransform.position, out serverPosition))
                return false;

            var localActionController = localPlayerPresenter.CurrentLocalActionController;
            var currentFacingLeft = localActionController != null ? localActionController.IsFacingLeft : false;
            var currentMovementPhase = localActionController != null
                ? localActionController.CurrentMovementSyncPhase
                : LocalCharacterActionController.MovementSyncPhase.Grounded;

            SendPosition(serverPosition, currentFacingLeft, currentMovementPhase);
            wasMovingLastFrame = false;
            return true;
        }

        private void HandleLoadCycleStarted(int loadVersion, string mapKey)
        {
            ResetSyncState();
        }

        private void TryBindRuntimeEvents()
        {
            if (runtimeEventsBound || !ClientRuntime.IsInitialized)
                return;

            if (readinessService != null)
                readinessService.LoadCycleStarted += HandleLoadCycleStarted;

            runtimeEventsBound = true;
        }

        private void UnbindRuntimeEvents()
        {
            if (!runtimeEventsBound || !ClientRuntime.IsInitialized)
                return;

            if (readinessService != null)
                readinessService.LoadCycleStarted -= HandleLoadCycleStarted;

            runtimeEventsBound = false;
        }

        private void AutoWireReferences()
        {
            if (localPlayerPresenter == null)
                localPlayerPresenter = GetComponent<WorldLocalPlayerPresenter>();

            if (worldMapPresenter == null)
                worldMapPresenter = GetComponent<WorldMapPresenter>();

            if (readinessService == null)
                readinessService = GetComponent<WorldSceneReadinessService>();

            if (readinessService == null && worldMapPresenter != null)
                readinessService = worldMapPresenter.GetComponent<WorldSceneReadinessService>();

            if (readinessService == null && WorldSceneController.Instance != null)
                readinessService = WorldSceneController.Instance.WorldSceneReadinessService;
        }

        private void ResetSyncState()
        {
            hasSentPosition = false;
            wasMovingLastFrame = false;
            timeSinceLastSend = 0f;
            movementObservationClock = 0f;
            movementObservationSamples.Clear();
            warnedMissingMapping = false;
            hasLastSentPresentationState = false;
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

        private float ObserveMovementDistanceWithinWindow(Vector2 serverPosition, float windowSeconds)
        {
            movementObservationSamples.Add(new MovementObservationSample(movementObservationClock, serverPosition));

            var cutoffTime = movementObservationClock - windowSeconds;
            while (movementObservationSamples.Count > 1 && movementObservationSamples[1].Time < cutoffTime)
                movementObservationSamples.RemoveAt(0);

            if (movementObservationSamples.Count <= 1)
                return 0f;

            var totalDistance = 0f;
            for (var i = 1; i < movementObservationSamples.Count; i++)
            {
                totalDistance += Vector2.Distance(
                    movementObservationSamples[i - 1].Position,
                    movementObservationSamples[i].Position);
            }

            return totalDistance;
        }

        private const int CultivatingStateCode = 3;
    }
}
