using System.Collections.Generic;

namespace ResortEmpire.Core
{
    public struct NeedDemand
    {
        public GuestNeedType Need;
        public int Units;

        public NeedDemand(GuestNeedType need, int units)
        {
            Need = need;
            Units = units;
        }
    }

    public static class WaveTable
    {
        public const int TotalWaves = 8;

        private static readonly int[][] Demand =
        {
            new[] { 8, 8, 6, 0, 0 },      // wave 1
            new[] { 12, 16, 10, 0, 0 },   // wave 2
            new[] { 16, 18, 14, 0, 0 },   // wave 3
            new[] { 16, 18, 14, 8, 0 },   // wave 4
            new[] { 20, 22, 18, 16, 0 },  // wave 5
            new[] { 25, 28, 22, 20, 0 },  // wave 6
            new[] { 25, 28, 22, 20, 10 }, // wave 7
            new[] { 31, 35, 28, 25, 16 }  // wave 8
        };

        private static readonly GuestNeedType[] NeedOrder =
        {
            GuestNeedType.Rooms,
            GuestNeedType.Drinks,
            GuestNeedType.Food,
            GuestNeedType.Relax,
            GuestNeedType.OceanView
        };

        public static List<NeedDemand> GetDemand(int waveNumber, ICollection<GuestNeedType> unlocked)
        {
            var result = new List<NeedDemand>();
            if (waveNumber < 1 || waveNumber > TotalWaves) return result;

            int[] row = Demand[waveNumber - 1];
            for (int i = 0; i < NeedOrder.Length; i++)
            {
                if (row[i] > 0 && unlocked.Contains(NeedOrder[i]))
                    result.Add(new NeedDemand(NeedOrder[i], row[i]));
            }
            return result;
        }
    }
}