using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PhamNhanOnline.Client.UI.World
{
    public sealed class MapZoneListItemView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Button button;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private TMP_Text zoneNameText;
        [SerializeField] private TMP_Text playerCountText;
        [SerializeField] private GameObject currentZoneBadgeRoot;
        [SerializeField] private TMP_Text currentZoneBadgeText;

        [Header("Display Text")]
        [SerializeField] private string currentZoneLabel = "Đang ở đây";

        private MapZoneListView.Entry entry;
        private bool hasEntry;

        public event Action<MapZoneListItemView> Clicked;

        public bool HasEntry => hasEntry;
        public MapZoneListView.Entry Entry => entry;

        private void Awake()
        {
            if (button == null)
                button = GetComponent<Button>();

            if (button != null)
            {
                button.onClick.RemoveListener(HandleClicked);
                button.onClick.AddListener(HandleClicked);
            }
        }

        private void OnDestroy()
        {
            if (button != null)
                button.onClick.RemoveListener(HandleClicked);
        }

        public void SetEntry(MapZoneListView.Entry value, bool force = false)
        {
            hasEntry = true;
            entry = value;

            ApplyText(zoneNameText, value.ZoneName, force);
            ApplyText(playerCountText, value.PlayerCountText, force);

            if (backgroundImage != null)
                backgroundImage.color = value.BackgroundColor;

            if (button != null)
                button.interactable = value.IsInteractable;

            if (currentZoneBadgeRoot != null)
                currentZoneBadgeRoot.SetActive(value.IsCurrentZone);

            if (currentZoneBadgeText != null)
                currentZoneBadgeText.text = value.IsCurrentZone ? currentZoneLabel : string.Empty;
        }

        public void Clear(bool force = false)
        {
            hasEntry = false;
            entry = default(MapZoneListView.Entry);

            ApplyText(zoneNameText, string.Empty, force);
            ApplyText(playerCountText, string.Empty, force);

            if (backgroundImage != null)
                backgroundImage.color = Color.white;

            if (button != null)
                button.interactable = false;

            if (currentZoneBadgeRoot != null)
                currentZoneBadgeRoot.SetActive(false);

            if (currentZoneBadgeText != null)
                currentZoneBadgeText.text = string.Empty;
        }

        private void HandleClicked()
        {
            if (!hasEntry)
                return;

            var handler = Clicked;
            if (handler != null)
                handler(this);
        }

        private static void ApplyText(TMP_Text textComponent, string value, bool force)
        {
            if (textComponent == null)
                return;

            var normalized = value ?? string.Empty;
            if (!force && string.Equals(textComponent.text, normalized, StringComparison.Ordinal))
                return;

            textComponent.text = normalized;
        }
    }
}
