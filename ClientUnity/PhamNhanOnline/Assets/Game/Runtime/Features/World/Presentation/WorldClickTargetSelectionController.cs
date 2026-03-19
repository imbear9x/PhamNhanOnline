using PhamNhanOnline.Client.Core.Application;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PhamNhanOnline.Client.Features.World.Presentation
{
    public sealed class WorldClickTargetSelectionController : MonoBehaviour
    {
        [SerializeField] private Camera worldCamera;
        [SerializeField] private LayerMask selectableLayers = ~0;
        [SerializeField] private bool clearTargetWhenClickingEmptySpace = true;
        [SerializeField] private bool blockClicksWhenPointerIsOverInteractiveUi = true;

        private readonly System.Collections.Generic.List<RaycastResult> uiRaycastResults =
            new System.Collections.Generic.List<RaycastResult>(8);

        public void Initialize(Camera camera)
        {
            if (camera != null)
                worldCamera = camera;
        }

        private void Awake()
        {
            if (worldCamera == null)
                worldCamera = Camera.main;
        }

        private void Update()
        {
            if (!ClientRuntime.IsInitialized || !Input.GetMouseButtonDown(0))
                return;

            string uiBlockReason;
            if (ShouldBlockClickBecauseOfUi(out uiBlockReason))
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
                WorldTravelDebugController.SetExternalCharacterStatsDebugLine(
                    $"Target click hit {hits.Length} collider(s): {bestTargetable.name} -> {handle.Kind}/{handle.TargetId}");
                bestTargetable.Select();
                return;
            }

            WorldTravelDebugController.SetExternalCharacterStatsDebugLine(
                $"Target click empty at {worldPosition.x:0.00},{worldPosition.y:0.00} with {hits.Length} collider(s).");
            if (clearTargetWhenClickingEmptySpace)
                ClientRuntime.Target.Clear();
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
                if (targetable == null)
                    continue;

                var score = Vector2.SqrMagnitude((Vector2)hit.bounds.center - worldPosition);
                if (score >= bestScore)
                    continue;

                bestScore = score;
                bestTargetable = targetable;
            }

            return bestTargetable;
        }

        private bool ShouldBlockClickBecauseOfUi(out string reason)
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
                if (!IsInteractiveUi(uiObject))
                    continue;

                blockingNames.Add(uiObject.name);
            }

            if (blockingNames.Count == 0)
                return false;

            reason = $"Target click blocked by UI: {string.Join(", ", blockingNames)}.";
            return true;
        }

        private static bool IsInteractiveUi(GameObject uiObject)
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
    }
}
