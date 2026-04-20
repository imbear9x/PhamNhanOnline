using DG.Tweening;
using TMPro;
using PhamNhanOnline.Client.Infrastructure.Pooling;
using UnityEngine;

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
        [SerializeField] private float riseDistanceWorldUnits = 0.7f;
        [SerializeField] private float startScale = 1f;
        [SerializeField] private float endScale = 0.82f;
        [SerializeField] private float fadeStartNormalized = 0.1f;
        [SerializeField] private Vector2 randomHorizontalOffsetRange = new Vector2(-0.12f, 0.12f);

        private Vector3 startWorldPosition;
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

        public void Play(string text, Color color, Vector3 worldPosition)
        {
            AutoWireReferences();
            KillTween();

            startWorldPosition = worldPosition + new Vector3(
                Random.Range(randomHorizontalOffsetRange.x, randomHorizontalOffsetRange.y),
                0f,
                0f);

            if (rectTransform != null)
            {
                rectTransform.position = startWorldPosition;
                rectTransform.localScale = Vector3.one * startScale;
            }
            else
            {
                transform.position = startWorldPosition;
                transform.localScale = Vector3.one * startScale;
            }

            if (canvasGroup != null)
                canvasGroup.alpha = 1f;

            if (valueText != null)
            {
                valueText.text = text ?? string.Empty;
                valueText.color = color;
            }

            var duration = Mathf.Max(0.01f, lifetimeSeconds);
            var fadeStartTime = Mathf.Clamp01(fadeStartNormalized) * duration;
            var fadeDuration = Mathf.Max(0.01f, duration - fadeStartTime);

            playSequence = DOTween.Sequence().SetUpdate(false);
            if (rectTransform != null)
            {
                playSequence.Join(rectTransform.DOMoveY(startWorldPosition.y + riseDistanceWorldUnits, duration).SetEase(Ease.Linear));
                playSequence.Join(rectTransform.DOScale(endScale, duration).SetEase(Ease.OutQuad));
            }
            else
            {
                playSequence.Join(transform.DOMoveY(startWorldPosition.y + riseDistanceWorldUnits, duration).SetEase(Ease.Linear));
                playSequence.Join(transform.DOScale(endScale, duration).SetEase(Ease.OutQuad));
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
    }
}
