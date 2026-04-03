using System;
using System.Collections.Generic;
using GameShared.Models;
using PhamNhanOnline.Client.Core.Application;
using PhamNhanOnline.Client.Core.Logging;
using UnityEngine;

namespace PhamNhanOnline.Client.Features.World.Presentation
{
    [DisallowMultipleComponent]
    public sealed class WorldRangeDebugOverlay : WorldSceneBehaviour
    {
        private sealed class OverlayVisual
        {


            
            public string Key;
            public GameObject RootObject;
            public SpriteRenderer FillRenderer;
            public bool UsedThisFrame;
        }

        [Header("References")]
        [SerializeField] private WorldMapPresenter worldMapPresenter;
        [SerializeField] private WorldLocalPlayerPresenter worldLocalPlayerPresenter;
        [SerializeField] private WorldClickTargetSelectionController targetSelectionController;
        [SerializeField] private WorldTargetActionController targetActionController;
        [SerializeField] private Transform overlayRoot;

        [Header("Activation")]
        [SerializeField] private bool overlayEnabled = true;
        [SerializeField] private bool developmentBuildOnly = true;
        [SerializeField] private float refreshIntervalSeconds = 0.05f;

        [Header("Player Ranges")]
        [SerializeField] private bool showAutoSelectRange = true;
        [SerializeField] private bool showContextInteractionRange = true;
        [SerializeField] private bool showBasicSkillRange = true;

        [Header("Portal Ranges")]
        [SerializeField] private bool showPortalServerRange = true;
        [SerializeField] private bool showPortalClientEffectiveRange = true;

        [Header("Visuals")]
        [SerializeField] private int sortingOrder = 180;
        [SerializeField] private Color autoSelectColor = new Color(0.15f, 0.85f, 1f, 0.12f);
        [SerializeField] private Color contextInteractionColor = new Color(1f, 0.85f, 0.2f, 0.10f);
        [SerializeField] private Color basicSkillColor = new Color(1f, 0.25f, 0.25f, 0.10f);
        [SerializeField] private Color portalServerColor = new Color(0.1f, 1f, 0.55f, 0.08f);
        [SerializeField] private Color portalEffectiveColor = new Color(0.8f, 0.35f, 1f, 0.08f);

        private readonly Dictionary<string, OverlayVisual> overlays = new Dictionary<string, OverlayVisual>();
        private static Sprite circleSprite;
        private float nextRefreshTime;
        private bool loggedMissingOverlayMapPresenter;
        private bool loggedMissingLocalPlayerPresenter;
        private bool loggedMissingSelectionController;
        private bool loggedMissingActionController;

        private void Awake()
        {
            AutoWireReferences();
        }

        private void Start()
        {
            AutoWireReferences();
            LogMissingCriticalWorldSceneDependenciesIfNeeded();
            LogMissingCriticalDependenciesIfNeeded();
            ActivateWorldSceneReadiness();
        }

        private void OnEnable()
        {
            ActivateWorldSceneReadiness();
        }

        private void OnDisable()
        {
            DeactivateWorldSceneReadiness();
            HideAllOverlays();
        }

        private void OnDestroy()
        {
            DeactivateWorldSceneReadiness();
            ClearOverlays();
        }

        private void Update()
        {
            if (!ShouldRenderOverlay())
            {
                HideAllOverlays();
                return;
            }

            if (Time.unscaledTime < nextRefreshTime)
                return;

            nextRefreshTime = Time.unscaledTime + Mathf.Max(0.01f, refreshIntervalSeconds);
            RefreshOverlays();
        }

        protected override void ConfigureReadyWaits()
        {
            WaitFor(WorldSceneReadyKey.MapVisual, RefreshOverlays);
            WaitFor(WorldSceneReadyKey.LocalPlayer, RefreshOverlays);
        }

        private bool ShouldRenderOverlay()
        {
            if (!overlayEnabled)
                return false;

            if (developmentBuildOnly && !UnityEngine.Application.isEditor && !Debug.isDebugBuild)
                return false;

            if (!ClientRuntime.IsInitialized)
                return false;

            if (!AreReady(WorldSceneReadyKey.MapVisual, WorldSceneReadyKey.LocalPlayer))
                return false;

            AutoWireReferences();
            LogMissingCriticalDependenciesIfNeeded();
            return worldMapPresenter != null &&
                   worldLocalPlayerPresenter != null &&
                   targetSelectionController != null &&
                   targetActionController != null;
        }

