using TMPro;
using UnityEngine;

namespace PhamNhanOnline.Client.Features.World.Presentation
{
    [DisallowMultipleComponent]
    public sealed class PortalVisualInstance : MonoBehaviour
    {
        [SerializeField] private TMP_Text labelText;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Collider2D interactionCollider;
        [SerializeField] private Collider2D touchTriggerLeftCollider;
        [SerializeField] private Collider2D touchTriggerRightCollider;
        [SerializeField] private float edgeVisualOffsetXWorldUnits;
        [SerializeField] private GameObject selectedHighlightRoot;

        private bool visualRootDefaultCaptured;
        private Vector3 visualRootDefaultLocalPosition;

        public TMP_Text LabelText
        {
            get { return labelText; }
        }

        public Transform VisualRoot
        {
            get { return visualRoot; }
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
            AutoResolveReferences();
        }

        private void OnValidate()
        {
            AutoResolveReferences();
        }

        public void Apply(string label)
        {
            AutoResolveReferences();
            if (labelText != null)
                labelText.text = label ?? string.Empty;
        }

        public void ApplyEdgeVisualOffset(float signedOffsetXWorldUnits)
        {
            AutoResolveReferences();
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

        private void AutoResolveReferences()
        {
            if (labelText == null)
                labelText = GetComponentInChildren<TMP_Text>(true);

            if (visualRoot == null)
            {
                var rootTransform = transform.Find("Root");
                if (rootTransform != null)
                    visualRoot = rootTransform;
            }

            if (interactionCollider == null)
            {
                if (labelText != null)
                    interactionCollider = labelText.GetComponent<Collider2D>();

                if (interactionCollider == null)
                    interactionCollider = GetComponentInChildren<Collider2D>(true);
            }

            CaptureDefaultVisualRootLocalPosition();
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
