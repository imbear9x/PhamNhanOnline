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
        [FormerlySerializedAs("jumpVelocity")]
        [SerializeField] private float flyUpSpeed = 7.5f;
        [FormerlySerializedAs("flySpeed")]
        [SerializeField] private float fallSpeed = 5.5f;
        [SerializeField] private float hoverActivationHeight = 1.5f;
        [SerializeField] private float hoverDuration = 0.35f;
        [SerializeField] private float verticalVelocityChangeRate = 35f;
        [SerializeField] private int speedStatBaseline = 100;
        [SerializeField] private float movementDeadZone = 0.05f;

        [Header("Combat")]
        [SerializeField] private float attackDuration = 0.45f;
        [SerializeField] private float attackCooldown = 0.15f;
        [SerializeField] private float attackMovementMultiplier = 0.35f;

        [Header("Ground Check")]
        [SerializeField] private float groundCheckRadius = 0.15f;

        public float BaseMoveSpeed
        {
            get { return baseMoveSpeed; }
        }

        public float FlyUpSpeed
        {
            get { return flyUpSpeed; }
        }

        public float FallSpeed
        {
            get { return fallSpeed; }
        }

        public float HoverActivationHeight
        {
            get { return hoverActivationHeight; }
        }

        public float HoverDuration
        {
            get { return hoverDuration; }
        }

        public float VerticalVelocityChangeRate
        {
            get { return verticalVelocityChangeRate; }
        }

        public int SpeedStatBaseline
        {
            get { return speedStatBaseline; }
        }

        public float MovementDeadZone
        {
            get { return movementDeadZone; }
        }

        public float AttackDuration
        {
            get { return attackDuration; }
        }

        public float AttackCooldown
        {
            get { return attackCooldown; }
        }

        public float AttackMovementMultiplier
        {
            get { return attackMovementMultiplier; }
        }

        public float GroundCheckRadius
        {
            get { return groundCheckRadius; }
        }

        public static LocalCharacterActionConfig CreateRuntimeDefaults()
        {
            var config = CreateInstance<LocalCharacterActionConfig>();
            config.name = "RuntimeDefaultLocalCharacterActionConfig";
            return config;
        }
    }
}