        private void RefreshOverlays()
        {
            if (!ShouldRenderOverlay())
                return;

            foreach (var overlay in overlays.Values)
                overlay.UsedThisFrame = false;

            if (!TryResolveLocalPlayerWorldPosition(out var playerWorldPosition))
            {
                HideUnusedOverlays();
                return;
            }

            if (showAutoSelectRange)
            {
                UpsertWorldCircle(
                    "player:auto-select",
                    playerWorldPosition,
                    targetSelectionController.AutoSelectRadiusWorldUnits,
                    autoSelectColor);
            }

            if (worldMapPresenter.TryGetWorldUnitsPerServerUnit(out var worldUnitsPerServerUnit))
            {
                if (showContextInteractionRange)
                {
                    var effectiveContextRange = targetActionController.InteractionRangeServerUnits +
                                                targetActionController.ActionRangeBufferServerUnits;
                    UpsertServerCircle(
                        "player:context-effective",
                        playerWorldPosition,
                        effectiveContextRange,
                        worldUnitsPerServerUnit,
                        contextInteractionColor);
                }

                if (showBasicSkillRange && targetActionController.TryGetBasicSkillCastRangeServerUnits(out var basicSkillCastRange))
                {
                    var effectiveBasicRange = basicSkillCastRange + targetActionController.ActionRangeBufferServerUnits;
                    UpsertServerCircle(
                        "player:basic-effective",
                        playerWorldPosition,
                        effectiveBasicRange,
                        worldUnitsPerServerUnit,
                        basicSkillColor);
                }

                foreach (var portal in ClientRuntime.World.CurrentPortals)
                {
                    if (!worldMapPresenter.TryMapServerPositionToWorld(new Vector2(portal.SourceX, portal.SourceY), out var portalWorldPosition))
                        continue;

                    if (showPortalServerRange)
                    {
                        UpsertServerCircle(
                            $"portal:{portal.Id}:server",
                            portalWorldPosition,
                            Mathf.Max(0f, portal.InteractionRadius),
                            worldUnitsPerServerUnit,
                            portalServerColor);
                    }

                    if (showPortalClientEffectiveRange)
                    {
                        var effectivePortalRange = Mathf.Max(0f, portal.InteractionRadius) +
                                                   targetActionController.PortalActionRangeBufferServerUnits;
                        UpsertServerCircle(
                            $"portal:{portal.Id}:effective",
                            portalWorldPosition,
                            effectivePortalRange,
                            worldUnitsPerServerUnit,
                            portalEffectiveColor);
                    }
                }
            }

            HideUnusedOverlays();
        }

        private bool TryResolveLocalPlayerWorldPosition(out Vector2 worldPosition)
        {
            worldPosition = default;
            if (worldLocalPlayerPresenter == null || worldLocalPlayerPresenter.CurrentPlayerTransform == null)
                return false;

            var position = worldLocalPlayerPresenter.CurrentPlayerTransform.position;
            worldPosition = new Vector2(position.x, position.y);
            return true;
        }

        private void UpsertServerCircle(string key, Vector2 center, float radiusServerUnits, Vector2 worldUnitsPerServerUnit, Color color)
        {
            var radiusX = Mathf.Max(0f, radiusServerUnits) * Mathf.Max(worldUnitsPerServerUnit.x, 0f);
            var radiusY = Mathf.Max(0f, radiusServerUnits) * Mathf.Max(worldUnitsPerServerUnit.y, 0f);
            UpsertEllipse(key, center, radiusX, radiusY, color);
        }

        private void UpsertWorldCircle(string key, Vector2 center, float radiusWorldUnits, Color color)
        {
            var radius = Mathf.Max(0f, radiusWorldUnits);
            UpsertEllipse(key, center, radius, radius, color);
        }

        private void UpsertEllipse(string key, Vector2 center, float radiusX, float radiusY, Color color)
        {
            if (radiusX <= Mathf.Epsilon || radiusY <= Mathf.Epsilon)
                return;

            var overlay = GetOrCreateOverlay(key);
            overlay.UsedThisFrame = true;
            overlay.RootObject.SetActive(true);
            overlay.RootObject.transform.position = new Vector3(center.x, center.y, 0f);
            overlay.RootObject.transform.localScale = new Vector3(radiusX * 2f, radiusY * 2f, 1f);
            overlay.FillRenderer.color = color;
            overlay.FillRenderer.sortingOrder = sortingOrder;
        }

