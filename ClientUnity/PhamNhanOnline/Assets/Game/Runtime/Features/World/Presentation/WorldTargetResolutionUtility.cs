using System;
using System.Globalization;
using GameShared.Models;
using PhamNhanOnline.Client.Core.Application;
using PhamNhanOnline.Client.Features.Targeting.Application;
using PhamNhanOnline.Client.Features.World.Application;
using UnityEngine;

namespace PhamNhanOnline.Client.Features.World.Presentation
{
    internal static class WorldTargetResolutionUtility
    {
        public static bool TryResolveSnapshot(WorldTargetHandle handle, out WorldTargetSnapshot snapshot)
        {
            snapshot = default;
            if (!ClientRuntime.IsInitialized || !handle.IsValid)
                return false;

            if (ClientRuntime.World != null && ClientRuntime.World.TryBuildTargetSnapshot(handle, out snapshot))
                return true;

            WorldTargetable targetable;
            return WorldTargetableRegistry.TryGet(handle, out targetable) &&
                   targetable != null &&
                   targetable.TryBuildFallbackSnapshot(out snapshot);
        }

        public static bool IsTargetValid(WorldTargetHandle handle)
        {
            WorldTargetSnapshot snapshot;
            return TryResolveSnapshot(handle, out snapshot) && !snapshot.IsDead;
        }

        public static bool TryResolveWorldPosition(
            WorldTargetHandle handle,
            WorldMapPresenter worldMapPresenter,
            WorldPortalPresenter worldPortalPresenter,
            out Vector2 worldPosition)
        {
            worldPosition = default;
            if (!ClientRuntime.IsInitialized || !handle.IsValid || worldMapPresenter == null)
                return false;

            MapPortalModel portal;
            if (ClientRuntime.World.TryGetPortal(handle, out portal))
            {
                if (worldPortalPresenter != null &&
                    worldPortalPresenter.TryResolvePortalWorldPosition(portal, out worldPosition))
                {
                    return true;
                }

                return worldMapPresenter.TryMapServerPositionToWorld(
                    new Vector2(portal.SourceX, portal.SourceY),
                    out worldPosition);
            }

            if (LocalFixPortalPresenter.TryResolveActionWorldPosition(handle, out worldPosition))
                return true;

            WorldTargetable targetable;
            if (WorldTargetableRegistry.TryGet(handle, out targetable) &&
                targetable != null &&
                targetable.isActiveAndEnabled &&
                targetable.TryGetWorldSelectionPosition(out worldPosition))
            {
                return true;
            }

            switch (handle.Kind)
            {
                case WorldTargetKind.Player:
                    return TryResolveObservedCharacterWorldPosition(handle.TargetId, worldMapPresenter, out worldPosition);
                case WorldTargetKind.Enemy:
                case WorldTargetKind.Boss:
                    return TryResolveEnemyWorldPosition(handle.TargetId, worldMapPresenter, out worldPosition);
                case WorldTargetKind.GroundReward:
                    return TryResolveGroundRewardWorldPosition(handle.TargetId, worldMapPresenter, out worldPosition);
                default:
                    return false;
            }
        }

        public static bool TryResolveIndicatorWorldPosition(
            WorldTargetHandle handle,
            WorldMapPresenter worldMapPresenter,
            float indicatorHeightOffset,
            float fallbackWorldHeightOffset,
            out Vector2 worldPosition)
        {
            worldPosition = default;
            if (!ClientRuntime.IsInitialized || !handle.IsValid)
                return false;

            WorldTargetable targetable;
            if (WorldTargetableRegistry.TryGet(handle, out targetable) &&
                targetable != null &&
                targetable.isActiveAndEnabled &&
                targetable.TryGetIndicatorAnchorPosition(indicatorHeightOffset, out worldPosition))
            {
                return true;
            }

            if (!TryResolveWorldPosition(handle, worldMapPresenter, null, out worldPosition))
                return false;

            worldPosition += new Vector2(0f, Mathf.Max(0f, fallbackWorldHeightOffset));
            return true;
        }

        private static bool TryResolveObservedCharacterWorldPosition(
            string targetId,
            WorldMapPresenter worldMapPresenter,
            out Vector2 worldPosition)
        {
            worldPosition = default;

            Guid characterId;
            if (!Guid.TryParse(targetId, out characterId))
                return false;

            ObservedCharacterModel observedCharacter;
            if (!ClientRuntime.World.TryGetObservedCharacter(characterId, out observedCharacter))
                return false;

            return worldMapPresenter.TryMapServerPositionToWorld(
                new Vector2(observedCharacter.CurrentState.CurrentPosX, observedCharacter.CurrentState.CurrentPosY),
                out worldPosition);
        }

        private static bool TryResolveEnemyWorldPosition(
            string targetId,
            WorldMapPresenter worldMapPresenter,
            out Vector2 worldPosition)
        {
            worldPosition = default;

            int runtimeId;
            if (!int.TryParse(targetId, NumberStyles.Integer, CultureInfo.InvariantCulture, out runtimeId))
                return false;

            EnemyRuntimeModel enemy;
            if (!ClientRuntime.World.TryGetEnemy(runtimeId, out enemy))
                return false;

            return worldMapPresenter.TryMapServerPositionToWorld(
                new Vector2(enemy.PosX, enemy.PosY),
                out worldPosition);
        }

        private static bool TryResolveGroundRewardWorldPosition(
            string targetId,
            WorldMapPresenter worldMapPresenter,
            out Vector2 worldPosition)
        {
            worldPosition = default;

            int rewardId;
            if (!ClientWorldState.TryParseGroundRewardTargetId(targetId, out rewardId))
                return false;

            GroundRewardModel reward;
            if (!ClientRuntime.World.TryGetGroundReward(rewardId, out reward))
                return false;

            return worldMapPresenter.TryMapServerPositionToWorld(
                new Vector2(reward.PosX, reward.PosY),
                out worldPosition);
        }
    }
}
