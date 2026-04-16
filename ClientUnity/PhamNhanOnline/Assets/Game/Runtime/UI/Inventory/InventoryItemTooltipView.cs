using GameShared.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PhamNhanOnline.Client.UI.Inventory
{
    public sealed class InventoryItemTooltipView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private RectTransform panelTransform;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text descriptionText;

        [Header("Display")]
        [SerializeField] private string emptyName = "-";
        [SerializeField] private string emptyDescription = "Khong co mo ta.";
        [SerializeField] private Vector2 cursorOffsetBelow = new Vector2(20f, -20f);
        [SerializeField] private Vector2 cursorOffsetAbove = new Vector2(20f, 20f);
        [SerializeField] private Vector2 screenPadding = new Vector2(16f, 16f);

        private string lastSnapshot = string.Empty;

        private void Awake()
        {
            if (panelRoot == null)
                panelRoot = gameObject;

            if (panelTransform == null)
                panelTransform = panelRoot.transform as RectTransform;
        }

        public void Show(InventoryItemModel item, InventoryItemPresentation presentation, bool force = false)
        {
            var snapshot = BuildSnapshot(item, presentation);
            if (!force && string.Equals(lastSnapshot, snapshot, System.StringComparison.Ordinal))
                return;

            lastSnapshot = snapshot;

            if (panelRoot != null && !panelRoot.activeSelf)
                panelRoot.SetActive(true);

            if (iconImage != null)
                iconImage.sprite = presentation.IconSprite;

            if (nameText != null)
            {
                nameText.text = string.IsNullOrWhiteSpace(item.Name) ? emptyName : item.Name.Trim();
                nameText.color = presentation.NameColor;
            }

            if (descriptionText != null)
                descriptionText.text = string.IsNullOrWhiteSpace(item.Description) ? emptyDescription : item.Description.Trim();

            PositionNearCursor();
        }

        public void Hide(bool force = false)
        {
            if (!force && string.IsNullOrEmpty(lastSnapshot) && panelRoot != null && !panelRoot.activeSelf)
                return;

            lastSnapshot = string.Empty;
            if (panelRoot != null && panelRoot.activeSelf)
                panelRoot.SetActive(false);
        }

        private static string BuildSnapshot(InventoryItemModel item, InventoryItemPresentation presentation)
        {
            return string.Concat(
                item.PlayerItemId.ToString(),
                "|",
                item.Name ?? string.Empty,
                "|",
                item.Description ?? string.Empty,
                "|",
                presentation.IconSprite != null ? presentation.IconSprite.GetInstanceID().ToString() : "0");
        }

        private void PositionNearCursor()
        {
            if (panelTransform == null)
                return;

            var parent = panelTransform.parent as RectTransform;
            if (parent == null)
                return;

            Canvas.ForceUpdateCanvases();

            var canvas = parent.GetComponentInParent<Canvas>();
            var eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;

            var cursorScreenPoint = (Vector2)Input.mousePosition;
            var panelSize = panelTransform.rect.size;
            var offset = cursorOffsetBelow;

            if (cursorScreenPoint.y - panelSize.y - Mathf.Abs(cursorOffsetBelow.y) < screenPadding.y)
                offset = cursorOffsetAbove;

            var targetScreenPoint = cursorScreenPoint + offset;

            var minX = screenPadding.x + (panelSize.x * panelTransform.pivot.x);
            var maxX = Screen.width - screenPadding.x - (panelSize.x * (1f - panelTransform.pivot.x));
            var minY = screenPadding.y + (panelSize.y * panelTransform.pivot.y);
            var maxY = Screen.height - screenPadding.y - (panelSize.y * (1f - panelTransform.pivot.y));
            targetScreenPoint.x = Mathf.Clamp(targetScreenPoint.x, minX, maxX);
            targetScreenPoint.y = Mathf.Clamp(targetScreenPoint.y, minY, maxY);

            Vector2 localPoint;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, targetScreenPoint, eventCamera, out localPoint))
                return;

            panelTransform.anchoredPosition = localPoint;
        }
    }
}
