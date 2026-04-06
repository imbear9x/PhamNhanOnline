using System;
using System.Collections.Generic;
using GameShared.Models;
using PhamNhanOnline.Client.Core.Application;
using PhamNhanOnline.Client.Features.Combat.Application;
using PhamNhanOnline.Client.Features.Skills.Application;
using PhamNhanOnline.Client.Features.Targeting.Application;
using PhamNhanOnline.Client.Features.World.Presentation;
using UnityEngine;

namespace PhamNhanOnline.Client.Features.Combat.Presentation
{
    public sealed class ClientSkillPresentationService
    {
        private sealed class ActiveExecution
        {
            public SkillPresentationExecutionSnapshot Snapshot;
            public SkillWorldPresentationDefinition Definition;
            public bool HasReleased;
        }

        private readonly ClientCombatState combatState;
        private readonly ClientSkillState skillState;
        private readonly ClientSkillPresentationState presentationState;
        private readonly Dictionary<SkillExecutionKey, ActiveExecution> activeExecutions =
            new Dictionary<SkillExecutionKey, ActiveExecution>();

        private SkillWorldPresentationCatalog catalog;

        public ClientSkillPresentationService(
            ClientCombatState combatState,
            ClientSkillState skillState,
            ClientSkillPresentationState presentationState)
        {
            this.combatState = combatState;
            this.skillState = skillState;
            this.presentationState = presentationState;

            combatState.SkillCastStarted += HandleSkillCastStarted;
            combatState.SkillImpactResolved += HandleSkillImpactResolved;
        }

        public void ConfigureCatalog(SkillWorldPresentationCatalog catalog)
        {
            this.catalog = catalog;
        }

        public void Tick(DateTime utcNow)
        {
            if (activeExecutions.Count == 0)
                return;

            var completedKeys = new List<SkillExecutionKey>();
            foreach (var pair in activeExecutions)
            {
                var execution = pair.Value;
                if (!execution.HasReleased &&
                    execution.Snapshot.CastCompletedAtUtc.HasValue &&
                    utcNow >= execution.Snapshot.CastCompletedAtUtc.Value)
                {
                    ReleaseExecution(execution);
                }

                if (execution.Snapshot.Phase == SkillPresentationPhase.Completed ||
                    execution.Snapshot.Phase == SkillPresentationPhase.Cancelled)
                {
                    completedKeys.Add(pair.Key);
                }
            }

            for (var i = 0; i < completedKeys.Count; i++)
                activeExecutions.Remove(completedKeys[i]);
        }

        public void Clear()
        {
            activeExecutions.Clear();
            presentationState.Clear();
            CharacterSkillPresenterRegistry.ClearAllPresentations();
        }

        private void HandleSkillCastStarted(SkillCastStartedNotice notice)
        {
            var key = BuildExecutionKey(notice.MapId, notice.InstanceId, notice.SkillExecutionId);
            if (!key.IsValid)
                return;

            var lookup = BuildLookupContext(
                notice.PlayerSkillId,
                notice.SkillId,
                notice.SkillSlotIndex,
                notice.SkillCode,
                notice.SkillGroupCode);
            var definition = ResolveDefinition(lookup);
            var snapshot = new SkillPresentationExecutionSnapshot(
                key,
                notice.Caster,
                notice.CasterCharacterId,
                notice.Target,
                notice.SkillSlotIndex,
                notice.PlayerSkillId,
                notice.SkillId,
                definition != null ? definition.Archetype : SkillPresentationArchetype.None,
                SkillPresentationPhase.CastStarted,
                notice.CastStartedAtUtc,
                notice.CastCompletedAtUtc,
                notice.ImpactAtUtc,
                null);

            activeExecutions[key] = new ActiveExecution
            {
                Snapshot = snapshot,
                Definition = definition,
                HasReleased = false
            };

            presentationState.BeginExecution(snapshot);

            CharacterSkillPresenter casterPresenter;
            if (TryResolveCasterPresenter(notice.Caster, notice.CasterCharacterId, out casterPresenter))
            {
                casterPresenter.HandleCastStarted(snapshot, definition, ResolveTargetWorldPosition(notice.Target));
            }
        }

