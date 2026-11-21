using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ResortEmpire.UI
{
    public class SuddenRushView : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] private GameObject root;

        [Header("Content")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private Image timerFill;

        [Header("Buttons")]
        [SerializeField] private Button moveWorkerButton;
        [SerializeField] private Button useEnergyButton;
        [SerializeField] private TMP_Text useEnergyText;
        [SerializeField] private Button ignoreButton;

        [Header("Settings")]
        [SerializeField] private float autoHideSeconds = 10f;

        public event Action MoveWorkerClicked;
        public event Action UseEnergyClicked;
        public event Action IgnoreClicked;

        private float timeLeft;
        private bool shown;

        private void Awake()
        {
            if (moveWorkerButton != null) moveWorkerButton.onClick.AddListener(() => { MoveWorkerClicked?.Invoke(); Hide(); });
            if (useEnergyButton != null) useEnergyButton.onClick.AddListener(() => { UseEnergyClicked?.Invoke(); Hide(); });
            if (ignoreButton != null) ignoreButton.onClick.AddListener(() => { IgnoreClicked?.Invoke(); Hide(); });
        }

        public void Show(string buildingName, int energyCost, bool canUseEnergy, bool canMoveWorker = true)
        {
            if (titleText != null) titleText.text = "SUDDEN RUSH!";
            if (messageText != null)
                messageText.text = $"A crowd is heading to {buildingName}. No worker there!";
            if (useEnergyText != null) useEnergyText.text = $"USE {energyCost} ENERGY";

            if (useEnergyButton != null) useEnergyButton.interactable = canUseEnergy;
            if (moveWorkerButton != null) moveWorkerButton.interactable = canMoveWorker;

            timeLeft = autoHideSeconds;
            shown = true;
            UpdateTimer();
            if (root != null) root.SetActive(true);
        }

        public void Hide()
        {
            shown = false;
            if (root != null) root.SetActive(false);
        }

        private void Update()
        {
            if (!shown || autoHideSeconds <= 0f) return;

            timeLeft -= Time.deltaTime;
            UpdateTimer();
            if (timeLeft <= 0f)
            {
                IgnoreClicked?.Invoke();
                Hide();
            }
        }

        private void UpdateTimer()
        {
            if (timerFill == null) return;

            float fraction = autoHideSeconds > 0f ? Mathf.Clamp01(timeLeft / autoHideSeconds) : 0f;
            RectTransform rect = timerFill.rectTransform;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(fraction, 1f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        [ContextMenu("Preview/Show")]
        private void PreviewShow() => Show("BeachBar", 3, true);

        [ContextMenu("Preview/No Energy")]
        private void PreviewNoEnergy() => Show("BeachBar", 3, false);
    }
}