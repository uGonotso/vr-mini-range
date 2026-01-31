using System;
using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using VRMiniRange.Core;
using VRMiniRange.Feedback;

namespace VRMiniRange.Shooting
{
    [RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable))]
    public class Gun : MonoBehaviour
    {
        [Header("Shooting")]
        [SerializeField] private Transform muzzle;
        [SerializeField] private float range = 50f;
        [SerializeField] private LayerMask targetLayer = -1; // Default to all layers

        [Header("Ammo")]
        [SerializeField] private int maxAmmo = 10;
        [SerializeField] private float reloadTime = 1.5f;
        [SerializeField] private bool autoReloadWhenEmpty = true;

        [Header("Visual Feedback")]
        [SerializeField] private ParticleSystem muzzleFlash;
        [SerializeField] private LineRenderer laserPointer; // Optional aim assist
        [SerializeField] private bool showDebugRay = true;

        [Header("Audio")]
        [SerializeField] private AudioSource shootSound;
        [SerializeField] private AudioSource emptyClickSound;
        [SerializeField] private AudioSource reloadSound;

        // Components
        private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable;
        private UnityEngine.XR.Interaction.Toolkit.Interactors.IXRInteractor currentInteractor;

        // State
        private int currentAmmo;
        private bool isReloading;
        private bool isGrabbed;

        // Properties
        public int CurrentAmmo => currentAmmo;
        public int MaxAmmo => maxAmmo;
        public bool IsReloading => isReloading;
        public bool IsGrabbed => isGrabbed;

        // Events
        public event Action OnAmmoChanged;
        public event Action<float> OnReloadProgress; // 0-1 progress
        public event Action OnShoot;

        private void Awake()
        {
            grabInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            currentAmmo = maxAmmo;

            // Auto-find muzzle if not set
            if (muzzle == null)
            {
                muzzle = transform.Find("Muzzle");
                if (muzzle == null)
                {
                    Debug.LogWarning("[Gun] No muzzle transform found. Creating one at gun tip.");
                    GameObject muzzleObj = new GameObject("Muzzle");
                    muzzleObj.transform.SetParent(transform);
                    muzzleObj.transform.localPosition = new Vector3(0, 0, 0.5f); // Adjust based on gun model
                    muzzle = muzzleObj.transform;
                }
            }
        }

        private void OnEnable()
        {
            grabInteractable.activated.AddListener(OnTriggerPulled);
            grabInteractable.selectEntered.AddListener(OnGrabbed);
            grabInteractable.selectExited.AddListener(OnReleased);

            Debug.Log("[Gun] Gun script enabled and listeners registered");
        }

        private void OnDisable()
        {
            grabInteractable.activated.RemoveListener(OnTriggerPulled);
            grabInteractable.selectEntered.RemoveListener(OnGrabbed);
            grabInteractable.selectExited.RemoveListener(OnReleased);
        }

        private void Update()
        {
            // Debug visualization
            if (showDebugRay && isGrabbed)
            {
                Debug.DrawRay(muzzle.position, muzzle.forward * range, Color.red);
            }

            // Optional laser pointer
            if (laserPointer != null && isGrabbed)
            {
                laserPointer.SetPosition(0, muzzle.position);
                laserPointer.SetPosition(1, muzzle.position + muzzle.forward * range);
            }
        }

        private void OnGrabbed(SelectEnterEventArgs args)
        {
            isGrabbed = true;
            currentInteractor = args.interactorObject;
            
            if (laserPointer != null)
                laserPointer.enabled = true;

            HapticFeedback.LightPulse(currentInteractor);
            
            Debug.Log("[Gun] Grabbed");
        }

        private void OnReleased(SelectExitEventArgs args)
        {
            isGrabbed = false;
            currentInteractor = null;
            
            if (laserPointer != null)
                laserPointer.enabled = false;

            Debug.Log("[Gun] Released");
        }

        private void OnTriggerPulled(ActivateEventArgs args)
        {
            Debug.Log("[Gun] TRIGGER PULLED");
            // Only shoot during gameplay
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing)
            {
                Debug.Log("[Gun] Not in playing state, ignoring trigger");
                return;
            }

            if (isReloading)
            {
                Debug.Log("[Gun] Currently reloading");
                return;
            }

            if (currentAmmo <= 0)
            {
                EmptyClick(args.interactorObject);
                return;
            }

            Shoot(args.interactorObject);
        }

        private void Shoot(UnityEngine.XR.Interaction.Toolkit.Interactors.IXRInteractor interactor)
        {
            currentAmmo--;
            OnAmmoChanged?.Invoke();
            OnShoot?.Invoke();

            // Haptic feedback
            HapticFeedback.FirePulse(interactor);

            // VFX
            if (muzzleFlash != null)
                muzzleFlash.Play();

            // Audio
            if (shootSound != null)
                shootSound.Play();

            // Raycast
            if (Physics.Raycast(muzzle.position, muzzle.forward, out RaycastHit hit, range, targetLayer))
            {
                Debug.Log($"[Gun] Hit: {hit.collider.name}");

                // Check for target
                if (hit.collider.TryGetComponent<Target>(out Target target))
                {
                    target.TakeHit(hit.point, interactor);
                    
                    // Stronger haptic on confirmed hit
                    HapticFeedback.HitConfirmPulse(interactor);
                }
            }

            Debug.Log($"[Gun] Fired. Ammo: {currentAmmo}/{maxAmmo}");

            // Auto reload when empty
            if (currentAmmo <= 0 && autoReloadWhenEmpty)
            {
                StartCoroutine(ReloadCoroutine());
            }
        }

        private void EmptyClick(UnityEngine.XR.Interaction.Toolkit.Interactors.IXRInteractor interactor)
        {
            // Light haptic for empty click
            HapticFeedback.Pulse(interactor, 0.1f, 0.05f);

            if (emptyClickSound != null)
                emptyClickSound.Play();

            Debug.Log("[Gun] Empty - click");

            // Auto reload on empty click
            if (autoReloadWhenEmpty)
            {
                StartCoroutine(ReloadCoroutine());
            }
        }

        public void StartReload()
        {
            if (!isReloading && currentAmmo < maxAmmo)
            {
                StartCoroutine(ReloadCoroutine());
            }
        }

        private IEnumerator ReloadCoroutine()
        {
            if (isReloading) yield break;

            isReloading = true;
            Debug.Log("[Gun] Reloading...");

            if (reloadSound != null)
                reloadSound.Play();

            float elapsed = 0f;
            while (elapsed < reloadTime)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / reloadTime;
                OnReloadProgress?.Invoke(progress);
                yield return null;
            }

            currentAmmo = maxAmmo;
            isReloading = false;
            
            OnAmmoChanged?.Invoke();
            OnReloadProgress?.Invoke(0f);

            // Haptic to confirm reload complete
            if (currentInteractor != null)
            {
                HapticFeedback.MediumPulse(currentInteractor);
            }

            Debug.Log("[Gun] Reload complete");
        }

        // Manual reload via button (can be called from input action)
        public void OnReloadButtonPressed()
        {
            StartReload();
        }
    }
}
