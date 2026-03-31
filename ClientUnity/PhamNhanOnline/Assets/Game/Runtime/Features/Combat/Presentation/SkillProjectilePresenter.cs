using PhamNhanOnline.Client.Features.Targeting.Application;
using PhamNhanOnline.Client.Features.World.Presentation;
using UnityEngine;

namespace PhamNhanOnline.Client.Features.Combat.Presentation
{
    [DisallowMultipleComponent]
    public sealed class SkillProjectilePresenter : MonoBehaviour
    {
        [SerializeField] private bool faceTravelDirection = true;
        [SerializeField] private bool followTarget = true;

        private Quaternion initialRotation;
        private WorldTargetHandle? targetHandle;
        private Vector3 startWorldPosition;
        private Vector3 fallbackTargetWorldPosition;
        private float durationSeconds;
        private float elapsedSeconds;
        private bool isInitialized;

        private void Awake()
        {
            initialRotation = transform.rotation;
        }

        public void Initialize(
            Vector3 sourceWorldPosition,
            Vector3 targetWorldPosition,
            WorldTargetHandle? target,
            float travelDurationSeconds)
        {
            targetHandle = target;
            startWorldPosition = sourceWorldPosition;
            fallbackTargetWorldPosition = targetWorldPosition;
            durationSeconds = Mathf.Max(0.01f, travelDurationSeconds);
            elapsedSeconds = 0f;
            isInitialized = true;
            transform.position = sourceWorldPosition;

            if (faceTravelDirection)
                ApplyFacing(targetWorldPosition - sourceWorldPosition);
            else
                transform.rotation = initialRotation;
        }

        private void Update()
        {
            if (!isInitialized)
                return;

            elapsedSeconds += Time.deltaTime;
            var progress = Mathf.Clamp01(elapsedSeconds / durationSeconds);
            var targetWorldPosition = ResolveCurrentTargetWorldPosition();
            var nextPosition = Vector3.Lerp(startWorldPosition, targetWorldPosition, progress);

            if (faceTravelDirection)
                ApplyFacing(nextPosition - transform.position);

            transform.position = nextPosition;
        }

        private Vector3 ResolveCurrentTargetWorldPosition()
        {
            if (!followTarget || !targetHandle.HasValue || !targetHandle.Value.IsValid)
                return fallbackTargetWorldPosition;

            WorldTargetable targetable;
            if (WorldTargetableRegistry.TryGet(targetHandle.Value, out targetable) &&
                targetable != null &&
                targetable.isActiveAndEnabled &&
                targetable.TryGetWorldSelectionPosition(out var worldPosition))
            {
                fallbackTargetWorldPosition = new Vector3(worldPosition.x, worldPosition.y, transform.position.z);
                return fallbackTargetWorldPosition;
            }

            return fallbackTargetWorldPosition;
        }

        private void ApplyFacing(Vector3 delta)
        {
            if (delta.sqrMagnitude <= Mathf.Epsilon)
                return;

            var angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }

        private void OnDisable()
        {
            targetHandle = null;
            elapsedSeconds = 0f;
            isInitialized = false;
            transform.rotation = initialRotation;
        }
    }
}
