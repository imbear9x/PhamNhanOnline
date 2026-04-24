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
            public Candidate(WorldTargetHandle handle, WorldTargetKind kind, float distanceSquared, int priority, string sortKey)
            {
                Handle = handle;
                Kind = kind;
                DistanceSquared = distanceSquared;
                Priority = priority;
                SortKey = sortKey ?? string.Empty;
            }

            public WorldTargetHandle Handle { get; }
            public WorldTargetKind Kind { get; }
            public float DistanceSquared { get; }
            public int Priority { get; }
            public string SortKey { get; }
        }

        private sealed class CandidateComparer : System.Collections.Generic.IComparer<Candidate>
        {
            public int Compare(Candidate left, Candidate right)
            {
                var priorityCompare = left.Priority.CompareTo(right.Priority);
                if (priorityCompare != 0)
                    return priorityCompare;

                var distanceCompare = left.DistanceSquared.CompareTo(right.DistanceSquared);
                if (distanceCompare != 0)
                    return distanceCompare;

                return System.StringComparer.Ordinal.Compare(left.SortKey, right.SortKey);
            }
        }

        [Header("World References")]
        private WorldMapPresenter worldMapPresenter;
        private WorldLocalPlayerPresenter worldLocalPlayerPresenter;

        [Header("Auto Selection")]
        [SerializeField] private bool autoSelectNearbyTargets = true;
        [SerializeField] private float autoSelectRadiusWorldUnits = 3.5f;
        [SerializeField] private float autoSelectRefreshIntervalSeconds = 0.2f;
        [SerializeField] private bool clearTargetWhenNoNearbyCandidates = true;

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

        private static readonly CandidateComparer candidateComparer = new CandidateComparer();

        private float lastAutoSelectionTime = float.NegativeInfinity;
        private bool autoSelectionSuppressedUntilManualMoveInput;
        private readonly System.Collections.Generic.Dictionary<WorldTargetHandle, Candidate> resolvedCandidates =
            new System.Collections.Generic.Dictionary<WorldTargetHandle, Candidate>();
        private readonly System.Collections.Generic.List<Candidate> sortedCandidates =
            new System.Collections.Generic.List<Candidate>(16);

        public float AutoSelectRadiusWorldUnits => Mathf.Max(0f, autoSelectRadiusWorldUnits);

        public void SuspendAutoSelectionUntilManualMoveInput()
        {
            autoSelectionSuppressedUntilManualMoveInput = true;
        }

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
            ActivateWorldSceneReadiness();
        }

        private void OnEnable()
        {
            AutoWireReferences();
            ActivateWorldSceneReadiness();
        }

        private void OnDisable()
        {
            DeactivateWorldSceneReadiness();
            ResetAutoSelectionRuntimeState();
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

            if (!TryBuildSortedNearbyCandidates() || sortedCandidates.Count == 0)
                return;

            var nextIndex = 0;
            var currentTarget = ClientRuntime.Target.CurrentTarget;
            if (currentTarget.HasValue)
            {
                for (var i = 0; i < sortedCandidates.Count; i++)
                {
                    if (!sortedCandidates[i].Handle.Equals(currentTarget.Value))
                        continue;

                    nextIndex = (i + 1) % sortedCandidates.Count;
                    break;
                }
            }

            SuspendAutoSelectionUntilManualMoveInput();
            ClientRuntime.Target.Select(sortedCandidates[nextIndex].Handle);
        }

        public void ClearSelectedTarget()
        {
            if (!ClientRuntime.IsInitialized)
                return;

            SuspendAutoSelectionUntilManualMoveInput();
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
            if (IsAutoSelectionSuppressed())
            {
                if (!TryReleaseSuppressedAutoSelectionFromManualMove())
                    return;

                currentTarget = ClientRuntime.Target.CurrentTarget;
            }

            if (ClientRuntime.Target.HasPinnedTarget)
            {
                if (currentTarget.HasValue && IsTargetStillResolvable(currentTarget.Value))
                    return;

                ClientRuntime.Target.ClearPin();
            }

            Candidate bestCandidate;
            if (!TryFindBestNearbyCandidate(out bestCandidate))
                return;

            if (!bestCandidate.Handle.IsValid)
            {
                if (clearTargetWhenNoNearbyCandidates)
                    ClientRuntime.Target.Clear();
                return;
            }

            if (currentTarget.HasValue && bestCandidate.Handle.Equals(currentTarget.Value))
                return;

            ClientRuntime.Target.SelectAuto(bestCandidate.Handle);
        }

        private bool TryBuildSortedNearbyCandidates()
        {
            if (!TryCollectNearbyCandidates())
                return false;

            sortedCandidates.Clear();
            foreach (var candidate in resolvedCandidates.Values)
                sortedCandidates.Add(candidate);

            if (sortedCandidates.Count <= 1)
                return true;

            sortedCandidates.Sort(candidateComparer);
            return true;
        }

        private bool TryFindBestNearbyCandidate(out Candidate bestCandidate)
        {
            bestCandidate = default;
            if (!TryCollectNearbyCandidates())
                return false;

            var hasBestCandidate = false;
            foreach (var candidate in resolvedCandidates.Values)
            {
                if (!hasBestCandidate || candidateComparer.Compare(candidate, bestCandidate) < 0)
                {
                    bestCandidate = candidate;
                    hasBestCandidate = true;
                }
            }

            return true;
        }

        private bool TryCollectNearbyCandidates()
        {
            resolvedCandidates.Clear();
            if (!TryResolveLocalPlayerWorldPosition(out var localPlayerWorldPosition))
                return false;

            var maxDistanceSquared = Mathf.Max(0f, autoSelectRadiusWorldUnits) * Mathf.Max(0f, autoSelectRadiusWorldUnits);

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

                TryAddCandidate(handle, worldPosition, localPlayerWorldPosition, maxDistanceSquared);
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
                    WorldTargetHandle.CreateEnemy(enemy.RuntimeId, enemy.Kind == 3),
                    worldPosition,
                    localPlayerWorldPosition,
                    maxDistanceSquared);
            }

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
                distanceSquared,
                ResolvePriority(kind),
                $"{(int)kind}:{handle.TargetId}");

            Candidate existing;
            if (resolvedCandidates.TryGetValue(handle, out existing))
            {
                if (candidate.DistanceSquared >= existing.DistanceSquared)
                    return;
            }

            resolvedCandidates[handle] = candidate;
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

        private bool IsAutoSelectionSuppressed()
        {
            return autoSelectionSuppressedUntilManualMoveInput || ClientRuntime.Target.IsManualSelection;
        }

        private bool TryReleaseSuppressedAutoSelectionFromManualMove()
        {
            var localActionController = worldLocalPlayerPresenter != null
                ? worldLocalPlayerPresenter.CurrentLocalActionController
                : null;
            if (localActionController == null || !localActionController.HasManualMovementInput)
                return false;

            autoSelectionSuppressedUntilManualMoveInput = false;
            ClientRuntime.Target.ReleaseManualSelectionControl();
            return true;
        }

        protected override void OnWorldLoadCycleStarted(int loadVersion, string mapKey)
        {
            ResetAutoSelectionRuntimeState();
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

        private void ResetAutoSelectionRuntimeState()
        {
            autoSelectionSuppressedUntilManualMoveInput = false;
            lastAutoSelectionTime = float.NegativeInfinity;
            resolvedCandidates.Clear();
            sortedCandidates.Clear();
        }
    }
}
