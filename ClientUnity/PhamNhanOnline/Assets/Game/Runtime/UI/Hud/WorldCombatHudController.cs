using System;
using System.Globalization;
using GameShared.Models;
using PhamNhanOnline.Client.Core.Application;
using PhamNhanOnline.Client.Core.Logging;
using PhamNhanOnline.Client.Features.Combat.Application;
using PhamNhanOnline.Client.Features.Skills.Application;
using PhamNhanOnline.Client.Features.Targeting.Application;
using PhamNhanOnline.Client.UI.Skills;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PhamNhanOnline.Client.UI.Hud
{
    public sealed class WorldCombatHudController : MonoBehaviour
    {
        private const int BasicSkillSlotIndex = 1;
        private const int SelfSkillTargetType = 1;

        [Header("References")]
        [SerializeField] private SkillPresentationCatalog presentationCatalog;
        [SerializeField] private CombatSkillButtonView basicSkillButton;
        [SerializeField] private CombatSkillButtonView[] additionalSkillButtons = Array.Empty<CombatSkillButtonView>();

        [Header("Cast Bar")]
        [SerializeField] private GameObject castBarRoot;
        [SerializeField] private Image castBarFillImage;
        [SerializeField] private TMP_Text castBarText;

        [Header("Behavior")]
        [SerializeField] private bool autoLoadSkillsOnEnable = true;
        [SerializeField] private float reloadRetryCooldownSeconds = 2f;

        [Header("Display Text")]
        [SerializeField] private string castBarDefaultText = "Dang thi trien...";

        private Guid? lastRequestedCharacterId;
        private float lastSkillReloadAttemptTime = float.NegativeInfinity;
        private bool skillReloadInFlight;

        private void Awake()
        {
            NormalizeButtonSlotIndices();
            SubscribeButtons();
            ApplyCastBar(false, 0f);
        }

        private void OnEnable()
        {
            Refresh(force: true);
            TryReloadSkillsOnOpen();
        }

        private void Update()
        {
            Refresh(force: false);
            TryReloadMissingSkills();
        }

        private void OnDestroy()
        {
            UnsubscribeButtons();
        }

        private void SubscribeButtons()
        {
            if (basicSkillButton != null)
                basicSkillButton.Clicked += HandleSkillButtonClicked;

            if (additionalSkillButtons == null)
                return;

            for (var i = 0; i < additionalSkillButtons.Length; i++)
            {
                var button = additionalSkillButtons[i];
                if (button != null)
                    button.Clicked += HandleSkillButtonClicked;
            }
        }

        private void NormalizeButtonSlotIndices()
        {
            if (basicSkillButton != null)
                basicSkillButton.SetSlotIndex(BasicSkillSlotIndex);

            if (additionalSkillButtons == null)
                return;

            for (var i = 0; i < additionalSkillButtons.Length; i++)
            {
                var button = additionalSkillButtons[i];
                if (button == null)
                    continue;

                button.SetSlotIndex(i + 2);
            }
        }

        private void UnsubscribeButtons()
        {
            if (basicSkillButton != null)
                basicSkillButton.Clicked -= HandleSkillButtonClicked;

            if (additionalSkillButtons == null)
                return;

            for (var i = 0; i < additionalSkillButtons.Length; i++)
            {
                var button = additionalSkillButtons[i];
                if (button != null)
                    button.Clicked -= HandleSkillButtonClicked;
            }
        }

        private void Refresh(bool force)
        {
            if (!ClientRuntime.IsInitialized)
            {
                ApplyMissingState();
                return;
            }

            var skillState = ClientRuntime.Skills;
            if (!skillState.HasLoadedSkills)
            {
                ApplyMissingState();
                return;
            }

            var utcNow = DateTime.UtcNow;
            ClientRuntime.Combat.IsLocalCastActive(utcNow);
            ApplyButtonState(basicSkillButton, BasicSkillSlotIndex, skillState, utcNow);

            if (additionalSkillButtons != null)
            {
                for (var i = 0; i < additionalSkillButtons.Length; i++)
                {
                    var button = additionalSkillButtons[i];
                    if (button == null)
                        continue;

                    ApplyButtonState(button, Math.Max(2, button.SkillSlotIndex), skillState, utcNow);
                }
            }

            RefreshCastBar(utcNow, force);
        }

        private void ApplyMissingState()
        {
            if (basicSkillButton != null)
                basicSkillButton.ApplyState(true, false, default(PlayerSkillModel), default(SkillPresentation), false, 0f, string.Empty, false);

            if (additionalSkillButtons != null)
            {
                for (var i = 0; i < additionalSkillButtons.Length; i++)
                {
                    var button = additionalSkillButtons[i];
                    if (button != null)
                        button.Hide();
                }
            }

            ApplyCastBar(false, 0f);
        }

        private void ApplyButtonState(
            CombatSkillButtonView buttonView,
            int slotIndex,
            ClientSkillState skillState,
            DateTime utcNow)
        {
            if (buttonView == null)
                return;

            PlayerSkillModel skill;
            if (!skillState.TryGetLoadoutSkill(slotIndex, out skill))
            {
                if (slotIndex == BasicSkillSlotIndex)
                {
                    buttonView.ApplyState(true, false, default(PlayerSkillModel), default(SkillPresentation), false, 0f, string.Empty, false);
                    return;
                }

                buttonView.Hide();
                return;
            }

            float cooldownFillAmount;
            int remainingMs;
            int durationMs;
            var hasCooldown = ClientRuntime.Combat.TryGetCooldownForSlot(
                slotIndex,
                skill.PlayerSkillId,
                utcNow,
                out cooldownFillAmount,
                out remainingMs,
                out durationMs);

            var interactable = !hasCooldown &&
                               !ClientRuntime.Combat.HasPendingAttackRequest &&
                               !ClientRuntime.Combat.IsLocalCastActive(utcNow);

            var cooldownLabel = hasCooldown ? FormatCooldownLabel(remainingMs) : string.Empty;
            var presentation = presentationCatalog != null
                ? presentationCatalog.Resolve(skill)
                : default(SkillPresentation);

            buttonView.ApplyState(
                true,
                true,
                skill,
                presentation,
                interactable,
                cooldownFillAmount,
                cooldownLabel,
                hasCooldown);
        }

        private void HandleSkillButtonClicked(int slotIndex)
        {
            if (!ClientRuntime.IsInitialized || slotIndex <= 0)
                return;

            PlayerSkillModel skill;
            if (!ClientRuntime.Skills.TryGetLoadoutSkill(slotIndex, out skill))
                return;

            if (skill.TargetType == SelfSkillTargetType)
            {
                if (!ClientRuntime.CombatService.TryUseSkill(slotIndex))
                    return;

                Refresh(force: true);
                return;
            }

            WorldTargetHandle targetHandle;
            if (!TryResolveSelectedTarget(out targetHandle))
                return;

            if (!ClientRuntime.CombatService.TryUseSkillOnTarget(slotIndex, targetHandle))
                return;

            Refresh(force: true);
        }

        private bool TryResolveSelectedTarget(out WorldTargetHandle targetHandle)
        {
            targetHandle = default;

            var currentTarget = ClientRuntime.Target.CurrentTarget;
            if (!currentTarget.HasValue)
                return false;

            var kind = currentTarget.Value.Kind;
            switch (kind)
            {
                case WorldTargetKind.Player:
                    targetHandle = currentTarget.Value;
                    return true;

                case WorldTargetKind.Enemy:
                case WorldTargetKind.Boss:
                    int enemyRuntimeId;
                    if (!int.TryParse(currentTarget.Value.TargetId, NumberStyles.Integer, CultureInfo.InvariantCulture, out enemyRuntimeId))
                        return false;

                    EnemyRuntimeModel enemy;
                    if (!ClientRuntime.World.TryGetEnemy(enemyRuntimeId, out enemy))
                        return false;

                    if (enemy.CurrentHp <= 0)
                        return false;

                    targetHandle = currentTarget.Value;
                    return true;

                default:
                    return false;
            }
        }

        private void TryReloadSkillsOnOpen()
        {
            if (!ClientRuntime.IsInitialized || !autoLoadSkillsOnEnable)
                return;

            var selectedCharacterId = ClientRuntime.Character.SelectedCharacterId;
            if (!selectedCharacterId.HasValue || skillReloadInFlight)
                return;

            if (!CanRetryReload(lastRequestedCharacterId, lastSkillReloadAttemptTime, selectedCharacterId.Value))
                return;

            _ = ReloadSkillsAsync(selectedCharacterId.Value);
        }

        private void TryReloadMissingSkills()
        {
            if (!ClientRuntime.IsInitialized || !autoLoadSkillsOnEnable || skillReloadInFlight)
                return;

            if (ClientRuntime.Skills.HasLoadedSkills)
                return;

            var selectedCharacterId = ClientRuntime.Character.SelectedCharacterId;
            if (!selectedCharacterId.HasValue)
                return;

            if (!CanRetryReload(lastRequestedCharacterId, lastSkillReloadAttemptTime, selectedCharacterId.Value))
                return;

            _ = ReloadSkillsAsync(selectedCharacterId.Value);
        }

        private async System.Threading.Tasks.Task ReloadSkillsAsync(Guid characterId)
        {
            skillReloadInFlight = true;
            lastRequestedCharacterId = characterId;
            lastSkillReloadAttemptTime = Time.unscaledTime;

            try
            {
                await ClientRuntime.SkillService.LoadOwnedSkillsAsync(forceRefresh: true);
            }
            catch (Exception ex)
            {
                ClientLog.Warn($"WorldCombatHudController skill reload exception: {ex.Message}");
            }
            finally
            {
                skillReloadInFlight = false;
                Refresh(force: true);
            }
        }

        private void RefreshCastBar(DateTime utcNow, bool force)
        {
            var activeCast = ClientRuntime.Combat.ActiveLocalCast;
            if (!activeCast.HasValue)
            {
                ApplyCastBar(false, 0f);
                return;
            }

            var cast = activeCast.Value;
            if (utcNow >= cast.CastCompletedAtUtc)
            {
                ApplyCastBar(false, 0f);
                return;
            }

            var durationSeconds = (cast.CastCompletedAtUtc - cast.CastStartedAtUtc).TotalSeconds;
            if (durationSeconds <= 0d)
            {
                ApplyCastBar(false, 0f);
                return;
            }

            var elapsedSeconds = Math.Max(0d, (utcNow - cast.CastStartedAtUtc).TotalSeconds);
            var progress = Mathf.Clamp01((float)(elapsedSeconds / durationSeconds));
            ApplyCastBar(true, progress);
        }

        private void ApplyCastBar(bool visible, float progress)
        {
            if (castBarRoot != null)
                castBarRoot.SetActive(visible);

            if (castBarFillImage != null)
                castBarFillImage.fillAmount = visible ? Mathf.Clamp01(progress) : 0f;

            if (castBarText != null)
                castBarText.text = visible ? castBarDefaultText : string.Empty;
        }

        private bool CanRetryReload(Guid? lastRequestedId, float lastAttemptTime, Guid characterId)
        {
            return lastRequestedId != characterId ||
                   Time.unscaledTime - lastAttemptTime >= reloadRetryCooldownSeconds;
        }

        private static string FormatCooldownLabel(int remainingMs)
        {
            if (remainingMs >= 10000)
                return Mathf.CeilToInt(remainingMs / 1000f).ToString(CultureInfo.InvariantCulture);

            if (remainingMs >= 1000)
                return (remainingMs / 1000f).ToString("0.0", CultureInfo.InvariantCulture);

            return (remainingMs / 1000f).ToString("0.0", CultureInfo.InvariantCulture);
        }
    }
}
