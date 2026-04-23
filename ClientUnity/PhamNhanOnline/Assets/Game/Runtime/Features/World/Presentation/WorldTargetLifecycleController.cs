using PhamNhanOnline.Client.Core.Application;
using PhamNhanOnline.Client.Features.World.Application;
using UnityEngine;

namespace PhamNhanOnline.Client.Features.World.Presentation
{
    [DisallowMultipleComponent]
    public sealed class WorldTargetLifecycleController : MonoBehaviour
    {
        private bool runtimeEventsBound;

        private void Start()
        {
            TryBindRuntimeEvents();
            ValidateCurrentTarget();
        }

        private void OnEnable()
        {
            TryBindRuntimeEvents();
            ValidateCurrentTarget();
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

            ClientRuntime.Target.CurrentTargetChanged += HandleCurrentTargetChanged;
            ClientRuntime.World.MapChanged += HandleWorldStateChanged;
            ClientRuntime.World.ObservedCharacterRemoved += HandleObservedCharacterRemoved;
            ClientRuntime.World.ObservedCharacterStateChanged += HandleObservedCharacterStateChanged;
            ClientRuntime.World.EnemyRemoved += HandleEnemyRemoved;
            ClientRuntime.World.EnemyHpChanged += HandleEnemyHpChanged;
            ClientRuntime.World.GroundRewardRemoved += HandleGroundRewardRemoved;
            runtimeEventsBound = true;
        }

        private void UnbindRuntimeEvents()
        {
            if (!runtimeEventsBound || !ClientRuntime.IsInitialized)
                return;

            ClientRuntime.Target.CurrentTargetChanged -= HandleCurrentTargetChanged;
            ClientRuntime.World.MapChanged -= HandleWorldStateChanged;
            ClientRuntime.World.ObservedCharacterRemoved -= HandleObservedCharacterRemoved;
            ClientRuntime.World.ObservedCharacterStateChanged -= HandleObservedCharacterStateChanged;
            ClientRuntime.World.EnemyRemoved -= HandleEnemyRemoved;
            ClientRuntime.World.EnemyHpChanged -= HandleEnemyHpChanged;
            ClientRuntime.World.GroundRewardRemoved -= HandleGroundRewardRemoved;
            runtimeEventsBound = false;
        }

        private void HandleCurrentTargetChanged()
        {
            ValidateCurrentTarget();
        }

        private void HandleWorldStateChanged()
        {
            ValidateCurrentTarget();
        }

        private void HandleObservedCharacterRemoved(System.Guid characterId)
        {
            if (ClientRuntime.IsInitialized && ClientRuntime.Target.IsSelectedObservedCharacter(characterId))
                ValidateCurrentTarget();
        }

        private void HandleObservedCharacterStateChanged(ObservedCharacterStateChangedNotice notice)
        {
            if (ClientRuntime.IsInitialized && ClientRuntime.Target.IsSelectedObservedCharacter(notice.CharacterId))
                ValidateCurrentTarget();
        }

        private void HandleEnemyRemoved(int runtimeId)
        {
            if (ClientRuntime.IsInitialized && ClientRuntime.Target.IsSelectedEnemy(runtimeId))
                ValidateCurrentTarget();
        }

        private void HandleEnemyHpChanged(EnemyHpChangedNotice notice)
        {
            if (ClientRuntime.IsInitialized && ClientRuntime.Target.IsSelectedEnemy(notice.RuntimeId))
                ValidateCurrentTarget();
        }

        private void HandleGroundRewardRemoved(int rewardId)
        {
            if (ClientRuntime.IsInitialized && ClientRuntime.Target.IsSelectedGroundReward(rewardId))
                ValidateCurrentTarget();
        }

        private static void ValidateCurrentTarget()
        {
            if (!ClientRuntime.IsInitialized)
                return;

            var currentTarget = ClientRuntime.Target.CurrentTarget;
            if (!currentTarget.HasValue || !currentTarget.Value.IsValid)
                return;

            if (WorldTargetResolutionUtility.IsTargetValid(currentTarget.Value))
                return;

            ClientRuntime.Target.Clear();
        }
    }
}
