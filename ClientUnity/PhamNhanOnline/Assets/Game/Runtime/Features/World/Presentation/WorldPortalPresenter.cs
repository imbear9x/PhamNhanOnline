using System;
using System.Collections.Generic;
using GameShared.Models;
using PhamNhanOnline.Client.Core.Application;
using PhamNhanOnline.Client.Core.Logging;
using PhamNhanOnline.Client.Features.Targeting.Application;
using TMPro;
using UnityEngine;

namespace PhamNhanOnline.Client.Features.World.Presentation
{
    public sealed partial class WorldPortalPresenter : WorldSceneBehaviour
    {
        private const int TouchInteractionMode = 1;
        private const int InteractInteractionMode = 2;
        private const string DefaultPortalVisualResourcePath = "World/Portals/PortalVisual_Default";

        private sealed class PortalRuntime
        {
            public MapPortalModel Portal;
            public GameObject RootObject;
            public PortalVisualInstance VisualInstance;
            public Collider2D TriggerCollider;
            public bool WasTouchingLastFrame;
            public string LastTouchDiagnosticKey;
        }

        [Header("References")]
        [SerializeField] private WorldMapPresenter worldMapPresenter;
        [SerializeField] private WorldTargetActionController worldTargetActionController;
        [SerializeField] private WorldLocalMovementSyncController worldLocalMovementSyncController;
        [SerializeField] private Transform portalRoot;
        [SerializeField] private GameObject portalVisualPrefab;

        [Header("Visuals")]
        [SerializeField] private int labelSortingOrder = 25;
        [SerializeField] private float edgePortalThresholdNormalized = 0.15f;
        [SerializeField] private Vector2 labelTargetPaddingWorldUnits = new Vector2(0.4f, 0.25f);
        [SerializeField] private Vector2 minLabelTargetSizeWorldUnits = new Vector2(2.2f, 0.9f);

        [Header("Behavior")]
        [SerializeField] private bool onlyShowEnabledPortals = true;
        [SerializeField] private float touchPortalRearmDelaySeconds = 0.75f;
        [SerializeField] private float rebuildRetryIntervalSeconds = 0.35f;
        [SerializeField] private float touchPortalHorizontalIntentDeadZone = 0.05f;

        [Header("Diagnostics")]
        [SerializeField] private bool logTouchPortalDiagnostics;

        private readonly Dictionary<int, PortalRuntime> spawnedPortals = new Dictionary<int, PortalRuntime>();
        private bool runtimeEventsBound;
        private bool usePortalInFlight;
        private float touchPortalSuppressedUntilTime;
        private bool rebuildRetryPending;
        private float nextRebuildRetryTime;
        private GameObject resolvedPortalVisualPrefab;
        private bool loggedMissingWorldMapPresenter;
        private bool loggedMissingTargetActionController;

        private void Awake()
        {
            AutoWireReferences();
        }

        private void Start()
        {
            AutoWireReferences();
            LogMissingCriticalWorldSceneDependenciesIfNeeded();
            LogMissingCriticalDependenciesIfNeeded();
            ActivateWorldSceneReadiness();
            TryBindRuntimeEvents();
            TryRebuildPortalsIfReady();
        }

        private void OnEnable()
        {
            AutoWireReferences();
            ActivateWorldSceneReadiness();
            TryBindRuntimeEvents();
            TryRebuildPortalsIfReady();
        }

        private void Update()
        {
            TryRetryDeferredRebuild();
            PollTouchPortals();
        }

        private void OnDisable()
        {
            DeactivateWorldSceneReadiness();
            UnbindRuntimeEvents();
            ClearPortals();
        }

        private void OnDestroy()
        {
            DeactivateWorldSceneReadiness();
            UnbindRuntimeEvents();
            ClearPortals();
        }

        protected override void ConfigureReadyWaits()
        {
            WaitFor(WorldSceneReadyKey.MapVisual, HandleMapVisualReady);
        }

        protected override void OnWorldLoadCycleStarted(int loadVersion, string mapKey)
        {
            rebuildRetryPending = false;
            ClearPortals();
        }

        private void HandleMapVisualReady()
        {
            RebuildPortals();
        }

        private void HandleCurrentTargetChanged()
        {
            RefreshPortalSelectionVisuals();
        }

        private void HandleInteractionRequested(WorldTargetHandle handle)
        {
            MapPortalModel portal;
            if (!ClientRuntime.IsInitialized || !ClientRuntime.World.TryGetPortal(handle, out portal))
                return;

            _ = UsePortalAsync(portal);
        }

        public bool TryResolvePortalWorldPosition(MapPortalModel portal, out Vector2 worldPosition)
        {
            worldPosition = default;
            if (portal.Equals(default(MapPortalModel)) || worldMapPresenter == null)
                return false;

            return worldMapPresenter.TryMapServerPositionToWorld(
                new Vector2(portal.SourceX, portal.SourceY),
                out worldPosition);
        }

        private enum TouchTriggerSide
        {
            None = 0,
            Left = 1,
            Right = 2
        }
    }
}
