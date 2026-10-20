using System;
using System.Collections.Generic;
using UnityEngine;

namespace ResortEmpire.Core
{
    public class GameState : MonoBehaviour
    {
        public static GameState Instance { get; private set; }

        [SerializeField] private int startingWorkers = 2;

        public ResortState CurrentState { get; private set; }
        public int AvailableWorkers { get; private set; }
        public int CurrentWaveNumber { get; private set; } = 1;

        public event Action<ResortState> OnStateChanged;

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

        private void SetState(ResortState newState)
        {
            CurrentState = newState;
            Debug.Log($"GameState: {CurrentState}");
            OnStateChanged?.Invoke(CurrentState);
        }
    }
}