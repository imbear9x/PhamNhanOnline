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
        private float elapsedSeconds;
        private bool isPlaying;
        private PooledInstance pooledInstance;

        private void Awake()
        {
            AutoWireReferences();
        }

        private void OnEnable()
        {
            AutoWireReferences();
        }

        private void Update()
        {
            if (!isPlaying)
                return;

            elapsedSeconds += Time.deltaTime;
            var normalized = lifetimeSeconds > 0f
                ? Mathf.Clamp01(elapsedSeconds / lifetimeSeconds)
                : 1f;

            var verticalOffset = Mathf.LerpUnclamped(0f, riseDistanceWorldUnits, normalized);
            var scale = Mathf.LerpUnclamped(startScale, endScale, normalized);
            var alpha = normalized <= fadeStartNormalized
                ? 1f
                : 1f - Mathf.InverseLerp(fadeStartNormalized, 1f, normalized);

            if (rectTransform != null)
            {
                rectTransform.position = startWorldPosition + (Vector3.up * verticalOffset);
                rectTransform.localScale = Vector3.one * scale;
            }
            else
            {
                transform.position = startWorldPosition + (Vector3.up * verticalOffset);
                transform.localScale = Vector3.one * scale;
            }

            if (canvasGroup != null)
                canvasGroup.alpha = Mathf.Clamp01(alpha);

            if (normalized >= 1f)
                CompleteAndRelease();
        }

        public void Play(string text, Color color, Vector3 worldPosition)
        {
            AutoWireReferences();

            elapsedSeconds = 0f;
            isPlaying = true;
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
        }

        private void CompleteAndRelease()
        {
            isPlaying = false;
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
    }
}
