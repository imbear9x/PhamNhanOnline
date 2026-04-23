using System.Linq;
using PhamNhanOnline.Client.Core.Application;
using PhamNhanOnline.Client.Features.Character.Application;
using PhamNhanOnline.Client.Features.Targeting.Application;
using UnityEngine;

namespace PhamNhanOnline.Client.Features.World.Presentation
{
    [DisallowMultipleComponent]
    public sealed class WorldAutoTargetSelectionController : WorldSceneBehaviour
    {
        [System.Serializable]
        private struct TargetKindPriorityRule
        {
            public WorldTargetKind kind;
            public int priority;
        }

        private readonly struct Candidate
        {
            public Candidate(WorldTargetHandle handle, WorldTargetKind kind, Vector2 worldPosition, float distanceSquared, int priority, string sortKey)
            {
                Handle = handle;
                Kind = kind;
                WorldPosition = worldPosition;
                DistanceSquared = distanceSquared;
                Priority = priority;
                SortKey = sortKey ?? string.Empty;
            }

            public WorldTargetHandle Handle { get; }
            public WorldTargetKind Kind { get; }
            public Vector2 WorldPosition { get; }
            public float DistanceSquared { get; }
            public int Priority { get; }
            public string SortKey { get; }
        }

        [Header("World References")]
        private WorldMapPresenter worldMapPresenter;
        private WorldLocalPlayerPresenter worldLocalPlayerPresenter;

        [Header("Auto Selection")]
        [SerializeField] private bool autoSelectNearbyTargets = true;
        [SerializeField] private float autoSelectRadiusWorldUnits = 3.5f;
        [SerializeField] private float autoSelectRefreshIntervalSeconds = 0.2f;
        [SerializeField] private bool clearTargetWhenNoNearbyCandidates = true;
        [SerializeField] private bool keepCurrentTargetWhileStillNearby = true;

        [Header("Cycle & Pin")]
        [SerializeField] private bool blockCycleWhilePinned = true;

        [Header("Priority Rules")]
        [SerializeField] private TargetKindPriorityRule[] priorityRules =
        {
            new TargetKindPriorityRule { kind = WorldTargetKind.Npc, priority = 0 },
            new TargetKindPriorityRule { kind = WorldTargetKind.Boss, priority = 1 },
            new TargetKindPriorityRule { kind = WorldTargetKind.Enemy, priority = 2 },
            new TargetKindPriorityRule { kind = WorldTargetKind.GroundReward, priority = 3 },
            new TargetKindPriorityRule { kind = WorldTargetKind.Player, priority = 4 }
        };

        private float lastAutoSelectionTime = float.NegativeInfinity;
        private WorldTargetHandle manualSelectionRangeTrackedTarget;
        private bool manualSelectionHasEnteredAutoRange;

        public float AutoSelectRadiusWorldUnits => Mathf.Max(0f, autoSelectRadiusWorldUnits);

        public void Initialize(WorldMapPresenter mapPresenter, WorldLocalPlayerPresenter localPlayerPresenter)
        {
            if (mapPresenter != null)
                worldMapPresenter = mapPresenter;
            if (localPlayerPresenter != null)
                worldLocalPlayerPresenter = localPlayerPresenter;
        }

        private void Awake()
        {
            AutoWireReferences();
        }

        private void Start()
        {
            AutoWireReferences();
            LogMissingCriticalWorldSceneDependenciesIfNeeded();
        }

        private void Update()
        {
            if (!ClientRuntime.IsInitialized)
                return;

            if (!IsSelectionRuntimeReady())
                return;

            TryAutoSelectNearbyTarget();
        }

        public void CycleNearbyTarget()
        {
            if (!ClientRuntime.IsInitialized)
                return;

            if (!IsSelectionRuntimeReady())
                return;

            if (blockCycleWhilePinned && ClientRuntime.Target.HasPinnedTarget)
                return;

            Candidate[] candidates;
            if (!TryBuildNearbyCandidates(out candidates) || candidates.Length == 0)
                return;

            var nextIndex = 0;
            var currentTarget = ClientRuntime.Target.CurrentTarget;
            if (currentTarget.HasValue)
            {
                for (var i = 0; i < candidates.Length; i++)
                {
                    if (!candidates[i].Handle.Equals(currentTarget.Value))
                        continue;

                    nextIndex = (i + 1) % candidates.Length;
                    break;
                }
            }

            ClientRuntime.Target.Select(candidates[nextIndex].Handle);
        }

        public void ClearSelectedTarget()
        {
            if (!ClientRuntime.IsInitialized)
                return;

            ClientRuntime.Target.Clear();
        }

