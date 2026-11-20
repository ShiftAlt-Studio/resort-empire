using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using ResortEmpire.Core;

namespace ResortEmpire.UI
{
    public class PauseMenu : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Button menuButton;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private WavePauseController wavePauseController;
        [SerializeField] private string mainMenuScene = "MainMenu";

        private bool pausedByMenu;

        private void Awake()
        {
            menuButton.onClick.AddListener(Open);
            resumeButton.onClick.AddListener(Close);
            mainMenuButton.onClick.AddListener(GoToMainMenu);
            restartButton.onClick.AddListener(Restart);
            panel.SetActive(false);
        }

        private void Start()
        {
            if (wavePauseController == null)
                wavePauseController = FindAnyObjectByType<WavePauseController>();
        }

        public void Open()
        {
            if (panel.activeSelf) return;

            pausedByMenu = GameState.Instance != null && GameState.Instance.PauseWave();
            panel.SetActive(true);
        }

        public void Close()
        {
            panel.SetActive(false);

            if (pausedByMenu && GameState.Instance != null)
                GameState.Instance.ResumeWave();
            pausedByMenu = false;
        }

        private void Restart()
        {
            Time.timeScale = 1f;

            if (wavePauseController != null)
                wavePauseController.ClearSavedWave();

            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void GoToMainMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(mainMenuScene);
        }
    }
}