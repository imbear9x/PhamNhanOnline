using DG.Tweening;
using TMPro;
using PhamNhanOnline.Client.Infrastructure.Pooling;
using UnityEngine;
using UnityEngine.Serialization;

namespace PhamNhanOnline.Client.UI.Hud
{
    [DisallowMultipleComponent]
    public sealed class CombatValuePopupView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TMP_Text valueText;

        [Header("Animation")]
        [SerializeField] private float lifetimeSeconds = 0.8f;
        [FormerlySerializedAs("riseDistanceWorldUnits")]
        [SerializeField] private float riseDistanceUiUnits = 56f;
        [SerializeField] private float startFontSize = 60f;
        [SerializeField] private float endFontSize = 42f;
        [SerializeField] private float fadeStartNormalized = 0.1f;
        [FormerlySerializedAs("randomHorizontalOffsetRange")]
        [SerializeField] private Vector2 randomHorizontalOffsetUiUnits = new Vector2(-18f, 18f);

        private Vector2 startAnchoredPosition;
        private PooledInstance pooledInstance;
        private Sequence playSequence;

        private void Awake()
        {
            AutoWireReferences();
        }

        private void OnEnable()
        {
            AutoWireReferences();
        }

        public void Play(string text, Color color, Vector2 anchoredPosition)
        {
            AutoWireReferences();
            KillTween();

            startAnchoredPosition = anchoredPosition + new Vector2(
                Random.Range(randomHorizontalOffsetUiUnits.x, randomHorizontalOffsetUiUnits.y),
                0f);

            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = startAnchoredPosition;
                rectTransform.localScale = Vector3.one;
            }
            else
            {
                transform.localPosition = startAnchoredPosition;
                transform.localScale = Vector3.one;
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            if (valueText != null)
            {
                valueText.text = text ?? string.Empty;
                valueText.color = color;
                valueText.enableAutoSizing = false;
                valueText.fontSize = ResolveStartFontSize();
            }

            var duration = Mathf.Max(0.01f, lifetimeSeconds);
            var fadeStartTime = Mathf.Clamp01(fadeStartNormalized) * duration;
            var fadeDuration = Mathf.Max(0.01f, duration - fadeStartTime);

            playSequence = DOTween.Sequence().SetUpdate(false);
            if (rectTransform != null)
            {
                playSequence.Join(rectTransform.DOAnchorPosY(startAnchoredPosition.y + riseDistanceUiUnits, duration).SetEase(Ease.Linear));
            }
            else
            {
                playSequence.Join(transform.DOLocalMoveY(startAnchoredPosition.y + riseDistanceUiUnits, duration).SetEase(Ease.Linear));
            }

            if (valueText != null)
            {
                var targetFontSize = ResolveEndFontSize(valueText.fontSize);
                if (!Mathf.Approximately(valueText.fontSize, targetFontSize))
                {
                    playSequence.Join(DOTween
                        .To(
                            () => valueText.fontSize,
                            value => valueText.fontSize = value,
                            targetFontSize,
                            duration)
                        .SetEase(Ease.OutQuad));
                }
            }

            if (canvasGroup != null)
            {
                playSequence.Insert(
                    fadeStartTime,
                    DOTween
                        .To(() => canvasGroup.alpha, value => canvasGroup.alpha = value, 0f, fadeDuration)
                        .SetEase(Ease.Linear));
            }

            playSequence.OnComplete(CompleteAndRelease);
        }

        private void CompleteAndRelease()
        {
            KillTween();
            if (pooledInstance == null)
                pooledInstance = GetComponent<PooledInstance>();

            if (pooledInstance != null)
                pooledInstance.Release();
            else
                Destroy(gameObject);
        }

        private void AutoWireReferences()
        {
            if (rectTransform == null)
                rectTransform = transform as RectTransform;

            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            if (valueText == null)
                valueText = GetComponentInChildren<TMP_Text>(true);

            if (pooledInstance == null)
                pooledInstance = GetComponent<PooledInstance>();
        }

        private void OnDisable()
        {
            KillTween();
        }

        private void KillTween()
        {
            if (playSequence != null)
            {
                playSequence.Kill();
                playSequence = null;
            }
        }

        private float ResolveStartFontSize()
        {
            if (startFontSize > 0f)
                return startFontSize;

            return valueText != null && valueText.fontSize > 0f
                ? valueText.fontSize
                : 60f;
        }

        private float ResolveEndFontSize(float fallback)
        {
            if (endFontSize > 0f)
                return endFontSize;

            return fallback > 0f ? fallback : ResolveStartFontSize();
        }
    }
}
