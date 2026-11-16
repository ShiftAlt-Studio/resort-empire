using System;
using System.Collections.Generic;
using UnityEngine;

namespace ResortEmpire.Core
{
    public enum NeedStatus
    {
        Normal,
        BOTTLENECK,
        RECOVERING
    }

    public class BottleneckDetector : MonoBehaviour
    {
        [SerializeField] private ServiceQueueProcessor queueProcessor;
        [SerializeField] private int queueThreshold = 4;         
        [SerializeField] private float hysteresisSeconds = 3f;

        private static readonly GuestNeedType[] AllNeeds =
            (GuestNeedType[])Enum.GetValues(typeof(GuestNeedType));

        private readonly Dictionary<GuestNeedType, NeedStatus> statuses =
            new Dictionary<GuestNeedType, NeedStatus>();
        private readonly int[] queue = new int[AllNeeds.Length];
        private readonly float[] growingSince = new float[AllNeeds.Length];

        public event Action<GuestNeedType, NeedStatus> StatusChanged;

        public NeedStatus GetStatus(GuestNeedType need)
        {
            return statuses.TryGetValue(need, out var s) ? s : NeedStatus.Normal;
        }

        private void Start()
        {
            ResetAll();
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

        private void ResetAll()
        {
            Array.Clear(queue, 0, queue.Length);
            for (int i = 0; i < growingSince.Length; i++) growingSince[i] = -1f;
            foreach (GuestNeedType n in AllNeeds) SetStatus(n, NeedStatus.Normal);
        }

        private void HandleStateChanged(ResortState state)
        {
            if (state == ResortState.WAVE_INCOMING) ResetAll();
        }

        private void HandleQueueChanged(GuestNeedType need, int queued)
        {
            int i = (int)need;
            if (queued > queue[i])
            {
                if (growingSince[i] < 0f) growingSince[i] = Time.time;
            }
            else if (queued < queue[i])
            {
                growingSince[i] = -1f;
                if (GetStatus(need) == NeedStatus.BOTTLENECK)
                    SetStatus(need, NeedStatus.RECOVERING);
            }
            queue[i] = queued;

            if (GetStatus(need) == NeedStatus.RECOVERING && queued <= queueThreshold)
                SetStatus(need, NeedStatus.Normal);
        }

        private void Update()
        {
            foreach (GuestNeedType need in AllNeeds)
            {
                int i = (int)need;
                if (GetStatus(need) == NeedStatus.BOTTLENECK) continue;
                if (queue[i] > queueThreshold && growingSince[i] >= 0f &&
                    Time.time - growingSince[i] >= hysteresisSeconds)
                    SetStatus(need, NeedStatus.BOTTLENECK);
            }
        }

        private void SetStatus(GuestNeedType need, NeedStatus status)
        {
            if (GetStatus(need) == status) return;
            statuses[need] = status;
            Debug.Log($"[BottleneckDetector] {need}: {status} (queue {queue[(int)need]})");
            StatusChanged?.Invoke(need, status);
        }
    }
}