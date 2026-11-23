using System;
using System.Collections.Generic;
using UnityEngine;

namespace ResortEmpire.Core
{
    public enum MoveResult
    {
        Success,
        SameBuilding,
        NoWorkerAtSource,
        TargetAlreadyStaffed,
        NoEmptyBuilding,
        NoBuilding
    }

    public class WorkerAllocation : MonoBehaviour
    {
        [SerializeField] private BuildingRegistry buildingRegistry;

        [Tooltip("Budovy, v ktorych Worker stoji na starte. Pocet ma zodpovedat GameState.startingWorkers.")]
        [SerializeField]
        private List<GuestNeedType> initiallyStaffed = new()
        {
            GuestNeedType.Rooms,
            GuestNeedType.Drinks
        };

        private static readonly GuestNeedType[] AllNeeds =
            (GuestNeedType[])Enum.GetValues(typeof(GuestNeedType));

        private readonly int[] workerCounts = new int[AllNeeds.Length];

        public event Action<GuestNeedType, GuestNeedType> WorkerMoved;

        public int StaffedCount => AssignedWorkerCount;

        private void Awake()
        {
            foreach (GuestNeedType need in initiallyStaffed)
            {
                int i = (int)need;
                if (i >= 0 && i < workerCounts.Length) workerCounts[i]++;
            }
        }

        private void Start()
        {
            if (buildingRegistry == null)
                buildingRegistry = FindAnyObjectByType<BuildingRegistry>();

            if (GameState.Instance != null && GameState.Instance.AvailableWorkers != AssignedWorkerCount)
                Debug.LogWarning($"[WorkerAllocation] startingWorkers = {GameState.Instance.AvailableWorkers}, assigned = {AssignedWorkerCount}.");
        }

        public int ActiveBuildingCount
        {
            get
            {
                if (buildingRegistry == null) return 0;
                int count = 0;
                foreach (GuestNeedType need in AllNeeds)
                    if (buildingRegistry.GetBuildingForNeed(need) != null) count++;
                return count;
            }
        }

        public int TotalWorkerSlots
        {
            get
            {
                if (buildingRegistry == null) return 0;
                int slots = 0;
                foreach (GuestNeedType need in AllNeeds)
                {
                    Building b = buildingRegistry.GetBuildingForNeed(need);
                    if (b != null) slots += b.WorkerSlots;
                }
                return slots;
            }
        }

        public int AssignedWorkerCount
        {
            get
            {
                int count = 0;
                foreach (GuestNeedType need in AllNeeds)
                    if (buildingRegistry != null && buildingRegistry.GetBuildingForNeed(need) != null)
                        count += GetWorkerCount(need);
                return count;
            }
        }

        public int EmptyBuildingCount
        {
            get
            {
                int count = 0;
                foreach (GuestNeedType need in AllNeeds)
                    if (buildingRegistry != null && buildingRegistry.GetBuildingForNeed(need) != null && !IsStaffed(need))
                        count++;
                return count;
            }
        }

        public bool HasDeficit => EmptyBuildingCount > 0;

        public int GetWorkerCount(GuestNeedType need)
        {
            int i = (int)need;
            return i >= 0 && i < workerCounts.Length ? workerCounts[i] : 0;
        }

        public int GetFreeSlots(GuestNeedType need)
        {
            Building b = buildingRegistry != null ? buildingRegistry.GetBuildingForNeed(need) : null;
            return b == null ? 0 : Mathf.Max(0, b.WorkerSlots - GetWorkerCount(need));
        }

        public bool IsStaffed(GuestNeedType need) => GetWorkerCount(need) > 0;

        public bool[] GetStaffedSnapshot()
        {
            bool[] result = new bool[workerCounts.Length];
            for (int i = 0; i < result.Length; i++) result[i] = workerCounts[i] > 0;
            return result;
        }

        public int[] GetWorkerCountsSnapshot() => (int[])workerCounts.Clone();

        public void RestoreStaffedSnapshot(bool[] savedStaffed)
        {
            if (savedStaffed == null) return;
            int[] converted = new int[workerCounts.Length];
            for (int i = 0; i < Mathf.Min(converted.Length, savedStaffed.Length); i++)
                converted[i] = savedStaffed[i] ? 1 : 0;
            RestoreWorkerCountsSnapshot(converted);
        }

        public void RestoreWorkerCountsSnapshot(int[] savedCounts)
        {
            if (savedCounts == null) return;
            Array.Clear(workerCounts, 0, workerCounts.Length);
            for (int i = 0; i < Mathf.Min(workerCounts.Length, savedCounts.Length); i++)
            {
                GuestNeedType need = (GuestNeedType)i;
                Building b = buildingRegistry != null ? buildingRegistry.GetBuildingForNeed(need) : null;
                workerCounts[i] = b == null ? 0 : Mathf.Clamp(savedCounts[i], 0, b.WorkerSlots);
            }
            WorkerMoved?.Invoke(GuestNeedType.Rooms, GuestNeedType.Rooms);
        }

        public bool GrantWorkerForUnlockedNeed(GuestNeedType need)
        {
            if (GetFreeSlots(need) <= 0) return false;
            if (GameState.Instance != null) GameState.Instance.AddWorker();
            workerCounts[(int)need]++;
            WorkerMoved?.Invoke(need, need);
            return true;
        }

        public bool TryGetEmptyNeed(GuestNeedType preferred, out GuestNeedType result)
        {
            result = preferred;
            if (GetFreeSlots(preferred) > 0) return true;
            foreach (GuestNeedType need in AllNeeds)
                if (GetFreeSlots(need) > 0) { result = need; return true; }
            return false;
        }

        public MoveResult TryMoveWorker(GuestNeedType from, GuestNeedType to)
        {
            if (from == to) return MoveResult.SameBuilding;
            if (buildingRegistry == null || buildingRegistry.GetBuildingForNeed(from) == null ||
                buildingRegistry.GetBuildingForNeed(to) == null) return MoveResult.NoBuilding;
            if (!IsStaffed(from)) return MoveResult.NoWorkerAtSource;
            if (GetFreeSlots(to) <= 0) return MoveResult.TargetAlreadyStaffed;

            workerCounts[(int)from]--;
            workerCounts[(int)to]++;
            WorkerMoved?.Invoke(from, to);
            return MoveResult.Success;
        }

        public MoveResult TryMoveWorkerFrom(GuestNeedType from)
        {
            if (!IsStaffed(from)) return MoveResult.NoWorkerAtSource;
            foreach (GuestNeedType need in AllNeeds)
                if (need != from && GetFreeSlots(need) > 0) return TryMoveWorker(from, need);
            return MoveResult.NoEmptyBuilding;
        }

        public MoveResult TryMoveWorkerTo(GuestNeedType to)
        {
            if (buildingRegistry == null || buildingRegistry.GetBuildingForNeed(to) == null)
                return MoveResult.NoBuilding;
            if (GetFreeSlots(to) <= 0) return MoveResult.TargetAlreadyStaffed;
            foreach (GuestNeedType need in AllNeeds)
                if (need != to && IsStaffed(need)) return TryMoveWorker(need, to);
            return MoveResult.NoWorkerAtSource;
        }
    }
}
