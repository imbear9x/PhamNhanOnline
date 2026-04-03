using System;
using System.Threading.Tasks;
using GameShared.Models;
using PhamNhanOnline.Client.Core.Application;
using PhamNhanOnline.Client.Core.Logging;
using PhamNhanOnline.Client.Features.Character.Presentation;
using PhamNhanOnline.Client.Features.World.Application;
using UnityEngine;

namespace PhamNhanOnline.Client.Features.World.Presentation
{
    public sealed partial class WorldPortalPresenter
    {
        private async Task UsePortalAsync(MapPortalModel portal)
        {
            if (usePortalInFlight)
            {
                if (logTouchPortalDiagnostics)
                    ClientLog.Info($"[PortalTouch] skipped UsePortalAsync for portal={portal.Id} because a portal use is already in flight.");
                return;
            }

            usePortalInFlight = true;
            try
            {
                if (worldLocalMovementSyncController != null)
                    worldLocalMovementSyncController.TryForceSyncCurrentPosition();

                Vector2 currentServerPosition;
                Vector2? reportedServerPosition = TryResolveCurrentLocalPlayerServerPosition(out currentServerPosition)
                    ? currentServerPosition
                    : (Vector2?)null;
                if (logTouchPortalDiagnostics)
                {
                    var reportedPosText = reportedServerPosition.HasValue
                        ? $"({reportedServerPosition.Value.x:0.##},{reportedServerPosition.Value.y:0.##})"
                        : "<unresolved>";
                    ClientLog.Info($"[PortalTouch] sending UsePortalAsync portal={portal.Id} map={ClientRuntime.World.CurrentMapId} interactionMode={portal.InteractionMode} reportedServerPos={reportedPosText}.");
                }

                var result = await ClientRuntime.WorldTravelService.UsePortalAsync(portal.Id, reportedServerPosition);
                if (result.Success)
                {
                    ClientLog.Info(
                        string.Format(
                            "Portal {0} used successfully. TargetMap={1}, TargetSpawnPoint={2}.",
                            portal.Id,
                            result.TargetMapId,
                            result.TargetSpawnPointId));
                    return;
                }

                ClientLog.Warn(
                    string.Format(
                        "Portal {0} use failed: {1} ({2}).",
                        portal.Id,
                        result.Code,
                        result.Message));
                WorldTravelDebugController.SetExternalCharacterStatsDebugLine(
                    $"Portal {portal.Id} that bai: {result.Code} ({result.Message})");
            }
            catch (Exception ex)
            {
                ClientLog.Warn(string.Format("Portal {0} use exception: {1}", portal.Id, ex.Message));
            }
            finally
            {
                usePortalInFlight = false;
            }
        }

        private void PollTouchPortals()
        {
            if (!ClientRuntime.IsInitialized || spawnedPortals.Count == 0)
                return;

            var allowTouchTravel = !usePortalInFlight &&
                                   Time.unscaledTime >= touchPortalSuppressedUntilTime;
            foreach (var runtime in spawnedPortals.Values)
            {
                if (runtime == null)
                    continue;

                if (runtime.TriggerCollider == null)
                {
                    if (logTouchPortalDiagnostics)
                        LogTouchPortalDiagnostics(runtime, allowTouchTravel, false, false, false, false, "missing-trigger");
                    continue;
                }

                var isTouchPortal = IsTouchPortal(runtime.Portal);
                var isColliderOverlap = IsPortalTouchingLocalPlayer(runtime);
                var hasPortalEntryIntent = HasPortalEntryIntent(runtime.Portal);
                var isTouching = isTouchPortal &&
                                 runtime.Portal.IsEnabled &&
                                 isColliderOverlap &&
                                 hasPortalEntryIntent;

                if (logTouchPortalDiagnostics)
                    LogTouchPortalDiagnostics(runtime, allowTouchTravel, isTouchPortal, isColliderOverlap, hasPortalEntryIntent, isTouching, "state");

                if (allowTouchTravel && isTouching && !runtime.WasTouchingLastFrame)
                {
                    if (logTouchPortalDiagnostics)
                        LogTouchPortalDiagnostics(runtime, allowTouchTravel, isTouchPortal, isColliderOverlap, hasPortalEntryIntent, isTouching, "enter-trigger");
                    _ = UsePortalAsync(runtime.Portal);
                }

                runtime.WasTouchingLastFrame = isTouching;
            }
        }

