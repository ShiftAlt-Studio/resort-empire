using UnityEngine;

namespace ResortEmpire.Core
{
    public static class WaveRewards
    {
        public const int CoinsPerUnit = 16;

        private static readonly float[] CoinsRatingMultiplier =
        {
            0.4f, 0.6f, 0.8f, 1.0f, 1.2f, 1.3f
        };

        public static int CalculateCoins(int totalWaveUnits, WaveRating rating)
        {
            float raw = totalWaveUnits * CoinsPerUnit * CoinsRatingMultiplier[(int)rating];
            int coins = Mathf.RoundToInt(raw);
            return Mathf.Max(1, coins);
        }

        private static readonly int[] EnergyRatingReward =
        {
            0, 1, 2, 3, 5, 5
        };

        public static int CalculateEnergy(WaveRating rating)
        {
            return EnergyRatingReward[(int)rating];
        }

        private static readonly int[] ResortValueRatingReward =
        {
            5, 8, 12, 15, 20, 25
        };

        public static int CalculateResortValue(WaveRating rating)
        {
            return ResortValueRatingReward[(int)rating];
        }
    }
}