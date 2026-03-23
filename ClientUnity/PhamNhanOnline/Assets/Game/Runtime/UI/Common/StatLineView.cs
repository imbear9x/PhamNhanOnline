using System;
using System.Globalization;
using TMPro;
using UnityEngine;

namespace PhamNhanOnline.Client.UI.Common
{
    public sealed class StatLineView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TMP_Text statNameText;
        [SerializeField] private TMP_Text statValueText;

        [Header("Formatting")]
        [SerializeField] private string emptyName = "-";
        [SerializeField] private string emptyValue = "-";

        private string lastStatName = string.Empty;
        private string lastStatValue = string.Empty;

        public void SetValues(string statName, string statValue, bool force = false)
        {
            statName = string.IsNullOrWhiteSpace(statName) ? emptyName : statName.Trim();
            statValue = string.IsNullOrWhiteSpace(statValue) ? emptyValue : statValue.Trim();

            if (!force &&
                string.Equals(lastStatName, statName, StringComparison.Ordinal) &&
                string.Equals(lastStatValue, statValue, StringComparison.Ordinal))
            {
                return;
            }

            lastStatName = statName;
            lastStatValue = statValue;

            if (statNameText != null)
                statNameText.text = statName;

            if (statValueText != null)
                statValueText.text = statValue;
        }

        public void SetValues(string statName, int statValue, bool force = false)
        {
            SetValues(statName, statValue.ToString(CultureInfo.InvariantCulture), force);
        }

        public void SetValues(string statName, long statValue, bool force = false)
        {
            SetValues(statName, statValue.ToString(CultureInfo.InvariantCulture), force);
        }

        public void SetValues(string statName, float statValue, string format = "0.##", bool force = false)
        {
            SetValues(statName, statValue.ToString(format, CultureInfo.InvariantCulture), force);
        }

        public void Clear(bool force = false)
        {
            SetValues(emptyName, emptyValue, force);
        }
    }
}
