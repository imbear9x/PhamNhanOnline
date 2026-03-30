using System;
using System.Collections.Generic;
using PhamNhanOnline.Client.Features.Character.Presentation;
using PhamNhanOnline.Client.Features.Targeting.Application;
using PhamNhanOnline.Client.Features.World.Presentation;
using UnityEngine;

namespace PhamNhanOnline.Client.Features.Combat.Presentation
{
    [DisallowMultipleComponent]
    public sealed class CharacterSkillPresenter : MonoBehaviour
    {
        [SerializeField] private PlayerView playerView;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Animator animator;
        [SerializeField] private CharacterPresentationSockets sockets;
        [SerializeField] private WorldTargetable worldTargetable;
        [SerializeField] private bool visualFacesLeftByDefault = true;

        private readonly Dictionary<SkillExecutionKey, List<GameObject>> activeFxByExecution =
            new Dictionary<SkillExecutionKey, List<GameObject>>();

        private float visualDefaultScaleX = 1f;

        public Guid? CharacterId { get; private set; }
        public WorldTargetHandle? TargetHandle { get; private set; }

        public void ConfigureCharacter(Guid? characterId)
        {
            CharacterSkillPresenterRegistry.Unregister(this);
            CharacterId = characterId;
            CharacterSkillPresenterRegistry.Register(this);
        }

        public void ConfigureTargetHandle(WorldTargetHandle? handle)
        {
            CharacterSkillPresenterRegistry.Unregister(this);
            TargetHandle = handle;
            CharacterSkillPresenterRegistry.Register(this);
        }

        public void HandleCastStarted(
            SkillPresentationExecutionSnapshot snapshot,
            SkillWorldPresentationDefinition definition,
            Vector2? targetWorldPosition)
        {
            AutoWireReferences();
            if (definition == null)
                return;

            if (definition.FaceTargetOnCast && targetWorldPosition.HasValue)
                FaceTowards(targetWorldPosition.Value);

            PlayState(definition.CastStateName);
            SpawnFx(snapshot.Key, definition.CastFxPrefab, definition.SourceSocket, null, definition.FxLifetimeSeconds);
        }

        public void HandleCastReleased(
            SkillPresentationExecutionSnapshot snapshot,
            SkillWorldPresentationDefinition definition,
            Vector2? targetWorldPosition)
        {
            AutoWireReferences();
            if (definition == null)
                return;

            if (definition.FaceTargetOnCast && targetWorldPosition.HasValue)
                FaceTowards(targetWorldPosition.Value);

            PlayState(definition.ReleaseStateName);

            if (IsProjectileArchetype(definition.Archetype))
            {
                SpawnProjectile(snapshot, definition, targetWorldPosition);
                return;
            }

            SpawnFx(snapshot.Key, definition.ReleaseFxPrefab, definition.SourceSocket, null, definition.FxLifetimeSeconds);
        }

        public void HandleImpactResolvedAsCaster(
            SkillPresentationExecutionSnapshot snapshot,
            SkillWorldPresentationDefinition definition)
        {
            CleanupExecutionFx(snapshot.Key);
        }

        public void HandleImpactResolvedAsTarget(
            SkillPresentationExecutionSnapshot snapshot,
            SkillWorldPresentationDefinition definition,
            Vector2? impactWorldPosition)
        {
            AutoWireReferences();
            if (definition == null)
                return;

            PlayState(definition.TargetImpactStateName);
            SpawnFx(
                snapshot.Key,
                definition.ImpactFxPrefab,
                definition.ImpactSocket,
                impactWorldPosition,
                definition.FxLifetimeSeconds,
                trackForCleanup: false);
        }

        private void Awake()
        {
            AutoWireReferences();
        }

        private void OnEnable()
        {
            CharacterSkillPresenterRegistry.Register(this);
        }

        private void OnDisable()
        {
            CharacterSkillPresenterRegistry.Unregister(this);
            ClearAllFx();
        }

        private void OnDestroy()
        {
            CharacterSkillPresenterRegistry.Unregister(this);
            ClearAllFx();
        }

        private void AutoWireReferences()
        {
            if (playerView == null)
                playerView = GetComponent<PlayerView>();

            if (playerView != null)
            {
                if (visualRoot == null)
                    visualRoot = playerView.VisualRoot;
                if (animator == null)
                    animator = playerView.Animator;
            }

            if (visualRoot == null)
            {
                var child = transform.Find("VisualRoot");
                visualRoot = child != null ? child : transform;
            }

            if (animator == null)
                animator = GetComponentInChildren<Animator>(true);

            if (sockets == null)
                sockets = GetComponent<CharacterPresentationSockets>();

            if (sockets == null)
                sockets = gameObject.AddComponent<CharacterPresentationSockets>();

            if (worldTargetable == null)
                worldTargetable = GetComponent<WorldTargetable>();

            if (!Mathf.Approximately(visualRoot.localScale.x, 0f))
                visualDefaultScaleX = visualRoot.localScale.x;
        }