        private void HandleSkillImpactResolved(SkillImpactResolvedNotice notice)
        {
            var key = BuildExecutionKey(notice.MapId, notice.InstanceId, notice.SkillExecutionId);
            ActiveExecution execution;
            if (!activeExecutions.TryGetValue(key, out execution))
            {
                var fallbackLookup = BuildLookupContext(
                    notice.PlayerSkillId,
                    notice.SkillId,
                    notice.SkillSlotIndex,
                    notice.SkillCode,
                    notice.SkillGroupCode);
                execution = new ActiveExecution
                {
                    Snapshot = new SkillPresentationExecutionSnapshot(
                        key,
                        notice.Caster,
                        notice.CasterCharacterId,
                        notice.Target,
                        notice.SkillSlotIndex,
                        notice.PlayerSkillId,
                        notice.SkillId,
                        ResolveDefinition(fallbackLookup).Archetype,
                        SkillPresentationPhase.ImpactResolved,
                        null,
                        null,
                        null,
                        notice.ResolvedAtUtc),
                    Definition = ResolveDefinition(fallbackLookup),
                    HasReleased = true
                };
                activeExecutions[key] = execution;
            }

            if (!execution.HasReleased)
                ReleaseExecution(execution);

            var impactSnapshot = new SkillPresentationExecutionSnapshot(
                execution.Snapshot.Key,
                execution.Snapshot.CasterHandle,
                execution.Snapshot.CasterCharacterId,
                notice.Target.HasValue ? notice.Target : execution.Snapshot.Target,
                execution.Snapshot.SkillSlotIndex,
                execution.Snapshot.PlayerSkillId,
                execution.Snapshot.SkillId,
                execution.Snapshot.Archetype,
                SkillPresentationPhase.ImpactResolved,
                execution.Snapshot.CastStartedAtUtc,
                execution.Snapshot.CastCompletedAtUtc,
                execution.Snapshot.ImpactAtUtc,
                notice.ResolvedAtUtc);

            execution.Snapshot = impactSnapshot;
            presentationState.UpdateExecution(impactSnapshot);

            CharacterSkillPresenter casterPresenter;
            if (TryResolveCasterPresenter(impactSnapshot.CasterHandle, impactSnapshot.CasterCharacterId, out casterPresenter))
            {
                casterPresenter.HandleImpactResolvedAsCaster(impactSnapshot, execution.Definition);
            }

            var impactWorldPosition = ResolveTargetWorldPosition(impactSnapshot.Target);
            CharacterSkillPresenter targetPresenter;
            if (impactSnapshot.Target.HasValue &&
                CharacterSkillPresenterRegistry.TryGetByTargetHandle(impactSnapshot.Target.Value, out targetPresenter))
            {
                targetPresenter.HandleImpactResolvedAsTarget(impactSnapshot, execution.Definition, impactWorldPosition);
            }

            var completedSnapshot = new SkillPresentationExecutionSnapshot(
                impactSnapshot.Key,
                impactSnapshot.CasterHandle,
                impactSnapshot.CasterCharacterId,
                impactSnapshot.Target,
                impactSnapshot.SkillSlotIndex,
                impactSnapshot.PlayerSkillId,
                impactSnapshot.SkillId,
                impactSnapshot.Archetype,
                SkillPresentationPhase.Completed,
                impactSnapshot.CastStartedAtUtc,
                impactSnapshot.CastCompletedAtUtc,
                impactSnapshot.ImpactAtUtc,
                impactSnapshot.ResolvedAtUtc);

            execution.Snapshot = completedSnapshot;
            presentationState.CompleteExecution(completedSnapshot);
            activeExecutions.Remove(key);
        }

        private void ReleaseExecution(ActiveExecution execution)
        {
            execution.HasReleased = true;
            execution.Snapshot = new SkillPresentationExecutionSnapshot(
                execution.Snapshot.Key,
                execution.Snapshot.CasterHandle,
                execution.Snapshot.CasterCharacterId,
                execution.Snapshot.Target,
                execution.Snapshot.SkillSlotIndex,
                execution.Snapshot.PlayerSkillId,
                execution.Snapshot.SkillId,
                execution.Snapshot.Archetype,
                SkillPresentationPhase.Released,
                execution.Snapshot.CastStartedAtUtc,
                execution.Snapshot.CastCompletedAtUtc,
                execution.Snapshot.ImpactAtUtc,
                execution.Snapshot.ResolvedAtUtc);
            presentationState.UpdateExecution(execution.Snapshot);

            CharacterSkillPresenter casterPresenter;
            if (TryResolveCasterPresenter(execution.Snapshot.CasterHandle, execution.Snapshot.CasterCharacterId, out casterPresenter))
            {
                casterPresenter.HandleCastReleased(
                    execution.Snapshot,
                    execution.Definition,
                    ResolveTargetWorldPosition(execution.Snapshot.Target));
            }
        }

        private SkillWorldPresentationDefinition ResolveDefinition(SkillPresentationLookupContext lookup)
        {
            return catalog != null ? catalog.Resolve(lookup) : SkillWorldPresentationDefinition.BuildSynthetic(
                lookup.SkillId,
                lookup.SkillCode,
                lookup.SkillGroupCode,
                SkillPresentationArchetype.MeleeWeaponSwing,
                string.Empty,
                string.Empty,
                string.Empty,
                CharacterPresentationSocketType.Root,
                CharacterPresentationSocketType.TargetCenter,
                true);
        }

