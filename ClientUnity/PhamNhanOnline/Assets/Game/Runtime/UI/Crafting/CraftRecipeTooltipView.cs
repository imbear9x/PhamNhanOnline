using System;
using System.Globalization;
using System.Text;
using GameShared.Models;
using PhamNhanOnline.Client.UI.Common;
using PhamNhanOnline.Client.UI.World;
using TMPro;
using UnityEngine;

namespace PhamNhanOnline.Client.UI.Crafting
{
    public sealed class CraftRecipeTooltipView : CursorPopupViewModelBase
    {
        [Header("References")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private RectTransform panelTransform;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text ingredientsText;

        [Header("Display")]
        [SerializeField] private string emptyName = "Dan phuong";
        [SerializeField] private string emptyDescription = "Khong co mo ta.";
        [SerializeField] private Vector2 cursorOffsetBelow = new Vector2(24f, -20f);
        [SerializeField] private Vector2 cursorOffsetAbove = new Vector2(24f, 20f);
        [SerializeField] private Vector2 screenPadding = new Vector2(16f, 16f);

        private string lastSnapshot = string.Empty;

        protected override bool HideOnFirstAwake => true;

        protected override GameObject ResolveViewRoot()
        {
            return panelRoot != null ? panelRoot : gameObject;
        }

        protected override RectTransform ResolveViewRectTransform()
        {
            return panelTransform != null ? panelTransform : base.ResolveViewRectTransform();
        }

        private void Start()
        {
            ValidateSerializedReferences();
        }

        public void Show(PillRecipeDetailModel detail, Func<PillRecipeInputModel, int> quantityResolver, bool force = false)
        {
            var snapshot = BuildSnapshot(detail, quantityResolver);
            if (!force && string.Equals(lastSnapshot, snapshot, StringComparison.Ordinal))
                return;

            lastSnapshot = snapshot;

            ShowView();

            if (nameText != null)
                nameText.text = string.IsNullOrWhiteSpace(detail.Name) ? emptyName : detail.Name.Trim();
            if (descriptionText != null)
                descriptionText.text = string.IsNullOrWhiteSpace(detail.Description) ? emptyDescription : detail.Description.Trim();
            if (ingredientsText != null)
                ingredientsText.text = BuildIngredientsText(detail, quantityResolver);

            PositionViewNearCursor(cursorOffsetBelow, cursorOffsetAbove, screenPadding);
        }

        public void Hide(bool force = false)
        {
            if (!force && string.IsNullOrEmpty(lastSnapshot) && panelRoot != null && !panelRoot.activeSelf)
                return;

            lastSnapshot = string.Empty;
            SetViewVisible(false);
        }

        private static string BuildIngredientsText(PillRecipeDetailModel detail, Func<PillRecipeInputModel, int> quantityResolver)
        {
            if (detail.Inputs == null || detail.Inputs.Count == 0)
                return "Khong co nguyen lieu.";

            var builder = new StringBuilder();
            for (var i = 0; i < detail.Inputs.Count; i++)
            {
                var input = detail.Inputs[i];
                var currentQuantity = Math.Max(0, quantityResolver != null ? quantityResolver(input) : 0);
                if (builder.Length > 0)
                    builder.AppendLine();

                builder.Append("* ");
                builder.Append(string.IsNullOrWhiteSpace(input.RequiredItem.Name) ? "Nguyen lieu" : input.RequiredItem.Name.Trim());
                builder.Append(' ');
                builder.Append(currentQuantity.ToString(CultureInfo.InvariantCulture));
                builder.Append('/');
                builder.Append(Math.Max(1, input.RequiredQuantity).ToString(CultureInfo.InvariantCulture));
                if (input.IsOptional)
                    builder.Append(" (tuy chon)");
            }

            return builder.ToString();
        }

        private static string BuildSnapshot(PillRecipeDetailModel detail, Func<PillRecipeInputModel, int> quantityResolver)
        {
            var builder = new StringBuilder();
            builder.Append(detail.PillRecipeTemplateId.ToString(CultureInfo.InvariantCulture));
            builder.Append('|');
            builder.Append(detail.Name ?? string.Empty);
            builder.Append('|');
            builder.Append(detail.Description ?? string.Empty);

            if (detail.Inputs != null)
            {
                for (var i = 0; i < detail.Inputs.Count; i++)
                {
                    var input = detail.Inputs[i];
                    builder.Append('|');
                    builder.Append(input.InputId.ToString(CultureInfo.InvariantCulture));
                    builder.Append(':');
                    builder.Append(quantityResolver != null ? quantityResolver(input).ToString(CultureInfo.InvariantCulture) : "0");
                }
            }

            return builder.ToString();
        }

        private void ValidateSerializedReferences()
        {
            ThrowIfMissing(panelRoot, nameof(panelRoot));
            ThrowIfMissing(panelTransform, nameof(panelTransform));
            ThrowIfMissing(nameText, nameof(nameText));
            ThrowIfMissing(descriptionText, nameof(descriptionText));
            ThrowIfMissing(ingredientsText, nameof(ingredientsText));

            if (panelRoot.GetComponent<WorldCraftingPanelController>() != null)
                throw new InvalidOperationException($"{nameof(CraftRecipeTooltipView)} on '{gameObject.name}' must use its own tooltip root, not the crafting panel root.");
        }

        private void ThrowIfMissing(UnityEngine.Object value, string fieldName)
        {
            if (value == null)
                throw new InvalidOperationException($"{nameof(CraftRecipeTooltipView)} on '{gameObject.name}' is missing required reference '{fieldName}'.");
        }
    }
}
