using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using VRMiniRange.Feedback;

namespace VRMiniRange.Interaction
{
    public class SocketPlacement : MonoBehaviour
    {
        [Header("Socket")]
        [SerializeField] private XRSocketInteractor socket;

        [Header("Visual Feedback")]
        [SerializeField] private Renderer highlightRenderer;
        [SerializeField] private Color normalColor = new Color(1f, 1f, 0f, 0.5f); // Yellow
        [SerializeField] private Color hoverColor = new Color(0f, 1f, 0f, 0.5f);  // Green
        [SerializeField] private Color successColor = new Color(0f, 1f, 1f, 0.8f); // Cyan

        [Header("Success Feedback")]
        [SerializeField] private GameObject successTextObject; // Optional world-space text
        [SerializeField] private AudioSource successSound;

        // State
        private bool isPlaced;

        // Properties
        public bool IsPlaced => isPlaced;

        // Events
        public event Action OnObjectPlaced;

        private void Awake()
        {
            // Auto-find socket if not assigned
            if (socket == null)
            {
                socket = GetComponent<XRSocketInteractor>();
            }

            // Auto-find highlight renderer if not assigned
            if (highlightRenderer == null)
            {
                highlightRenderer = GetComponentInChildren<Renderer>();
            }

            // Hide success text initially
            if (successTextObject != null)
            {
                successTextObject.SetActive(false);
            }
        }

        private void Start()
        {
            // Set initial color
            SetHighlightColor(normalColor);

            // Subscribe to socket events
            if (socket != null)
            {
                socket.hoverEntered.AddListener(OnHoverEnter);
                socket.hoverExited.AddListener(OnHoverExit);
                socket.selectEntered.AddListener(OnPlaced);
                socket.selectExited.AddListener(OnRemoved);
            }
            else
            {
                Debug.LogError("[SocketPlacement] No XRSocketInteractor found!");
            }
        }

        private void OnDestroy()
        {
            if (socket != null)
            {
                socket.hoverEntered.RemoveListener(OnHoverEnter);
                socket.hoverExited.RemoveListener(OnHoverExit);
                socket.selectEntered.RemoveListener(OnPlaced);
                socket.selectExited.RemoveListener(OnRemoved);
            }
        }

        private void OnHoverEnter(HoverEnterEventArgs args)
        {
            if (!isPlaced)
            {
                SetHighlightColor(hoverColor);
                HapticFeedback.LightPulse(args.interactorObject);
                Debug.Log("[SocketPlacement] Object hovering over socket");
            }
        }

        private void OnHoverExit(HoverExitEventArgs args)
        {
            if (!isPlaced)
            {
                SetHighlightColor(normalColor);
            }
        }

        private void OnPlaced(SelectEnterEventArgs args)
        {
            isPlaced = true;
            SetHighlightColor(successColor);

            // Haptic feedback
            HapticFeedback.StrongPulse(args.interactorObject);

            // Show success text
            if (successTextObject != null)
            {
                successTextObject.SetActive(true);
            }

            // Play sound
            if (successSound != null)
            {
                successSound.Play();
            }

            Debug.Log("[SocketPlacement] Object placed successfully!");

            // Notify listeners
            OnObjectPlaced?.Invoke();
        }

        private void OnRemoved(SelectExitEventArgs args)
        {
            // Optional: allow removal or keep it locked
            // For this assessment, once placed stays placed
            // If you want to allow removal, uncomment below:
            
            // isPlaced = false;
            // SetHighlightColor(normalColor);
            // if (successTextObject != null) successTextObject.SetActive(false);
        }

        private void SetHighlightColor(Color color)
        {
            if (highlightRenderer != null)
            {
                // Use material property block to avoid material instance creation
                MaterialPropertyBlock props = new MaterialPropertyBlock();
                highlightRenderer.GetPropertyBlock(props);
                props.SetColor("_Color", color);
                props.SetColor("_BaseColor", color); // For URP
                highlightRenderer.SetPropertyBlock(props);
            }
        }

        public void ResetSocket()
        {
            isPlaced = false;
            SetHighlightColor(normalColor);
            
            if (successTextObject != null)
            {
                successTextObject.SetActive(false);
            }

            // Force release any socketed object
            if (socket != null && socket.hasSelection)
            {
                // This requires the interactable to be removed manually
                // For a full reset, you'd need to handle this in GameManager
            }
        }
    }
}