        private SkillPresentationLookupContext BuildLookupContext(
            long playerSkillId,
            int skillId,
            int skillSlotIndex,
            string skillCode,
            string skillGroupCode)
        {
            if (!string.IsNullOrWhiteSpace(skillCode) || !string.IsNullOrWhiteSpace(skillGroupCode))
                return new SkillPresentationLookupContext(skillId, playerSkillId, skillSlotIndex, skillCode, skillGroupCode);

            var skills = skillState != null ? skillState.Skills : Array.Empty<PlayerSkillModel>();
            for (var i = 0; i < skills.Length; i++)
            {
                var skill = skills[i];
                if (playerSkillId > 0 && skill.PlayerSkillId == playerSkillId)
                {
                    return new SkillPresentationLookupContext(
                        skill.SkillId,
                        skill.PlayerSkillId,
                        skillSlotIndex > 0 ? skillSlotIndex : skill.EquippedSlotIndex,
                        skill.Code ?? string.Empty,
                        skill.SkillGroupCode ?? string.Empty);
                }

                if (skillId > 0 && skill.SkillId == skillId)
                {
                    return new SkillPresentationLookupContext(
                        skill.SkillId,
                        skill.PlayerSkillId,
                        skillSlotIndex > 0 ? skillSlotIndex : skill.EquippedSlotIndex,
                        skill.Code ?? string.Empty,
                        skill.SkillGroupCode ?? string.Empty);
                }
            }

            return new SkillPresentationLookupContext(
                skillId,
                playerSkillId,
                skillSlotIndex,
                skillCode ?? string.Empty,
                skillGroupCode ?? string.Empty);
        }

        private static SkillExecutionKey BuildExecutionKey(int? mapId, int? instanceId, int skillExecutionId)
        {
            return new SkillExecutionKey(
                mapId ?? 0,
                instanceId ?? 0,
                skillExecutionId);
        }

        private static bool TryResolveCasterPresenter(
            WorldTargetHandle? casterHandle,
            Guid? casterCharacterId,
            out CharacterSkillPresenter presenter)
        {
            presenter = null;
            return (casterHandle.HasValue &&
                    casterHandle.Value.IsValid &&
                    CharacterSkillPresenterRegistry.TryGetByTargetHandle(casterHandle.Value, out presenter)) ||
                   (casterCharacterId.HasValue &&
                    CharacterSkillPresenterRegistry.TryGetByCharacterId(casterCharacterId.Value, out presenter));
        }

        private static Vector2? ResolveTargetWorldPosition(WorldTargetHandle? target)
        {
            if (!target.HasValue || !target.Value.IsValid)
                return null;

            WorldTargetable targetable;
            if (WorldTargetableRegistry.TryGet(target.Value, out targetable) &&
                targetable != null &&
                targetable.isActiveAndEnabled &&
                targetable.TryGetWorldSelectionPosition(out var targetWorldPosition))
            {
                return targetWorldPosition;
            }

            var sceneController = WorldSceneController.Instance;
            var worldMapPresenter = sceneController != null ? sceneController.WorldMapPresenter : null;
            if (worldMapPresenter == null)
                return null;

            switch (target.Value.Kind)
            {
                case WorldTargetKind.Player:
                    Guid characterId;
                    if (!Guid.TryParse(target.Value.TargetId, out characterId))
                        return null;

                    ObservedCharacterModel observedCharacter;
                    if (ClientRuntime.World.TryGetObservedCharacter(characterId, out observedCharacter))
                    {
                        Vector2 worldPosition;
                        if (worldMapPresenter.TryMapServerPositionToWorld(
                                new Vector2(
                                    observedCharacter.CurrentState.CurrentPosX,
                                    observedCharacter.CurrentState.CurrentPosY),
                                out worldPosition))
                        {
                            return worldPosition;
                        }
                    }

                    return null;

                case WorldTargetKind.Enemy:
                case WorldTargetKind.Boss:
                    int runtimeId;
                    if (!int.TryParse(target.Value.TargetId, out runtimeId))
                        return null;

                    EnemyRuntimeModel enemy;
                    if (ClientRuntime.World.TryGetEnemy(runtimeId, out enemy))
                    {
                        Vector2 worldPosition;
                        if (worldMapPresenter.TryMapServerPositionToWorld(new Vector2(enemy.PosX, enemy.PosY), out worldPosition))
                            return worldPosition;
                    }

                    return null;

                default:
                    return null;
            }
        }
    }
}
