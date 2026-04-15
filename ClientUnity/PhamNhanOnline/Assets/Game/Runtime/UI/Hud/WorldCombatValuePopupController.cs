using System;
using PhamNhanOnline.Client.Core.Application;
using PhamNhanOnline.Client.Features.Character.Application;
using PhamNhanOnline.Client.Features.Targeting.Application;
using PhamNhanOnline.Client.Features.World.Application;
using PhamNhanOnline.Client.Features.World.Presentation;
using PhamNhanOnline.Client.Infrastructure.Pooling;
using UnityEngine;

namespace PhamNhanOnline.Client.UI.Hud
{
    public sealed class WorldCombatValuePopupController : MonoBehaviour
    {
        private const float DefaultAnchorHeightOffset = 0.35f;

        [Header("References")]
        [SerializeField] private WorldLocalPlayerPresenter localPlayerPresenter;
        [SerializeField] private Transform popupRoot;

        [Header("Popup Prefabs")]
        [SerializeField] private CombatValuePopupView defaultPopupPrefab;
        [SerializeField] private CombatValuePopupView hpDamagePopupPrefab;
        [SerializeField] private CombatValuePopupView hpHealPopupPrefab;
        [SerializeField] private CombatValuePopupView mpDamagePopupPrefab;
        [SerializeField] private CombatValuePopupView mpRestorePopupPrefab;

        [Header("Behavior")]
        [SerializeField] private int prewarmPerAssignedPrefab = 6;
        [SerializeField] private float anchorHeightOffset = DefaultAnchorHeightOffset;
        [SerializeField] private string mpSuffix = " MP";
        [SerializeField] private Color fallbackDamageColor = new Color(1f, 0.3f, 0.3f, 1f);
        [SerializeField] private Color fallbackHealColor = new Color(0.35f, 1f, 0.45f, 1f);
        [SerializeField] private Color fallbackManaDamageColor = new Color(0.35f, 0.8f, 1f, 1f);
        [SerializeField] private Color fallbackManaRestoreColor = new Color(0.6f, 1f, 1f, 1f);

        private bool runtimeEventsBound;
        private bool prefabsPrewarmed;

        private static WorldSceneController SceneController => WorldSceneController.Instance;

        private static ClientPoolService ResolvePoolService()
        {
            var instance = ClientPoolService.Instance;
            if (instance != null)
                return instance;

            var sceneController = SceneController;
            return ClientPoolService.Ensure(sceneController != null ? sceneController.transform : null);
        }

        private void Awake()
        {
        }

        private void Start()
        {
            AutoWireReferences();
            TryBindRuntimeEvents();
            PrewarmPrefabs();
        }

        private void OnEnable()
        {
            AutoWireReferences();
            TryBindRuntimeEvents();
        }

        private void OnDisable()
        {
            UnbindRuntimeEvents();
        }

        private void OnDestroy()
        {
            UnbindRuntimeEvents();
        }

        private void TryBindRuntimeEvents()
        {
            if (runtimeEventsBound || !ClientRuntime.IsInitialized)
                return;

            ClientRuntime.Character.CurrentStateChanged += HandleLocalCharacterCurrentStateChanged;
            ClientRuntime.World.ObservedCharacterStateChanged += HandleObservedCharacterStateChanged;
            ClientRuntime.World.EnemyHpChanged += HandleEnemyHpChanged;
            runtimeEventsBound = true;
        }

        private void UnbindRuntimeEvents()
        {
            if (!runtimeEventsBound || !ClientRuntime.IsInitialized)
                return;

            ClientRuntime.Character.CurrentStateChanged -= HandleLocalCharacterCurrentStateChanged;
            ClientRuntime.World.ObservedCharacterStateChanged -= HandleObservedCharacterStateChanged;
            ClientRuntime.World.EnemyHpChanged -= HandleEnemyHpChanged;
            runtimeEventsBound = false;
        }

        private void HandleLocalCharacterCurrentStateChanged(CharacterCurrentStateChangeNotice notice)
        {
            if (!notice.PreviousState.HasValue || !notice.CurrentState.HasValue)
                return;

            Vector2 anchorPosition;
            if (!TryGetLocalPlayerAnchorPosition(out anchorPosition))
                return;

            PublishResourceDeltaPopups(
                notice.PreviousState.Value.CurrentHp,
                notice.CurrentState.Value.CurrentHp,
                notice.PreviousState.Value.CurrentMp,
                notice.CurrentState.Value.CurrentMp,
                anchorPosition);
        }

        private void HandleObservedCharacterStateChanged(ObservedCharacterStateChangedNotice notice)
        {
            Vector2 anchorPosition;
            if (!TryGetAnchorPosition(
                    WorldTargetHandle.CreateObservedCharacter(notice.CharacterId),
                    out anchorPosition))
                return;

            PublishResourceDeltaPopups(
                notice.PreviousState.CurrentHp,
                notice.CurrentState.CurrentHp,
                notice.PreviousState.CurrentMp,
                notice.CurrentState.CurrentMp,
                anchorPosition);
        }

        private void HandleEnemyHpChanged(EnemyHpChangedNotice notice)
        {
            var deltaHp = notice.CurrentCurrentHp - notice.PreviousCurrentHp;
            if (deltaHp == 0)
                return;

            Vector2 anchorPosition;
            if (!TryGetAnchorPosition(
                    WorldTargetHandle.CreateEnemy(notice.RuntimeId, notice.Enemy.Kind == 3),
                    out anchorPosition))
                return;

            if (deltaHp < 0)
            {
                ShowPopup(
                    ResolvePrefab(CombatValuePopupKind.HpDamage),
                    FormatSignedValue(deltaHp, includePlusSign: false, suffix: string.Empty),
                    fallbackDamageColor,
                    anchorPosition);
            }
            else
            {
                ShowPopup(
                    ResolvePrefab(CombatValuePopupKind.HpHeal),
                    FormatSignedValue(deltaHp, includePlusSign: true, suffix: string.Empty),
                    fallbackHealColor,
                    anchorPosition);
            }
        }

