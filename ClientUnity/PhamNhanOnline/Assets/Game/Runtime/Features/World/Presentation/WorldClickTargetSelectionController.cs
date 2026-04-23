using PhamNhanOnline.Client.Core.Application;
using PhamNhanOnline.Client.Features.Targeting.Application;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PhamNhanOnline.Client.Features.World.Presentation
{
    [DisallowMultipleComponent]
    public sealed class WorldClickTargetSelectionController : WorldSceneBehaviour
    {
        [Header("World References")]
        private WorldMapPresenter worldMapPresenter;
        private Camera worldCamera;
        [SerializeField] private LayerMask selectableLayers = ~0;

        [Header("Click Selection")]
        [SerializeField] private bool clearTargetWhenClickingEmptySpace = true;
        [SerializeField] private bool blockClicksWhenPointerIsOverInteractiveUi = true;
        [SerializeField] private float doubleClickThresholdSeconds = 0.3f;

        private readonly System.Collections.Generic.List<RaycastResult> uiRaycastResults =
            new System.Collections.Generic.List<RaycastResult>(8);
        private WorldTargetHandle lastClickedTargetHandle;
        private float lastTargetClickTime = float.NegativeInfinity;

        public void Initialize(Camera camera, WorldMapPresenter mapPresenter)
        {
            if (camera != null)
                worldCamera = camera;
            if (mapPresenter != null)
                worldMapPresenter = mapPresenter;
        }

        private void Awake()
        {
            if (worldCamera == null)
                worldCamera = Camera.main;

            AutoWireReferences();
            EnsureSelectableLayersConfigured();
        }

        private void Start()
        {
            AutoWireReferences();
            LogMissingCriticalWorldSceneDependenciesIfNeeded();
        }

        private void Update()
        {
            if (!ClientRuntime.IsInitialized)
                return;

            if (!IsSelectionRuntimeReady())
                return;

            if (!Input.GetMouseButtonDown(0))
                return;

            string uiBlockReason;
            if (ShouldBlockClickBecauseOfUI(out uiBlockReason))
            {
                WorldTravelDebugController.AppendExternalCharacterStatsDebugLine(uiBlockReason);
                return;
            }

            HandlePointerDown();
        }

        private void HandlePointerDown()
        {
            if (worldCamera == null)
            {
                WorldTravelDebugController.SetExternalCharacterStatsDebugLine("Target click: no world camera.");
                return;
            }

            var worldPosition3 = worldCamera.ScreenToWorldPoint(Input.mousePosition);
            var worldPosition = new Vector2(worldPosition3.x, worldPosition3.y);
            var hits = Physics2D.OverlapPointAll(worldPosition, selectableLayers);
            var bestTargetable = ResolveBestTargetable(hits, worldPosition);
            if (bestTargetable != null)
            {
                var handle = bestTargetable.Handle;
                var isDoubleClick = IsDoubleClickOnSameTarget(handle);
                WorldTravelDebugController.SetExternalCharacterStatsDebugLine(
                    $"Target click hit {hits.Length} collider(s): {bestTargetable.name} -> {handle.Kind}/{handle.TargetId}");
                bestTargetable.Select();
                RecordTargetClick(handle);
                if (isDoubleClick)
                {
                    var sceneController = SceneController;
                    if (sceneController != null)
                        sceneController.RequestPrimaryTargetAction(handle);
                }

                return;
            }

            WorldTravelDebugController.SetExternalCharacterStatsDebugLine(
                $"Target click empty at {worldPosition.x:0.00},{worldPosition.y:0.00} with {hits.Length} collider(s).");
            if (clearTargetWhenClickingEmptySpace)
                ClientRuntime.Target.Clear();
        }

        private bool IsDoubleClickOnSameTarget(WorldTargetHandle handle)
        {
            if (!handle.IsValid)
                return false;

            return lastClickedTargetHandle.IsValid &&
                   lastClickedTargetHandle.Equals(handle) &&
                   Time.unscaledTime - lastTargetClickTime <= Mathf.Max(0.05f, doubleClickThresholdSeconds);
        }

        private void RecordTargetClick(WorldTargetHandle handle)
        {
            lastClickedTargetHandle = handle;
            lastTargetClickTime = Time.unscaledTime;
        }

        private bool IsSelectionRuntimeReady()
        {
            return AreReady(WorldSceneReadyKey.MapVisual, WorldSceneReadyKey.LocalPlayer);
        }

        private bool ShouldBlockClickBecauseOfUI(out string reason)
        {
            reason = string.Empty;
            if (!blockClicksWhenPointerIsOverInteractiveUi || EventSystem.current == null)
                return false;

            if (!EventSystem.current.IsPointerOverGameObject())
                return false;

            var eventData = new PointerEventData(EventSystem.current)
            {
                position = Input.mousePosition
            };

            uiRaycastResults.Clear();
            EventSystem.current.RaycastAll(eventData, uiRaycastResults);
            if (uiRaycastResults.Count == 0)
            {
                reason = "Target click blocked by UI pointer state, but no UI raycast result was found.";
                return true;
            }

            var blockingNames = new System.Collections.Generic.List<string>();
            for (var i = 0; i < uiRaycastResults.Count; i++)
            {
                var uiObject = uiRaycastResults[i].gameObject;
                if (!IsInteractiveUI(uiObject))
                    continue;

                blockingNames.Add(uiObject.name);
            }

            if (blockingNames.Count == 0)
                return false;

            reason = $"Target click blocked by UI: {string.Join(", ", blockingNames)}.";
            return true;
        }

        private static bool IsInteractiveUI(GameObject uiObject)
        {
            if (uiObject == null)
                return false;

            if (uiObject.GetComponentInParent<Selectable>() != null)
                return true;

            if (ExecuteEvents.GetEventHandler<IPointerClickHandler>(uiObject) != null)
                return true;

            if (ExecuteEvents.GetEventHandler<IBeginDragHandler>(uiObject) != null)
                return true;

            if (ExecuteEvents.GetEventHandler<IDragHandler>(uiObject) != null)
                return true;

            if (ExecuteEvents.GetEventHandler<IScrollHandler>(uiObject) != null)
                return true;

            return false;
        }

        private static WorldTargetable ResolveBestTargetable(Collider2D[] hits, Vector2 worldPosition)
        {
            WorldTargetable bestTargetable = null;
            var bestScore = float.MaxValue;

            for (var i = 0; i < hits.Length; i++)
            {
                var hit = hits[i];
                if (hit == null)
                    continue;

                var targetable = hit.GetComponentInParent<WorldTargetable>();
                if (targetable == null || !targetable.isActiveAndEnabled)
                    continue;

                var score = Vector2.SqrMagnitude((Vector2)hit.bounds.center - worldPosition);
                if (score >= bestScore)
                    continue;

                bestScore = score;
                bestTargetable = targetable;
            }

            return bestTargetable;
        }

        private void EnsureSelectableLayersConfigured()
        {
            var targetableLayer = LayerMask.NameToLayer("Targetable");
            if (targetableLayer < 0)
                return;

            var targetableMask = 1 << targetableLayer;
            if (selectableLayers == ~0 || selectableLayers.value == 0)
                selectableLayers = targetableMask;
        }

        private void AutoWireReferences()
        {
            InitializeWorldSceneBehaviour(ref worldMapPresenter);

            if (worldCamera == null && SceneContext != null)
                worldCamera = SceneContext.WorldCamera;
        }
    }
}
