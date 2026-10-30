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

        public ResortState CurrentState { get; private set; }
        public int AvailableWorkers { get; private set; }
        public int CurrentWaveNumber { get; private set; } = 1;
        public int Coins { get; private set; }

        public event Action<ResortState> OnStateChanged;
        public event Action<int> CoinsChanged;

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
            AvailableWorkers = startingWorkers;
            Coins = startingCoins;

            Debug.Log($"GameState: {CurrentState}, workers: {AvailableWorkers}");
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

        public void AddCoins(int amount)
        {
            if (amount <= 0) return;
            Coins += amount;
            CoinsChanged?.Invoke(Coins);
        }

        public bool TrySpendCoins(int amount)
        {
            if (amount < 0 || Coins < amount) return false;
            Coins -= amount;
            CoinsChanged?.Invoke(Coins);
            return true;
        }

        private void SetState(ResortState newState)
        {
            CurrentState = newState;
            Debug.Log($"GameState: {CurrentState}");
            OnStateChanged?.Invoke(CurrentState);
        }
    }
}