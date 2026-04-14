using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PhamNhanOnline.Client.UI.Crafting
{
    public sealed class CraftRecipeDragGhost
    {
        private readonly RectTransform rootRect;
        private readonly RectTransform canvasRect;
        private readonly GameObject rootObject;

        private CraftRecipeDragGhost(GameObject rootObject, RectTransform rootRect, RectTransform canvasRect)
        {
            this.rootObject = rootObject;
            this.rootRect = rootRect;
            this.canvasRect = canvasRect;
        }

        public static CraftRecipeDragGhost Create(Transform source, Transform visualSource, Sprite iconSprite, string label, PointerEventData eventData)
        {
            if (source == null)
                return null;

            var canvas = source.GetComponentInParent<Canvas>();
            if (canvas == null || canvas.rootCanvas == null)
                return null;

            var rootCanvas = canvas.rootCanvas;
            if (visualSource != null && TryCreateClonedVisualGhost(rootCanvas, visualSource, eventData, out var clonedGhost))
                return clonedGhost;

            var rootObject = new GameObject("CraftRecipeDragGhost", typeof(RectTransform), typeof(CanvasGroup));
            var rootRect = rootObject.GetComponent<RectTransform>();
            rootRect.SetParent(rootCanvas.transform, false);
            rootRect.SetAsLastSibling();
            rootRect.sizeDelta = new Vector2(188f, 64f);

            var canvasGroup = rootObject.GetComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
            canvasGroup.alpha = 0.94f;

            var backgroundObject = new GameObject("Background", typeof(RectTransform), typeof(Image));
            var backgroundRect = backgroundObject.GetComponent<RectTransform>();
            backgroundRect.SetParent(rootRect, false);
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;

            var backgroundImage = backgroundObject.GetComponent<Image>();
            backgroundImage.raycastTarget = false;
            backgroundImage.color = new Color(0f, 0f, 0f, 0.82f);

            var iconObject = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            var iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.SetParent(rootRect, false);
            iconRect.anchorMin = new Vector2(0f, 0.5f);
            iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.sizeDelta = new Vector2(44f, 44f);
            iconRect.anchoredPosition = new Vector2(30f, 0f);

            var iconImage = iconObject.GetComponent<Image>();
            iconImage.raycastTarget = false;
            iconImage.preserveAspect = true;
            iconImage.sprite = iconSprite;
            iconImage.color = iconSprite != null ? Color.white : new Color(1f, 1f, 1f, 0f);

            var labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            var labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.SetParent(rootRect, false);
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(1f, 1f);
            labelRect.offsetMin = new Vector2(58f, 8f);
            labelRect.offsetMax = new Vector2(-10f, -8f);

            var labelText = labelObject.GetComponent<TextMeshProUGUI>();
            labelText.raycastTarget = false;
            labelText.text = string.IsNullOrWhiteSpace(label) ? "Dan phuong" : label.Trim();
            labelText.fontSize = 22f;
            labelText.color = Color.white;
            labelText.alignment = TextAlignmentOptions.Left;
            labelText.enableWordWrapping = true;
            labelText.overflowMode = TextOverflowModes.Ellipsis;

            var ghost = new CraftRecipeDragGhost(rootObject, rootRect, rootCanvas.transform as RectTransform);
            ghost.UpdatePosition(eventData);
            return ghost;
        }

        public void UpdatePosition(PointerEventData eventData)
        {
            if (rootRect == null || canvasRect == null || eventData == null)
                return;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect,
                    eventData.position,
                    eventData.pressEventCamera,
                    out var localPoint))
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

        private static bool TryCreateClonedVisualGhost(Canvas rootCanvas, Transform visualSource, PointerEventData eventData, out CraftRecipeDragGhost ghost)
        {
            ghost = null;
            if (rootCanvas == null || visualSource == null)
                return false;

            var sourceRect = visualSource as RectTransform;
            if (sourceRect == null)
                return false;

            var rootObject = new GameObject("CraftRecipeDragGhost", typeof(RectTransform), typeof(CanvasGroup));
            var rootRect = rootObject.GetComponent<RectTransform>();
            rootRect.SetParent(rootCanvas.transform, false);
            rootRect.SetAsLastSibling();
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.sizeDelta = sourceRect.rect.size;

            var clone = Object.Instantiate(sourceRect.gameObject, rootRect.transform, false);
            clone.name = sourceRect.gameObject.name;
            clone.SetActive(true);

            var cloneRect = clone.transform as RectTransform;
            if (cloneRect != null)
            {
                cloneRect.anchorMin = new Vector2(0.5f, 0.5f);
                cloneRect.anchorMax = new Vector2(0.5f, 0.5f);
                cloneRect.pivot = sourceRect.pivot;
                cloneRect.sizeDelta = sourceRect.rect.size;
                cloneRect.anchoredPosition = Vector2.zero;
                cloneRect.localScale = Vector3.one;
                cloneRect.localRotation = Quaternion.identity;
            }

            DisableRaycasts(clone.transform);

            var canvasGroup = rootObject.GetComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
            canvasGroup.alpha = 0.82f;

            ghost = new CraftRecipeDragGhost(rootObject, rootRect, rootCanvas.transform as RectTransform);
            ghost.UpdatePosition(eventData);
            return true;
        }

        private static void DisableRaycasts(Transform root)
        {
            if (root == null)
                return;

            var graphics = root.GetComponentsInChildren<Graphic>(true);
            for (var i = 0; i < graphics.Length; i++)
                graphics[i].raycastTarget = false;
        }
    }
}
