using System;
using UnityEngine;

namespace ResortEmpire.Core
{
    public enum SpeedUpResult
    {
        Success,
        WaveNotActive,
        NoBuilding,
        AlreadyUsed,
        NotEnoughEnergy
    }

    public enum SpeedUpState
    {
        Unavailable,
        Available,
        Active,
        Used,
        NotEnoughEnergy
    }

    public class SpeedUpService : MonoBehaviour
    {
        [SerializeField] private BuildingRegistry buildingRegistry;

        [Header("Build Doc 4.6 (TUNING)")]
        [SerializeField] private int energyCost = 3;
        [SerializeField] private float durationSeconds = 15f;
        [SerializeField] private float speedMultiplier = 2f;

        private static readonly GuestNeedType[] AllNeeds =
            (GuestNeedType[])Enum.GetValues(typeof(GuestNeedType));

        private readonly bool[] used = new bool[AllNeeds.Length];
        private readonly float[] remaining = new float[AllNeeds.Length];

        public int EnergyCost => energyCost;
        public float DurationSeconds => durationSeconds;

        public event Action<GuestNeedType> SpeedUpChanged;

        private void Start()
        {
            if (buildingRegistry == null)
                buildingRegistry = FindAnyObjectByType<BuildingRegistry>();

            if (GameState.Instance != null)
            {
                GameState.Instance.OnStateChanged += HandleStateChanged;
                GameState.Instance.EnergyChanged += HandleEnergyChanged;
            }
        }

        private void OnDestroy()
        {
            if (GameState.Instance != null)
            {
                GameState.Instance.OnStateChanged -= HandleStateChanged;
                GameState.Instance.EnergyChanged -= HandleEnergyChanged;
            }
        }

        private void HandleStateChanged(ResortState state)
        {
            if (GameState.Instance != null && GameState.Instance.IsResumingFromPause)
                return;

            if (state == ResortState.WAVE_INCOMING)
                ResetAll();
        }

        private void HandleEnergyChanged(int energy)
        {
            foreach (GuestNeedType need in AllNeeds)
                SpeedUpChanged?.Invoke(need);
        }

        private void Update()
        {
            if (!IsWaveRunning()) return;

            for (int i = 0; i < remaining.Length; i++)
            {
                if (remaining[i] <= 0f) continue;

                remaining[i] -= Time.deltaTime;
                if (remaining[i] <= 0f)
                {
                    remaining[i] = 0f;
                    Debug.Log($"[SpeedUpService] {AllNeeds[i]}: speed up ended");
                    SpeedUpChanged?.Invoke(AllNeeds[i]);
                }
            }
        }

        public float GetMultiplier(GuestNeedType need)
        {
            return remaining[(int)need] > 0f ? speedMultiplier : 1f;
        }

        public float GetRemainingSeconds(GuestNeedType need) => remaining[(int)need];

        public SpeedUpState GetState(GuestNeedType need)
        {
            if (!IsWaveRunning()) return SpeedUpState.Unavailable;
            if (buildingRegistry == null || buildingRegistry.GetBuildingForNeed(need) == null)
                return SpeedUpState.Unavailable;

            int i = (int)need;
            if (used[i]) return remaining[i] > 0f ? SpeedUpState.Active : SpeedUpState.Used;
            if (GameState.Instance.Energy < energyCost) return SpeedUpState.NotEnoughEnergy;
            return SpeedUpState.Available;
        }

        public SpeedUpResult TrySpeedUp(GuestNeedType need)
        {
            if (!IsWaveRunning()) return SpeedUpResult.WaveNotActive;
            if (buildingRegistry == null || buildingRegistry.GetBuildingForNeed(need) == null)
                return SpeedUpResult.NoBuilding;

            int i = (int)need;
            if (used[i]) return SpeedUpResult.AlreadyUsed;
            if (!GameState.Instance.TrySpendEnergy(energyCost)) return SpeedUpResult.NotEnoughEnergy;

            used[i] = true;
            remaining[i] = durationSeconds;
            Debug.Log($"[SpeedUpService] {need}: speed up x{speedMultiplier} for {durationSeconds}s, paid {energyCost} Energy");
            SpeedUpChanged?.Invoke(need);
            return SpeedUpResult.Success;
        }

        private static bool IsWaveRunning()
        {
            if (GameState.Instance == null) return false;
            ResortState s = GameState.Instance.CurrentState;
            return s == ResortState.WAVE_INCOMING || s == ResortState.WAVE_ACTIVE;
        }

        private void ResetAll()
        {
            Array.Clear(used, 0, used.Length);
            Array.Clear(remaining, 0, remaining.Length);
            foreach (GuestNeedType need in AllNeeds)
                SpeedUpChanged?.Invoke(need);
        }
    }
}