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
        private float timer;

        public event Action<GuestNeedType, NeedDisplayStatus> StatusChanged;

        public NeedDisplayStatus GetStatus(GuestNeedType need) => current[(int)need];

        private void Start()
        {
            if (needAggregator == null)
                needAggregator = FindAnyObjectByType<NeedAggregator>();
            if (bottleneckDetector == null)
                bottleneckDetector = FindAnyObjectByType<BottleneckDetector>();

            Refresh(true);
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
                NeedDisplayStatus status = Evaluate(need);

                if (!force && status == current[i]) continue;

                current[i] = status;
                StatusChanged?.Invoke(need, status);
            }
        }

        private NeedDisplayStatus Evaluate(GuestNeedType need)
        {
            if (!IsUnlocked(need))
                return NeedDisplayStatus.LOCKED;

            if (bottleneckDetector != null)
            {
                NeedStatus s = bottleneckDetector.GetStatus(need);
                if (s == NeedStatus.BOTTLENECK) return NeedDisplayStatus.BOTTLENECK;
                if (s == NeedStatus.RECOVERING) return NeedDisplayStatus.RECOVERING;
            }

            // GDD: SATISFIED = príchod uzavretý AND rad 0 AND nikto nesedí
            if (needAggregator.IsDemandClosed &&
                needAggregator.GetArrived(need) > 0 &&
                needAggregator.GetBacklog(need) == 0)
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