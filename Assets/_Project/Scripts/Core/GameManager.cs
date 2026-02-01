using System;
using UnityEngine;

namespace VRMiniRange.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Game Settings")]
        [SerializeField] private int totalTargets = 10;

        // State
        public GameState CurrentState { get; private set; } = GameState.Menu;
        public int TargetsHit { get; private set; }
        public int TotalTargets => totalTargets;
        public float SessionTime { get; private set; }

        // Events
        public event Action<GameState> OnStateChanged;
        public event Action<int, int> OnScoreChanged; // (current, total)

        private bool isTimerRunning;

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
            // TEMP: Auto-start for testing
            ChangeState(GameState.Playing);
        }

        private void Update()
        {
            if (isTimerRunning)
            {
                SessionTime += Time.deltaTime;
            }
        }

        public void StartGame()
        {
            TargetsHit = 0;
            SessionTime = 0f;
            isTimerRunning = true;
            
            OnScoreChanged?.Invoke(TargetsHit, totalTargets);
            ChangeState(GameState.Playing);
            
            Debug.Log("[GameManager] Game Started");
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
            isTimerRunning = false;
            ChangeState(GameState.Complete);
            
            Debug.Log($"[GameManager] Game Complete - Time: {GetFormattedTime()}");
        }

        public void RestartGame()
        {
            Debug.Log("[GameManager] Restarting Game");
            
            // TargetPool will listen to state change and reset
            StartGame();
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