        private bool HasPortalEntryIntent(MapPortalModel portal)
        {
            var side = ResolveTouchTriggerSide(portal);
            if (side == TouchTriggerSide.None)
                return true;

            var horizontalIntent = ResolveHorizontalMoveIntent();
            var deadZone = Mathf.Max(0f, touchPortalHorizontalIntentDeadZone);
            switch (side)
            {
                case TouchTriggerSide.Left:
                    return horizontalIntent < -deadZone;

                case TouchTriggerSide.Right:
                    return horizontalIntent > deadZone;

                default:
                    return true;
            }
        }

        private void LogTouchPortalDiagnostics(
            PortalRuntime runtime,
            bool allowTouchTravel,
            bool isTouchPortal,
            bool isColliderOverlap,
            bool hasPortalEntryIntent,
            bool isTouching,
            string reason)
        {
            if (!logTouchPortalDiagnostics || runtime == null)
                return;

            var playerPositionText = TryResolveCurrentLocalPlayerServerPosition(out var playerServerPosition)
                ? $"({playerServerPosition.x:0.##},{playerServerPosition.y:0.##})"
                : "<unresolved>";
            var key = string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "{0}|{1}|{2}|{3}|{4}|{5}|{6}|{7}|{8}",
                reason,
                allowTouchTravel,
                isTouchPortal,
                runtime.Portal.IsEnabled,
                isColliderOverlap,
                hasPortalEntryIntent,
                isTouching,
                runtime.WasTouchingLastFrame,
                usePortalInFlight);
            if (string.Equals(reason, "state", StringComparison.Ordinal) &&
                string.Equals(runtime.LastTouchDiagnosticKey, key, StringComparison.Ordinal))
            {
                return;
            }

            runtime.LastTouchDiagnosticKey = key;
            ClientLog.Info(
                $"[PortalTouch] portal={runtime.Portal.Id} reason={reason} side={ResolveTouchTriggerSide(runtime.Portal)} enabled={runtime.Portal.IsEnabled} touchMode={runtime.Portal.InteractionMode} triggerEnabled={(runtime.TriggerCollider != null && runtime.TriggerCollider.enabled)} overlap={isColliderOverlap} intent={hasPortalEntryIntent} isTouching={isTouching} wasTouching={runtime.WasTouchingLastFrame} allowTouch={allowTouchTravel} inFlight={usePortalInFlight} suppressedUntil={touchPortalSuppressedUntilTime:0.##} playerPos={playerPositionText} portalPos=({runtime.Portal.SourceX:0.##},{runtime.Portal.SourceY:0.##}) radius={runtime.Portal.InteractionRadius:0.##}");
        }

        private float ResolveHorizontalMoveIntent()
        {
            WorldLocalPlayerPresenter localPlayerPresenter = null;
            if (worldSceneController != null)
                localPlayerPresenter = worldSceneController.WorldLocalPlayerPresenter;
            if (localPlayerPresenter == null)
                localPlayerPresenter = GetComponent<WorldLocalPlayerPresenter>();

            var localActionController = localPlayerPresenter != null
                ? localPlayerPresenter.CurrentLocalActionController
                : null;
            if (localActionController != null)
            {
                var input = localActionController.CurrentHorizontalMoveInput;
                if (Mathf.Abs(input) > Mathf.Epsilon)
                    return input;
            }

            var playerTransform = localPlayerPresenter != null
                ? localPlayerPresenter.CurrentPlayerTransform
                : null;
            if (playerTransform != null)
            {
                var playerView = playerTransform.GetComponent<PlayerView>();
                if (playerView != null && playerView.Body != null)
                {
                    var velocityX = playerView.Body.velocity.x;
                    if (Mathf.Abs(velocityX) > Mathf.Epsilon)
                        return velocityX;
                }
            }

            return 0f;
        }

