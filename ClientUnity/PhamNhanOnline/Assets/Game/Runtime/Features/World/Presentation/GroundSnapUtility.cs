using System;
using UnityEngine;

namespace PhamNhanOnline.Client.Features.World.Presentation
{
    internal static class GroundSnapUtility
    {
        private const float DefaultMinimumVerticalSeparation = 0.01f;

        public static int ResolveGroundLayerMask(LayerMask configuredMask)
        {
            if (configuredMask.value != 0)
                return configuredMask.value;

            var worldMapLayer = LayerMask.NameToLayer("WorldMap");
            if (worldMapLayer >= 0)
                return 1 << worldMapLayer;

            return Physics2D.DefaultRaycastLayers;
        }

        public static bool TryFindGroundHit(
            Vector2 rayOrigin,
            float rayDistance,
            int preferredLayerMask,
            Action<string> logDiagnostic,
            out RaycastHit2D hit)
        {
            hit = FindGroundHit(rayOrigin, rayDistance, preferredLayerMask, logDiagnostic);
            if (hit.collider != null)
                return true;

            if (preferredLayerMask == Physics2D.DefaultRaycastLayers)
                return false;

            hit = FindGroundHit(rayOrigin, rayDistance, Physics2D.DefaultRaycastLayers, logDiagnostic);
            if (hit.collider == null)
                return false;

            logDiagnostic?.Invoke(
                $"ray-fallback-hit rayOrigin={rayOrigin} rayDistance={rayDistance} primaryMask={preferredLayerMask} " +
                $"fallbackMask={Physics2D.DefaultRaycastLayers} hitCollider={hit.collider.name} " +
                $"hitLayer={LayerMask.LayerToName(hit.collider.gameObject.layer)}.");
            return true;
        }

        private static RaycastHit2D FindGroundHit(
            Vector2 rayOrigin,
            float rayDistance,
            int layerMask,
            Action<string> logDiagnostic)
        {
            var hits = Physics2D.RaycastAll(rayOrigin, Vector2.down, rayDistance, layerMask);
            for (var i = 0; i < hits.Length; i++)
            {
                var candidate = hits[i];
                if (IsValidGroundHit(candidate, rayOrigin))
                    return candidate;

                if (candidate.collider != null)
                {
                    logDiagnostic?.Invoke(
                        $"ray-skip collider={candidate.collider.name} hitPoint={candidate.point} " +
                        $"hitLayer={LayerMask.LayerToName(candidate.collider.gameObject.layer)} isTrigger={candidate.collider.isTrigger}.");
                }
            }

            return default;
        }

        private static bool IsValidGroundHit(RaycastHit2D hit, Vector2 rayOrigin)
        {
            if (hit.collider == null)
                return false;

            if (hit.collider.isTrigger)
                return false;

            var colliderName = hit.collider.name ?? string.Empty;
            if (colliderName.IndexOf("PlayableBounds", StringComparison.OrdinalIgnoreCase) >= 0)
                return false;

            if (hit.point.y >= rayOrigin.y - DefaultMinimumVerticalSeparation)
                return false;

            return true;
        }
    }
}
