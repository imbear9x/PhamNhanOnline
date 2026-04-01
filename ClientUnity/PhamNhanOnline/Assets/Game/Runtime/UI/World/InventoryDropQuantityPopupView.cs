using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PhamNhanOnline.Client.UI.World
{
    public sealed class InventoryDropQuantityPopupView : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text itemNameText;
        [SerializeField] private TMP_Text quantityText;
        [SerializeField] private Slider quantitySlider;
        [SerializeField] private TMP_InputField quantityInput;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private Button dimmerButton;
        [SerializeField] private string defaultTitle = "So luong vut";

        private bool suppressCallbacks;
        private int maxQuantity;
        private int currentQuantity;
        private Action<int> confirmAction;
        private Action cancelAction;

        public bool IsVisible
        {
            get { return panelRoot != null ? panelRoot.activeSelf : gameObject.activeSelf; }
        }

        private void Awake()
        {
            BindUi();
            Hide(force: true);
        }

        private void OnDestroy()
        {
            UnbindUi();
        }

        public void Show(
            string itemName,
            int maxDropQuantity,
            Action<int> onConfirm,
            Action onCancel = null,
            string titleOverride = null)
        {
            confirmAction = onConfirm;
            cancelAction = onCancel;
            maxQuantity = Mathf.Max(1, maxDropQuantity);
            currentQuantity = Mathf.Clamp(1, 1, maxQuantity);

            if (titleText != null)
                titleText.text = string.IsNullOrWhiteSpace(titleOverride) ? defaultTitle : titleOverride;

            if (itemNameText != null)
                itemNameText.text = string.IsNullOrWhiteSpace(itemName) ? "Vat pham" : itemName;

            if (quantitySlider != null)
            {
                quantitySlider.minValue = 1f;
                quantitySlider.maxValue = maxQuantity;
                quantitySlider.wholeNumbers = true;
            }

            ApplyQuantity(currentQuantity, force: true);
            SetVisible(true);
        }

        public void Hide(bool force = false)
        {
            confirmAction = null;
            cancelAction = null;
            if (!force && !IsVisible)
                return;

            SetVisible(false);
        }

        private void BindUi()
        {
            if (quantitySlider != null)
                quantitySlider.onValueChanged.AddListener(HandleSliderValueChanged);
            if (quantityInput != null)
                quantityInput.onValueChanged.AddListener(HandleInputValueChanged);
            if (confirmButton != null)
                confirmButton.onClick.AddListener(HandleConfirmClicked);
            if (cancelButton != null)
                cancelButton.onClick.AddListener(HandleCancelClicked);
            if (dimmerButton != null)
                dimmerButton.onClick.AddListener(HandleCancelClicked);
        }

        private void UnbindUi()
        {
            if (quantitySlider != null)
                quantitySlider.onValueChanged.RemoveListener(HandleSliderValueChanged);
            if (quantityInput != null)
                quantityInput.onValueChanged.RemoveListener(HandleInputValueChanged);
            if (confirmButton != null)
                confirmButton.onClick.RemoveListener(HandleConfirmClicked);
            if (cancelButton != null)
                cancelButton.onClick.RemoveListener(HandleCancelClicked);
            if (dimmerButton != null)
                dimmerButton.onClick.RemoveListener(HandleCancelClicked);
        }

        private void HandleSliderValueChanged(float value)
        {
            if (suppressCallbacks)
                return;

            ApplyQuantity(Mathf.RoundToInt(value), force: false);
        }

        private void HandleInputValueChanged(string rawValue)
        {
            if (suppressCallbacks)
                return;

            int parsedValue;
            if (!int.TryParse(rawValue, out parsedValue))
                parsedValue = currentQuantity;

            ApplyQuantity(parsedValue, force: false);
        }

        private void HandleConfirmClicked()
        {
            var callback = confirmAction;
            Hide(force: true);
            if (callback != null)
                callback(currentQuantity);
        }

        private void HandleCancelClicked()
        {
            var callback = cancelAction;
            Hide(force: true);
            if (callback != null)
                callback();
        }

        private void ApplyQuantity(int quantity, bool force)
        {
            var clampedQuantity = Mathf.Clamp(quantity, 1, Mathf.Max(1, maxQuantity));
            if (!force && clampedQuantity == currentQuantity)
                return;

            currentQuantity = clampedQuantity;
            suppressCallbacks = true;

            if (quantitySlider != null && Mathf.RoundToInt(quantitySlider.value) != currentQuantity)
                quantitySlider.SetValueWithoutNotify(currentQuantity);

            if (quantityInput != null && !string.Equals(quantityInput.text, currentQuantity.ToString(), StringComparison.Ordinal))
                quantityInput.SetTextWithoutNotify(currentQuantity.ToString());

            if (quantityText != null)
                quantityText.text = currentQuantity + " / " + maxQuantity;

            suppressCallbacks = false;
        }

        private void SetVisible(bool visible)
        {
            if (panelRoot != null)
                panelRoot.SetActive(visible);
            else
                gameObject.SetActive(visible);
        }
    }
}
