using System;
using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using VRMiniRange.Core;
using VRMiniRange.Feedback;

namespace VRMiniRange.Shooting
{
    [RequireComponent(typeof(XRGrabInteractable))]
    public class Gun : MonoBehaviour
    {
        [Header("Shooting")]
        [SerializeField] private Transform muzzle;
        [SerializeField] private float range = 50f;
        [SerializeField] private LayerMask targetLayer = -1;

        [Header("Ammo")]
        [SerializeField] private int maxAmmo = 10;
        [SerializeField] private float reloadTime = 1.5f;
        [SerializeField] private bool autoReloadWhenEmpty = false;

        [Header("Visual Feedback")]
        [SerializeField] private ParticleSystem muzzleFlash;
        [SerializeField] private LineRenderer laserPointer;
        [SerializeField] private bool showDebugRay = true;
        [SerializeField] private float laserFlashDuration = 0.1f;

        [Header("Audio")]
        [SerializeField] private AudioSource shootSound;
        [SerializeField] private AudioSource emptyClickSound;
        [SerializeField] private AudioSource reloadSound;

        // Components
        private XRGrabInteractable grabInteractable;
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
        public event Action<float> OnReloadProgress;
        public event Action OnShoot;
        public event Action OnOutOfAmmo;

        private void Awake()
        {
            grabInteractable = GetComponent<XRGrabInteractable>();
            currentAmmo = maxAmmo;

            if (muzzle == null)
            {
                muzzle = transform.Find("Muzzle");
                if (muzzle == null)
                {
                    Debug.LogWarning("[Gun] No muzzle transform found. Creating one at gun tip.");
                    GameObject muzzleObj = new GameObject("Muzzle");
                    muzzleObj.transform.SetParent(transform);
                    muzzleObj.transform.localPosition = new Vector3(0, 0, 0.5f);
                    muzzle = muzzleObj.transform;
                }
            }

            // Ensure laser starts disabled
            if (laserPointer != null)
                laserPointer.enabled = false;
        }

        private void OnEnable()
        {
            grabInteractable.activated.AddListener(OnTriggerPulled);
            grabInteractable.selectEntered.AddListener(OnGrabbed);
            grabInteractable.selectExited.AddListener(OnReleased);
        }

        private void OnDisable()
        {
            grabInteractable.activated.RemoveListener(OnTriggerPulled);
            grabInteractable.selectEntered.RemoveListener(OnGrabbed);
            grabInteractable.selectExited.RemoveListener(OnReleased);
        }

        private void Update()
        {
            if (showDebugRay && isGrabbed)
            {
                Debug.DrawRay(muzzle.position, muzzle.forward * range, Color.red);
            }
        }

        private void OnGrabbed(SelectEnterEventArgs args)
        {
            isGrabbed = true;
            currentInteractor = args.interactorObject;

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

            HapticFeedback.FirePulse(interactor);

            if (muzzleFlash != null)
                muzzleFlash.Play();

            if (laserPointer != null)
                StartCoroutine(LaserFlash());

            if (shootSound != null)
                shootSound.Play();

            if (Physics.Raycast(muzzle.position, muzzle.forward, out RaycastHit hit, range, targetLayer))
            {
                Debug.Log($"[Gun] Hit: {hit.collider.name}");

                if (hit.collider.TryGetComponent<Target>(out Target target))
                {
                    target.TakeHit(hit.point, interactor);
                    HapticFeedback.HitConfirmPulse(interactor);
                }
            }

            Debug.Log($"[Gun] Fired. Ammo: {currentAmmo}/{maxAmmo}");

            // Check if out of ammo
            if (currentAmmo <= 0)
            {
                if (autoReloadWhenEmpty)
                {
                    StartCoroutine(ReloadCoroutine());
                }
                else
                {
                    // Notify that we're out of ammo
                    Debug.Log("[Gun] Out of ammo!");
                    OnOutOfAmmo?.Invoke();
                }
            }
        }

        private IEnumerator LaserFlash()
        {
            laserPointer.SetPosition(0, muzzle.position);
            laserPointer.SetPosition(1, muzzle.position + muzzle.forward * range);
            laserPointer.enabled = true;
            yield return new WaitForSeconds(laserFlashDuration);
            laserPointer.enabled = false;
        }

        private void EmptyClick(UnityEngine.XR.Interaction.Toolkit.Interactors.IXRInteractor interactor)
        {
            HapticFeedback.Pulse(interactor, 0.1f, 0.05f);

            if (emptyClickSound != null)
                emptyClickSound.Play();

            Debug.Log("[Gun] Empty - click");

            // Notify out of ammo
            OnOutOfAmmo?.Invoke();
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

            if (currentInteractor != null)
            {
                HapticFeedback.MediumPulse(currentInteractor);
            }

            Debug.Log("[Gun] Reload complete");
        }

        public void ResetGun()
        {
            currentAmmo = maxAmmo;
            isReloading = false;
            OnAmmoChanged?.Invoke();
            OnReloadProgress?.Invoke(0f);
        }

        public void OnReloadButtonPressed()
        {
            StartReload();
        }
    }
}
