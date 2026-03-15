using UnityEngine;

namespace PhamNhanOnline.Client.Features.Character.Presentation
{
    public sealed class PlayerView : MonoBehaviour
    {
        [SerializeField] private Rigidbody2D body;
        [SerializeField] private Collider2D bodyCollider;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Transform groundCheck;
        [SerializeField] private Animator animator;

        public Rigidbody2D Body
        {
            get { return body; }
        }

        public Collider2D BodyCollider
        {
            get { return bodyCollider; }
        }

        public Transform VisualRoot
        {
            get { return visualRoot; }
        }

        public Transform GroundCheck
        {
            get { return groundCheck; }
        }

        public Animator Animator
        {
            get { return animator; }
        }
    }
}
