using System;
using GameShared.Models;
using PhamNhanOnline.Client.UI.Skills;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PhamNhanOnline.Client.UI.Hud
{
    public sealed class CombatSkillButtonView : MonoBehaviour,
        IPointerDownHandler,
        IPointerUpHandler,
        IPointerExitHandler
    {
        [Header("Identity")]
        [SerializeField] private int skillSlotIndex = 1;
        [SerializeField] private bool alwaysVisible;

        [Header("References")]
        [SerializeField] private GameObject contentRoot;
        [SerializeField] private Button button;
        [SerializeField] private Image iconImage;
        [SerializeField] private GameObject disabledStateRoot;
        [SerializeField] private Image cooldownFillImage;
        [SerializeField] private TMP_Text cooldownText;

        [Header("Press Animation")]
        [SerializeField] private RectTransform pressVisualRoot;
        [SerializeField] private float pressedScale = 1.06f;
        [SerializeField] private float pressedYOffset = -6f;
        [SerializeField] private float pressLerpSpeed = 20f;

        private bool isVisible;
        private bool hasSkill;
        private bool isInteractable;
        private bool isPressed;
        private Sprite currentIconSprite;
        private string currentCooldownLabel = string.Empty;
        private Vector3 idleScale = Vector3.one;
        private Vector2 idleAnchoredPosition;
        private bool pressVisualInitialized;

        public event Action<int> Clicked;

        public int SkillSlotIndex
        {
            get { return skillSlotIndex; }
        }

        private void Awake()
        {
            AutoWireReferences();
            if (button != null)
                button.onClick.AddListener(HandleButtonClicked);
        }

        private void Update()
        {
            UpdatePressAnimation();
        }

        private void OnEnable()
        {
            AutoWireReferences();
            ResetPressVisuals(immediate: true);
        }

        private void OnDisable()
        {
            ResetPressVisuals(immediate: true);
        }

        private void OnDestroy()
        {
            if (button != null)
                button.onClick.RemoveListener(HandleButtonClicked);
        }

        public void SetSlotIndex(int value)
        {
            skillSlotIndex = Math.Max(1, value);
        }

        public void ApplyState(
            bool visible,
            bool hasAssignedSkill,
            PlayerSkillModel skill,
            SkillPresentation presentation,
            bool interactable,
            float cooldownFillAmount,
            string cooldownLabel,
            bool showCooldown)
        {
            var resolvedVisible = visible && hasAssignedSkill;
            if (contentRoot != null && contentRoot.activeSelf != resolvedVisible)
                contentRoot.SetActive(resolvedVisible);
            else if (contentRoot == null && gameObject.activeSelf != resolvedVisible)
                gameObject.SetActive(resolvedVisible);

            isVisible = resolvedVisible;
            hasSkill = hasAssignedSkill;
            isInteractable = resolvedVisible && hasAssignedSkill && interactable;
            if (!isInteractable)
                isPressed = false;

            if (button != null)
                button.interactable = isInteractable;

            if (disabledStateRoot != null)
                disabledStateRoot.SetActive(resolvedVisible && hasAssignedSkill && !interactable);

            var nextIcon = hasAssignedSkill ? presentation.IconSprite : null;
            if (iconImage != null)
            {
                if (currentIconSprite != nextIcon)
                {
                    currentIconSprite = nextIcon;
                    iconImage.sprite = nextIcon;
                }

                iconImage.enabled = resolvedVisible && hasAssignedSkill && nextIcon != null;
            }

            if (cooldownFillImage != null)
            {
                cooldownFillImage.gameObject.SetActive(resolvedVisible && hasAssignedSkill && showCooldown);
                cooldownFillImage.fillAmount = showCooldown ? Mathf.Clamp01(cooldownFillAmount) : 0f;
            }

            var resolvedCooldownLabel = showCooldown ? (cooldownLabel ?? string.Empty) : string.Empty;
            if (cooldownText != null)
            {
                if (!string.Equals(currentCooldownLabel, resolvedCooldownLabel, StringComparison.Ordinal))
                {
                    currentCooldownLabel = resolvedCooldownLabel;
                    cooldownText.text = resolvedCooldownLabel;
                }

                cooldownText.gameObject.SetActive(resolvedVisible && hasAssignedSkill && showCooldown && !string.IsNullOrEmpty(resolvedCooldownLabel));
            }
        }

        public void Hide()
        {
            ApplyState(false, false, default(PlayerSkillModel), default(SkillPresentation), false, 0f, string.Empty, false);
        }

        private void HandleButtonClicked()
        {
            if (!isVisible || !hasSkill || !isInteractable)
                return;

            var handler = Clicked;
            if (handler != null)
                handler(skillSlotIndex);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!CanAnimatePress(eventData))
                return;

            isPressed = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            isPressed = false;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isPressed = false;
        }

        private void AutoWireReferences()
        {
            if (button == null)
                button = GetComponent<Button>();

            if (pressVisualRoot == null)
            {
                if (contentRoot != null)
                    pressVisualRoot = contentRoot.transform as RectTransform;
                else
                    pressVisualRoot = transform as RectTransform;
            }

            if (pressVisualRoot != null && !pressVisualInitialized)
            {
                idleScale = pressVisualRoot.localScale;
                idleAnchoredPosition = pressVisualRoot.anchoredPosition;
                pressVisualInitialized = true;
            }
        }

        private void UpdatePressAnimation()
        {
            if (pressVisualRoot == null)
                return;

            var targetScale = isPressed && isInteractable
                ? idleScale * Mathf.Max(0.01f, pressedScale)
                : idleScale;
            var targetAnchoredPosition = isPressed && isInteractable
                ? idleAnchoredPosition + new Vector2(0f, pressedYOffset)
                : idleAnchoredPosition;
            var lerpFactor = 1f - Mathf.Exp(-Mathf.Max(0.01f, pressLerpSpeed) * Time.unscaledDeltaTime);

            pressVisualRoot.localScale = Vector3.LerpUnclamped(pressVisualRoot.localScale, targetScale, lerpFactor);
            pressVisualRoot.anchoredPosition = Vector2.LerpUnclamped(pressVisualRoot.anchoredPosition, targetAnchoredPosition, lerpFactor);
        }

        private void ResetPressVisuals(bool immediate)
        {
            isPressed = false;
            AutoWireReferences();
            if (pressVisualRoot == null)
                return;

            if (immediate)
            {
                pressVisualRoot.localScale = idleScale;
                pressVisualRoot.anchoredPosition = idleAnchoredPosition;
            }
        }

        private bool CanAnimatePress(PointerEventData eventData)
        {
            return eventData != null &&
                   eventData.button == PointerEventData.InputButton.Left &&
                   isVisible &&
                   hasSkill &&
                   isInteractable;
        }
    }
}
