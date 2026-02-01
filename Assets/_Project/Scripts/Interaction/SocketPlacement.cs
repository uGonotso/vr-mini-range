using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using VRMiniRange.Core;
using VRMiniRange.Feedback;

namespace VRMiniRange.Interaction
{
    public class SocketPlacement : MonoBehaviour
    {
        [Header("Socket")]
        [SerializeField] private XRSocketInteractor socket;

        [Header("Visual Feedback")]
        [SerializeField] private Renderer highlightRenderer;
        [SerializeField] private Color normalColor = new Color(1f, 1f, 0f, 0.5f);
        [SerializeField] private Color hoverColor = new Color(0f, 1f, 0f, 0.5f);
        [SerializeField] private Color successColor = new Color(0f, 1f, 1f, 0.8f);

        [Header("Success Feedback")]
        [SerializeField] private GameObject successTextObject;
        [SerializeField] private AudioSource successSound;

        [Header("Game Integration")]
        [SerializeField] private bool unlockGunOnPlace = true;

        // State
        private bool isPlaced;

        public bool IsPlaced => isPlaced;

        public event Action OnObjectPlaced;

        private void Awake()
        {
            if (socket == null)
                socket = GetComponent<XRSocketInteractor>();

            if (highlightRenderer == null)
                highlightRenderer = GetComponentInChildren<Renderer>();

            if (successTextObject != null)
                successTextObject.SetActive(false);
        }

        private void Start()
        {
            SetHighlightColor(normalColor);

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

            HapticFeedback.StrongPulse(args.interactorObject);

            if (successTextObject != null)
                successTextObject.SetActive(true);

            if (successSound != null)
                successSound.Play();

            Debug.Log("[SocketPlacement] Object placed successfully!");

            OnObjectPlaced?.Invoke();

            // Unlock gun - GameManager will guard against duplicate calls
            if (unlockGunOnPlace && GameManager.Instance != null)
            {
                GameManager.Instance.UnlockGun();
            }
        }

        private void OnRemoved(SelectExitEventArgs args)
        {
            // Only process if we were actually placed (not during reset)
            if (isPlaced)
            {
                isPlaced = false;
                SetHighlightColor(normalColor);

                if (successTextObject != null)
                    successTextObject.SetActive(false);
            }
        }

        private void SetHighlightColor(Color color)
        {
            if (highlightRenderer != null)
            {
                MaterialPropertyBlock props = new MaterialPropertyBlock();
                highlightRenderer.GetPropertyBlock(props);
                props.SetColor("_Color", color);
                props.SetColor("_BaseColor", color);
                highlightRenderer.SetPropertyBlock(props);
            }
        }

        /// <summary>
        /// Reset socket visuals only. GameManager handles the actual socket release.
        /// </summary>
        public void ResetSocket()
        {
            isPlaced = false;
            SetHighlightColor(normalColor);

            if (successTextObject != null)
                successTextObject.SetActive(false);

            Debug.Log("[SocketPlacement] Socket visuals reset");
        }
    }
}
