using UnityEngine;

namespace ResortEmpire.Core
{
    public enum UpgradeResult
    {
        Success,
        WaveInProgress,
        MaxLevel,
        NotEnoughCoins,
        NoBuilding
    }

    public class BuildingUpgradeService : MonoBehaviour
    {
        public bool IsUpgradeWindowOpen
        {
            get
            {
                if (GameState.Instance == null) return false;
                ResortState s = GameState.Instance.CurrentState;
                return s == ResortState.AWAITING_WAVE
                    || s == ResortState.WAVE_COMPLETE
                    || s == ResortState.UPGRADE;
            }
        }

        public bool CanUpgrade(Building b) => Check(b) == UpgradeResult.Success;

        public UpgradeResult TryUpgrade(Building b)
        {
            UpgradeResult check = Check(b);
            if (check != UpgradeResult.Success)
            {
                Debug.Log($"[BuildingUpgradeService] Upgrade refused: {check}");
                return check;
            }

            int cost = b.NextUpgradeCost;
            if (!GameState.Instance.TrySpendCoins(cost))
                return UpgradeResult.NotEnoughCoins;

            b.SetLevel(b.Level + 1);
            Debug.Log($"[BuildingUpgradeService] {b.Type} -> Lv{b.Level}, seats {b.Capacity}, paid {cost}");
            return UpgradeResult.Success;
        }

        private UpgradeResult Check(Building b)
        {
            if (b == null) return UpgradeResult.NoBuilding;
            if (!IsUpgradeWindowOpen) return UpgradeResult.WaveInProgress;
            if (b.IsMaxLevel) return UpgradeResult.MaxLevel;
            if (GameState.Instance.Coins < b.NextUpgradeCost) return UpgradeResult.NotEnoughCoins;
            return UpgradeResult.Success;
        }
    }
}