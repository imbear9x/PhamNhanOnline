using System;
using PhamNhanOnline.Client.Core.Application;
using PhamNhanOnline.Client.Features.Character.Application;
using PhamNhanOnline.Client.Features.Targeting.Application;
using PhamNhanOnline.Client.Features.World.Application;
using PhamNhanOnline.Client.Features.World.Presentation;
using PhamNhanOnline.Client.Infrastructure.Pooling;
using UnityEngine;
using UnityEngine.Serialization;

namespace PhamNhanOnline.Client.UI.Hud
{
    public sealed class WorldCombatValuePopupController : MonoBehaviour
    {
        private const float DefaultAnchorHeightOffset = 0.35f;

        [Header("References")]
        [SerializeField] private WorldLocalPlayerPresenter localPlayerPresenter;
        [SerializeField] private Canvas popupCanvas;
        [SerializeField] private RectTransform popupRoot;

        [Header("Popup Prefab")]
        [FormerlySerializedAs("defaultPopupPrefab")]
        [SerializeField] private CombatValuePopupView combatValuePopupPrefab;

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
        private bool loggedMissingPopupRoot;
        private bool loggedMissingSceneController;
        private bool loggedMissingWorldCamera;
        private bool loggedMissingPopupCanvas;

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
                    FormatSignedValue(deltaHp, includePlusSign: false, suffix: string.Empty),
                    fallbackDamageColor,
                    anchorPosition);
            }
            else
            {
                ShowPopup(
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
                        FormatSignedValue(hpDelta, includePlusSign: false, suffix: string.Empty),
                        fallbackDamageColor,
                        anchorPosition);
                }
                else
                {
                    ShowPopup(
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
                        FormatSignedValue(mpDelta, includePlusSign: false, suffix: mpSuffix),
                        fallbackManaDamageColor,
                        anchorPosition);
                }
                else
                {
                    ShowPopup(
                        FormatSignedValue(mpDelta, includePlusSign: true, suffix: mpSuffix),
                        fallbackManaRestoreColor,
                        anchorPosition);
                }
            }
        }

        public void ShowAtWorldPosition(string text, Color color, Vector2 anchorPosition)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            ShowPopup(text, color, anchorPosition);
        }

        private void ShowPopup(string text, Color color, Vector2 anchorPosition)
        {
            if (combatValuePopupPrefab == null)
                return;

            AutoWireReferences();
            var poolService = ResolvePoolService();
            if (poolService == null)
                return;

            Vector2 popupAnchoredPosition;
            if (!TryConvertWorldAnchorToPopupPosition(anchorPosition, out popupAnchoredPosition))
                return;

            var popup = poolService.Spawn(combatValuePopupPrefab, popupRoot, worldPositionStays: false);
            if (popup == null)
                return;

            popup.Play(text, color, popupAnchoredPosition);
        }

        private void PrewarmPrefabs()
        {
            if (prefabsPrewarmed)
                return;

            AutoWireReferences();
            var poolService = ResolvePoolService();
            if (poolService == null)
                return;

            WarmPrefab(poolService, combatValuePopupPrefab);
            prefabsPrewarmed = true;
        }

        private void WarmPrefab(ClientPoolService poolService, CombatValuePopupView prefab)
        {
            if (prefab == null || poolService == null || prewarmPerAssignedPrefab <= 0)
                return;

            poolService.Warm(prefab.gameObject, prewarmPerAssignedPrefab);
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

            if (worldSceneController == null && !loggedMissingSceneController)
            {
                Debug.LogWarning($"{nameof(WorldCombatValuePopupController)} could not resolve {nameof(WorldSceneController)}.");
                loggedMissingSceneController = true;
            }
        }

        private bool TryConvertWorldAnchorToPopupPosition(Vector2 worldAnchorPosition, out Vector2 popupAnchoredPosition)
        {
            popupAnchoredPosition = default;
            AutoWireReferences();

            if (popupRoot == null)
            {
                if (!loggedMissingPopupRoot)
                {
                    Debug.LogWarning($"{nameof(WorldCombatValuePopupController)} is missing Popup Root. Assign a RectTransform under the combat popup overlay canvas.");
                    loggedMissingPopupRoot = true;
                }

                return false;
            }

            var worldSceneController = SceneController;
            var activeWorldCamera = worldSceneController != null ? worldSceneController.WorldCamera : null;
            if (activeWorldCamera == null)
            {
                if (!loggedMissingWorldCamera)
                {
                    Debug.LogWarning($"{nameof(WorldCombatValuePopupController)} could not resolve World Camera from {nameof(WorldSceneController)}.");
                    loggedMissingWorldCamera = true;
                }

                return false;
            }

            if (popupCanvas == null)
            {
                if (!loggedMissingPopupCanvas)
                {
                    Debug.LogWarning($"{nameof(WorldCombatValuePopupController)} is missing Popup Canvas. Popup Root must be under a Canvas.");
                    loggedMissingPopupCanvas = true;
                }

                return false;
            }

            var screenPoint = activeWorldCamera.WorldToScreenPoint(worldAnchorPosition);
            if (screenPoint.z < 0f)
                return false;

            var eventCamera = popupCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : popupCanvas.worldCamera;

            return RectTransformUtility.ScreenPointToLocalPointInRectangle(
                popupRoot,
                screenPoint,
                eventCamera,
                out popupAnchoredPosition);
        }

        private static string FormatSignedValue(int delta, bool includePlusSign, string suffix)
        {
            if (delta == 0)
                return string.Empty;

            if (delta > 0)
                return includePlusSign ? "+" + delta + suffix : delta + suffix;

            return delta + suffix;
        }
    }
}
