using PhamNhanOnline.Client.Core.Application;
using PhamNhanOnline.Client.Features.Targeting.Application;
using PhamNhanOnline.Client.Features.World.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PhamNhanOnline.Client.UI.Hud
{
    public sealed class TargetStatusPanelController : MonoBehaviour
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

        [Header("Fallback Sprites")]
        [SerializeField] private Sprite defaultAvatarSprite;
        [SerializeField] private Sprite playerAvatarSprite;
        [SerializeField] private Sprite enemyAvatarSprite;
        [SerializeField] private Sprite bossAvatarSprite;
        [SerializeField] private Sprite npcAvatarSprite;

        [Header("Display")]
        [SerializeField] private bool hideWhenNoTarget = true;
        [SerializeField] private string noTargetName = string.Empty;

        private bool lastVisibleState = true;
        private string lastTargetKey = string.Empty;
        private string lastDisplayName = string.Empty;
        private int lastPrimaryCurrent = int.MinValue;
        private int lastPrimaryMax = int.MinValue;
        private int lastSecondaryCurrent = int.MinValue;
        private int lastSecondaryMax = int.MinValue;
        private bool lastHasSecondary;
        private WorldTargetKind lastKind = WorldTargetKind.None;

        private void Awake()
        {
            if (contentCanvasGroup == null && contentRoot == null)
                contentCanvasGroup = GetComponent<CanvasGroup>();

            ShowNoTarget(force: true);
        }

        public void ShowSnapshot(WorldTargetSnapshot snapshot, bool force = false)
        {
            ApplySnapshot(snapshot, force);
        }

        public void ShowNoTarget(bool force = false)
        {
            ApplyNoTarget(force);
        }

        private void ApplySnapshot(WorldTargetSnapshot snapshot, bool force)
        {
            const bool visible = true;
            var targetKey = snapshot.Kind + ":" + snapshot.TargetId;
            var changed =
                force ||
                lastVisibleState != visible ||
                !string.Equals(lastTargetKey, targetKey) ||
                !string.Equals(lastDisplayName, snapshot.DisplayName) ||
                lastPrimaryCurrent != snapshot.PrimaryCurrentValue ||
                lastPrimaryMax != snapshot.PrimaryMaxValue ||
                lastSecondaryCurrent != snapshot.SecondaryCurrentValue ||
                lastSecondaryMax != snapshot.SecondaryMaxValue ||
                lastHasSecondary != snapshot.HasSecondaryResource ||
                lastKind != snapshot.Kind;

            if (!changed)
                return;

            lastVisibleState = visible;
            lastTargetKey = targetKey;
            lastDisplayName = snapshot.DisplayName;
            lastPrimaryCurrent = snapshot.PrimaryCurrentValue;
            lastPrimaryMax = snapshot.PrimaryMaxValue;
            lastSecondaryCurrent = snapshot.SecondaryCurrentValue;
            lastSecondaryMax = snapshot.SecondaryMaxValue;
            lastHasSecondary = snapshot.HasSecondaryResource;
            lastKind = snapshot.Kind;

            SetContentVisible(true);

            if (nameText != null)
                nameText.text = snapshot.DisplayName;

            if (primaryBarRoot != null)
                primaryBarRoot.SetActive(snapshot.HasPrimaryResource);
            if (primaryBar != null)
                primaryBar.SetValues(snapshot.PrimaryCurrentValue, snapshot.PrimaryMaxValue, force: true);

            if (secondaryBarRoot != null)
                secondaryBarRoot.SetActive(snapshot.HasSecondaryResource);
            if (secondaryBar != null)
                secondaryBar.SetValues(snapshot.SecondaryCurrentValue, snapshot.SecondaryMaxValue, force: true);

            ApplyAvatar(snapshot.Kind);
        }

        private void ApplyNoTarget(bool force)
        {
            var visible = !hideWhenNoTarget;
            var changed =
                force ||
                lastVisibleState != visible ||
                !string.Equals(lastDisplayName, noTargetName) ||
                !string.IsNullOrEmpty(lastTargetKey) ||
                lastKind != WorldTargetKind.None;

            if (!changed)
                return;

            lastVisibleState = visible;
            lastTargetKey = string.Empty;
            lastDisplayName = noTargetName;
            lastPrimaryCurrent = int.MinValue;
            lastPrimaryMax = int.MinValue;
            lastSecondaryCurrent = int.MinValue;
            lastSecondaryMax = int.MinValue;
            lastHasSecondary = false;
            lastKind = WorldTargetKind.None;

            SetContentVisible(visible);

            if (nameText != null)
                nameText.text = noTargetName;

            if (primaryBarRoot != null)
                primaryBarRoot.SetActive(false);
            if (secondaryBarRoot != null)
                secondaryBarRoot.SetActive(false);

            ApplyAvatar(WorldTargetKind.None);
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

        private void ApplyAvatar(WorldTargetKind kind)
        {
            if (avatarImage == null)
                return;

            var sprite = ResolveAvatarSprite(kind);
            if (sprite != null && avatarImage.sprite != sprite)
                avatarImage.sprite = sprite;
        }

        private Sprite ResolveAvatarSprite(WorldTargetKind kind)
        {
            switch (kind)
            {
                case WorldTargetKind.Player:
                    return playerAvatarSprite != null ? playerAvatarSprite : defaultAvatarSprite;
                case WorldTargetKind.Enemy:
                    return enemyAvatarSprite != null ? enemyAvatarSprite : defaultAvatarSprite;
                case WorldTargetKind.Boss:
                    return bossAvatarSprite != null ? bossAvatarSprite : (enemyAvatarSprite != null ? enemyAvatarSprite : defaultAvatarSprite);
                case WorldTargetKind.Npc:
                    return npcAvatarSprite != null ? npcAvatarSprite : defaultAvatarSprite;
                default:
                    return defaultAvatarSprite;
            }
        }
    }

}
