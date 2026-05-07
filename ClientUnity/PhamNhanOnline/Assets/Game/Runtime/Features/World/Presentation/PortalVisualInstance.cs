using TMPro;
using PhamNhanOnline.Client.Core.Logging;
using UnityEngine;

namespace PhamNhanOnline.Client.Features.World.Presentation
{
    [DisallowMultipleComponent]
    public sealed class PortalVisualInstance : MonoBehaviour
    {
        [SerializeField] private TMP_Text labelText;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private WorldTargetable worldTargetable;
        [SerializeField] private Collider2D interactionCollider;
        [SerializeField] private Collider2D touchTriggerLeftCollider;
        [SerializeField] private Collider2D touchTriggerRightCollider;
        [SerializeField] private float edgeVisualOffsetXWorldUnits;
        [SerializeField] private GameObject selectedHighlightRoot;

        private bool visualRootDefaultCaptured;
        private Vector3 visualRootDefaultLocalPosition;
        private bool loggedMissingRequiredReferences;

        public TMP_Text LabelText
        {
            get { return labelText; }
        }

        public Transform VisualRoot
        {
            get { return visualRoot; }
        }

        public WorldTargetable WorldTargetable
        {
            get { return worldTargetable; }
        }

        public Collider2D InteractionCollider
        {
            get { return interactionCollider; }
        }

        public Collider2D TouchTriggerLeftCollider
        {
            get { return touchTriggerLeftCollider; }
        }

        public Collider2D TouchTriggerRightCollider
        {
            get { return touchTriggerRightCollider; }
        }

        public GameObject SelectedHighlightRoot
        {
            get { return selectedHighlightRoot; }
        }

        public GameObject LabelObject
        {
            get
            {
                if (interactionCollider != null)
                    return interactionCollider.gameObject;

                return labelText != null ? labelText.gameObject : null;
            }
        }

        private void Awake()
        {
            ValidateReferences();
            CaptureDefaultVisualRootLocalPosition();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ValidateReferences();
            CaptureDefaultVisualRootLocalPosition();
        }
#endif

        public void Apply(string label)
        {
            ValidateReferences();
            if (labelText != null)
                labelText.text = label ?? string.Empty;
        }

        public void ApplyEdgeVisualOffset(float signedOffsetXWorldUnits)
        {
            ValidateReferences();
            if (visualRoot == null)
                return;

            CaptureDefaultVisualRootLocalPosition();
            visualRoot.localPosition = visualRootDefaultLocalPosition + new Vector3(signedOffsetXWorldUnits, 0f, 0f);
        }

        public float ResolveSignedEdgeVisualOffsetX(bool isLeftEdge, bool isRightEdge)
        {
            var magnitude = Mathf.Max(0f, edgeVisualOffsetXWorldUnits);
            if (magnitude <= Mathf.Epsilon)
                return 0f;

            if (isLeftEdge)
                return magnitude;

            if (isRightEdge)
                return -magnitude;

            return 0f;
        }

        public void SetSelected(bool selected)
        {
            if (selectedHighlightRoot != null && selectedHighlightRoot.activeSelf != selected)
                selectedHighlightRoot.SetActive(selected);
        }

        private void ValidateReferences()
        {
            if (labelText != null && visualRoot != null && worldTargetable != null && interactionCollider != null)
                return;

            if (loggedMissingRequiredReferences)
                return;

            ClientLog.Error(
                $"PortalVisualInstance on '{name}' is missing serialized references. " +
                $"labelText={(labelText != null)}, visualRoot={(visualRoot != null)}, " +
                $"worldTargetable={(worldTargetable != null)}, interactionCollider={(interactionCollider != null)}. " +
                "Assign these on the portal prefab; client presentation must not auto-wire prefab-owned references.");
            loggedMissingRequiredReferences = true;
        }

        private void CaptureDefaultVisualRootLocalPosition()
        {
            if (visualRoot == null || visualRootDefaultCaptured)
                return;

            visualRootDefaultLocalPosition = visualRoot.localPosition;
            visualRootDefaultCaptured = true;
        }
    }
}
