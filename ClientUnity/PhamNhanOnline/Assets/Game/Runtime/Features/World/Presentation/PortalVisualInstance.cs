using TMPro;
using UnityEngine;

namespace PhamNhanOnline.Client.Features.World.Presentation
{
    [DisallowMultipleComponent]
    public sealed class PortalVisualInstance : MonoBehaviour
    {
        [SerializeField] private TMP_Text labelText;
        [SerializeField] private Collider2D interactionCollider;
        [SerializeField] private Collider2D touchTriggerLeftCollider;
        [SerializeField] private Collider2D touchTriggerRightCollider;
        [SerializeField] private float visualEdgeOffsetXServerUnits;
        [SerializeField] private float visualOffsetYServerUnits;
        [SerializeField] private GameObject selectedHighlightRoot;

        public TMP_Text LabelText
        {
            get { return labelText; }
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

        public float VisualOffsetYServerUnits
        {
            get { return visualOffsetYServerUnits; }
        }

        public float VisualEdgeOffsetXServerUnits
        {
            get { return visualEdgeOffsetXServerUnits; }
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

        public void SetSelected(bool selected)
        {
            if (selectedHighlightRoot != null && selectedHighlightRoot.activeSelf != selected)
                selectedHighlightRoot.SetActive(selected);
        }

        private void AutoResolveReferences()
        {
            if (labelText == null)
                labelText = GetComponentInChildren<TMP_Text>(true);

            if (interactionCollider == null)
            {
                if (labelText != null)
                    interactionCollider = labelText.GetComponent<Collider2D>();

                if (interactionCollider == null)
                    interactionCollider = GetComponentInChildren<Collider2D>(true);
            }
        }
    }
}
