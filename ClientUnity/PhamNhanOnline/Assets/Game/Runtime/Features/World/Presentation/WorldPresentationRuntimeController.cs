using System;
using PhamNhanOnline.Client.Core.Application;
using PhamNhanOnline.Client.Features.Combat.Presentation;
using UnityEngine;

namespace PhamNhanOnline.Client.Features.World.Presentation
{
    [DisallowMultipleComponent]
    public sealed class WorldPresentationRuntimeController : MonoBehaviour
    {
        [SerializeField] private SkillWorldPresentationCatalog skillWorldPresentationCatalog;

        private void Awake()
        {
            ConfigurePresentationCatalog();
        }

        private void OnEnable()
        {
            ConfigurePresentationCatalog();
        }

        private void Update()
        {
            if (!ClientRuntime.IsInitialized)
                return;

            ConfigurePresentationCatalog();

            var utcNow = DateTime.UtcNow;
            ClientRuntime.SkillPresentationService.Tick(utcNow);
            ClientRuntime.PresentationReplicationService.Tick(utcNow);
        }

        private void OnDestroy()
        {
            if (!ClientRuntime.IsInitialized)
                return;

            ClientRuntime.SkillPresentationService.Clear();
            ClientRuntime.PresentationReplicationService.Clear();
        }

        private void ConfigurePresentationCatalog()
        {
            if (!ClientRuntime.IsInitialized)
                return;

            ClientRuntime.SkillPresentationService.ConfigureCatalog(skillWorldPresentationCatalog);
        }
    }
}
