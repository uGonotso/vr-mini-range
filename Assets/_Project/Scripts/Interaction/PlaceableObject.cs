using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using VRMiniRange.Feedback;

namespace VRMiniRange.Interaction
{
    [RequireComponent(typeof(XRGrabInteractable))]
    public class PlaceableObject : MonoBehaviour
    {
        [Header("Visual Feedback")]
        [SerializeField] private Renderer objectRenderer;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color grabbedColor = new Color(0.8f, 0.8f, 1f);
        [SerializeField] private Color placedColor = new Color(0.5f, 1f, 0.5f); // Green tint when placed

        private XRGrabInteractable grabInteractable;
        private Rigidbody rb;
        private bool isPlaced;

        public bool IsPlaced => isPlaced;

        private void Awake()
        {
            grabInteractable = GetComponent<XRGrabInteractable>();
            rb = GetComponent<Rigidbody>();

            if (objectRenderer == null)
            {
                objectRenderer = GetComponentInChildren<Renderer>();
            }

            if (objectRenderer != null)
            {
                objectRenderer.material = new Material(objectRenderer.material);
                objectRenderer.material.color = normalColor;
            }

            // Start disabled until game starts
            SetInteractable(false);
        }

        private void OnEnable()
        {
            grabInteractable.selectEntered.AddListener(OnGrabbed);
            grabInteractable.selectExited.AddListener(OnReleased);
        }

        private void OnDisable()
        {
            grabInteractable.selectEntered.RemoveListener(OnGrabbed);
            grabInteractable.selectExited.RemoveListener(OnReleased);
        }

        private void OnGrabbed(SelectEnterEventArgs args)
        {
            if (args.interactorObject is XRSocketInteractor)
            {
                // Placed in socket
                isPlaced = true;
                SetColor(placedColor);
                
                // Disable further interaction once placed
                SetInteractable(false);
                
                Debug.Log("[PlaceableObject] Placed in socket - interaction disabled");
            }
            else
            {
                // Grabbed by hand
                isPlaced = false;
                SetColor(grabbedColor);
                HapticFeedback.LightPulse(args.interactorObject);
                Debug.Log("[PlaceableObject] Grabbed by hand");
            }
        }

        private void OnReleased(SelectExitEventArgs args)
        {
            if (!(args.interactorObject is XRSocketInteractor))
            {
                SetColor(normalColor);
                Debug.Log("[PlaceableObject] Released from hand");
            }
        }

        private void SetColor(Color color)
        {
            if (objectRenderer != null)
            {
                objectRenderer.material.color = color;
            }
        }

        public void SetInteractable(bool interactable)
        {
            if (grabInteractable != null)
            {
                grabInteractable.enabled = interactable;
            }
            
            Debug.Log($"[PlaceableObject] Interactable set to: {interactable}");
        }

        public void ResetObject(Vector3 position, Quaternion rotation)
        {
            isPlaced = false;
            
            // Re-enable interaction
            SetInteractable(true);
            
            // Reset transform
            transform.position = position;
            transform.rotation = rotation;
            
            // Reset color
            SetColor(normalColor);

            // Reset rigidbody
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = false;
            }
            
            Debug.Log("[PlaceableObject] Reset complete");
        }

        public void ForceRelease()
        {
            // Force drop if currently held
            if (grabInteractable != null && grabInteractable.isSelected)
            {
                // Get the interactor and force exit
                var interactor = grabInteractable.firstInteractorSelecting;
                if (interactor != null)
                {
                    grabInteractable.interactionManager.SelectExit(interactor, grabInteractable);
                }
            }
        }
    }
}
