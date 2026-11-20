using System;
using UnityEngine;

namespace ResortEmpire.Core
{
    [Serializable]
    public struct WaveRewardSummary
    {
        public int WaveNumber;
        public float SatisfactionPercent;
        public WaveRating Rating;
        public int GuestsServed;
        public int TotalWaveUnits;
        public int CoinsGranted;
        public int EnergyGranted;
        public int ResortValueGranted;
    }

    public class WaveRewardService : MonoBehaviour
    {
        [SerializeField] private WaveSatisfaction waveSatisfaction;
        [SerializeField] private NeedAggregator needAggregator;
        [SerializeField] private WorkerAllocation workerAllocation;
        [SerializeField] private SpeedUpService speedUpService;

        private static readonly GuestNeedType[] AllNeeds =
            (GuestNeedType[])Enum.GetValues(typeof(GuestNeedType));

        public WaveRewardSummary LastSummary { get; private set; }
        public bool HasUnclaimedRewards { get; private set; }

        private bool playerActedInCurrentWave;

        public event Action<WaveRewardSummary> RewardsGranted;
        public event Action<WaveRewardSummary> RewardsClaimed;

        private void Start()
        {
            if (waveSatisfaction == null)
                waveSatisfaction = FindAnyObjectByType<WaveSatisfaction>();
            if (needAggregator == null)
                needAggregator = FindAnyObjectByType<NeedAggregator>();
            if (workerAllocation == null)
                workerAllocation = FindAnyObjectByType<WorkerAllocation>();
            if (speedUpService == null)
                speedUpService = FindAnyObjectByType<SpeedUpService>();

            if (waveSatisfaction != null)
                waveSatisfaction.ResultReady += HandleResultReady;
            if (workerAllocation != null)
                workerAllocation.WorkerMoved += HandleWorkerMoved;
            if (speedUpService != null)
                speedUpService.SpeedUpChanged += HandleSpeedUpChanged;
            if (GameState.Instance != null)
                GameState.Instance.OnStateChanged += HandleStateChanged;
        }

        private void OnDestroy()
        {
            if (waveSatisfaction != null)
                waveSatisfaction.ResultReady -= HandleResultReady;
            if (workerAllocation != null)
                workerAllocation.WorkerMoved -= HandleWorkerMoved;
            if (speedUpService != null)
                speedUpService.SpeedUpChanged -= HandleSpeedUpChanged;
            if (GameState.Instance != null)
                GameState.Instance.OnStateChanged -= HandleStateChanged;
        }

        private void HandleStateChanged(ResortState state)
        {
            if (state == ResortState.WAVE_INCOMING &&
                (GameState.Instance == null || !GameState.Instance.IsResumingFromPause))
            {
                playerActedInCurrentWave = false;
            }
        }

        private void HandleWorkerMoved(GuestNeedType from, GuestNeedType to)
        {
            if (from != to && IsWaveInProgress())
                playerActedInCurrentWave = true;
        }

        private void HandleSpeedUpChanged(GuestNeedType need)
        {
            if (IsWaveInProgress() && speedUpService != null &&
                speedUpService.GetState(need) == SpeedUpState.Active)
            {
                playerActedInCurrentWave = true;
            }
        }

        private static bool IsWaveInProgress()
        {
            if (GameState.Instance == null) return false;
            ResortState s = GameState.Instance.CurrentState;
            return s == ResortState.WAVE_INCOMING || s == ResortState.WAVE_ACTIVE || s == ResortState.WAVE_PAUSED;
        }

        private void HandleResultReady(float satisfactionPercent)
        {
            if (GameState.Instance == null) return;

            int totalUnits = 0;
            int servedUnits = 0;
            if (needAggregator != null)
            {
                foreach (GuestNeedType need in AllNeeds)
                {
                    totalUnits += needAggregator.GetArrived(need);
                    servedUnits += needAggregator.GetServed(need);
                }
            }

            WaveRating rating = WaveRatingCalculator.FromSatisfaction(satisfactionPercent, playerActedInCurrentWave);
            int coins = WaveRewards.CalculateCoins(totalUnits, rating);
            int energy = WaveRewards.CalculateEnergy(rating);
            int resortValue = WaveRewards.CalculateResortValue(rating);

            LastSummary = new WaveRewardSummary
            {
                WaveNumber = GameState.Instance.CurrentWaveNumber,
                SatisfactionPercent = satisfactionPercent,
                Rating = rating,
                GuestsServed = servedUnits,
                TotalWaveUnits = totalUnits,
                CoinsGranted = coins,
                EnergyGranted = energy,
                ResortValueGranted = resortValue
            };

            GameState.Instance.AddCoins(coins);
            GameState.Instance.AddEnergy(energy);
            GameState.Instance.AddResortValue(resortValue);

            HasUnclaimedRewards = true;

            Debug.Log($"[WaveRewardService] Wave {LastSummary.WaveNumber} rewards granted: " +
                      $"Rating={rating} ({satisfactionPercent:F0}%), action={playerActedInCurrentWave}, " +
                      $"+{coins} Coins, +{energy} Energy, +{resortValue} Resort Value (units: {servedUnits}/{totalUnits})");

            RewardsGranted?.Invoke(LastSummary);
        }

        public void ClaimRewards()
        {
            if (!HasUnclaimedRewards) return;

            HasUnclaimedRewards = false;
            Debug.Log($"[WaveRewardService] Player claimed Wave {LastSummary.WaveNumber} rewards.");
            RewardsClaimed?.Invoke(LastSummary);

            if (GameState.Instance != null)
                GameState.Instance.CompleteClaimAndAdvanceWave();
        }
    }
}