        private void PublishResourceDeltaPopups(int previousHp, int currentHp, int previousMp, int currentMp, Vector2 anchorPosition)
        {
            var hpDelta = currentHp - previousHp;
            if (hpDelta != 0)
            {
                if (hpDelta < 0)
                {
                    ShowPopup(
                        ResolvePrefab(CombatValuePopupKind.HpDamage),
                        FormatSignedValue(hpDelta, includePlusSign: false, suffix: string.Empty),
                        fallbackDamageColor,
                        anchorPosition);
                }
                else
                {
                    ShowPopup(
                        ResolvePrefab(CombatValuePopupKind.HpHeal),
                        FormatSignedValue(hpDelta, includePlusSign: true, suffix: string.Empty),
                        fallbackHealColor,
                        anchorPosition);
                }
            }

            var mpDelta = currentMp - previousMp;
            if (mpDelta != 0)
            {
                if (mpDelta < 0)
                {
                    ShowPopup(
                        ResolvePrefab(CombatValuePopupKind.MpDamage),
                        FormatSignedValue(mpDelta, includePlusSign: false, suffix: mpSuffix),
                        fallbackManaDamageColor,
                        anchorPosition);
                }
                else
                {
                    ShowPopup(
                        ResolvePrefab(CombatValuePopupKind.MpRestore),
                        FormatSignedValue(mpDelta, includePlusSign: true, suffix: mpSuffix),
                        fallbackManaRestoreColor,
                        anchorPosition);
                }
            }
        }

        private void ShowPopup(CombatValuePopupView prefab, string text, Color color, Vector2 anchorPosition)
        {
            if (prefab == null)
                return;

            AutoWireReferences();
            var poolService = ResolvePoolService();
            if (poolService == null)
                return;

            var parent = popupRoot != null ? popupRoot : transform;
            var popup = poolService.Spawn(prefab, parent, worldPositionStays: false);
            if (popup == null)
                return;

            popup.Play(text, color, anchorPosition);
        }

        private void PrewarmPrefabs()
        {
            if (prefabsPrewarmed)
                return;

            AutoWireReferences();
            var poolService = ResolvePoolService();
            if (poolService == null)
                return;

            WarmPrefab(poolService, defaultPopupPrefab);
            WarmPrefab(poolService, hpDamagePopupPrefab);
            WarmPrefab(poolService, hpHealPopupPrefab);
            WarmPrefab(poolService, mpDamagePopupPrefab);
            WarmPrefab(poolService, mpRestorePopupPrefab);
            prefabsPrewarmed = true;
        }

        private void WarmPrefab(ClientPoolService poolService, CombatValuePopupView prefab)
        {
            if (prefab == null || poolService == null || prewarmPerAssignedPrefab <= 0)
                return;

            poolService.Warm(prefab.gameObject, prewarmPerAssignedPrefab);
        }

        private CombatValuePopupView ResolvePrefab(CombatValuePopupKind kind)
        {
            switch (kind)
            {
                case CombatValuePopupKind.HpDamage:
                    return hpDamagePopupPrefab != null ? hpDamagePopupPrefab : defaultPopupPrefab;
                case CombatValuePopupKind.HpHeal:
                    return hpHealPopupPrefab != null ? hpHealPopupPrefab : defaultPopupPrefab;
                case CombatValuePopupKind.MpDamage:
                    return mpDamagePopupPrefab != null ? mpDamagePopupPrefab : defaultPopupPrefab;
                case CombatValuePopupKind.MpRestore:
                    return mpRestorePopupPrefab != null ? mpRestorePopupPrefab : defaultPopupPrefab;
                default:
                    return defaultPopupPrefab;
            }
        }

        private bool TryGetLocalPlayerAnchorPosition(out Vector2 anchorPosition)
        {
            AutoWireReferences();
            if (localPlayerPresenter != null &&
                localPlayerPresenter.TryGetPopupAnchorPosition(anchorHeightOffset, out anchorPosition))
            {
                return true;
            }

            anchorPosition = default;
            return false;
        }

        private bool TryGetAnchorPosition(WorldTargetHandle handle, out Vector2 anchorPosition)
        {
            anchorPosition = default;
            WorldTargetable targetable;
            if (!WorldTargetableRegistry.TryGet(handle, out targetable) || targetable == null)
                return false;

            return targetable.TryGetIndicatorAnchorPosition(anchorHeightOffset, out anchorPosition);
        }

        private void AutoWireReferences()
        {
            var worldSceneController = SceneController;

            if (localPlayerPresenter == null && worldSceneController != null)
                localPlayerPresenter = worldSceneController.WorldLocalPlayerPresenter;

            if (popupRoot == null && worldSceneController != null)
                popupRoot = worldSceneController.WorldUiRoot != null ? worldSceneController.WorldUiRoot : transform;
        }

        private static string FormatSignedValue(int delta, bool includePlusSign, string suffix)
        {
            if (delta == 0)
                return string.Empty;

            if (delta > 0)
                return includePlusSign ? "+" + delta + suffix : delta + suffix;

            return delta + suffix;
        }

        private enum CombatValuePopupKind
        {
            HpDamage = 1,
            HpHeal = 2,
            MpDamage = 3,
            MpRestore = 4
        }
    }
}
