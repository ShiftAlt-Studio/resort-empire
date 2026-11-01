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

        private readonly bool[] staffed = new bool[AllNeeds.Length];

        public event Action<GuestNeedType, GuestNeedType> WorkerMoved;

        public int StaffedCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < staffed.Length; i++)
                    if (staffed[i]) n++;
                return n;
            }
        }

        private void Awake()
        {
            foreach (GuestNeedType need in initiallyStaffed)
                staffed[(int)need] = true;
        }

        private void Start()
        {
            if (buildingRegistry == null)
                buildingRegistry = FindAnyObjectByType<BuildingRegistry>();

            if (GameState.Instance != null && GameState.Instance.AvailableWorkers != StaffedCount)
                Debug.LogWarning($"[WorkerAllocation] startingWorkers = {GameState.Instance.AvailableWorkers}, " +
                                 $"ale initiallyStaffed ma {StaffedCount}.");
        }

        public bool IsStaffed(GuestNeedType need) => staffed[(int)need];

        public MoveResult TryMoveWorker(GuestNeedType from, GuestNeedType to)
        {
            if (from == to) return MoveResult.SameBuilding;

            if (buildingRegistry == null ||
                buildingRegistry.GetBuildingForNeed(from) == null ||
                buildingRegistry.GetBuildingForNeed(to) == null)
                return MoveResult.NoBuilding;

            if (!staffed[(int)from]) return MoveResult.NoWorkerAtSource;
            if (staffed[(int)to]) return MoveResult.TargetAlreadyStaffed;

            staffed[(int)from] = false;
            staffed[(int)to] = true;

            Debug.Log($"[WorkerAllocation] Worker {from} -> {to}");
            WorkerMoved?.Invoke(from, to);
            return MoveResult.Success;
        }

        public MoveResult TryMoveWorkerFrom(GuestNeedType from)
        {
            if (!staffed[(int)from]) return MoveResult.NoWorkerAtSource;

            for (int i = 0; i < AllNeeds.Length; i++)
            {
                if (staffed[i] || AllNeeds[i] == from) continue;
                if (buildingRegistry == null || buildingRegistry.GetBuildingForNeed(AllNeeds[i]) == null) continue;
                return TryMoveWorker(from, AllNeeds[i]);
            }
            return MoveResult.NoEmptyBuilding;
        }

        public MoveResult TryMoveWorkerTo(GuestNeedType to)
        {
            for (int i = 0; i < AllNeeds.Length; i++)
            {
                if (staffed[i] && AllNeeds[i] != to)
                {
                    MoveResult r = TryMoveWorker(AllNeeds[i], to);
                    if (r == MoveResult.Success || r == MoveResult.TargetAlreadyStaffed)
                        return r;
                }
            }
            return MoveResult.NoWorkerAtSource;
        }
    }
}