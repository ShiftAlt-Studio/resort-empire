using System;
using System.Collections.Generic;
using UnityEngine;

namespace ResortEmpire.Core
{
    public class ServiceQueueProcessor : MonoBehaviour
    {
        [SerializeField] private NeedAggregator needAggregator;
        [SerializeField] private ArrivalPhase arrivalPhase;
        [SerializeField] private BuildingRegistry buildingRegistry;
        [SerializeField] private WorkerAllocation workerAllocation;

        [Header("(Build Doc §4.8)")]
        [SerializeField] private int seatsPerBuilding = 3;
        [SerializeField] private float serviceTime = 12f;
        [SerializeField] private float staffedUtilization = 1.0f;
        [SerializeField] private float emptyUtilization = 0.33f;

        private static readonly GuestNeedType[] AllNeeds =
            (GuestNeedType[])Enum.GetValues(typeof(GuestNeedType));

        private readonly List<float>[] activeSeats = new List<float>[AllNeeds.Length];
        private readonly int[] seatsPerNeed = new int[AllNeeds.Length];
        private readonly int[] lastQueueLength = new int[AllNeeds.Length];

        public event Action<GuestNeedType, int> QueueChanged;

        private void Awake()
        {
            for (int i = 0; i < AllNeeds.Length; i++)
                activeSeats[i] = new List<float>(seatsPerBuilding);
        }

        private void Start()
        {
            if (needAggregator == null)
                needAggregator = GetComponent<NeedAggregator>() ?? FindAnyObjectByType<NeedAggregator>();

            if (arrivalPhase == null)
                arrivalPhase = GetComponent<ArrivalPhase>() ?? FindAnyObjectByType<ArrivalPhase>();

            if (buildingRegistry == null)
                buildingRegistry = FindAnyObjectByType<BuildingRegistry>();

            if (workerAllocation == null)
                workerAllocation = FindAnyObjectByType<WorkerAllocation>();

            RefreshSeats();

            if (GameState.Instance != null)
                GameState.Instance.OnStateChanged += HandleStateChanged;
        }

        private void OnDestroy()
        {
            if (GameState.Instance != null)
                GameState.Instance.OnStateChanged -= HandleStateChanged;
        }

        private void HandleStateChanged(ResortState state)
        {
            if (GameState.Instance != null && GameState.Instance.IsResumingFromPause)
                return;

            if (state == ResortState.WAVE_INCOMING)
            {
                for (int i = 0; i < activeSeats.Length; i++)
                    activeSeats[i].Clear();

                RefreshSeats();
                Array.Clear(lastQueueLength, 0, lastQueueLength.Length);
            }
            else if (state == ResortState.WAVE_ACTIVE)
            {
                Debug.Log("[ServiceQueueProcessor] Drain phase (dobeh) started — processing remaining queues.");
                EvaluateWaveCompletion();
            }
        }

        private void RefreshSeats()
        {
            for (int i = 0; i < AllNeeds.Length; i++)
            {
                Building b = buildingRegistry != null ? buildingRegistry.GetBuildingForNeed(AllNeeds[i]) : null;
                seatsPerNeed[i] = b != null ? b.Capacity : seatsPerBuilding;
            }
        }

        private void Update()
        {
            if (GameState.Instance == null || needAggregator == null)
                return;

            ResortState state = GameState.Instance.CurrentState;

            if (state != ResortState.WAVE_INCOMING && state != ResortState.WAVE_ACTIVE)
                return;

            ProcessQueues(Time.deltaTime);

            if (state == ResortState.WAVE_ACTIVE)
            {
                EvaluateWaveCompletion();
            }
        }

        private void ProcessQueues(float deltaTime)
        {
            RefreshSeats();
            float safeServiceTime = Mathf.Max(0.1f, serviceTime);

            for (int i = 0; i < AllNeeds.Length; i++)
            {
                GuestNeedType need = AllNeeds[i];
                List<float> seats = activeSeats[i];

                int freeSeats = seatsPerNeed[i] - seats.Count;
                if (freeSeats > 0)
                {
                    int started = needAggregator.TryBeginService(need, freeSeats);
                    for (int s = 0; s < started; s++)
                        seats.Add(0f);
                }

                TrackQueue(i, need);

                if (seats.Count == 0)
                    continue;

                bool staffed = workerAllocation != null && workerAllocation.IsStaffed(need);
                float utilization = staffed ? staffedUtilization : emptyUtilization;
                float step = (utilization / safeServiceTime) * deltaTime;

                int completedCount = 0;
                for (int s = seats.Count - 1; s >= 0; s--)
                {
                    seats[s] += step;
                    if (seats[s] >= 1f)
                    {
                        seats.RemoveAt(s);
                        completedCount++;
                    }
                }

                if (completedCount > 0)
                {
                    needAggregator.CompleteService(need, completedCount);

                    int newlyFree = seatsPerNeed[i] - seats.Count;
                    if (newlyFree > 0)
                    {
                        int started = needAggregator.TryBeginService(need, newlyFree);
                        for (int s = 0; s < started; s++)
                            seats.Add(0f);
                    }
                }
            }
        }

        private void TrackQueue(int index, GuestNeedType need)
        {
            int queued = needAggregator.GetQueueLength(need);
            if (queued == lastQueueLength[index]) return;

            if (queued > lastQueueLength[index])
                Debug.Log($"[ServiceQueueProcessor] {need}: demand queued, queue = {queued}");

            lastQueueLength[index] = queued;
            QueueChanged?.Invoke(need, queued);
        }

        public float[] GetSeatProgressForNeed(GuestNeedType need)
        {
            return activeSeats[(int)need].ToArray();
        }

        public void RestoreSeatProgressForNeed(GuestNeedType need, float[] progress)
        {
            List<float> list = activeSeats[(int)need];
            list.Clear();
            if (progress != null)
                list.AddRange(progress);
        }

        private void EvaluateWaveCompletion()
        {
            bool arrivalDone = (arrivalPhase == null) || arrivalPhase.IsFinished;
            if (!arrivalDone)
                return;

            if (needAggregator.AllServed)
            {
                Debug.Log("[ServiceQueueProcessor] All remaining queues are empty. Completing wave!");
                GameState.Instance.CompleteWave();
            }
        }
    }
}