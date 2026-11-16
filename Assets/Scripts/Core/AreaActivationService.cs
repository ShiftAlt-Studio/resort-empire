using System;
using UnityEngine;

namespace ResortEmpire.Core
{
    public enum AreaType
    {
        Pool,
        Bungalows
    }

    public enum AreaState
    {
        Locked,
        Available,
        NotAffordable,
        Unlocked
    }

    public enum PurchaseResult
    {
        Success,
        WaveInProgress,
        NotEligible,
        NotEnoughCoins,
        AlreadyUnlocked,
        NotConfigured
    }

    public class AreaActivationService : MonoBehaviour
    {
        [Serializable]
        private class Area
        {
            public AreaType type;
            public GuestNeedType need;
            public GameObject siteObject;
            public Building building;
        }

        [SerializeField] private BuildingRegistry buildingRegistry;
        [SerializeField] private WorkerAllocation workerAllocation;
        [SerializeField] private Area[] areas;

        public event Action<AreaType> AreaActivated;

        private void Start()
        {
            if (buildingRegistry == null)
                buildingRegistry = FindAnyObjectByType<BuildingRegistry>();
            if (workerAllocation == null)
                workerAllocation = FindAnyObjectByType<WorkerAllocation>();
        }

        public bool IsActivated(AreaType type)
        {
            Area a = Find(type);
            return a != null && GameState.Instance != null && GameState.Instance.IsNeedUnlocked(a.need);
        }

        public static int RequiredWave(AreaType type)
        {
            switch (type)
            {
                case AreaType.Pool: return 3;
                case AreaType.Bungalows: return 6;
                default: return int.MaxValue;
            }
        }

        public bool IsEligible(AreaType type)
        {
            return GameState.Instance != null
                && GameState.Instance.CompletedWaves >= RequiredWave(type);
        }

        public static int GetCost(AreaType type)
        {
            switch (type)
            {
                case AreaType.Pool: return 1200;
                case AreaType.Bungalows: return 2500;
                default: return int.MaxValue;
            }
        }

        public bool IsPurchaseWindowOpen
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

        public AreaState GetState(AreaType type)
        {
            if (IsActivated(type)) return AreaState.Unlocked;
            if (!IsEligible(type)) return AreaState.Locked;
            if (GameState.Instance.Coins < GetCost(type)) return AreaState.NotAffordable;
            return AreaState.Available;
        }

        public PurchaseResult TryPurchase(AreaType type)
        {
            Area a = Find(type);
            if (a == null || a.building == null) return PurchaseResult.NotConfigured;
            if (!IsPurchaseWindowOpen) return PurchaseResult.WaveInProgress;

            switch (GetState(type))
            {
                case AreaState.Unlocked: return PurchaseResult.AlreadyUnlocked;
                case AreaState.Locked: return PurchaseResult.NotEligible;
                case AreaState.NotAffordable: return PurchaseResult.NotEnoughCoins;
            }

            int cost = GetCost(type);
            if (!GameState.Instance.TrySpendCoins(cost))
                return PurchaseResult.NotEnoughCoins;

            if (!Activate(type))
            {
                GameState.Instance.AddCoins(cost);
                return PurchaseResult.NotConfigured;
            }

            Debug.Log($"[AreaActivationService] {type} purchased for {cost} Coins");
            return PurchaseResult.Success;
        }

        public bool Activate(AreaType type)
        {
            Area a = Find(type);
            if (a == null || a.building == null)
            {
                Debug.LogError($"[AreaActivationService] Area {type} is not configured.");
                return false;
            }

            if (GameState.Instance == null || GameState.Instance.IsNeedUnlocked(a.need))
                return false;

            if (a.siteObject != null) a.siteObject.SetActive(false);
            a.building.gameObject.SetActive(true);

            buildingRegistry.Register(a.building);
            GameState.Instance.UnlockNeed(a.need);
            workerAllocation.GrantWorkerForUnlockedNeed(a.need);

            Debug.Log($"[AreaActivationService] {type} activated: building, need {a.need}, +1 Worker");
            AreaActivated?.Invoke(type);
            return true;
        }

        private Area Find(AreaType type)
        {
            if (areas == null) return null;
            foreach (Area a in areas)
                if (a.type == type) return a;
            return null;
        }
    }
}