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
        [SerializeField] private Color grabbedColor = new Color(0.8f, 0.8f, 1f); // Light blue tint

        private XRGrabInteractable grabInteractable;
        private bool isPlaced;

        public bool IsPlaced => isPlaced;

        private void Awake()
        {
            grabInteractable = GetComponent<XRGrabInteractable>();

            if (objectRenderer == null)
            {
                objectRenderer = GetComponentInChildren<Renderer>();
            }

            // Create material instance
            if (objectRenderer != null)
            {
                objectRenderer.material = new Material(objectRenderer.material);
                objectRenderer.material.color = normalColor;
            }
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
            // Check if grabbed by socket
            if (args.interactorObject is XRSocketInteractor)
            {
                // Placed in socket
                isPlaced = true;
                Debug.Log("[PlaceableObject] Placed in socket");
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
            // Only reset color if released from hand (not socket)
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

        public void ResetObject(Vector3 position, Quaternion rotation)
        {
            isPlaced = false;
            transform.position = position;
            transform.rotation = rotation;
            SetColor(normalColor);

            // Reset rigidbody
            var rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
    }
}
