using PhamNhanOnline.Client.Core.Application;
using PhamNhanOnline.Client.Features.Targeting.Application;
using PhamNhanOnline.Client.Features.World.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace PhamNhanOnline.Client.UI.Hud
{
    public sealed class TargetHudController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private WorldSceneController worldSceneController;
        [SerializeField] private TargetStatusPanelController targetStatusPanel;

        [Header("Buttons")]
        [SerializeField] private Button nextTargetButton;
        private bool runtimeEventsBound;

        private void Awake()
        {
            if (worldSceneController == null)
                worldSceneController = WorldSceneController.Instance;

            if (targetStatusPanel == null)
                targetStatusPanel = GetComponent<TargetStatusPanelController>();

            BindButtons();
        }

        private void Start()
        {
            TryBindRuntimeEvents();
            Refresh(force: true);
        }

        private void OnEnable()
        {
            TryBindRuntimeEvents();
            Refresh(force: true);
        }

        private void OnDisable()
        {
            UnbindRuntimeEvents();
        }

        private void OnDestroy()
        {
            UnbindButtons();
            UnbindRuntimeEvents();
        }

        public void Refresh(bool force)
        {
            if (targetStatusPanel == null)
                return;

            if (!ClientRuntime.IsInitialized)
            {
                targetStatusPanel.ShowNoTarget(force);
                return;
            }

            var currentTarget = ClientRuntime.Target.CurrentTarget;
            if (!currentTarget.HasValue)
            {
                targetStatusPanel.ShowNoTarget(force);
                return;
            }

            WorldTargetSnapshot snapshot;
            if (!ClientRuntime.World.TryBuildTargetSnapshot(currentTarget.Value, out snapshot))
            {
                WorldTargetable targetable;
                if (!WorldTargetableRegistry.TryGet(currentTarget.Value, out targetable) ||
                    targetable == null ||
                    !targetable.TryBuildFallbackSnapshot(out snapshot))
                {
                    ClientRuntime.Target.Clear();
                    targetStatusPanel.ShowNoTarget(force: true);
                    return;
                }
            }

            targetStatusPanel.ShowSnapshot(snapshot, force);
        }

        private void BindButtons()
        {
            if (nextTargetButton != null)
            {
                nextTargetButton.onClick.RemoveListener(HandleNextTargetClicked);
                nextTargetButton.onClick.AddListener(HandleNextTargetClicked);
            }
        }

        private void UnbindButtons()
        {
            if (nextTargetButton != null)
                nextTargetButton.onClick.RemoveListener(HandleNextTargetClicked);
        }

        private void TryBindRuntimeEvents()
        {
            if (runtimeEventsBound || !ClientRuntime.IsInitialized)
                return;

            ClientRuntime.Target.CurrentTargetChanged += HandleTargetChanged;
            ClientRuntime.Target.PinStateChanged += HandleTargetChanged;
            ClientRuntime.World.MapChanged += HandleWorldChanged;
            ClientRuntime.World.ObservedCharacterUpserted += HandleObservedCharacterUpserted;
            ClientRuntime.World.ObservedCharacterRemoved += HandleObservedCharacterRemoved;
            ClientRuntime.World.ObservedCharacterStateChanged += HandleObservedCharacterStateChanged;
            ClientRuntime.World.EnemyUpserted += HandleEnemyUpserted;
            ClientRuntime.World.EnemyRemoved += HandleEnemyRemoved;
            ClientRuntime.World.EnemyHpChanged += HandleEnemyHpChanged;
            runtimeEventsBound = true;
        }

        private void UnbindRuntimeEvents()
        {
            if (!runtimeEventsBound || !ClientRuntime.IsInitialized)
                return;

            ClientRuntime.Target.CurrentTargetChanged -= HandleTargetChanged;
            ClientRuntime.Target.PinStateChanged -= HandleTargetChanged;
            ClientRuntime.World.MapChanged -= HandleWorldChanged;
            ClientRuntime.World.ObservedCharacterUpserted -= HandleObservedCharacterUpserted;
            ClientRuntime.World.ObservedCharacterRemoved -= HandleObservedCharacterRemoved;
            ClientRuntime.World.ObservedCharacterStateChanged -= HandleObservedCharacterStateChanged;
            ClientRuntime.World.EnemyUpserted -= HandleEnemyUpserted;
            ClientRuntime.World.EnemyRemoved -= HandleEnemyRemoved;
            ClientRuntime.World.EnemyHpChanged -= HandleEnemyHpChanged;
            runtimeEventsBound = false;
        }

        private void HandleNextTargetClicked()
        {
            if (worldSceneController != null)
                worldSceneController.CycleNearbyTarget();
        }

        private void HandleTargetChanged()
        {
            Refresh(force: false);
        }

        private void HandleWorldChanged()
        {
            Refresh(force: false);
        }

        private void HandleObservedCharacterUpserted(GameShared.Models.ObservedCharacterModel observedCharacter)
        {
            if (IsCurrentObservedCharacter(observedCharacter.Character.CharacterId))
                Refresh(force: false);
        }

        private void HandleObservedCharacterRemoved(System.Guid characterId)
        {
            if (IsCurrentObservedCharacter(characterId))
                Refresh(force: false);
        }

        private void HandleObservedCharacterStateChanged(PhamNhanOnline.Client.Features.World.Application.ObservedCharacterStateChangedNotice notice)
        {
            if (IsCurrentObservedCharacter(notice.CharacterId))
                Refresh(force: false);
        }

        private void HandleEnemyUpserted(GameShared.Models.EnemyRuntimeModel enemy)
        {
            if (IsCurrentEnemy(enemy.RuntimeId))
                Refresh(force: false);
        }

        private void HandleEnemyRemoved(int runtimeId)
        {
            if (IsCurrentEnemy(runtimeId))
                Refresh(force: false);
        }

        private void HandleEnemyHpChanged(PhamNhanOnline.Client.Features.World.Application.EnemyHpChangedNotice notice)
        {
            if (IsCurrentEnemy(notice.RuntimeId))
                Refresh(force: false);
        }

        private bool IsCurrentObservedCharacter(System.Guid characterId)
        {
            return ClientRuntime.IsInitialized && ClientRuntime.Target.IsSelectedObservedCharacter(characterId);
        }

        private bool IsCurrentEnemy(int runtimeId)
        {
            return ClientRuntime.IsInitialized && ClientRuntime.Target.IsSelectedEnemy(runtimeId);
        }
    }
}
