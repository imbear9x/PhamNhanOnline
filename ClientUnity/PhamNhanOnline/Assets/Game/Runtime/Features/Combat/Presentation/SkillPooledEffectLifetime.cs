using PhamNhanOnline.Client.Infrastructure.Pooling;
using UnityEngine;

namespace PhamNhanOnline.Client.Features.Combat.Presentation
{
    [DisallowMultipleComponent]
    public sealed class SkillPooledEffectLifetime : MonoBehaviour
    {
        private ParticleSystem[] particleSystems = System.Array.Empty<ParticleSystem>();
        private TrailRenderer[] trailRenderers = System.Array.Empty<TrailRenderer>();
        private PooledInstance pooledInstance;
        private float lifetimeSeconds;
        private float elapsedSeconds;
        private bool autoReleaseArmed;

        private void Awake()
        {
            CacheReferences();
        }

        private void OnEnable()
        {
            CacheReferences();
        }

        private void OnDisable()
        {
            autoReleaseArmed = false;
            elapsedSeconds = 0f;
            StopVisuals();
        }

        private void Update()
        {
            if (!autoReleaseArmed)
                return;

            elapsedSeconds += Time.deltaTime;
            if (elapsedSeconds >= lifetimeSeconds)
                ReleaseNow();
        }

        public void Begin(float autoReleaseAfterSeconds)
        {
            CacheReferences();
            elapsedSeconds = 0f;
            lifetimeSeconds = Mathf.Max(0f, autoReleaseAfterSeconds);
            autoReleaseArmed = lifetimeSeconds > 0f;
            ResetVisuals();
        }

        public void ReleaseNow()
        {
            if (!gameObject.activeSelf)
                return;

            autoReleaseArmed = false;
            elapsedSeconds = 0f;
            CacheReferences();
            StopVisuals();

            if (pooledInstance != null)
                pooledInstance.Release();
            else
                Destroy(gameObject);
        }

        private void CacheReferences()
        {
            if (pooledInstance == null)
                pooledInstance = GetComponent<PooledInstance>();

            particleSystems = GetComponentsInChildren<ParticleSystem>(true);
            trailRenderers = GetComponentsInChildren<TrailRenderer>(true);
        }

        private void ResetVisuals()
        {
            for (var i = 0; i < trailRenderers.Length; i++)
            {
                var trail = trailRenderers[i];
                if (trail != null)
                    trail.Clear();
            }

            for (var i = 0; i < particleSystems.Length; i++)
            {
                var particleSystem = particleSystems[i];
                if (particleSystem == null)
                    continue;

                particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                particleSystem.Clear(true);
                particleSystem.Play(true);
            }
        }

        private void StopVisuals()
        {
            for (var i = 0; i < trailRenderers.Length; i++)
            {
                var trail = trailRenderers[i];
                if (trail != null)
                    trail.Clear();
            }

            for (var i = 0; i < particleSystems.Length; i++)
            {
                var particleSystem = particleSystems[i];
                if (particleSystem == null)
                    continue;

                particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                particleSystem.Clear(true);
            }
        }
    }
}
