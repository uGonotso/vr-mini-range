using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VRMiniRange.Core;

namespace VRMiniRange.UI
{
    public class EndPanel : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI timeText;
        [SerializeField] private Button restartButton;

        [Header("Settings")]
        [SerializeField] private string completeTitleText = "Range Complete!";
        [SerializeField] private string scoreFormat = "Targets Hit: {0}/{1}";
        [SerializeField] private string timeFormat = "Time: {0}";

        private void Start()
        {
            // Setup button
            if (restartButton != null)
                restartButton.onClick.AddListener(OnRestartPressed);

            // Subscribe to game state
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnStateChanged += OnGameStateChanged;
            }

            // Hide at start
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnStateChanged -= OnGameStateChanged;
            }

            if (restartButton != null)
                restartButton.onClick.RemoveListener(OnRestartPressed);
        }

        private void OnGameStateChanged(GameState newState)
        {
            if (newState == GameState.Complete)
            {
                ShowResults();
                gameObject.SetActive(true);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        private void ShowResults()
        {
            if (GameManager.Instance == null) return;

            // Set title
            if (titleText != null)
                titleText.text = completeTitleText;

            // Set score
            if (scoreText != null)
                scoreText.text = string.Format(scoreFormat, 
                    GameManager.Instance.TargetsHit, 
                    GameManager.Instance.TotalTargets);

            // Set time
            if (timeText != null)
                timeText.text = string.Format(timeFormat, 
                    GameManager.Instance.GetFormattedTime());

            Debug.Log($"[EndPanel] Showing results - Score: {GameManager.Instance.TargetsHit}/{GameManager.Instance.TotalTargets}, Time: {GameManager.Instance.GetFormattedTime()}");
        }

        private void OnRestartPressed()
        {
            Debug.Log("[EndPanel] Restart button pressed");
            
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RestartGame();
            }
        }
    }
}
