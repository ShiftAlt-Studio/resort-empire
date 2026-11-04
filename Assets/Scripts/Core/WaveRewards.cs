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
    }
}