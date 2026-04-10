using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PhamNhanOnline.Client.UI.Alchemy
{
    public sealed class AlchemyRecipeDragGhost
    {
        private readonly RectTransform rootRect;
        private readonly RectTransform canvasRect;
        private readonly GameObject rootObject;

        private AlchemyRecipeDragGhost(GameObject rootObject, RectTransform rootRect, RectTransform canvasRect)
        {
            this.rootObject = rootObject;
            this.rootRect = rootRect;
            this.canvasRect = canvasRect;
        }

        public static AlchemyRecipeDragGhost Create(Transform source, Sprite iconSprite, string label, PointerEventData eventData)
        {
            if (source == null)
                return null;

            var canvas = source.GetComponentInParent<Canvas>();
            if (canvas == null || canvas.rootCanvas == null)
                return null;

            var rootCanvas = canvas.rootCanvas;
            var rootObject = new GameObject("AlchemyRecipeDragGhost", typeof(RectTransform), typeof(CanvasGroup));
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

            var ghost = new AlchemyRecipeDragGhost(rootObject, rootRect, rootCanvas.transform as RectTransform);
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
    }
}
