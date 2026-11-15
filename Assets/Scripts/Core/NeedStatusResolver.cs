using System;
using UnityEngine;

namespace ResortEmpire.Core
{
    public enum NeedDisplayStatus
    {
        OK,
        BOTTLENECK,
        RECOVERING,
        SATISFIED,
        LOCKED
    }

    public class NeedStatusResolver : MonoBehaviour
    {
        [SerializeField] private NeedAggregator needAggregator;
        [SerializeField] private BottleneckDetector bottleneckDetector;
        [SerializeField] private float refreshInterval = 0.25f;

        private static readonly GuestNeedType[] AllNeeds =
            (GuestNeedType[])Enum.GetValues(typeof(GuestNeedType));

        private readonly NeedDisplayStatus[] current = new NeedDisplayStatus[AllNeeds.Length];
        private readonly bool[] wasBottleneck = new bool[AllNeeds.Length];
        private float timer;

        public event Action<GuestNeedType, NeedDisplayStatus> StatusChanged;

        public NeedDisplayStatus GetStatus(GuestNeedType need) => current[(int)need];

        private void Start()
        {
            if (needAggregator == null)
                needAggregator = FindAnyObjectByType<NeedAggregator>();
            if (bottleneckDetector == null)
                bottleneckDetector = FindAnyObjectByType<BottleneckDetector>();

            if (GameState.Instance != null)
                GameState.Instance.OnStateChanged += HandleStateChanged;

            Refresh(true);
        }

        private void OnDestroy()
        {
            if (GameState.Instance != null)
                GameState.Instance.OnStateChanged -= HandleStateChanged;
        }

        private void HandleStateChanged(ResortState state)
        {
            if (state == ResortState.WAVE_INCOMING)
                Array.Clear(wasBottleneck, 0, wasBottleneck.Length);
        }

        private void Update()
        {
            timer += Time.deltaTime;
            if (timer < refreshInterval) return;
            timer = 0f;
            Refresh(false);
        }

        private void Refresh(bool force)
        {
            if (needAggregator == null || GameState.Instance == null) return;

            for (int i = 0; i < AllNeeds.Length; i++)
            {
                GuestNeedType need = AllNeeds[i];
                NeedDisplayStatus status = Evaluate(i, need);

                if (!force && status == current[i]) continue;

                current[i] = status;
                StatusChanged?.Invoke(need, status);
            }
        }

        private NeedDisplayStatus Evaluate(int i, GuestNeedType need)
        {
            if (!IsUnlocked(need))
                return NeedDisplayStatus.LOCKED;

            if (bottleneckDetector != null &&
                bottleneckDetector.GetStatus(need) == NeedStatus.BOTTLENECK)
            {
                wasBottleneck[i] = true;
                return NeedDisplayStatus.BOTTLENECK;
            }

            if (wasBottleneck[i])
            {
                if (needAggregator.GetQueueLength(need) > 0)
                    return NeedDisplayStatus.RECOVERING;
                wasBottleneck[i] = false;
            }

            if (needAggregator.GetArrived(need) > 0 && needAggregator.GetBacklog(need) == 0)
                return NeedDisplayStatus.SATISFIED;

            return NeedDisplayStatus.OK;
        }

        private bool IsUnlocked(GuestNeedType need)
        {
            foreach (NeedDemand d in GameState.Instance.GetForecast())
                if (d.Need == need) return true;
            return false;
        }
    }
}