        private void FaceTowards(Vector2 targetWorldPosition)
        {
            if (visualRoot == null)
                return;

            var deltaX = targetWorldPosition.x - transform.position.x;
            if (Mathf.Abs(deltaX) <= Mathf.Epsilon)
                return;

            var targetScaleX = deltaX < 0f
                ? (visualFacesLeftByDefault ? Mathf.Abs(visualDefaultScaleX) : -Mathf.Abs(visualDefaultScaleX))
                : (visualFacesLeftByDefault ? -Mathf.Abs(visualDefaultScaleX) : Mathf.Abs(visualDefaultScaleX));

            var scale = visualRoot.localScale;
            scale.x = targetScaleX;
            visualRoot.localScale = scale;
        }

        private void PlayState(string stateName)
        {
            if (animator == null || string.IsNullOrWhiteSpace(stateName))
                return;

            var stateHash = Animator.StringToHash(stateName);
            if (!animator.HasState(0, stateHash))
                return;

            animator.Play(stateHash, 0, 0f);
        }

        private void SpawnProjectile(
            SkillPresentationExecutionSnapshot snapshot,
            SkillWorldPresentationDefinition definition,
            Vector2? targetWorldPosition)
        {
            if (definition.ReleaseFxPrefab == null)
                return;

            AutoWireReferences();

            Transform sourceAnchor;
            if (sockets == null || !sockets.TryGetSocket(definition.SourceSocket, out sourceAnchor) || sourceAnchor == null)
                sourceAnchor = transform;

            var sourceWorldPosition = sourceAnchor.position;
            var targetPosition = targetWorldPosition.HasValue
                ? new Vector3(targetWorldPosition.Value.x, targetWorldPosition.Value.y, sourceWorldPosition.z)
                : sourceWorldPosition;
            var lifetimeSeconds = ResolveTravelDurationSeconds(snapshot, definition.FxLifetimeSeconds);

            var projectileObject = Instantiate(definition.ReleaseFxPrefab, sourceWorldPosition, Quaternion.identity);
            var projectilePresenter = projectileObject.GetComponent<SkillProjectilePresenter>();
            if (projectilePresenter == null)
                projectilePresenter = projectileObject.AddComponent<SkillProjectilePresenter>();

            projectilePresenter.Initialize(
                sourceWorldPosition,
                targetPosition,
                snapshot.Target,
                lifetimeSeconds);

            TrackExecutionObject(snapshot.Key, projectileObject);
            Destroy(projectileObject, Mathf.Max(lifetimeSeconds + 0.25f, definition.FxLifetimeSeconds));
        }

        private void SpawnFx(
            SkillExecutionKey executionKey,
            GameObject prefab,
            CharacterPresentationSocketType socketType,
            Vector2? worldOverride,
            float lifetimeSeconds,
            bool trackForCleanup = true)
        {
            if (prefab == null)
                return;

            AutoWireReferences();

            Transform parent;
            Vector3 spawnPosition;
            if (worldOverride.HasValue)
            {
                parent = null;
                spawnPosition = new Vector3(worldOverride.Value.x, worldOverride.Value.y, 0f);
            }
            else
            {
                if (sockets == null || !sockets.TryGetSocket(socketType, out parent) || parent == null)
                    parent = transform;

                spawnPosition = parent.position;
            }

            var instance = parent != null
                ? Instantiate(prefab, spawnPosition, Quaternion.identity, parent)
                : Instantiate(prefab, spawnPosition, Quaternion.identity);

            if (trackForCleanup)
            {
                TrackExecutionObject(executionKey, instance);
            }

            if (lifetimeSeconds > 0f)
                Destroy(instance, lifetimeSeconds);
        }

        private void TrackExecutionObject(SkillExecutionKey executionKey, GameObject instance)
        {
            if (instance == null)
                return;

            if (!activeFxByExecution.TryGetValue(executionKey, out var executionFx))
            {
                executionFx = new List<GameObject>();
                activeFxByExecution[executionKey] = executionFx;
            }

            executionFx.Add(instance);
        }

        private static bool IsProjectileArchetype(SkillPresentationArchetype archetype)
        {
            return archetype == SkillPresentationArchetype.WeaponProjectile ||
                   archetype == SkillPresentationArchetype.HandProjectile;
        }

        private static float ResolveTravelDurationSeconds(
            SkillPresentationExecutionSnapshot snapshot,
            float fallbackLifetimeSeconds)
        {
            if (snapshot.ImpactAtUtc.HasValue && snapshot.CastCompletedAtUtc.HasValue)
            {
                var travelDuration = snapshot.ImpactAtUtc.Value - snapshot.CastCompletedAtUtc.Value;
                if (travelDuration > TimeSpan.Zero)
                    return Mathf.Max(0.01f, (float)travelDuration.TotalSeconds);
            }

            return Mathf.Max(0.01f, fallbackLifetimeSeconds);
        }

        private void CleanupExecutionFx(SkillExecutionKey executionKey)
        {
            if (!activeFxByExecution.TryGetValue(executionKey, out var executionFx))
                return;

            for (var i = 0; i < executionFx.Count; i++)
            {
                var fx = executionFx[i];
                if (fx != null)
                    Destroy(fx);
            }

            activeFxByExecution.Remove(executionKey);
        }

        private void ClearAllFx()
        {
            foreach (var pair in activeFxByExecution)
            {
                var fxList = pair.Value;
                for (var i = 0; i < fxList.Count; i++)
                {
                    if (fxList[i] != null)
                        Destroy(fxList[i]);
                }
            }

            activeFxByExecution.Clear();
        }
    }
}
