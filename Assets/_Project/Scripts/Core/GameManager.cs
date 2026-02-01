using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using VRMiniRange.Shooting;
using VRMiniRange.Interaction;

namespace VRMiniRange.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Game Settings")]
        [SerializeField] private int totalTargets = 10;

        [Header("Gun Settings")]
        [SerializeField] private Gun gun;
        [SerializeField] private Transform gunSpawnPoint;
        [SerializeField] private Transform gunRestPoint;

        [Header("Pickup & Place")]
        [SerializeField] private PlaceableObject placeableObject;
        [SerializeField] private SocketPlacement socketPlacement;
        [SerializeField] private Transform placeableObjectStartPoint;

        // State
        public GameState CurrentState { get; private set; } = GameState.Menu;
        public int TargetsHit { get; private set; }
        public int TotalTargets => totalTargets;
        public float SessionTime { get; private set; }

        // Phase tracking
        private bool gunUnlocked;
        private bool timerStarted;
        private bool isResetting; // Guard flag to prevent events during reset

        // Cached start positions
        private Vector3 placeableObjectStartPosition;
        private Quaternion placeableObjectStartRotation;
        private Vector3 gunStartPosition;
        private Quaternion gunStartRotation;

        // Events
        public event Action<GameState> OnStateChanged;
        public event Action<int, int> OnScoreChanged;
        public event Action OnGunUnlocked;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            // Find references if not assigned
            if (gun == null)
                gun = FindObjectOfType<Gun>();

            if (placeableObject == null)
                placeableObject = FindObjectOfType<PlaceableObject>();

            if (socketPlacement == null)
                socketPlacement = FindObjectOfType<SocketPlacement>();

            // Cache starting positions
            CacheStartPositions();

            // Subscribe to gun events
            if (gun != null)
            {
                gun.OnShoot += OnGunFired;
                gun.OnOutOfAmmo += OnOutOfAmmo;
                DisableGun();
            }

            // Disable placeable object interaction at start
            if (placeableObject != null)
            {
                placeableObject.SetInteractable(false);
            }

            // Start in Menu state
            ChangeState(GameState.Menu);
        }

        private void CacheStartPositions()
        {
            if (placeableObject != null)
            {
                if (placeableObjectStartPoint != null)
                {
                    placeableObjectStartPosition = placeableObjectStartPoint.position;
                    placeableObjectStartRotation = placeableObjectStartPoint.rotation;
                }
                else
                {
                    placeableObjectStartPosition = placeableObject.transform.position;
                    placeableObjectStartRotation = placeableObject.transform.rotation;
                }
            }

            if (gun != null)
            {
                gunStartPosition = gun.transform.position;
                gunStartRotation = gun.transform.rotation;
            }
        }

        private void OnDestroy()
        {
            if (gun != null)
            {
                gun.OnShoot -= OnGunFired;
                gun.OnOutOfAmmo -= OnOutOfAmmo;
            }
        }

        private void Update()
        {
            if (timerStarted && CurrentState == GameState.Playing)
            {
                SessionTime += Time.deltaTime;
            }
        }

        public void StartGame()
        {
            // Set reset flag
            isResetting = true;

            TargetsHit = 0;
            SessionTime = 0f;
            timerStarted = false;
            gunUnlocked = false;

            // Disable gun first
            DisableGun();

            // Reset socket (disables it temporarily)
            ResetSocket();

            // Reset placeable object
            ResetPlaceableObject();

            // Clear reset flag
            isResetting = false;

            OnScoreChanged?.Invoke(TargetsHit, totalTargets);
            ChangeState(GameState.Playing);

            Debug.Log("[GameManager] Game Started - Place object in socket to unlock gun");
        }

        private void ResetPlaceableObject()
        {
            if (placeableObject == null) return;

            // Force release if held
            placeableObject.ForceRelease();

            // Reset position and state
            placeableObject.ResetObject(placeableObjectStartPosition, placeableObjectStartRotation);

            // Enable interaction
            placeableObject.SetInteractable(true);

            Debug.Log("[GameManager] PlaceableObject reset to start position");
        }

        private void ResetSocket()
        {
            if (socketPlacement == null) return;

            // Force release any object in socket
            var socket = socketPlacement.GetComponent<XRSocketInteractor>();
            if (socket != null)
            {
                // Temporarily disable socket to prevent re-triggering
                socket.enabled = false;

                if (socket.hasSelection)
                {
                    var selectedInteractable = socket.firstInteractableSelected;
                    if (selectedInteractable != null)
                    {
                        socket.interactionManager.SelectExit(socket, selectedInteractable);
                        Debug.Log("[GameManager] Forced release from socket");
                    }
                }

                // Re-enable socket after a frame
                StartCoroutine(ReenableSocketDelayed(socket));
            }

            socketPlacement.ResetSocket();

            Debug.Log("[GameManager] Socket reset");
        }

        private System.Collections.IEnumerator ReenableSocketDelayed(XRSocketInteractor socket)
        {
            yield return null; // Wait one frame
            yield return null; // Wait another frame for safety
            if (socket != null)
            {
                socket.enabled = true;
                Debug.Log("[GameManager] Socket re-enabled");
            }
        }

        public void UnlockGun()
        {
            // Guard against calls during reset
            if (isResetting)
            {
                Debug.Log("[GameManager] UnlockGun ignored - currently resetting");
                return;
            }

            if (gunUnlocked)
            {
                Debug.Log("[GameManager] Gun already unlocked");
                return;
            }

            if (CurrentState != GameState.Playing)
            {
                Debug.Log("[GameManager] UnlockGun ignored - not in Playing state");
                return;
            }

            gunUnlocked = true;

            Debug.Log("[GameManager] Gun Unlocked!");

            SpawnGunFromSky();

            OnGunUnlocked?.Invoke();
        }

        private void SpawnGunFromSky()
        {
            if (gun == null) return;

            // Reset gun ammo
            gun.ResetGun();

            // Position gun at spawn point (high up)
            if (gunSpawnPoint != null)
            {
                gun.transform.position = gunSpawnPoint.position;
                gun.transform.rotation = gunSpawnPoint.rotation;
            }
            else
            {
                Vector3 spawnPos = gunStartPosition + Vector3.up * 3f;
                gun.transform.position = spawnPos;
                gun.transform.rotation = gunStartRotation;
            }

            // Enable gun
            gun.gameObject.SetActive(true);
            EnableGunInteraction();

            // Let it fall
            Rigidbody rb = gun.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            Debug.Log("[GameManager] Gun spawned from sky!");
        }

        private void DisableGun()
        {
            if (gun == null) return;

            // Force drop if held
            var grabInteractable = gun.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            if (grabInteractable != null && grabInteractable.isSelected)
            {
                var interactor = grabInteractable.firstInteractorSelecting;
                if (interactor != null)
                {
                    grabInteractable.interactionManager.SelectExit(interactor, grabInteractable);
                }
            }

            gun.gameObject.SetActive(false);
        }

        private void EnableGunInteraction()
        {
            if (gun == null) return;

            var grabInteractable = gun.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            if (grabInteractable != null)
                grabInteractable.enabled = true;
        }

        private void OnGunFired()
        {
            if (!timerStarted && CurrentState == GameState.Playing)
            {
                timerStarted = true;
                Debug.Log("[GameManager] Timer started on first shot!");
            }
        }

        private void OnOutOfAmmo()
        {
            if (CurrentState != GameState.Playing) return;

            Debug.Log("[GameManager] Out of ammo - ending game!");
            EndGame();
        }

        public void RegisterTargetHit()
        {
            if (CurrentState != GameState.Playing) return;

            TargetsHit++;
            OnScoreChanged?.Invoke(TargetsHit, totalTargets);

            Debug.Log($"[GameManager] Target hit: {TargetsHit}/{totalTargets}");

            if (TargetsHit >= totalTargets)
            {
                EndGame();
            }
        }

        public void EndGame()
        {
            ChangeState(GameState.Complete);

            Debug.Log($"[GameManager] Game Complete - Time: {GetFormattedTime()}");
        }

        public void RestartGame()
        {
            Debug.Log("[GameManager] Restarting Game");

            // Disable gun
            DisableGun();

            // Go back to menu
            ChangeState(GameState.Menu);
        }

        public string GetFormattedTime()
        {
            int minutes = Mathf.FloorToInt(SessionTime / 60f);
            int seconds = Mathf.FloorToInt(SessionTime % 60f);
            return $"{minutes:00}:{seconds:00}";
        }

        private void ChangeState(GameState newState)
        {
            CurrentState = newState;
            OnStateChanged?.Invoke(newState);

            Debug.Log($"[GameManager] State changed to: {newState}");
        }
    }
}
