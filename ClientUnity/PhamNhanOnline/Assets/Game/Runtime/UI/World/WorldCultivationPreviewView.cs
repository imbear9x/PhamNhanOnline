using System;
using DG.Tweening;
using PhamNhanOnline.Client.Features.Character.Application;
using PhamNhanOnline.Client.UI.Common;
using TMPro;
using UnityEngine;

namespace PhamNhanOnline.Client.UI.World
{
    [DisallowMultipleComponent]
    public sealed class WorldCultivationPreviewView : ViewModelBase
    {
        [SerializeField] private GameObject previewRoot;
        [SerializeField] private GameObject estimateRoot;
        [SerializeField] private ArtMaterialSummaryView artMaterialSummaryView;
        [SerializeField] private TMP_Text mapQiDensityText;
        [SerializeField] private TMP_Text realmQiBonusText;
        [SerializeField] private TMP_Text estimateText;
        [SerializeField] private TMP_Text breakthroughText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text rewardExpText;

        [Header("Reward Exp Animation")]
        [SerializeField] private float rewardExpFloatDistance = 42f;
        [SerializeField] private float rewardExpAnimationDuration = 0.9f;

        private Sequence rewardExpSequence;
        private RectTransform rewardExpRectTransform;
        private Vector2 rewardExpBaseAnchoredPosition;
        private Color rewardExpBaseColor = Color.white;

        protected override GameObject ResolveViewRoot()
        {
            return previewRoot != null ? previewRoot : gameObject;
        }

        protected override void Awake()
        {
            base.Awake();

            if (rewardExpText != null)
            {
                rewardExpRectTransform = rewardExpText.rectTransform;
                rewardExpBaseAnchoredPosition = rewardExpRectTransform.anchoredPosition;
                rewardExpBaseColor = rewardExpText.color;
                rewardExpText.gameObject.SetActive(false);
            }
        }

        private void OnDisable()
        {
            StopRewardExpAnimation();
        }

        public void ValidateSerializedReferences()
        {
            ThrowIfMissing(estimateRoot, nameof(estimateRoot));
            ThrowIfMissing(artMaterialSummaryView, nameof(artMaterialSummaryView));
            ThrowIfMissing(mapQiDensityText, nameof(mapQiDensityText));
            ThrowIfMissing(realmQiBonusText, nameof(realmQiBonusText));
            ThrowIfMissing(estimateText, nameof(estimateText));
            ThrowIfMissing(breakthroughText, nameof(breakthroughText));
            ThrowIfMissing(statusText, nameof(statusText));
            ThrowIfMissing(rewardExpText, nameof(rewardExpText));
            artMaterialSummaryView.ValidateSerializedReferences();
        }

        public void SetData(
            string activeMartialArtName,
            Sprite activeMartialArtIcon,
            string activeMartialArtLevel,
            string activeMartialArtExp,
            long activeMartialArtCurrentExp,
            long activeMartialArtRequiredExp,
            string artQiRate,
            string mapQiDensity,
            string realmQiBonus,
            string estimate,
            string breakthrough,
            string status,
            bool estimateVisible,
            bool force)
        {
            SetEstimateVisible(estimateVisible);
            artMaterialSummaryView?.SetData(
                activeMartialArtIcon,
                activeMartialArtName,
                artQiRate,
                activeMartialArtLevel,
                activeMartialArtExp,
                activeMartialArtCurrentExp,
                activeMartialArtRequiredExp,
                force);
            ApplyText(mapQiDensityText, mapQiDensity, force);
            ApplyText(realmQiBonusText, realmQiBonus, force);
            ApplyText(estimateText, estimate, force);
            ApplyText(breakthroughText, breakthrough, force);
            ApplyText(statusText, status, force);
        }

        public void Clear(bool force)
        {
            artMaterialSummaryView?.Clear(force);
            StopRewardExpAnimation();
            SetData(
                string.Empty,
                null,
                string.Empty,
                string.Empty,
                0L,
                0L,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                false,
                force);
        }

        public void ShowRewardExp(CultivationRewardNotice notice)
        {
            if (notice.CultivationGranted <= 0L || rewardExpText == null)
                return;

            if (rewardExpRectTransform == null)
            {
                rewardExpRectTransform = rewardExpText.rectTransform;
                rewardExpBaseAnchoredPosition = rewardExpRectTransform.anchoredPosition;
            }

            StopRewardExpAnimation();
            rewardExpText.text = string.Format("+{0} tu vi", notice.CultivationGranted);
            rewardExpText.gameObject.SetActive(true);
            rewardExpText.color = rewardExpBaseColor;
            rewardExpRectTransform.anchoredPosition = rewardExpBaseAnchoredPosition;

            var duration = Mathf.Max(0.01f, rewardExpAnimationDuration);
            rewardExpSequence = DOTween.Sequence().SetUpdate(true);
            rewardExpSequence.Join(
                rewardExpRectTransform
                    .DOAnchorPosY(rewardExpBaseAnchoredPosition.y + rewardExpFloatDistance, duration)
                    .SetEase(Ease.OutQuad));
            rewardExpSequence.Join(
                rewardExpText
                    .DOFade(0f, duration)
                    .SetEase(Ease.Linear));
            rewardExpSequence.OnComplete(StopRewardExpAnimation);
        }

        private void SetEstimateVisible(bool visible)
        {
            if (estimateRoot != null && estimateRoot.activeSelf != visible)
                estimateRoot.SetActive(visible);
        }

        private static void ApplyText(TMP_Text text, string value, bool force)
        {
            if (text == null)
                return;

            var normalized = value ?? string.Empty;
            if (!force && string.Equals(text.text, normalized, StringComparison.Ordinal))
                return;

            text.text = normalized;
        }

        private void StopRewardExpAnimation()
        {
            if (rewardExpSequence != null)
            {
                rewardExpSequence.Kill();
                rewardExpSequence = null;
            }

            if (rewardExpRectTransform != null)
                rewardExpRectTransform.anchoredPosition = rewardExpBaseAnchoredPosition;

            if (rewardExpText != null)
            {
                rewardExpText.color = rewardExpBaseColor;
                rewardExpText.gameObject.SetActive(false);
            }
        }

        private void ThrowIfMissing(UnityEngine.Object value, string fieldName)
        {
            if (value == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(WorldCultivationPreviewView)} on '{gameObject.name}' is missing required reference '{fieldName}'.");
            }
        }
    }
}
