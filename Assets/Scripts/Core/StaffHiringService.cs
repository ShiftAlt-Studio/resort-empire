using System;
using UnityEngine;

namespace ResortEmpire.Core
{
    public enum HireResult
    {
        Success,
        WrongPhase,
        NoFreeSlot,
        NoBuilding,
        AlreadyStaffed,
        AlreadyHasManager,
        NotEnoughCoins
    }

    public class StaffHiringService : MonoBehaviour
    {
        [SerializeField] private WorkerAllocation workerAllocation;
        [SerializeField] private BuildingRegistry buildingRegistry;

        [Header("Worker (Build Doc 5.3.5, TUNING)")]
        [SerializeField] private int workerBaseCost = 400;
        [SerializeField] private int workerCostStep = 200;
        [SerializeField, Range(0.1f, 1f)] private float workerCapRatio = 0.8f;

        [Header("Manager (Build Doc 5.3.2, TUNING)")]
        [SerializeField] private int managerCost = 800;

        private int workersHired;

        public int WorkerCost => workerBaseCost + workerCostStep * workersHired;
        public int ManagerCost => managerCost;

        public event Action Changed;

        private void Start()
        {
            if (workerAllocation == null) workerAllocation = FindAnyObjectByType<WorkerAllocation>();
            if (buildingRegistry == null) buildingRegistry = FindAnyObjectByType<BuildingRegistry>();

            if (GameState.Instance != null)
            {
                GameState.Instance.CoinsChanged += HandleCoinsChanged;
                GameState.Instance.OnStateChanged += HandleStateChanged;
            }
        }

        private void OnDestroy()
        {
            if (GameState.Instance != null)
            {
                GameState.Instance.CoinsChanged -= HandleCoinsChanged;
                GameState.Instance.OnStateChanged -= HandleStateChanged;
            }
        }

        private void HandleCoinsChanged(int coins) => Changed?.Invoke();
        private void HandleStateChanged(ResortState state) => Changed?.Invoke();

        public static bool IsBetweenWaves =>
            GameState.Instance != null && GameState.Instance.CurrentState == ResortState.AWAITING_WAVE;

        public bool CanAfford(int cost) =>
            GameState.Instance != null && GameState.Instance.Coins >= cost;

        private bool TrySpend(int cost) =>
            GameState.Instance != null && GameState.Instance.TrySpendCoins(cost);

        public int TotalWorkerSlots => workerAllocation != null ? workerAllocation.TotalWorkerSlots : 0;
        public int WorkerCap
        {
            get
            {
                int slots = TotalWorkerSlots;
                int cap = Mathf.FloorToInt(workerCapRatio * slots + 0.0001f);
                return Mathf.Clamp(cap, 0, Mathf.Max(0, slots - 1));
            }
        }

        public bool HasFreeWorkerSlot =>
            workerAllocation != null && workerAllocation.AssignedWorkerCount + 1 <= WorkerCap;

        public bool CanHireWorkerFor(GuestNeedType need) =>
            IsBetweenWaves &&
            workerAllocation != null &&
            buildingRegistry != null &&
            buildingRegistry.GetBuildingForNeed(need) != null &&
            workerAllocation.GetFreeSlots(need) > 0 &&
            HasFreeWorkerSlot &&
            CanAfford(WorkerCost);

        public HireResult TryHireWorker(GuestNeedType need)
        {
            if (!IsBetweenWaves) return HireResult.WrongPhase;
            if (workerAllocation == null || buildingRegistry == null ||
                buildingRegistry.GetBuildingForNeed(need) == null) return HireResult.NoBuilding;
            if (workerAllocation.GetFreeSlots(need) <= 0) return HireResult.AlreadyStaffed;
            if (!HasFreeWorkerSlot) return HireResult.NoFreeSlot;

            int cost = WorkerCost;
            if (!TrySpend(cost)) return HireResult.NotEnoughCoins;

            if (!workerAllocation.GrantWorkerForUnlockedNeed(need))
            {
                GameState.Instance.AddCoins(cost);
                return HireResult.AlreadyStaffed;
            }
            workersHired++;
            Debug.Log($"[StaffHiringService] Hired Worker -> {need} for {cost} Coins (cap {WorkerCap}).");
            Changed?.Invoke();
            return HireResult.Success;
        }

        public bool CanHireManager(Building building) =>
            building != null && !building.HasManager && IsBetweenWaves && CanAfford(managerCost);

        public HireResult TryHireManager(Building building)
        {
            if (building == null) return HireResult.NoBuilding;
            if (!IsBetweenWaves) return HireResult.WrongPhase;
            if (building.HasManager) return HireResult.AlreadyHasManager;
            if (!TrySpend(managerCost)) return HireResult.NotEnoughCoins;

            building.SetManager(true);
            Debug.Log($"[StaffHiringService] Hired Manager for {building.Type} for {managerCost} Coins.");
            Changed?.Invoke();
            return HireResult.Success;
        }
    }
}