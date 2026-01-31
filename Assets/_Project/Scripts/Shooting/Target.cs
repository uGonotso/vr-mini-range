using System;
using System.Collections;
using UnityEngine;

using VRMiniRange.Core;
using VRMiniRange.Feedback;

namespace VRMiniRange.Shooting
{
    public class Target : MonoBehaviour
    {
        [Header("Visual Feedback")]
        [SerializeField] private Renderer targetRenderer;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color hitColor = Color.red;
        [SerializeField] private float hitAnimationDuration = 0.3f;

        [Header("Hit Response")]
        [SerializeField] private HitResponseType hitResponse = HitResponseType.ScaleDown;
        [SerializeField] private AudioSource hitSound;

        // State
        private Vector3 originalScale;
        private bool isHit;

        // Events
        public event Action<Target> OnHit;

        public enum HitResponseType
        {
            ScaleDown,
            FallOver,
            Disappear
        }

        private void Awake()
        {
            originalScale = transform.localScale;

            // Auto-find renderer if not set
            if (targetRenderer == null)
            {
                targetRenderer = GetComponentInChildren<Renderer>();
            }

            if (targetRenderer != null)
            {
                // Create material instance to avoid shared material issues
                targetRenderer.material = new Material(targetRenderer.material);
                targetRenderer.material.color = normalColor;
            }
        }

        public void ResetTarget()
        {
            isHit = false;
            transform.localScale = originalScale;
            transform.localRotation = Quaternion.identity;
            
            if (targetRenderer != null)
            {
                targetRenderer.material.color = normalColor;
            }
            
            gameObject.SetActive(true);
        }

        public void TakeHit(Vector3 hitPoint, UnityEngine.XR.Interaction.Toolkit.Interactors.IXRInteractor interactor = null)
        {
            if (isHit) return; // Prevent double hits
            
            isHit = true;

            Debug.Log($"[Target] {gameObject.name} hit at {hitPoint}");

            // Audio
            if (hitSound != null)
                hitSound.Play();

            // Notify GameManager
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RegisterTargetHit();
            }

            // Notify pool
            OnHit?.Invoke(this);

            // Visual response
            StartCoroutine(HitAnimation());
        }

        private IEnumerator HitAnimation()
        {
            // Change color immediately
            if (targetRenderer != null)
            {
                targetRenderer.material.color = hitColor;
            }

            float elapsed = 0f;

            switch (hitResponse)
            {
                case HitResponseType.ScaleDown:
                    Vector3 startScale = originalScale;
                    Vector3 endScale = originalScale * 0.1f;
                    
                    // Quick pop up then scale down
                    Vector3 popScale = originalScale * 1.2f;
                    
                    // Pop phase (first 20% of animation)
                    while (elapsed < hitAnimationDuration * 0.2f)
                    {
                        elapsed += Time.deltaTime;
                        float t = elapsed / (hitAnimationDuration * 0.2f);
                        transform.localScale = Vector3.Lerp(startScale, popScale, t);
                        yield return null;
                    }
                    
                    // Shrink phase (remaining 80%)
                    elapsed = 0f;
                    while (elapsed < hitAnimationDuration * 0.8f)
                    {
                        elapsed += Time.deltaTime;
                        float t = elapsed / (hitAnimationDuration * 0.8f);
                        transform.localScale = Vector3.Lerp(popScale, endScale, t);
                        yield return null;
                    }
                    break;

                case HitResponseType.FallOver:
                    Quaternion startRot = transform.localRotation;
                    Quaternion endRot = Quaternion.Euler(-90f, 0f, 0f) * startRot;
                    
                    while (elapsed < hitAnimationDuration)
                    {
                        elapsed += Time.deltaTime;
                        float t = elapsed / hitAnimationDuration;
                        // Ease out for satisfying fall
                        t = 1f - Mathf.Pow(1f - t, 2f);
                        transform.localRotation = Quaternion.Lerp(startRot, endRot, t);
                        yield return null;
                    }
                    break;

                case HitResponseType.Disappear:
                    // Just wait a tiny bit so player sees the color change
                    yield return new WaitForSeconds(0.05f);
                    break;
            }

            // Deactivate after animation
            gameObject.SetActive(false);
        }
    }
}
