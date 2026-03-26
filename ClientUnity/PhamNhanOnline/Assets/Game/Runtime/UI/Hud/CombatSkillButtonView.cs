using System;
using GameShared.Models;
using PhamNhanOnline.Client.UI.Skills;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PhamNhanOnline.Client.UI.Hud
{
    public sealed class CombatSkillButtonView : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private int skillSlotIndex = 1;
        [SerializeField] private bool alwaysVisible;

        [Header("References")]
        [SerializeField] private GameObject contentRoot;
        [SerializeField] private Button button;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text slotLabelText;
        [SerializeField] private GameObject occupiedStateRoot;
        [SerializeField] private GameObject emptyStateRoot;
        [SerializeField] private GameObject disabledStateRoot;
        [SerializeField] private Image cooldownFillImage;
        [SerializeField] private TMP_Text cooldownText;

        private bool isVisible;
        private bool hasSkill;
        private bool isInteractable;
        private Sprite currentIconSprite;
        private string currentCooldownLabel = string.Empty;

        public event Action<int> Clicked;

        public int SkillSlotIndex
        {
            get { return skillSlotIndex; }
        }

        private void Awake()
        {
            if (button != null)
                button.onClick.AddListener(HandleButtonClicked);

            ApplySlotLabel();
        }

        private void OnDestroy()
        {
            if (button != null)
                button.onClick.RemoveListener(HandleButtonClicked);
        }

        public void SetSlotIndex(int value)
        {
            skillSlotIndex = Math.Max(1, value);
            ApplySlotLabel();
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
            var resolvedVisible = alwaysVisible || visible;
            if (contentRoot != null && contentRoot.activeSelf != resolvedVisible)
                contentRoot.SetActive(resolvedVisible);
            else if (contentRoot == null && gameObject.activeSelf != resolvedVisible)
                gameObject.SetActive(resolvedVisible);

            isVisible = resolvedVisible;
            hasSkill = hasAssignedSkill;
            isInteractable = resolvedVisible && hasAssignedSkill && interactable;

            if (button != null)
                button.interactable = isInteractable;

            if (occupiedStateRoot != null)
                occupiedStateRoot.SetActive(resolvedVisible && hasAssignedSkill);

            if (emptyStateRoot != null)
                emptyStateRoot.SetActive(resolvedVisible && !hasAssignedSkill);

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

        private void ApplySlotLabel()
        {
            if (slotLabelText != null)
                slotLabelText.text = skillSlotIndex.ToString();
        }
    }
}
