using System;
using System.Collections.Generic;
using UnityEngine;

namespace ResortEmpire.Core
{
    public enum NeedStatus
    {
        Normal,
        BOTTLENECK
    }

    public class BottleneckDetector : MonoBehaviour
    {
        [SerializeField] private ServiceQueueProcessor queueProcessor;
        [SerializeField] private int queueThreshold = 10;

        private readonly Dictionary<GuestNeedType, NeedStatus> statuses =
            new Dictionary<GuestNeedType, NeedStatus>();

        public event Action<GuestNeedType, NeedStatus> StatusChanged;

        public NeedStatus GetStatus(GuestNeedType need)
        {
            return statuses.TryGetValue(need, out var s) ? s : NeedStatus.Normal;
        }

        private void Start()
        {
            if (queueProcessor == null)
                queueProcessor = FindAnyObjectByType<ServiceQueueProcessor>();

            if (queueProcessor != null)
                queueProcessor.QueueChanged += HandleQueueChanged;

            if (GameState.Instance != null)
                GameState.Instance.OnStateChanged += HandleStateChanged;
        }

        private void OnDestroy()
        {
            if (queueProcessor != null)
                queueProcessor.QueueChanged -= HandleQueueChanged;

            if (GameState.Instance != null)
                GameState.Instance.OnStateChanged -= HandleStateChanged;
        }

        private void HandleStateChanged(ResortState state)
        {
            if (state == ResortState.WAVE_INCOMING)
            {
                foreach (GuestNeedType need in Enum.GetValues(typeof(GuestNeedType)))
                    SetStatus(need, NeedStatus.Normal, 0);
            }
        }

        private void HandleQueueChanged(GuestNeedType need, int queued)
        {
            var newStatus = queued >= queueThreshold
                ? NeedStatus.BOTTLENECK
                : NeedStatus.Normal;
            SetStatus(need, newStatus, queued);
        }

        private void SetStatus(GuestNeedType need, NeedStatus newStatus, int queued)
        {
            if (GetStatus(need) == newStatus) return;

            statuses[need] = newStatus;
            Debug.Log($"[BottleneckDetector] {need}: {newStatus} (queue {queued})");
            StatusChanged?.Invoke(need, newStatus);
        }
    }
}