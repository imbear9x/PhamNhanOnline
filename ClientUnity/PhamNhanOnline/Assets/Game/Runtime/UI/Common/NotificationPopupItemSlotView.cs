using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PhamNhanOnline.Client.UI.Common
{
    public sealed class NotificationPopupItemSlotView : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text quantityText;

        private void Start()
        {
            ValidateSerializedReferences();
        }

        public void Bind(Sprite iconSprite, Sprite backgroundSprite, int quantity)
        {
            var targetRoot = root != null ? root : gameObject;
            if (!targetRoot.activeSelf)
                targetRoot.SetActive(true);

            if (backgroundImage != null)
            {
                backgroundImage.sprite = backgroundSprite;
                backgroundImage.enabled = backgroundImage.sprite != null;
            }

            if (iconImage != null)
            {
                iconImage.sprite = iconSprite;
                iconImage.enabled = iconImage.sprite != null;
            }

            if (quantityText != null)
                quantityText.text = quantity > 1 ? quantity.ToString() : string.Empty;
        }

        public void Clear(bool force = false)
        {
            var targetRoot = root != null ? root : gameObject;
            if (!force && !targetRoot.activeSelf)
                return;

            targetRoot.SetActive(false);
            if (quantityText != null)
                quantityText.text = string.Empty;
        }

        private void ValidateSerializedReferences()
        {
            ThrowIfMissing(root, nameof(root));
            ThrowIfMissing(backgroundImage, nameof(backgroundImage));
            ThrowIfMissing(iconImage, nameof(iconImage));
            ThrowIfMissing(quantityText, nameof(quantityText));
        }

        private void ThrowIfMissing(UnityEngine.Object value, string fieldName)
        {
            if (value == null)
                throw new InvalidOperationException($"{nameof(NotificationPopupItemSlotView)} on '{gameObject.name}' is missing required reference '{fieldName}'.");
        }
    }
}
