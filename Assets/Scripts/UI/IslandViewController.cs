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
            if (GameState.Instance != null)
            {
                UpdateHUD(GameState.Instance.ResortValue, GameState.Instance.Coins, GameState.Instance.Energy);
                SetResortState(GameState.Instance.CurrentState);

                GameState.Instance.CoinsChanged += HandleCoinsChanged;
                GameState.Instance.EnergyChanged += HandleEnergyChanged;
                GameState.Instance.ResortValueChanged += HandleResortValueChanged;
                GameState.Instance.OnStateChanged += SetResortState;
            }
            else
            {
                UpdateHUD(700, 1000, 45);
                SetResortState(currentState);
            }
        }

        private void OnDestroy()
        {
            if (startWaveButton != null)
                startWaveButton.onClick.RemoveListener(HandleStartWaveClicked);

            if (GameState.Instance != null)
            {
                GameState.Instance.CoinsChanged -= HandleCoinsChanged;
                GameState.Instance.EnergyChanged -= HandleEnergyChanged;
                GameState.Instance.ResortValueChanged -= HandleResortValueChanged;
                GameState.Instance.OnStateChanged -= SetResortState;
            }
        }

        private void HandleCoinsChanged(int coins)
        {
            if (coinsText != null) coinsText.text = $" {coins:N0}";
        }

        private void HandleEnergyChanged(int energy)
        {
            if (energyText != null) energyText.text = $"{energy}/50";
        }

        private void HandleResortValueChanged(int resortVal)
        {
            if (resortValueText != null) resortValueText.text = $"Resort Value: {resortVal}";
        }

        public void UpdateHUD(int resortVal, int coinsVal, int energyVal)
        {
            HandleResortValueChanged(resortVal);
            HandleCoinsChanged(coinsVal);
            HandleEnergyChanged(energyVal);
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
    }
}