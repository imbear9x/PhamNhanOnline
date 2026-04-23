using PhamNhanOnline.Client.Core.Application;
using PhamNhanOnline.Client.Features.Targeting.Application;
using UnityEngine;

namespace PhamNhanOnline.Client.UI.Hud
{
    public sealed class TargetStatusPanelController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private StatusPanelView statusPanelView;

        [Header("Fallback Sprites")]
        [SerializeField] private Sprite defaultAvatarSprite;
        [SerializeField] private Sprite playerAvatarSprite;
        [SerializeField] private Sprite enemyAvatarSprite;
        [SerializeField] private Sprite bossAvatarSprite;
        [SerializeField] private Sprite npcAvatarSprite;

        [Header("Display")]
        [SerializeField] private bool hideWhenNoTarget = true;
        [SerializeField] private string noTargetName = string.Empty;

        private void Awake()
        {
            AutoWireReferences();
            ShowNoTarget(force: true);
        }

        public void ShowSnapshot(WorldTargetSnapshot snapshot, bool force = false)
        {
            if (statusPanelView == null)
                return;

            statusPanelView.Apply(
                new StatusPanelViewData(
                    true,
                    snapshot.Kind + ":" + snapshot.TargetId,
                    snapshot.DisplayName,
                    ResolveAvatarSprite(snapshot.Kind),
                    snapshot.HasPrimaryResource,
                    snapshot.PrimaryCurrentValue,
                    snapshot.PrimaryMaxValue,
                    snapshot.HasSecondaryResource,
                    snapshot.SecondaryCurrentValue,
                    snapshot.SecondaryMaxValue),
                force);
        }

        public void ShowNoTarget(bool force = false)
        {
            if (statusPanelView == null)
                return;

            statusPanelView.Apply(
                new StatusPanelViewData(
                    !hideWhenNoTarget,
                    string.Empty,
                    noTargetName,
                    ResolveAvatarSprite(WorldTargetKind.None),
                    false,
                    0,
                    0,
                    false,
                    0,
                    0),
                force);
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

        private void AutoWireReferences()
        {
            if (statusPanelView == null)
                statusPanelView = GetComponent<StatusPanelView>();
        }
    }
}