        public void PinCurrentTargetForCombat()
        {
            if (!ClientRuntime.IsInitialized)
                return;

            ClientRuntime.Target.PinCurrent(TargetPinMode.CombatLocked);
        }

        public void PinCurrentTargetManually()
        {
            if (!ClientRuntime.IsInitialized)
                return;

            ClientRuntime.Target.PinCurrent(TargetPinMode.Manual);
        }

        public void ClearPinnedTarget()
        {
            if (!ClientRuntime.IsInitialized)
                return;

            ClientRuntime.Target.ClearPin();
        }

        private void TryAutoSelectNearbyTarget()
        {
            if (!autoSelectNearbyTargets)
                return;

            if (Time.unscaledTime - lastAutoSelectionTime < autoSelectRefreshIntervalSeconds)
                return;

            lastAutoSelectionTime = Time.unscaledTime;

            var currentTarget = ClientRuntime.Target.CurrentTarget;
            SyncManualSelectionTracking(currentTarget);
            if (ClientRuntime.Target.HasPinnedTarget)
            {
                if (currentTarget.HasValue && IsTargetStillResolvable(currentTarget.Value))
                    return;

                ClientRuntime.Target.ClearPin();
            }

            Candidate[] candidates;
            if (!TryBuildNearbyCandidates(out candidates))
                return;

            if (ClientRuntime.Target.IsManualSelection)
            {
                if (!currentTarget.HasValue || !IsTargetStillResolvable(currentTarget.Value))
                {
                    ClientRuntime.Target.Clear();
                    return;
                }

                for (var i = 0; i < candidates.Length; i++)
                {
                    if (!candidates[i].Handle.Equals(currentTarget.Value))
                        continue;

                    manualSelectionHasEnteredAutoRange = true;
                    return;
                }

                if (manualSelectionHasEnteredAutoRange)
                    ClientRuntime.Target.Clear();

                return;
            }

            if (candidates.Length == 0)
            {
                if (clearTargetWhenNoNearbyCandidates)
                    ClientRuntime.Target.Clear();
                return;
            }

            if (keepCurrentTargetWhileStillNearby && currentTarget.HasValue)
            {
                for (var i = 0; i < candidates.Length; i++)
                {
                    if (candidates[i].Handle.Equals(currentTarget.Value))
                    {
                        if (ClientRuntime.Target.IsManualSelection)
                            manualSelectionHasEnteredAutoRange = true;

                        return;
                    }
                }
            }

            ClientRuntime.Target.SelectAuto(candidates[0].Handle);
        }

        private bool TryBuildNearbyCandidates(out Candidate[] candidates)
        {
            candidates = System.Array.Empty<Candidate>();
            if (!TryResolveLocalPlayerWorldPosition(out var localPlayerWorldPosition))
                return false;

            var maxDistanceSquared = Mathf.Max(0f, autoSelectRadiusWorldUnits) * Mathf.Max(0f, autoSelectRadiusWorldUnits);
            var resolved = new System.Collections.Generic.Dictionary<WorldTargetHandle, Candidate>();

            var registeredTargetables = WorldTargetableRegistry.GetSnapshot();
            for (var i = 0; i < registeredTargetables.Length; i++)
            {
                var targetable = registeredTargetables[i];
                if (targetable == null || !targetable.isActiveAndEnabled)
                    continue;

                var handle = targetable.Handle;
                if (!handle.IsValid)
                    continue;

                if (!targetable.TryGetWorldSelectionPosition(out var worldPosition))
                    continue;

                TryAddCandidate(resolved, handle, worldPosition, localPlayerWorldPosition, maxDistanceSquared);
            }

            foreach (var observedCharacter in ClientRuntime.World.ObservedCharacters)
            {
                if (ClientCharacterRuntimeStateCodes.IsDefeated(observedCharacter.CurrentState))
                    continue;

                Vector2 worldPosition;
                if (!TryMapServerPositionToWorld(
                        new Vector2(observedCharacter.CurrentState.CurrentPosX, observedCharacter.CurrentState.CurrentPosY),
                        out worldPosition))
                {
                    continue;
                }

                TryAddCandidate(
                    resolved,
                    WorldTargetHandle.CreateObservedCharacter(observedCharacter.Character.CharacterId),
                    worldPosition,
                    localPlayerWorldPosition,
                    maxDistanceSquared);
            }

            foreach (var enemy in ClientRuntime.World.Enemies)
            {
                if (enemy.CurrentHp <= 0)
                    continue;

                Vector2 worldPosition;
                if (!TryMapServerPositionToWorld(new Vector2(enemy.PosX, enemy.PosY), out worldPosition))
                    continue;

                TryAddCandidate(
                    resolved,
                    WorldTargetHandle.CreateEnemy(enemy.RuntimeId, enemy.Kind == 3),
                    worldPosition,
                    localPlayerWorldPosition,
                    maxDistanceSquared);
            }

            candidates = resolved.Values
                .OrderBy(candidate => candidate.Priority)
                .ThenBy(candidate => candidate.DistanceSquared)
                .ThenBy(candidate => candidate.SortKey, System.StringComparer.Ordinal)
                .ToArray();
            return true;
        }

