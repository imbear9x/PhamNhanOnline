using System.Collections.Generic;
using GameShared.Packets;
using PhamNhanOnline.Client.Core.Application;
using PhamNhanOnline.Client.Core.Logging;
using PhamNhanOnline.Client.Features.Character.Presentation;
using UnityEngine;

namespace PhamNhanOnline.Client.Features.World.Presentation
{
    public sealed class WorldLocalMovementSyncController : WorldSceneBehaviour
    {
        [SerializeField] private WorldLocalPlayerPresenter localPlayerPresenter;
        [SerializeField] private WorldMapPresenter worldMapPresenter;
        [SerializeField] private WorldLocalMovementSyncConfig syncConfig;
        [Header("Debug")]
        [SerializeField] private bool logSyncSendReasons;

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
            ActivateWorldSceneReadiness();
            ResetSyncState();
        }

        private void OnEnable()
        {
            AutoWireReferences();
            ActivateWorldSceneReadiness();
        }

        private void OnDestroy()
        {
            DeactivateWorldSceneReadiness();
        }

        private void Update()
        {
            if (!ClientRuntime.IsInitialized)
                return;

            if (ClientRuntime.Connection.State != PhamNhanOnline.Client.Network.Session.ClientConnectionState.Connected)
                return;

            if (!IsReady(WorldSceneReadyKey.LocalPlayer))
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
                SendPosition(
                    "init",
                    serverPosition,
                    currentFacingLeft,
                    currentMovementPhase,
                    0f,
                    movementDistanceWithinWindow,
                    isMoving,
                    startedMoving: false,
                    stoppedMoving: false,
                    facingChanged: true,
                    phaseChanged: true);
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
                    SendPosition(
                        "state",
                        serverPosition,
                        currentFacingLeft,
                        currentMovementPhase,
                        distanceSinceLastSend,
                        movementDistanceWithinWindow,
                        isMoving,
                        startedMoving,
                        stoppedMoving,
                        facingChanged,
                        phaseChanged);
                    wasMovingLastFrame = isMoving;
                    return;
                }
            }

            if (timeSinceLastSend >= activeConfig.MinSyncIntervalSeconds && distanceSinceLastSend >= activeConfig.SyncDistanceThresholdMapUnits)
            {
                SendPosition(
                    "distance",
                    serverPosition,
                    currentFacingLeft,
                    currentMovementPhase,
                    distanceSinceLastSend,
                    movementDistanceWithinWindow,
                    isMoving,
                    startedMoving,
                    stoppedMoving,
                    facingChanged,
                    phaseChanged);
                wasMovingLastFrame = isMoving;
                return;
            }

            if (isMoving && timeSinceLastSend >= activeConfig.MaxSyncIntervalSeconds)
            {
                SendPosition(
                    "max",
                    serverPosition,
                    currentFacingLeft,
                    currentMovementPhase,
                    distanceSinceLastSend,
                    movementDistanceWithinWindow,
                    isMoving,
                    startedMoving,
                    stoppedMoving,
                    facingChanged,
                    phaseChanged);
                wasMovingLastFrame = true;
                return;
            }

            if (stoppedMoving
                && timeSinceLastSend >= activeConfig.MinSyncIntervalSeconds
                && distanceSinceLastSend >= activeConfig.FinalStopSyncThresholdMapUnits)
            {
                SendPosition(
                    "stop",
                    serverPosition,
                    currentFacingLeft,
                    currentMovementPhase,
                    distanceSinceLastSend,
                    movementDistanceWithinWindow,
                    isMoving,
                    startedMoving,
                    stoppedMoving,
                    facingChanged,
                    phaseChanged);
            }

            wasMovingLastFrame = isMoving;
        }

        public bool TryForceSyncCurrentPosition()
        {
            if (!ClientRuntime.IsInitialized)
                return false;

            if (ClientRuntime.Connection.State != PhamNhanOnline.Client.Network.Session.ClientConnectionState.Connected)
                return false;

            if (!IsReady(WorldSceneReadyKey.LocalPlayer))
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

            SendPosition(
                "force",
                serverPosition,
                currentFacingLeft,
                currentMovementPhase,
                hasSentPosition ? Vector2.Distance(lastSentServerPosition, serverPosition) : 0f,
                0f,
                false,
                false,
                false,
                !hasLastSentPresentationState || currentFacingLeft != lastSentFacingLeft,
                !hasLastSentPresentationState || currentMovementPhase != lastSentMovementPhase);
            wasMovingLastFrame = false;
            return true;
        }

        protected override void OnWorldLoadCycleStarted(int loadVersion, string mapKey)
        {
            ResetSyncState();
        }

        private void AutoWireReferences()
        {
            InitializeWorldSceneBehaviour(ref worldMapPresenter);

            if (localPlayerPresenter == null)
                localPlayerPresenter = SceneController != null ? SceneController.WorldLocalPlayerPresenter : GetComponent<WorldLocalPlayerPresenter>();
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
            string reason,
            Vector2 serverPosition,
            bool currentFacingLeft,
            LocalCharacterActionController.MovementSyncPhase currentMovementPhase,
            float distanceSinceLastSend,
            float movementDistanceWithinWindow,
            bool isMoving,
            bool startedMoving,
            bool stoppedMoving,
            bool facingChanged,
            bool phaseChanged)
        {
            ClientRuntime.Connection.Send(new CharacterPositionSyncPacket
            {
                CurrentPosX = serverPosition.x,
                CurrentPosY = serverPosition.y
            });

            if (logSyncSendReasons)
            {
                ClientLog.Info(
                    $"[MoveSync] reason={reason} dt={timeSinceLastSend:0.000}s dist={distanceSinceLastSend:0.###} " +
                    $"windowDist={movementDistanceWithinWindow:0.###} moving={isMoving} " +
                    $"start={startedMoving} stop={stoppedMoving} face={facingChanged} phase={phaseChanged} " +
                    $"pos=({serverPosition.x:0.##},{serverPosition.y:0.##})");
            }

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
