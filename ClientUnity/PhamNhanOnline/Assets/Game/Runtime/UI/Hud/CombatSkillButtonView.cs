using System;
using GameShared.Models;
using PhamNhanOnline.Client.UI.Common;
using PhamNhanOnline.Client.UI.Skills;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PhamNhanOnline.Client.UI.Hud
{
    public sealed class CombatSkillButtonView : UIButtonView
    {
        [Header("Identity")]
        [SerializeField] private int skillSlotIndex = 1;
        [SerializeField] private bool alwaysVisible;

        [Header("References")]
        [SerializeField] private GameObject contentRoot;
        [SerializeField] private Image iconImage;
        [SerializeField] private GameObject emptyStateRoot;
        [SerializeField] private GameObject disabledStateRoot;
        [SerializeField] private Image cooldownFillImage;
        [SerializeField] private TMP_Text cooldownText;

        private bool isVisible;
        private bool hasSkill;
        private bool isInteractable;
        private Sprite currentIconSprite;
        private string currentCooldownLabel = string.Empty;

        public new event Action<int> Clicked;

        public int SkillSlotIndex
        {
            get { return skillSlotIndex; }
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
            var canShowWithoutSkill = alwaysVisible || skillSlotIndex == 1;
            var resolvedVisible = visible && (hasAssignedSkill || canShowWithoutSkill);
            if (contentRoot != null && contentRoot.activeSelf != resolvedVisible)
                contentRoot.SetActive(resolvedVisible);
            else if (contentRoot == null && gameObject.activeSelf != resolvedVisible)
                gameObject.SetActive(resolvedVisible);

            isVisible = resolvedVisible;
            hasSkill = hasAssignedSkill;
            isInteractable = resolvedVisible && interactable && (hasAssignedSkill || canShowWithoutSkill);
            SetInteractable(isInteractable, force: true);

            if (emptyStateRoot != null)
                emptyStateRoot.SetActive(resolvedVisible && !hasAssignedSkill);

            if (disabledStateRoot != null)
                disabledStateRoot.SetActive(resolvedVisible && !interactable);

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

        protected override void InvokeLeftClick()
        {
            var canShowWithoutSkill = alwaysVisible || skillSlotIndex == 1;
            if (!isVisible || !isInteractable || (!hasSkill && !canShowWithoutSkill))
                return;

            base.InvokeLeftClick();
            Clicked?.Invoke(skillSlotIndex);
        }
    }
}
