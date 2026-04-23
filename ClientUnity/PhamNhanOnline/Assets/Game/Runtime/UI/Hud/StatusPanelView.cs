using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PhamNhanOnline.Client.UI.Hud
{
    public readonly struct StatusPanelViewData
    {
        public StatusPanelViewData(
            bool visible,
            string identityKey,
            string displayName,
            Sprite avatarSprite,
            bool hasPrimaryResource,
            int primaryCurrentValue,
            int primaryMaxValue,
            bool hasSecondaryResource,
            int secondaryCurrentValue,
            int secondaryMaxValue)
        {
            Visible = visible;
            IdentityKey = identityKey ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            AvatarSprite = avatarSprite;
            HasPrimaryResource = hasPrimaryResource;
            PrimaryCurrentValue = primaryCurrentValue;
            PrimaryMaxValue = primaryMaxValue;
            HasSecondaryResource = hasSecondaryResource;
            SecondaryCurrentValue = secondaryCurrentValue;
            SecondaryMaxValue = secondaryMaxValue;
        }

        public bool Visible { get; }
        public string IdentityKey { get; }
        public string DisplayName { get; }
        public Sprite AvatarSprite { get; }
        public bool HasPrimaryResource { get; }
        public int PrimaryCurrentValue { get; }
        public int PrimaryMaxValue { get; }
        public bool HasSecondaryResource { get; }
        public int SecondaryCurrentValue { get; }
        public int SecondaryMaxValue { get; }
    }

    [DisallowMultipleComponent]
    public sealed class StatusPanelView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject contentRoot;
        [SerializeField] private CanvasGroup contentCanvasGroup;
        [SerializeField] private Image avatarImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private GameObject primaryBarRoot;
        [SerializeField] private StatBarView primaryBar;
        [SerializeField] private GameObject secondaryBarRoot;
        [SerializeField] private StatBarView secondaryBar;

        private bool lastVisibleState = true;
        private string lastIdentityKey = string.Empty;
        private string lastDisplayName = string.Empty;
        private Sprite lastAvatarSprite;
        private int lastPrimaryCurrent = int.MinValue;
        private int lastPrimaryMax = int.MinValue;
        private int lastSecondaryCurrent = int.MinValue;
        private int lastSecondaryMax = int.MinValue;
        private bool lastHasPrimary;
        private bool lastHasSecondary;

        private void Awake()
        {
            if (contentCanvasGroup == null && contentRoot == null)
                contentCanvasGroup = GetComponent<CanvasGroup>();
        }

        public void Apply(StatusPanelViewData data, bool force = false)
        {
            var changed =
                force ||
                lastVisibleState != data.Visible ||
                !string.Equals(lastIdentityKey, data.IdentityKey) ||
                !string.Equals(lastDisplayName, data.DisplayName) ||
                lastAvatarSprite != data.AvatarSprite ||
                lastPrimaryCurrent != data.PrimaryCurrentValue ||
                lastPrimaryMax != data.PrimaryMaxValue ||
                lastSecondaryCurrent != data.SecondaryCurrentValue ||
                lastSecondaryMax != data.SecondaryMaxValue ||
                lastHasPrimary != data.HasPrimaryResource ||
                lastHasSecondary != data.HasSecondaryResource;

            if (!changed)
                return;

            lastVisibleState = data.Visible;
            lastIdentityKey = data.IdentityKey;
            lastDisplayName = data.DisplayName;
            lastAvatarSprite = data.AvatarSprite;
            lastPrimaryCurrent = data.PrimaryCurrentValue;
            lastPrimaryMax = data.PrimaryMaxValue;
            lastSecondaryCurrent = data.SecondaryCurrentValue;
            lastSecondaryMax = data.SecondaryMaxValue;
            lastHasPrimary = data.HasPrimaryResource;
            lastHasSecondary = data.HasSecondaryResource;

            SetContentVisible(data.Visible);
            if (!data.Visible)
                return;

            if (nameText != null)
                nameText.text = data.DisplayName;

            if (avatarImage != null && avatarImage.sprite != data.AvatarSprite)
                avatarImage.sprite = data.AvatarSprite;

            if (primaryBarRoot != null)
                primaryBarRoot.SetActive(data.HasPrimaryResource);
            if (primaryBar != null)
                primaryBar.SetValues(data.PrimaryCurrentValue, data.PrimaryMaxValue, force: true);

            if (secondaryBarRoot != null)
                secondaryBarRoot.SetActive(data.HasSecondaryResource);
            if (secondaryBar != null)
                secondaryBar.SetValues(data.SecondaryCurrentValue, data.SecondaryMaxValue, force: true);
        }

        private void SetContentVisible(bool visible)
        {
            if (contentRoot != null)
            {
                if (contentRoot.activeSelf != visible)
                    contentRoot.SetActive(visible);
                return;
            }

            if (contentCanvasGroup != null)
            {
                contentCanvasGroup.alpha = visible ? 1f : 0f;
                contentCanvasGroup.interactable = visible;
                contentCanvasGroup.blocksRaycasts = visible;
            }
        }
    }
}
