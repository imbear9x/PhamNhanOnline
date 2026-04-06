using UnityEngine;

namespace PhamNhanOnline.Client.Features.World.Presentation
{
    [DisallowMultipleComponent]
    public sealed class GroundSnapBindings : MonoBehaviour
    {
        [SerializeField] private Transform groundContactAnchor;

        public Transform GroundContactAnchor
        {
            get { return groundContactAnchor != null ? groundContactAnchor : transform.Find("GroundContactAnchor"); }
        }
    }
}
