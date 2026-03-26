using GameShared.Models;
using PhamNhanOnline.Client.Core.Logging;
using PhamNhanOnline.Client.Features.Targeting.Application;
using UnityEngine;

namespace PhamNhanOnline.Client.Features.World.Presentation
{
    [DisallowMultipleComponent]
    public sealed class EnemyPresenter : MonoBehaviour
    {
        [SerializeField] private Transform visualRoot;
        [SerializeField] private WorldTargetable targetable;
        [SerializeField] private bool hideWhenDead;

        private int runtimeId;
        private bool warnedPositionMapping;

        public int RuntimeId { get { return runtimeId; } }

        public void ApplySnapshot(EnemyRuntimeModel enemy, WorldMapPresenter worldMapPresenter)
        {
            runtimeId = enemy.RuntimeId;
            AutoWireReferences();
            ConfigureTargetable(enemy);
            UpdateWorldPosition(enemy, worldMapPresenter);
            UpdateLifeState(enemy);
        }

        private void Awake()
        {
            AutoWireReferences();
        }

        private void AutoWireReferences()
        {
            if (visualRoot == null)
                visualRoot = transform;

            if (targetable == null)
                targetable = GetComponent<WorldTargetable>();
        }

        private void ConfigureTargetable(EnemyRuntimeModel enemy)
        {
            if (targetable == null)
                targetable = gameObject.AddComponent<WorldTargetable>();

            targetable.Configure(WorldTargetHandle.CreateEnemy(enemy.RuntimeId, enemy.Kind == 3));
        }

        private void UpdateWorldPosition(EnemyRuntimeModel enemy, WorldMapPresenter worldMapPresenter)
        {
            Vector2 worldPosition;
            var serverPosition = new Vector2(enemy.PosX, enemy.PosY);

            if (worldMapPresenter != null && worldMapPresenter.TryMapServerPositionToWorld(serverPosition, out worldPosition))
            {
                transform.position = new Vector3(worldPosition.x, worldPosition.y, transform.position.z);
                warnedPositionMapping = false;
                return;
            }

            if (!warnedPositionMapping)
            {
                ClientLog.Warn($"EnemyPresenter on {name} could not map server position into Unity world space. Falling back to raw coordinates.");
                warnedPositionMapping = true;
            }

            transform.position = new Vector3(serverPosition.x, serverPosition.y, transform.position.z);
        }

        private void UpdateLifeState(EnemyRuntimeModel enemy)
        {
            var isAlive = enemy.CurrentHp > 0 && enemy.RuntimeState != 4;
            if (targetable != null && targetable.enabled != isAlive)
                targetable.enabled = isAlive;

            if (visualRoot != null)
                visualRoot.gameObject.SetActive(!hideWhenDead || isAlive);
        }
    }
}