        private bool TryResolveLocalPlayerWorldPosition(out Vector2 worldPosition)
        {
            worldPosition = default;

            if (worldLocalPlayerPresenter != null && worldLocalPlayerPresenter.CurrentPlayerTransform != null)
            {
                var transformPosition = worldLocalPlayerPresenter.CurrentPlayerTransform.position;
                worldPosition = new Vector2(transformPosition.x, transformPosition.y);
                return true;
            }

            return TryMapServerPositionToWorld(ClientRuntime.World.LocalPlayerPosition, out worldPosition);
        }

        private bool TryMapServerPositionToWorld(Vector2 serverPosition, out Vector2 worldPosition)
        {
            worldPosition = default;
            if (worldMapPresenter == null)
                return false;

            return worldMapPresenter.TryMapServerPositionToWorld(serverPosition, out worldPosition);
        }

        private void TryAddCandidate(
            System.Collections.Generic.IDictionary<WorldTargetHandle, Candidate> candidates,
            WorldTargetHandle handle,
            Vector2 worldPosition,
            Vector2 localPlayerWorldPosition,
            float maxDistanceSquared)
        {
            var kind = handle.Kind;
            if (kind == WorldTargetKind.None)
                return;

            if (IsLocalPlayerHandle(handle))
                return;

            var distanceSquared = Vector2.SqrMagnitude(worldPosition - localPlayerWorldPosition);
            if (distanceSquared > maxDistanceSquared)
                return;

            var candidate = new Candidate(
                handle,
                kind,
                worldPosition,
                distanceSquared,
                ResolvePriority(kind),
                $"{(int)kind}:{handle.TargetId}");

            Candidate existing;
            if (candidates.TryGetValue(handle, out existing))
            {
                if (candidate.DistanceSquared >= existing.DistanceSquared)
                    return;
            }

            candidates[handle] = candidate;
        }

        private int ResolvePriority(WorldTargetKind kind)
        {
            if (priorityRules != null)
            {
                for (var i = 0; i < priorityRules.Length; i++)
                {
                    if (priorityRules[i].kind == kind)
                        return priorityRules[i].priority;
                }
            }

            return 100 + (int)kind;
        }

        private static bool IsLocalPlayerHandle(WorldTargetHandle handle)
        {
            if (handle.Kind != WorldTargetKind.Player)
                return false;

            var selectedCharacterId = ClientRuntime.Character.SelectedCharacterId;
            if (!selectedCharacterId.HasValue)
                return false;

            return string.Equals(
                handle.TargetId,
                selectedCharacterId.Value.ToString("D"),
                System.StringComparison.Ordinal);
        }

        private static bool IsTargetStillResolvable(WorldTargetHandle handle)
        {
            return WorldTargetResolutionUtility.IsTargetValid(handle);
        }

        private void SyncManualSelectionTracking(WorldTargetHandle? currentTarget)
        {
            if (!ClientRuntime.Target.IsManualSelection || !currentTarget.HasValue || !currentTarget.Value.IsValid)
            {
                manualSelectionRangeTrackedTarget = default;
                manualSelectionHasEnteredAutoRange = false;
                return;
            }

            if (manualSelectionRangeTrackedTarget.Equals(currentTarget.Value))
                return;

            manualSelectionRangeTrackedTarget = currentTarget.Value;
            manualSelectionHasEnteredAutoRange = false;
        }

        private bool IsSelectionRuntimeReady()
        {
            return AreReady(WorldSceneReadyKey.MapVisual, WorldSceneReadyKey.LocalPlayer);
        }

        private void AutoWireReferences()
        {
            InitializeWorldSceneBehaviour(ref worldMapPresenter);

            if (worldLocalPlayerPresenter == null)
                worldLocalPlayerPresenter = SceneController != null ? SceneController.WorldLocalPlayerPresenter : null;
        }
    }
}
