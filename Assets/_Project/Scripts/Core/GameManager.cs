using System;
using UnityEngine;
using VRMiniRange.Shooting;

namespace VRMiniRange.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Game Settings")]
        [SerializeField] private int totalTargets = 10;

        [Header("Gun Settings")]
        [SerializeField] private Gun gun;
        [SerializeField] private Transform gunSpawnPoint; // High position to fall from
        [SerializeField] private Transform gunRestPoint;  // Where gun lands (table)

        // State
        public GameState CurrentState { get; private set; } = GameState.Menu;
        public int TargetsHit { get; private set; }
        public int TotalTargets => totalTargets;
        public float SessionTime { get; private set; }

        // Phase tracking
        private bool gunUnlocked;
        private bool timerStarted;

        // Events
        public event Action<GameState> OnStateChanged;
        public event Action<int, int> OnScoreChanged; // (current, total)
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
            // Find gun if not assigned
            if (gun == null)
            {
                gun = FindObjectOfType<Gun>();
            }

            // Subscribe to gun events
            if (gun != null)
            {
                gun.OnShoot += OnGunFired;
                
                // Disable gun at start
                DisableGun();
            }

            // Start in Menu state
            ChangeState(GameState.Menu);
        }

        private void OnDestroy()
        {
            if (gun != null)
            {
                gun.OnShoot -= OnGunFired;
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
            TargetsHit = 0;
            SessionTime = 0f;
            timerStarted = false;
            gunUnlocked = false;
            
            // Disable gun until socket is placed
            DisableGun();
            
            OnScoreChanged?.Invoke(TargetsHit, totalTargets);
            ChangeState(GameState.Playing);
            
            Debug.Log("[GameManager] Game Started - Place object in socket to unlock gun");
        }

        public void UnlockGun()
        {
            if (gunUnlocked) return;
            
            gunUnlocked = true;
            
            Debug.Log("[GameManager] Gun Unlocked!");
            
            // Spawn gun from sky
            SpawnGunFromSky();
            
            OnGunUnlocked?.Invoke();
        }

        private void SpawnGunFromSky()
        {
            if (gun == null) return;

            // Position gun at spawn point (high up)
            if (gunSpawnPoint != null)
            {
                gun.transform.position = gunSpawnPoint.position;
                gun.transform.rotation = gunSpawnPoint.rotation;
            }
            else
            {
                // Default: 3 meters above rest point or current position
                Vector3 spawnPos = gun.transform.position + Vector3.up * 3f;
                gun.transform.position = spawnPos;
            }

            // Enable gun GameObject and interactable
            gun.gameObject.SetActive(true);
            EnableGunInteraction();

            // Rigidbody will handle the falling
            Rigidbody rb = gun.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.velocity = Vector3.zero;
            }

            Debug.Log("[GameManager] Gun spawned from sky!");
        }

        private void DisableGun()
        {
            if (gun == null) return;
            
            // Option 1: Hide completely
            gun.gameObject.SetActive(false);
            
            // Option 2: Just disable interaction (if you want it visible but not grabbable)
            // var grabInteractable = gun.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            // if (grabInteractable != null)
            //     grabInteractable.enabled = false;
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
            // Start timer on first shot
            if (!timerStarted && CurrentState == GameState.Playing)
            {
                timerStarted = true;
                Debug.Log("[GameManager] Timer started on first shot!");
            }
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
            
            // Reset gun position (hide it again)
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