        private OverlayVisual GetOrCreateOverlay(string key)
        {
            if (overlays.TryGetValue(key, out var existing) && existing != null)
                return existing;

            var root = new GameObject($"RangeDebug_{key}");
            root.transform.SetParent(ResolveOverlayRoot(), false);
            var fillRenderer = root.AddComponent<SpriteRenderer>();
            fillRenderer.sprite = GetCircleSprite();
            fillRenderer.drawMode = SpriteDrawMode.Simple;
            fillRenderer.maskInteraction = SpriteMaskInteraction.None;
            fillRenderer.sortingOrder = sortingOrder;

            var created = new OverlayVisual
            {
                Key = key,
                RootObject = root,
                FillRenderer = fillRenderer,
                UsedThisFrame = true
            };
            overlays[key] = created;
            return created;
        }

        private Transform ResolveOverlayRoot()
        {
            if (overlayRoot != null)
                return overlayRoot;

            overlayRoot = new GameObject("WorldRangeDebugOverlayRoot").transform;
            overlayRoot.SetParent(transform, false);
            overlayRoot.localPosition = Vector3.zero;
            overlayRoot.localRotation = Quaternion.identity;
            overlayRoot.localScale = Vector3.one;
            return overlayRoot;
        }

        private void HideUnusedOverlays()
        {
            foreach (var overlay in overlays.Values)
            {
                if (overlay == null || overlay.RootObject == null)
                    continue;

                overlay.RootObject.SetActive(overlay.UsedThisFrame);
            }
        }

        private void HideAllOverlays()
        {
            foreach (var overlay in overlays.Values)
            {
                if (overlay == null || overlay.RootObject == null)
                    continue;

                overlay.RootObject.SetActive(false);
            }
        }

        private void ClearOverlays()
        {
            foreach (var overlay in overlays.Values)
            {
                if (overlay == null || overlay.RootObject == null)
                    continue;

                Destroy(overlay.RootObject);
            }

            overlays.Clear();
        }

        private void AutoWireReferences()
        {
            InitializeWorldSceneBehaviour(ref worldMapPresenter);

            if (worldLocalPlayerPresenter == null)
                worldLocalPlayerPresenter = SceneController != null ? SceneController.WorldLocalPlayerPresenter : GetComponent<WorldLocalPlayerPresenter>();

            if (targetSelectionController == null)
                targetSelectionController = GetComponent<WorldClickTargetSelectionController>();

            if (targetActionController == null)
                targetActionController = GetComponent<WorldTargetActionController>();
        }

        private void LogMissingCriticalDependenciesIfNeeded()
        {
            if (worldMapPresenter == null && !loggedMissingOverlayMapPresenter)
            {
                ClientLog.Error("WorldRangeDebugOverlay could not resolve WorldMapPresenter.");
                loggedMissingOverlayMapPresenter = true;
            }

            if (worldLocalPlayerPresenter == null && !loggedMissingLocalPlayerPresenter)
            {
                ClientLog.Error("WorldRangeDebugOverlay could not resolve WorldLocalPlayerPresenter.");
                loggedMissingLocalPlayerPresenter = true;
            }

            if (targetSelectionController == null && !loggedMissingSelectionController)
            {
                ClientLog.Error("WorldRangeDebugOverlay could not resolve WorldClickTargetSelectionController.");
                loggedMissingSelectionController = true;
            }

            if (targetActionController == null && !loggedMissingActionController)
            {
                ClientLog.Error("WorldRangeDebugOverlay could not resolve WorldTargetActionController.");
                loggedMissingActionController = true;
            }
        }

        private static Sprite GetCircleSprite()
        {
            if (circleSprite != null)
                return circleSprite;

            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "WorldRangeDebugCircle",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };

            var pixels = new Color[size * size];
            var center = (size - 1) * 0.5f;
            var featherStart = 0.86f;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var normalizedX = (x - center) / center;
                    var normalizedY = (y - center) / center;
                    var distance = Mathf.Sqrt((normalizedX * normalizedX) + (normalizedY * normalizedY));
                    var alpha = 0f;
                    if (distance <= 1f)
                    {
                        alpha = distance <= featherStart
                            ? 1f
                            : Mathf.InverseLerp(1f, featherStart, distance);
                    }

                    pixels[(y * size) + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, true);
            circleSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
            circleSprite.name = "WorldRangeDebugCircleSprite";
            return circleSprite;
        }
    }
}