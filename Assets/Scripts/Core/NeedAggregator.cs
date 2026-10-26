using System;
using UnityEngine;

namespace ResortEmpire.Core
{
    public class NeedAggregator : MonoBehaviour
    {
        [SerializeField] private ArrivalPhase arrivalPhase;
        [SerializeField] private bool logChanges = true;

        public event Action<GuestNeedType> NeedChanged;

        private static readonly int NeedCount = Enum.GetValues(typeof(GuestNeedType)).Length;

        private readonly int[] arrived = new int[NeedCount];
        private readonly int[] inService = new int[NeedCount];
        private readonly int[] served = new int[NeedCount];

        public bool IsDemandClosed { get; private set; }

        public int GetArrived(GuestNeedType need) => arrived[(int)need];
        public int GetInService(GuestNeedType need) => inService[(int)need];
        public int GetServed(GuestNeedType need) => served[(int)need];
        public int GetBacklog(GuestNeedType need) => arrived[(int)need] - served[(int)need];
        public int GetWaiting(GuestNeedType need) => GetBacklog(need) - inService[(int)need];

        public bool AllServed
        {
            get
            {
                for (int i = 0; i < NeedCount; i++)
                    if (arrived[i] - served[i] > 0) return false;
                return true;
            }
        }

        private void Start()
        {
            if (arrivalPhase == null)
                arrivalPhase = FindAnyObjectByType<ArrivalPhase>();

            if (arrivalPhase != null)
            {
                arrivalPhase.DemandAdded += HandleDemandAdded;
                arrivalPhase.ArrivalFinished += HandleArrivalFinished;
                Debug.Log("[NeedAggregator] Ready, subscribed to ArrivalPhase.");
            }
            else
                Debug.LogWarning("NeedAggregator: ArrivalPhase not found, no demand will be aggregated.");

            if (GameState.Instance != null)
                GameState.Instance.OnStateChanged += HandleStateChanged;
        }

        private void OnDestroy()
        {
            if (arrivalPhase != null)
            {
                arrivalPhase.DemandAdded -= HandleDemandAdded;
                arrivalPhase.ArrivalFinished -= HandleArrivalFinished;
            }

            if (GameState.Instance != null)
                GameState.Instance.OnStateChanged -= HandleStateChanged;
        }

        private void HandleStateChanged(ResortState state)
        {
            if (state == ResortState.WAVE_INCOMING)
                ResetAll();
        }

        private void HandleArrivalFinished()
        {
            IsDemandClosed = true;
            if (logChanges)
                Debug.Log("[NeedAggregator] Arrival phase ended. New demand is no longer accepted.");
        }

        private void HandleDemandAdded(GuestNeedType need, int units)
        {
            if (IsDemandClosed || units <= 0) return;

            arrived[(int)need] += units;
            if (logChanges)
                Debug.Log($"[NeedAggregator] {need}: arrived {GetArrived(need)}, backlog {GetBacklog(need)}");
            NeedChanged?.Invoke(need);
        }

        public int TryBeginService(GuestNeedType need, int requested)
        {
            if (requested <= 0) return 0;

            int taken = Mathf.Min(requested, GetWaiting(need));
            if (taken <= 0) return 0;

            inService[(int)need] += taken;
            NeedChanged?.Invoke(need);
            return taken;
        }

        public void CompleteService(GuestNeedType need, int count)
        {
            if (count <= 0) return;

            int done = Mathf.Min(count, inService[(int)need]);
            if (done <= 0) return;

            inService[(int)need] -= done;
            served[(int)need] += done;
            if (logChanges)
                Debug.Log($"[NeedAggregator] {need}: served {GetServed(need)}/{GetArrived(need)}, backlog {GetBacklog(need)}");
            NeedChanged?.Invoke(need);
        }

        public void ResetAll()
        {
            IsDemandClosed = false;
            Array.Clear(arrived, 0, NeedCount);
            Array.Clear(inService, 0, NeedCount);
            Array.Clear(served, 0, NeedCount);

            foreach (GuestNeedType need in Enum.GetValues(typeof(GuestNeedType)))
                NeedChanged?.Invoke(need);
        }
    }
}