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
        [SerializeField] private int startingMaterials = 0;
        [SerializeField] private int startingGems = 0;
        [SerializeField] private int firstPerfectStayGems = 10;

        public ResortState CurrentState { get; private set; }
        public ResortState PreviousState { get; private set; }
        public int AvailableWorkers { get; private set; }
        public int CurrentWaveNumber { get; private set; } = 1;
        public int Coins { get; private set; }
        public int Energy { get; private set; }
        public int ResortValue { get; private set; }
        public int Materials { get; private set; }
        public int Gems { get; private set; }
        public bool FirstPerfectStayRewarded { get; private set; }

        public bool IsResumingFromPause { get; private set; }
        public int CompletedWaves
        {
            get
            {
                bool currentDone = CurrentState == ResortState.WAVE_COMPLETE
                                || CurrentState == ResortState.UPGRADE;
                return currentDone ? CurrentWaveNumber : CurrentWaveNumber - 1;
            }
        }

        public event Action<ResortState> OnStateChanged;
        public event Action<int> CoinsChanged;
        public event Action<int> EnergyChanged;
        public event Action<int> ResortValueChanged;
        public event Action<int> MaterialsChanged;
        public event Action<int> GemsChanged;
        public event Action<GuestNeedType> NeedUnlocked;

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
            Materials = startingMaterials;
            Gems = startingGems;

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

        public void CompleteClaimAndAdvanceWave()
        {
            if (CurrentState != ResortState.WAVE_COMPLETE && CurrentState != ResortState.UPGRADE)
                return;

            if (CurrentWaveNumber < WaveTable.TotalWaves)
                CurrentWaveNumber++;

            SetState(ResortState.AWAITING_WAVE);
        }

        public bool PauseWave()
        {
            if (CurrentState != ResortState.WAVE_INCOMING && CurrentState != ResortState.WAVE_ACTIVE)
                return false;

            stateBeforePause = CurrentState;
            SetState(ResortState.WAVE_PAUSED);
            return true;
        }

        public bool ResumeWave()
        {
            if (CurrentState != ResortState.WAVE_PAUSED)
                return false;

            IsResumingFromPause = true;
            SetState(stateBeforePause);
            IsResumingFromPause = false;
            return true;
        }

        public void AddWorker()
        {
            AvailableWorkers += 1;
            Debug.Log($"[GameState] +1 Worker -> AvailableWorkers: {AvailableWorkers}");
        }

        public void SetWorkers(int count)
        {
            AvailableWorkers = Mathf.Max(0, count);
            Debug.Log($"[GameState] AvailableWorkers restored -> {AvailableWorkers}");
        }

        public bool IsNeedUnlocked(GuestNeedType need) => unlockedNeeds.Contains(need);

        public List<GuestNeedType> GetUnlockedNeedsSnapshot()
        {
            return new List<GuestNeedType>(unlockedNeeds);
        }

        public void RestoreUnlockedNeeds(IEnumerable<GuestNeedType> needs)
        {
            unlockedNeeds.Clear();
            unlockedNeeds.Add(GuestNeedType.Rooms);
            unlockedNeeds.Add(GuestNeedType.Drinks);
            unlockedNeeds.Add(GuestNeedType.Food);

            if (needs != null)
            {
                foreach (GuestNeedType need in needs)
                    unlockedNeeds.Add(need);
            }
        }

        public bool UnlockNeed(GuestNeedType need)
        {
            if (!unlockedNeeds.Add(need))
                return false;

            Debug.Log($"[GameState] Need unlocked: {need}");
            NeedUnlocked?.Invoke(need);
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

        public void AddMaterials(int amount)
        {
            if (amount <= 0) return;
            Materials += amount;
            Debug.Log($"[GameState] +{amount} Materials -> {Materials}");
            MaterialsChanged?.Invoke(Materials);
        }

        public bool TrySpendMaterials(int amount)
        {
            if (amount < 0 || Materials < amount) return false;
            Materials -= amount;
            MaterialsChanged?.Invoke(Materials);
            return true;
        }

        public void RestoreMaterials(int amount)
        {
            Materials = Mathf.Max(0, amount);
            MaterialsChanged?.Invoke(Materials);
        }
         public void AddGems(int amount)
        {
            if (amount <= 0) return;
            Gems += amount;
            Debug.Log($"[GameState] +{amount} Gems -> {Gems}");
            GemsChanged?.Invoke(Gems);
        }

        public bool TrySpendGems(int amount)
        {
            if (amount < 0 || Gems < amount) return false;
            Gems -= amount;
            GemsChanged?.Invoke(Gems);
            return true;
        }

        public void RestoreGems(int amount, bool firstPerfectStayRewarded)
        {
            Gems = Mathf.Max(0, amount);
            FirstPerfectStayRewarded = firstPerfectStayRewarded;
            GemsChanged?.Invoke(Gems);
        }

        public int GrantFirstPerfectStayGems()
        {
            if (FirstPerfectStayRewarded) return 0;
            FirstPerfectStayRewarded = true;
            AddGems(firstPerfectStayGems);
            return firstPerfectStayGems;
        }
        
        [ContextMenu("Debug/Grant First Perfect Stay Gems")]
        private void DebugFirstPerfectStay() => Debug.Log($"First Perfect Stay: +{GrantFirstPerfectStayGems()} Gems");
        
        [ContextMenu("Debug/Spend All Energy")]
        private void DebugSpendAllEnergy() => TrySpendEnergy(Energy);

        [ContextMenu("Debug/Add 5 Gems")]
        private void DebugAddGems() => AddGems(5);

        [ContextMenu("Debug/Add 5 Materials")]
        private void DebugAddMaterials() => AddMaterials(5);

        public void AddResortValue(int amount)
        {
            if (amount <= 0) return;
            ResortValue += amount;
            Debug.Log($"[GameState] +{amount} Resort Value -> {ResortValue}");
            ResortValueChanged?.Invoke(ResortValue);
        }

        public bool TrySpendResortValue(int amount)
        {
            Debug.LogWarning("[GameState] Resort Value cannot be spent or used as currency (SS-83).");
            return false;
        }

        public void RestoreSnapshot(
            int waveNumber,
            int coins,
            int energy,
            int resortValue,
            ResortState activeState,
            IEnumerable<GuestNeedType> savedUnlockedNeeds = null)
        {
            CurrentWaveNumber = Mathf.Clamp(waveNumber, 1, WaveTable.TotalWaves);
            Coins = Mathf.Max(0, coins);
            Energy = Mathf.Max(0, energy);
            ResortValue = Mathf.Max(0, resortValue);

            if (savedUnlockedNeeds != null)
                RestoreUnlockedNeeds(savedUnlockedNeeds);

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