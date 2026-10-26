using System;
using System.Collections.Generic;
using UnityEngine;

namespace ResortEmpire.Core
{
    public class ServiceQueueProcessor : MonoBehaviour
    {
        [SerializeField] private NeedAggregator needAggregator;
        [SerializeField] private ArrivalPhase arrivalPhase;

        [Header("(Build Doc §4.8)")]
        [SerializeField] private int seatsPerBuilding = 3;
        [SerializeField] private float serviceTime = 12f;
        [SerializeField] private float staffedUtilization = 1.0f;
        [SerializeField] private float emptyUtilization = 0.33f;

        private static readonly GuestNeedType[] AllNeeds =
            (GuestNeedType[])Enum.GetValues(typeof(GuestNeedType));

        private readonly List<float>[] activeSeats = new List<float>[AllNeeds.Length];
        private readonly bool[] hasWorker = new bool[AllNeeds.Length];

        private void Awake()
        {
            for (int i = 0; i < AllNeeds.Length; i++)
                activeSeats[i] = new List<float>(seatsPerBuilding);

            hasWorker[(int)GuestNeedType.Rooms] = true;
            hasWorker[(int)GuestNeedType.Drinks] = true;
            hasWorker[(int)GuestNeedType.Food] = false;
        }

        private void Start()
        {
            if (needAggregator == null)
                needAggregator = GetComponent<NeedAggregator>() ?? FindAnyObjectByType<NeedAggregator>();

            if (arrivalPhase == null)
                arrivalPhase = GetComponent<ArrivalPhase>() ?? FindAnyObjectByType<ArrivalPhase>();

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
            if (state == ResortState.WAVE_INCOMING)
            {
                for (int i = 0; i < activeSeats.Length; i++)
                    activeSeats[i].Clear();
            }
            else if (state == ResortState.WAVE_ACTIVE)
            {
                Debug.Log("[ServiceQueueProcessor] Drain phase (dobeh) started — processing remaining queues.");
                EvaluateWaveCompletion();
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
            float safeServiceTime = Mathf.Max(0.1f, serviceTime);

            for (int i = 0; i < AllNeeds.Length; i++)
            {
                GuestNeedType need = AllNeeds[i];
                List<float> seats = activeSeats[i];

                int freeSeats = seatsPerBuilding - seats.Count;
                if (freeSeats > 0)
                {
                    int started = needAggregator.TryBeginService(need, freeSeats);
                    for (int s = 0; s < started; s++)
                        seats.Add(0f);
                }

                if (seats.Count == 0)
                    continue;

                float utilization = hasWorker[i] ? staffedUtilization : emptyUtilization;
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

                    int newlyFree = seatsPerBuilding - seats.Count;
                    if (newlyFree > 0)
                    {
                        int started = needAggregator.TryBeginService(need, newlyFree);
                        for (int s = 0; s < started; s++)
                            seats.Add(0f);
                    }
                }
            }
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