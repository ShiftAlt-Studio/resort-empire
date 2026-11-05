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

    /// <summary>
    /// SS-33 (SS-74, SS-75, SS-76): Рассчитывает и начисляет Coins, Energy и Resort Value после завершения волны.
    /// </summary>
    public class WaveRewardService : MonoBehaviour
    {
        [SerializeField] private WaveSatisfaction waveSatisfaction;
        [SerializeField] private NeedAggregator needAggregator;

        private static readonly GuestNeedType[] AllNeeds =
            (GuestNeedType[])Enum.GetValues(typeof(GuestNeedType));

        public WaveRewardSummary LastSummary { get; private set; }
        public bool HasUnclaimedRewards { get; private set; }

        public event Action<WaveRewardSummary> RewardsGranted;
        public event Action<WaveRewardSummary> RewardsClaimed;

        private void Start()
        {
            if (waveSatisfaction == null)
                waveSatisfaction = FindAnyObjectByType<WaveSatisfaction>();

            if (needAggregator == null)
                needAggregator = FindAnyObjectByType<NeedAggregator>();

            if (waveSatisfaction != null)
                waveSatisfaction.ResultReady += HandleResultReady;
        }

        private void OnDestroy()
        {
            if (waveSatisfaction != null)
                waveSatisfaction.ResultReady -= HandleResultReady;
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

            WaveRating rating = WaveRatingCalculator.FromSatisfaction(satisfactionPercent);
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

            // SS-74, SS-75, SS-76: Начисляем Coins, Energy и Resort Value по завершении волны
            GameState.Instance.AddCoins(coins);
            GameState.Instance.AddEnergy(energy);
            GameState.Instance.AddResortValue(resortValue);

            HasUnclaimedRewards = true;

            Debug.Log($"[WaveRewardService] Wave {LastSummary.WaveNumber} rewards granted: " +
                      $"Rating={rating} ({satisfactionPercent:F0}%), " +
                      $"+{coins} Coins, +{energy} Energy, +{resortValue} Resort Value (units: {servedUnits}/{totalUnits})");

            RewardsGranted?.Invoke(LastSummary);
        }

        /// <summary>
        /// Вызывается кнопкой Claim Rewards на экране Wave Complete (SS-37).
        /// </summary>
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