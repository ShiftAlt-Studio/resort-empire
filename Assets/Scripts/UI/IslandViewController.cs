using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ResortEmpire.Core;

namespace ResortEmpire.UI
{
    public class IslandViewController : MonoBehaviour
    {
        [Header("Состояние курорта")]
        [SerializeField] private ResortState currentState = ResortState.AWAITING_WAVE;

        [Header("HUD Тексты")]
        [SerializeField] private TMP_Text resortValueText;
        [SerializeField] private TMP_Text coinsText;
        [SerializeField] private TMP_Text energyText;

        [Header("AWAITING_WAVE элементы")]
        [SerializeField] private Button startWaveButton;
        [SerializeField] private GameObject readyForGuestsBadge;
        [SerializeField] private TMP_Text readyForGuestsText;

        public event Action OnStartWaveRequested;

        private void Awake()
        {
            if (startWaveButton != null)
                startWaveButton.onClick.AddListener(HandleStartWaveClicked);
        }

        private void Start()
        {
            UpdateHUD(700, 12450, 45);
            SetResortState(currentState);
        }

        private void OnDestroy()
        {
            if (startWaveButton != null)
                startWaveButton.onClick.RemoveListener(HandleStartWaveClicked);
        }

        public void UpdateHUD(int resortVal, int coinsVal, int energyVal)
        {
            if (resortValueText != null) resortValueText.text = $"Resort Value: {resortVal}";
            if (coinsText != null) coinsText.text = $" {coinsVal:N0}";
            if (energyText != null) energyText.text = $"{energyVal}/50";
        }

        public void SetResortState(ResortState newState)
        {
            currentState = newState;
            bool isAwaiting = (currentState == ResortState.AWAITING_WAVE);

            if (startWaveButton != null) startWaveButton.gameObject.SetActive(isAwaiting);
            if (readyForGuestsBadge != null) readyForGuestsBadge.SetActive(isAwaiting);
            if (readyForGuestsText != null && isAwaiting) readyForGuestsText.text = "Ready for Guests";
        }

        private void HandleStartWaveClicked()
        {
            if (currentState != ResortState.AWAITING_WAVE) return;
            Debug.Log("[IslandView] Start Wave clicked (FR-3 hook).");
            OnStartWaveRequested?.Invoke();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                SetResortState(currentState);
                UpdateHUD(700, 12450, 45);
            }
        }
#endif
    }
}