        private bool IsPortalTouchingLocalPlayer(PortalRuntime runtime)
        {
            if (runtime == null || runtime.TriggerCollider == null || !runtime.TriggerCollider.enabled)
                return false;

            Collider2D playerCollider;
            if (TryResolveLocalPlayerCollider(out playerCollider) && playerCollider != null && playerCollider.enabled)
            {
                return runtime.TriggerCollider.Distance(playerCollider).isOverlapped;
            }

            if (!TryResolveCurrentLocalPlayerServerPosition(out var playerServerPosition))
                return false;

            var portalServerPosition = new Vector2(runtime.Portal.SourceX, runtime.Portal.SourceY);
            return Vector2.Distance(playerServerPosition, portalServerPosition) <= Mathf.Max(0f, runtime.Portal.InteractionRadius);
        }

        private bool TryResolveLocalPlayerCollider(out Collider2D playerCollider)
        {
            playerCollider = null;

            WorldLocalPlayerPresenter localPlayerPresenter = null;
            if (worldSceneController != null)
                localPlayerPresenter = worldSceneController.WorldLocalPlayerPresenter;
            if (localPlayerPresenter == null)
                localPlayerPresenter = GetComponent<WorldLocalPlayerPresenter>();
            if (localPlayerPresenter == null || localPlayerPresenter.CurrentPlayerTransform == null)
                return false;

            var playerTransform = localPlayerPresenter.CurrentPlayerTransform;
            var playerView = playerTransform.GetComponent<PlayerView>();
            if (playerView != null && playerView.BodyCollider != null && playerView.BodyCollider.enabled)
            {
                playerCollider = playerView.BodyCollider;
                return true;
            }

            playerCollider = playerTransform.GetComponent<Collider2D>();
            return playerCollider != null && playerCollider.enabled;
        }

        private bool TryResolveCurrentLocalPlayerServerPosition(out Vector2 playerServerPosition)
        {
            playerServerPosition = default;

            if (!ClientRuntime.IsInitialized)
                return false;

            if (worldMapPresenter != null)
            {
                WorldLocalPlayerPresenter localPlayerPresenter = null;
                if (worldSceneController != null)
                    localPlayerPresenter = worldSceneController.WorldLocalPlayerPresenter;
                if (localPlayerPresenter == null)
                    localPlayerPresenter = GetComponent<WorldLocalPlayerPresenter>();

                var playerTransform = localPlayerPresenter != null
                    ? localPlayerPresenter.CurrentPlayerTransform
                    : null;
                if (playerTransform != null &&
                    worldMapPresenter.TryMapWorldPositionToServer(playerTransform.position, out playerServerPosition))
                {
                    return true;
                }
            }

            playerServerPosition = ClientRuntime.World.LocalPlayerPosition;
            return true;
        }

        private TouchTriggerSide ResolveTouchTriggerSide(MapPortalModel portal)
        {
            var mapWidth = ClientRuntime.World.CurrentMapWidth;
            if (mapWidth <= Mathf.Epsilon)
                return TouchTriggerSide.None;

            var normalizedX = Mathf.Clamp01(portal.SourceX / mapWidth);
            var edgeThreshold = Mathf.Clamp(edgePortalThresholdNormalized, 0.01f, 0.49f);
            if (normalizedX <= edgeThreshold)
                return TouchTriggerSide.Left;

            if (normalizedX >= 1f - edgeThreshold)
                return TouchTriggerSide.Right;

            return TouchTriggerSide.None;
        }

        private static bool IsTouchPortal(MapPortalModel portal)
        {
            return portal.InteractionMode == 0 || portal.InteractionMode == TouchInteractionMode;
        }
    }
}
