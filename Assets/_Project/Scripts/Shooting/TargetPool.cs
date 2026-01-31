using System;
using System.Collections.Generic;
using UnityEngine;
using VRMiniRange.Core;

namespace VRMiniRange.Shooting
{
    public class TargetPool : MonoBehaviour
    {
        public static TargetPool Instance { get; private set; }

        [Header("Pool Settings")]
        [SerializeField] private Target targetPrefab;
        [SerializeField] private int poolSize = 10;

        [Header("Spawn Points")]
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private bool autoFindSpawnPoints = true;

        // Pool
        private List<Target> pool = new List<Target>();
        private int activeCount;

        // Events
        public event Action OnAllTargetsHit;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Auto-find spawn points if not manually assigned
            if (autoFindSpawnPoints && (spawnPoints == null || spawnPoints.Length == 0))
            {
                FindSpawnPoints();
            }

            InitializePool();
        }

        private void Start()
        {
            // Subscribe to game state changes
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnStateChanged += OnGameStateChanged;
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnStateChanged -= OnGameStateChanged;
            }
        }

        private void FindSpawnPoints()
        {
            // Find all child transforms named "SpawnPoint" or tagged
            List<Transform> points = new List<Transform>();
            
            foreach (Transform child in GetComponentsInChildren<Transform>())
            {
                if (child != transform && child.name.Contains("SpawnPoint"))
                {
                    points.Add(child);
                }
            }

            if (points.Count > 0)
            {
                spawnPoints = points.ToArray();
                Debug.Log($"[TargetPool] Auto-found {spawnPoints.Length} spawn points");
            }
            else
            {
                Debug.LogWarning("[TargetPool] No spawn points found. Create child objects with 'SpawnPoint' in name.");
            }
        }

        private void InitializePool()
        {
            if (targetPrefab == null)
            {
                Debug.LogError("[TargetPool] Target prefab not assigned!");
                return;
            }

            for (int i = 0; i < poolSize; i++)
            {
                Target target = Instantiate(targetPrefab, transform);
                target.gameObject.SetActive(false);
                target.gameObject.name = $"Target_{i}";
                target.OnHit += HandleTargetHit;
                pool.Add(target);
            }

            Debug.Log($"[TargetPool] Initialized pool with {poolSize} targets");
        }

        private void OnGameStateChanged(GameState newState)
        {
            if (newState == GameState.Playing)
            {
                ResetAllTargets();
            }
        }

        public void ActivateAllTargets()
        {
            activeCount = 0;
            int pointsToUse = Mathf.Min(spawnPoints.Length, pool.Count);

            for (int i = 0; i < pointsToUse; i++)
            {
                pool[i].transform.position = spawnPoints[i].position;
                pool[i].transform.rotation = spawnPoints[i].rotation;
                pool[i].ResetTarget();
                activeCount++;
            }

            Debug.Log($"[TargetPool] Activated {activeCount} targets");
        }

        public void ResetAllTargets()
        {
            // Deactivate all first
            foreach (var target in pool)
            {
                target.gameObject.SetActive(false);
            }

            // Then activate at spawn points
            ActivateAllTargets();
        }

        private void HandleTargetHit(Target target)
        {
            activeCount--;
            Debug.Log($"[TargetPool] Target hit. Remaining: {activeCount}");

            if (activeCount <= 0)
            {
                Debug.Log("[TargetPool] All targets hit!");
                OnAllTargetsHit?.Invoke();
                
                // Notify GameManager
                if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Playing)
                {
                    GameManager.Instance.EndGame();
                }
            }
        }

        // Editor helper: Create spawn points
        [ContextMenu("Create Default Spawn Points")]
        private void CreateDefaultSpawnPoints()
        {
            // Create 10 spawn points in a range pattern
            // 3 rows: back (4), middle (3), front (3)
            float[] depths = { 8f, 6f, 4f };
            int[] countsPerRow = { 4, 3, 3 };
            float spacing = 1.5f;
            float height = 1.5f;

            List<Transform> points = new List<Transform>();
            int pointIndex = 0;

            for (int row = 0; row < depths.Length; row++)
            {
                int count = countsPerRow[row];
                float startX = -((count - 1) * spacing) / 2f;

                for (int i = 0; i < count; i++)
                {
                    GameObject point = new GameObject($"SpawnPoint_{pointIndex}");
                    point.transform.SetParent(transform);
                    point.transform.localPosition = new Vector3(
                        startX + (i * spacing),
                        height,
                        depths[row]
                    );
                    point.transform.localRotation = Quaternion.identity;
                    points.Add(point.transform);
                    pointIndex++;
                }
            }

            spawnPoints = points.ToArray();
            Debug.Log($"[TargetPool] Created {spawnPoints.Length} spawn points");
        }
    }
}
