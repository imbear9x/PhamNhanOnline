using System;
using System.Collections.Generic;
using System.Globalization;
using GameShared.Models;
using PhamNhanOnline.Client.Core.Application;
using PhamNhanOnline.Client.Core.Logging;
using PhamNhanOnline.Client.Features.Combat.Application;
using PhamNhanOnline.Client.Features.Character.Application;
using PhamNhanOnline.Client.Features.Skills.Application;
using PhamNhanOnline.Client.Features.Targeting.Application;
using PhamNhanOnline.Client.Features.World.Presentation;
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
        [SerializeField] private Transform additionalSkillButtonsRoot;
        [SerializeField] private CombatSkillButtonView additionalSkillButtonTemplate;
        [SerializeField] private bool hideAdditionalSkillButtonTemplate = true;

        [Header("Cast Bar")]
        [SerializeField] private GameObject castBarRoot;
        [SerializeField] private Image castBarFillImage;
        [SerializeField] private TMP_Text castBarText;

        [Header("Display Text")]
        [SerializeField] private string castBarDefaultText = "Dang thi trien...";

        private readonly List<CombatSkillButtonView> spawnedAdditionalSkillButtons = new List<CombatSkillButtonView>(8);
        private bool loggedMissingWorldSceneController;

        private static WorldSceneController SceneController => WorldSceneController.Instance;

        private void Awake()
        {
            InitializeAdditionalSkillButtonTemplate();
            NormalizeBasicButtonSlotIndex();
            SubscribeButtons();
            ApplyCastBar(false, 0f);
        }

        private void Start()
        {
            if (SceneController == null && !loggedMissingWorldSceneController)
            {
                ClientLog.Error("WorldCombatHudController could not resolve WorldSceneController.");
                loggedMissingWorldSceneController = true;
            }
        }

        private void OnEnable()
        {
            Refresh(force: true);
        }

        private void Update()
        {
            Refresh(force: false);
        }

        private void OnDestroy()
        {
            UnsubscribeButtons();
        }

        private void SubscribeButtons()
        {
            if (basicSkillButton != null)
                basicSkillButton.Clicked += HandleSkillButtonClicked;
        }

        private void NormalizeBasicButtonSlotIndex()
        {
            if (basicSkillButton != null)
                basicSkillButton.SetSlotIndex(BasicSkillSlotIndex);
        }

        private void UnsubscribeButtons()
        {
            if (basicSkillButton != null)
                basicSkillButton.Clicked -= HandleSkillButtonClicked;

            for (var i = 0; i < spawnedAdditionalSkillButtons.Count; i++)
            {
                var button = spawnedAdditionalSkillButtons[i];
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
            EnsureAdditionalButtonCount(skillState.MaxLoadoutSlotCount);
            ApplyButtonState(basicSkillButton, BasicSkillSlotIndex, skillState, utcNow);

            for (var i = 0; i < spawnedAdditionalSkillButtons.Count; i++)
            {
                var button = spawnedAdditionalSkillButtons[i];
                if (button == null)
                    continue;

                var slotIndex = i + 2;
                if (slotIndex <= skillState.MaxLoadoutSlotCount)
                    ApplyButtonState(button, slotIndex, skillState, utcNow);
                else
                    button.Hide();
            }

            RefreshCastBar(utcNow, force);
        }

        private void ApplyMissingState()
        {
            if (basicSkillButton != null)
                basicSkillButton.Hide();

            for (var i = 0; i < spawnedAdditionalSkillButtons.Count; i++)
            {
                var button = spawnedAdditionalSkillButtons[i];
                if (button != null)
                    button.Hide();
            }

            ApplyCastBar(false, 0f);
        }

        private void InitializeAdditionalSkillButtonTemplate()
        {
            if (additionalSkillButtonTemplate == null)
                return;

            if (additionalSkillButtonsRoot == null)
                additionalSkillButtonsRoot = additionalSkillButtonTemplate.transform.parent;

            if (hideAdditionalSkillButtonTemplate && additionalSkillButtonTemplate.gameObject.activeSelf)
                additionalSkillButtonTemplate.gameObject.SetActive(false);
        }

        private void EnsureAdditionalButtonCount(int maxLoadoutSlotCount)
        {
            var targetCount = Math.Max(0, maxLoadoutSlotCount - 1);
            if (targetCount <= spawnedAdditionalSkillButtons.Count)
                return;

            if (additionalSkillButtonTemplate == null)
            {
                ClientLog.Warn("WorldCombatHudController is missing additionalSkillButtonTemplate.");
                return;
            }

            var parent = additionalSkillButtonsRoot != null
                ? additionalSkillButtonsRoot
                : additionalSkillButtonTemplate.transform.parent;

            for (var i = spawnedAdditionalSkillButtons.Count; i < targetCount; i++)
            {
                var instance = Instantiate(additionalSkillButtonTemplate, parent);
                instance.name = string.Format("{0}_{1}", additionalSkillButtonTemplate.name, i + 2);
                instance.gameObject.SetActive(true);
                instance.SetSlotIndex(i + 2);
                instance.Clicked += HandleSkillButtonClicked;
                spawnedAdditionalSkillButtons.Add(instance);
            }
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
                    var interactableWithoutSkill = !IsLocalCharacterDead() &&
                                                  !ClientRuntime.Combat.HasPendingAttackRequest &&
                                                  !ClientRuntime.Combat.IsLocalCastActive(utcNow);
                    buttonView.ApplyState(
                        true,
                        false,
                        default(PlayerSkillModel),
                        default(SkillPresentation),
                        interactableWithoutSkill,
                        0f,
                        string.Empty,
                        false);
                }
                else
                {
                    buttonView.Hide();
                }

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

            var interactable = !IsLocalCharacterDead() &&
                               !hasCooldown &&
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
            var hasSkill = ClientRuntime.Skills.TryGetLoadoutSkill(slotIndex, out skill);
            if (!hasSkill && slotIndex == BasicSkillSlotIndex)
            {
                var worldSceneController = SceneController;
                if (worldSceneController == null ||
                    !worldSceneController.RequestPrimaryActionForCurrentSelection())
                {
                    return;
                }

                Refresh(force: true);
                return;
            }

            if (!hasSkill)
                return;

            if (skill.TargetType == SelfSkillTargetType)
            {
                if (!ClientRuntime.CombatService.TryUseSkill(slotIndex))
                    return;

                Refresh(force: true);
                return;
            }

            if (slotIndex == BasicSkillSlotIndex)
            {
                var worldSceneController = SceneController;
                if (worldSceneController == null ||
                    !worldSceneController.RequestPrimaryActionForCurrentSelection())
                {
                    return;
                }

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

                case WorldTargetKind.Npc:
                    MapPortalModel portal;
                    if (!ClientRuntime.World.TryGetPortal(currentTarget.Value, out portal))
                        return false;

                    targetHandle = currentTarget.Value;
                    return true;

                case WorldTargetKind.GroundReward:
                    int rewardId;
                    if (!PhamNhanOnline.Client.Features.World.Application.ClientWorldState.TryParseGroundRewardTargetId(
                            currentTarget.Value.TargetId,
                            out rewardId))
                    {
                        return false;
                    }

                    GroundRewardModel reward;
                    if (!ClientRuntime.World.TryGetGroundReward(rewardId, out reward))
                        return false;

                    targetHandle = currentTarget.Value;
                    return true;

                default:
                    return false;
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

        private static string FormatCooldownLabel(int remainingMs)
        {
            if (remainingMs >= 10000)
                return Mathf.CeilToInt(remainingMs / 1000f).ToString(CultureInfo.InvariantCulture);

            if (remainingMs >= 1000)
                return (remainingMs / 1000f).ToString("0.0", CultureInfo.InvariantCulture);

            return (remainingMs / 1000f).ToString("0.0", CultureInfo.InvariantCulture);
        }

        private static bool IsLocalCharacterDead()
        {
            var currentState = ClientRuntime.Character.CurrentState;
            return currentState.HasValue &&
                   ClientCharacterRuntimeStateCodes.IsDefeated(currentState.Value);
        }

    }
}


