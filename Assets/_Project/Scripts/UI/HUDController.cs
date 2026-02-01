using UnityEngine;
using TMPro;
using VRMiniRange.Core;
using VRMiniRange.Shooting;

namespace VRMiniRange.UI
{
    public class HUDController : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI ammoText;
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private GameObject reloadIndicator;

        [Header("Settings")]
        [SerializeField] private string scoreFormat = "Targets: {0}/{1}";
        [SerializeField] private string ammoFormat = "Ammo: {0}/{1}";

        private Gun currentGun;

        private void Start()
        {
            // Subscribe to GameManager events
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnScoreChanged += UpdateScore;
                GameManager.Instance.OnStateChanged += OnGameStateChanged;
            }

            // Find gun and subscribe
            currentGun = FindObjectOfType<Gun>();
            if (currentGun != null)
            {
                currentGun.OnAmmoChanged += UpdateAmmo;
                currentGun.OnReloadProgress += UpdateReloadIndicator;
                UpdateAmmo();
            }

            // Hide at start
            gameObject.SetActive(false);

            // Hide reload indicator
            if (reloadIndicator != null)
                reloadIndicator.SetActive(false);
        }

        private void Update()
        {
            // Update timer during gameplay
            if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Playing)
            {
                UpdateTimer();
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnScoreChanged -= UpdateScore;
                GameManager.Instance.OnStateChanged -= OnGameStateChanged;
            }

            if (currentGun != null)
            {
                currentGun.OnAmmoChanged -= UpdateAmmo;
                currentGun.OnReloadProgress -= UpdateReloadIndicator;
            }
        }

        private void OnGameStateChanged(GameState newState)
        {
            if (newState == GameState.Playing)
            {
                gameObject.SetActive(true);
                UpdateScore(0, GameManager.Instance.TotalTargets);
                UpdateAmmo();
            }
            else if (newState == GameState.Complete || newState == GameState.Menu)
            {
                gameObject.SetActive(false);
            }
        }

        private void UpdateScore(int current, int total)
        {
            if (scoreText != null)
            {
                scoreText.text = string.Format(scoreFormat, current, total);
            }
        }

        private void UpdateAmmo()
        {
            if (ammoText != null && currentGun != null)
            {
                ammoText.text = string.Format(ammoFormat, currentGun.CurrentAmmo, currentGun.MaxAmmo);
            }
        }

        private void UpdateTimer()
        {
            if (timerText != null && GameManager.Instance != null)
            {
                timerText.text = GameManager.Instance.GetFormattedTime();
            }
        }

        private void UpdateReloadIndicator(float progress)
        {
            if (reloadIndicator != null)
            {
                reloadIndicator.SetActive(progress > 0f);
            }
        }
    }
}
