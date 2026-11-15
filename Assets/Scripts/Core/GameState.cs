using System;
using System.Collections.Generic;
using UnityEngine;

namespace ResortEmpire.Core
{
    public class GameState : MonoBehaviour
    {
        public static GameState Instance { get; private set; }

        [SerializeField] private int startingWorkers = 2;
        [SerializeField] private int startingCoins = 1000;
        [SerializeField] private int startingEnergy = 45;
        [SerializeField] private int startingResortValue = 700;

        public ResortState CurrentState { get; private set; }
        public ResortState PreviousState { get; private set; }
        public int AvailableWorkers { get; private set; }
        public int CurrentWaveNumber { get; private set; } = 1;
        public int Coins { get; private set; }
        public int Energy { get; private set; }
        public int ResortValue { get; private set; }

        public bool IsResumingFromPause { get; private set; }

        public event Action<ResortState> OnStateChanged;
        public event Action<int> CoinsChanged;
        public event Action<int> EnergyChanged;
        public event Action<int> ResortValueChanged;

        private ResortState stateBeforePause = ResortState.AWAITING_WAVE;

        private readonly HashSet<GuestNeedType> unlockedNeeds = new HashSet<GuestNeedType>
        {
            GuestNeedType.Rooms,
            GuestNeedType.Drinks,
            GuestNeedType.Food
        };

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            CurrentState = ResortState.AWAITING_WAVE;
            PreviousState = ResortState.AWAITING_WAVE;
            AvailableWorkers = startingWorkers;
            Coins = startingCoins;
            Energy = startingEnergy;
            ResortValue = startingResortValue;

            Debug.Log($"GameState: {CurrentState}, workers: {AvailableWorkers}, coins: {Coins}, energy: {Energy}, resortValue: {ResortValue}");
        }

        public List<NeedDemand> GetForecast()
        {
            return WaveTable.GetDemand(CurrentWaveNumber, unlockedNeeds);
        }

        public bool TryStartWave()
        {
            if (CurrentState != ResortState.AWAITING_WAVE)
                return false;

            SetState(ResortState.WAVE_INCOMING);
            return true;
        }

        public void FinishArrival()
        {
            if (CurrentState == ResortState.WAVE_INCOMING)
                SetState(ResortState.WAVE_ACTIVE);
        }

        public void CompleteWave()
        {
            if (CurrentState != ResortState.WAVE_ACTIVE)
                return;

            SetState(ResortState.WAVE_RESOLVING);
            SetState(ResortState.WAVE_COMPLETE);
        }

        /// <summary>
        /// Вызывается после нажатия Claim Rewards на экране Wave Complete (SS-37).
        /// </summary>
        public void CompleteClaimAndAdvanceWave()
        {
            if (CurrentState != ResortState.WAVE_COMPLETE && CurrentState != ResortState.UPGRADE)
                return;

            if (CurrentWaveNumber < WaveTable.TotalWaves)
                CurrentWaveNumber++;

            SetState(ResortState.AWAITING_WAVE);
        }

        /// <summary>
        /// SS-16: Приостанавливает активную волну (в фазе прихода или добега).
        /// </summary>
        public bool PauseWave()
        {
            if (CurrentState != ResortState.WAVE_INCOMING && CurrentState != ResortState.WAVE_ACTIVE)
                return false;

            stateBeforePause = CurrentState;
            SetState(ResortState.WAVE_PAUSED);
            return true;
        }

        /// <summary>
        /// SS-16: Возобновляет приостановленную волну ровно в том состоянии, где она была.
        /// </summary>
        public bool ResumeWave()
        {
            if (CurrentState != ResortState.WAVE_PAUSED)
                return false;

            IsResumingFromPause = true;
            SetState(stateBeforePause);
            IsResumingFromPause = false;
            return true;
        }

        public void AddCoins(int amount)
        {
            if (amount <= 0) return;
            Coins += amount;
            Debug.Log($"[GameState] +{amount} Coins -> {Coins}");
            CoinsChanged?.Invoke(Coins);
        }

        public bool TrySpendCoins(int amount)
        {
            if (amount < 0 || Coins < amount) return false;
            Coins -= amount;
            CoinsChanged?.Invoke(Coins);
            return true;
        }

        public void AddEnergy(int amount)
        {
            if (amount <= 0) return;
            Energy += amount;
            Debug.Log($"[GameState] +{amount} Energy -> {Energy}");
            EnergyChanged?.Invoke(Energy);
        }

        public bool TrySpendEnergy(int amount)
        {
            if (amount < 0 || Energy < amount) return false;
            Energy -= amount;
            EnergyChanged?.Invoke(Energy);
            return true;
        }

        /// <summary>
        /// SS-36 / SS-82: Resort Value только увеличивается с прогрессом курорта.
        /// </summary>
        public void AddResortValue(int amount)
        {
            if (amount <= 0) return;
            ResortValue += amount;
            Debug.Log($"[GameState] +{amount} Resort Value -> {ResortValue}");
            ResortValueChanged?.Invoke(ResortValue);
        }

        /// <summary>
        /// SS-36 / SS-83: Resort Value — индикатор прогресса, его нельзя тратить как валюту.
        /// </summary>
        public bool TrySpendResortValue(int amount)
        {
            Debug.LogWarning("[GameState] Resort Value cannot be spent or used as currency (SS-83).");
            return false;
        }

        public void RestoreSnapshot(int waveNumber, int coins, int energy, int resortValue, ResortState activeState)
        {
            CurrentWaveNumber = Mathf.Clamp(waveNumber, 1, WaveTable.TotalWaves);
            Coins = Mathf.Max(0, coins);
            Energy = Mathf.Max(0, energy);
            ResortValue = Mathf.Max(0, resortValue);

            CoinsChanged?.Invoke(Coins);
            EnergyChanged?.Invoke(Energy);
            ResortValueChanged?.Invoke(ResortValue);

            stateBeforePause = activeState;
            IsResumingFromPause = true;
            SetState(activeState);
            IsResumingFromPause = false;
        }

        private void SetState(ResortState newState)
        {
            PreviousState = CurrentState;
            CurrentState = newState;
            Debug.Log($"GameState: {CurrentState}");
            OnStateChanged?.Invoke(CurrentState);
        }
    }
}