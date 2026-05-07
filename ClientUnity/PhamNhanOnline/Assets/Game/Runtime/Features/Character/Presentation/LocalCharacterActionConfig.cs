using UnityEngine;
using UnityEngine.Serialization;

namespace PhamNhanOnline.Client.Features.Character.Presentation
{
    [CreateAssetMenu(
        fileName = "LocalCharacterActionConfig",
        menuName = "PhamNhanOnline/Character/Local Character Action Config")]
    public sealed class LocalCharacterActionConfig : ScriptableObject
    {
        [Header("Movement")]
        [SerializeField] private float baseMoveSpeed = 4.5f;
        [SerializeField] private float serverMoveSpeedScale = 0.013f;
        [FormerlySerializedAs("jumpVelocity")]
        [FormerlySerializedAs("flyUpSpeed")]
        [SerializeField] private float flyUpSpeedMultiplier = 1.75f;
        [FormerlySerializedAs("flySpeed")]
        [FormerlySerializedAs("fallSpeed")]
        [SerializeField] private float fallSpeedMultiplier = 2f;
        [SerializeField] private float hoverActivationHeight = 1.5f;
        [SerializeField] private float hoverDuration = 0.35f;
        [FormerlySerializedAs("verticalVelocityChangeRate")]
        [SerializeField] private float verticalVelocityChangeRateMultiplier = 6f;
        [SerializeField] private int speedStatBaseline = 100;
        [SerializeField] private float movementDeadZone = 0.05f;

        [Header("Ground Check")]
        [SerializeField] private float groundCheckRadius = 0.15f;

        public float BaseMoveSpeed
        {
            get { return baseMoveSpeed; }
        }

        public float ServerMoveSpeedScale
        {
            get { return serverMoveSpeedScale; }
        }

        public float FlyUpSpeedMultiplier
        {
            get { return flyUpSpeedMultiplier; }
        }

        public float FallSpeedMultiplier
        {
            get { return fallSpeedMultiplier; }
        }

        public float HoverActivationHeight
        {
            get { return hoverActivationHeight; }
        }

        public float HoverDuration
        {
            get { return hoverDuration; }
        }

        public float VerticalVelocityChangeRateMultiplier
        {
            get { return verticalVelocityChangeRateMultiplier; }
        }

        public int SpeedStatBaseline
        {
            get { return speedStatBaseline; }
        }

        public float MovementDeadZone
        {
            get { return movementDeadZone; }
        }

        public float GroundCheckRadius
        {
            get { return groundCheckRadius; }
        }

        public float ConvertServerUnitsToWorldUnits(float serverUnits)
        {
            return Mathf.Max(0f, serverUnits) * Mathf.Max(0f, serverMoveSpeedScale);
        }

        public static LocalCharacterActionConfig CreateRuntimeDefaults()
        {
            var config = CreateInstance<LocalCharacterActionConfig>();
            config.name = "RuntimeDefaultLocalCharacterActionConfig";
            return config;
        }
    }

    public static class EntityMovementPresentationPolicy
    {
        public static float ConvertServerUnitsToWorldUnits(
            float serverUnits,
            float? worldUnitsPerServerUnit,
            LocalCharacterActionConfig fallbackConfig)
        {
            var safeServerUnits = Mathf.Max(0f, serverUnits);
            if (worldUnitsPerServerUnit.HasValue &&
                IsFinite(worldUnitsPerServerUnit.Value) &&
                worldUnitsPerServerUnit.Value > 0f)
            {
                return safeServerUnits * worldUnitsPerServerUnit.Value;
            }

            return fallbackConfig != null
                ? fallbackConfig.ConvertServerUnitsToWorldUnits(safeServerUnits)
                : safeServerUnits;
        }

        public static float ResolveAuthoritativeMoveDurationSeconds(
            float serverDistance,
            float serverMoveSpeed)
        {
            if (serverMoveSpeed <= 0f)
                return 0f;

            return Mathf.Max(0f, serverDistance) / serverMoveSpeed;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
