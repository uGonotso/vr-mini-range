using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VRMiniRange.Core;

namespace VRMiniRange.UI
{
    public class StartPanel : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI instructionsText;
        [SerializeField] private Button startButton;

        [Header("Settings")]
        [SerializeField] private string title = "VR Mini Range";
        [SerializeField] [TextArea(2, 4)] private string instructions = "1. Pick up the object and place it in the socket\n2. Grab the gun and shoot all targets\n\nPress START to begin!";

        private void Start()
        {
            // Set text
            if (titleText != null)
                titleText.text = title;

            if (instructionsText != null)
                instructionsText.text = instructions;

            // Setup button
            if (startButton != null)
                startButton.onClick.AddListener(OnStartPressed);

            // Subscribe to game state
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnStateChanged += OnGameStateChanged;
            }

            // Show panel at start
            gameObject.SetActive(true);
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnStateChanged -= OnGameStateChanged;
            }

            if (startButton != null)
                startButton.onClick.RemoveListener(OnStartPressed);
        }

        private void OnStartPressed()
        {
            Debug.Log("[StartPanel] Start button pressed");
            
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StartGame();
            }
        }

        private void OnGameStateChanged(GameState newState)
        {
            // Hide panel when game starts
            if (newState == GameState.Playing)
            {
                gameObject.SetActive(false);
            }
            // Show panel when returning to menu
            else if (newState == GameState.Menu)
            {
                gameObject.SetActive(true);
            }
        }
    }
}
