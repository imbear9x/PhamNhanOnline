using GameShared.Models;
using PhamNhanOnline.Client.UI.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PhamNhanOnline.Client.UI.Inventory
{
    public sealed class InventoryItemTooltipView : CursorPopupViewModelBase
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

        protected override bool HideOnFirstAwake => true;

        protected override void Awake()
        {
            if (panelRoot == null)
                panelRoot = gameObject;

            if (panelTransform == null)
                panelTransform = panelRoot.transform as RectTransform;

            base.Awake();
        }

        protected override GameObject ResolveViewRoot()
        {
            return panelRoot != null ? panelRoot : gameObject;
        }

        protected override RectTransform ResolveViewRectTransform()
        {
            return panelTransform != null ? panelTransform : base.ResolveViewRectTransform();
        }

        public void Show(InventoryItemModel item, InventoryItemPresentation presentation, bool force = false)
        {
            var snapshot = BuildSnapshot(item, presentation);
            if (!force && string.Equals(lastSnapshot, snapshot, System.StringComparison.Ordinal))
                return;

            lastSnapshot = snapshot;

            ShowView();

            if (iconImage != null)
                iconImage.sprite = presentation.IconSprite;

            if (nameText != null)
            {
                nameText.text = string.IsNullOrWhiteSpace(item.Name) ? emptyName : item.Name.Trim();
                nameText.color = presentation.NameColor;
            }

            if (descriptionText != null)
                descriptionText.text = string.IsNullOrWhiteSpace(item.Description) ? emptyDescription : item.Description.Trim();

            PositionViewNearCursor(cursorOffsetBelow, cursorOffsetAbove, screenPadding);
        }

        public void Hide(bool force = false)
        {
            if (!force && string.IsNullOrEmpty(lastSnapshot) && panelRoot != null && !panelRoot.activeSelf)
                return;

            lastSnapshot = string.Empty;
            SetViewVisible(false);
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

    }
}
