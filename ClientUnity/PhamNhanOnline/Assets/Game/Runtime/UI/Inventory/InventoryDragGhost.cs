using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PhamNhanOnline.Client.UI.Inventory
{
    public sealed class InventoryDragGhost
    {
        private const float DefaultGhostScale = 1f;

        private readonly RectTransform rootRect;
        private readonly RectTransform canvasRect;
        private readonly GameObject rootObject;

        private InventoryDragGhost(GameObject rootObject, RectTransform rootRect, RectTransform canvasRect)
        {
            this.rootObject = rootObject;
            this.rootRect = rootRect;
            this.canvasRect = canvasRect;
        }

        public static InventoryDragGhost Create(
            Transform source,
            InventoryItemPresentation presentation,
            PointerEventData eventData,
            RectTransform sizeReference = null)
        {
            if (source == null)
                return null;

            var canvas = source.GetComponentInParent<Canvas>();
            if (canvas == null || canvas.rootCanvas == null)
                return null;

            var rootCanvas = canvas.rootCanvas;
            var rootObject = new GameObject("InventoryDragGhost", typeof(RectTransform), typeof(CanvasGroup));
            var rootRect = rootObject.GetComponent<RectTransform>();
            rootRect.SetParent(rootCanvas.transform, false);
            rootRect.SetAsLastSibling();
            rootRect.sizeDelta = ResolveGhostSize(sizeReference, rootCanvas.transform as RectTransform);

            var canvasGroup = rootObject.GetComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
            canvasGroup.alpha = 0.92f;

            var backgroundObject = new GameObject("Background", typeof(RectTransform), typeof(Image));
            var backgroundRect = backgroundObject.GetComponent<RectTransform>();
            backgroundRect.SetParent(rootRect, false);
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;
            var backgroundImage = backgroundObject.GetComponent<Image>();
            backgroundImage.raycastTarget = false;
            backgroundImage.sprite = presentation.BackgroundSprite;
            backgroundImage.color = presentation.BackgroundSprite != null ? Color.white : new Color(1f, 1f, 1f, 0f);

            var iconObject = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            var iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.SetParent(rootRect, false);
            iconRect.anchorMin = new Vector2(0.12f, 0.12f);
            iconRect.anchorMax = new Vector2(0.88f, 0.88f);
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;
            var iconImage = iconObject.GetComponent<Image>();
            iconImage.raycastTarget = false;
            iconImage.sprite = presentation.IconSprite;
            iconImage.color = presentation.IconSprite != null ? Color.white : new Color(1f, 1f, 1f, 0f);
            iconImage.preserveAspect = true;

            var ghost = new InventoryDragGhost(rootObject, rootRect, rootCanvas.transform as RectTransform);
            ghost.UpdatePosition(eventData);
            return ghost;
        }

        private static Vector2 ResolveGhostSize(RectTransform sizeReference, RectTransform canvasRect)
        {
            if (sizeReference == null || canvasRect == null)
                return new Vector2(56f, 56f);

            var corners = new Vector3[4];
            sizeReference.GetWorldCorners(corners);

            Vector2 localMin;
            Vector2 localMax;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, corners[0], null, out localMin) ||
                !RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, corners[2], null, out localMax))
            {
                return new Vector2(56f, 56f);
            }

            var size = localMax - localMin;
            var width = Mathf.Max(24f, Mathf.Abs(size.x) * DefaultGhostScale);
            var height = Mathf.Max(24f, Mathf.Abs(size.y) * DefaultGhostScale);
            return new Vector2(width, height);
        }

        public void UpdatePosition(PointerEventData eventData)
        {
            if (rootRect == null || canvasRect == null || eventData == null)
                return;

            Vector2 localPoint;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect,
                    eventData.position,
                    eventData.pressEventCamera,
                    out localPoint))
            {
                return;
            }

            rootRect.anchoredPosition = localPoint;
        }

        public void Dispose()
        {
            if (rootObject != null)
                Object.Destroy(rootObject);
        }
    }
}
