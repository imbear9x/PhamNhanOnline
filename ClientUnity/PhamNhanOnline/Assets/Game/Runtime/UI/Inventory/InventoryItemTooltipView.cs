using System.Globalization;
using System.Text;
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
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text metaText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text quantityText;

        [Header("Display")]
        [SerializeField] private string emptyName = "-";
        [SerializeField] private string emptyDescription = "Khong co mo ta.";

        private string lastSnapshot = string.Empty;

        private void Awake()
        {
            if (panelRoot == null)
                panelRoot = gameObject;

            Hide(force: true);
        }

        public void Show(InventoryItemModel item, InventoryItemPresentation presentation, bool force = false)
        {
            var snapshot = BuildSnapshot(item, presentation);
            if (!force && string.Equals(lastSnapshot, snapshot, System.StringComparison.Ordinal))
                return;

            lastSnapshot = snapshot;

            if (panelRoot != null && !panelRoot.activeSelf)
                panelRoot.SetActive(true);

            if (backgroundImage != null)
                backgroundImage.sprite = presentation.BackgroundSprite;

            if (iconImage != null)
                iconImage.sprite = presentation.IconSprite;

            if (nameText != null)
            {
                nameText.text = string.IsNullOrWhiteSpace(item.Name) ? emptyName : item.Name.Trim();
                nameText.color = presentation.NameColor;
            }

            if (metaText != null)
                metaText.text = BuildMetaText(item);

            if (descriptionText != null)
                descriptionText.text = string.IsNullOrWhiteSpace(item.Description) ? emptyDescription : item.Description.Trim();

            if (quantityText != null)
                quantityText.text = item.Quantity > 1 ? string.Format(CultureInfo.InvariantCulture, "x{0}", item.Quantity) : string.Empty;
        }

        public void Hide(bool force = false)
        {
            if (!force && string.IsNullOrEmpty(lastSnapshot) && panelRoot != null && !panelRoot.activeSelf)
                return;

            lastSnapshot = string.Empty;
            if (panelRoot != null && panelRoot.activeSelf)
                panelRoot.SetActive(false);
        }

        private static string BuildMetaText(InventoryItemModel item)
        {
            var builder = new StringBuilder();
            builder.Append(InventoryItemPresentationCatalog.GetRarityLabel(item.Rarity));
            builder.Append(" | ");
            builder.Append(InventoryItemPresentationCatalog.GetItemTypeLabel(item.ItemType));

            var slotLabel = InventoryItemPresentationCatalog.GetEquipmentSlotLabel(item.EquippedSlot);
            if (!string.IsNullOrWhiteSpace(slotLabel))
            {
                builder.AppendLine();
                builder.Append("Slot: ");
                builder.Append(slotLabel);
            }

            if (item.IsEquipped)
            {
                builder.AppendLine();
                builder.Append("Dang trang bi");
            }

            if (item.EnhanceLevel > 0)
            {
                builder.AppendLine();
                builder.Append("Tang cap +");
                builder.Append(item.EnhanceLevel.ToString(CultureInfo.InvariantCulture));
            }

            if (item.Durability.HasValue)
            {
                builder.AppendLine();
                builder.Append("Do ben: ");
                builder.Append(item.Durability.Value.ToString(CultureInfo.InvariantCulture));
            }

            if (item.IsBound)
            {
                builder.AppendLine();
                builder.Append("Da khoa");
            }

            return builder.ToString();
        }

        private static string BuildSnapshot(InventoryItemModel item, InventoryItemPresentation presentation)
        {
            return string.Concat(
                item.PlayerItemId.ToString(CultureInfo.InvariantCulture),
                "|",
                item.Name ?? string.Empty,
                "|",
                item.Description ?? string.Empty,
                "|",
                item.Quantity.ToString(CultureInfo.InvariantCulture),
                "|",
                item.Rarity.ToString(CultureInfo.InvariantCulture),
                "|",
                item.ItemType.ToString(CultureInfo.InvariantCulture),
                "|",
                item.IsEquipped ? "1" : "0",
                "|",
                item.EnhanceLevel.ToString(CultureInfo.InvariantCulture),
                "|",
                item.Durability.HasValue ? item.Durability.Value.ToString(CultureInfo.InvariantCulture) : "-",
                "|",
                presentation.IconSprite != null ? presentation.IconSprite.GetInstanceID().ToString(CultureInfo.InvariantCulture) : "0",
                "|",
                presentation.BackgroundSprite != null ? presentation.BackgroundSprite.GetInstanceID().ToString(CultureInfo.InvariantCulture) : "0");
        }
    }
}
