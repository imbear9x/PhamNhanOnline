using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PhamNhanOnline.Client.UI.Hud
{
    public sealed class StatBarView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Image fillImage;
        [SerializeField] private TMP_Text valueText;

        [Header("Formatting")]
        [SerializeField] private string valueFormat = "{0}/{1}";

        private int lastCurrentValue = int.MinValue;
        private int lastMaxValue = int.MinValue;

        public void SetValues(int currentValue, int maxValue, bool force = false)
        {
            currentValue = Mathf.Max(0, currentValue);
            maxValue = Mathf.Max(0, maxValue);

            if (!force && currentValue == lastCurrentValue && maxValue == lastMaxValue)
                return;

            lastCurrentValue = currentValue;
            lastMaxValue = maxValue;

            var normalizedValue = maxValue > 0
                ? Mathf.Clamp01((float)currentValue / maxValue)
                : 0f;

            if (fillImage != null)
                fillImage.fillAmount = normalizedValue;

            if (valueText != null)
                valueText.text = string.Format(valueFormat, currentValue, maxValue);
        }

        public void Clear(bool force = false)
        {
            SetValues(0, 0, force);
        }
    }
}
