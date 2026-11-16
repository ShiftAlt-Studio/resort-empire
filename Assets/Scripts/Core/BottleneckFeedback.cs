using System;
using UnityEngine;

namespace ResortEmpire.Core
{
    public enum BottleneckCause { MissingWorker, LowCapacity }

    public class BottleneckFeedback : MonoBehaviour
    {
        private BottleneckDetector detector;
        private WorkerAllocation workers;
        private BuildingRegistry registry;

        public event Action<GuestNeedType, NeedStatus, string> FeedbackRaised;

        private void Start()
        {
            detector = FindAnyObjectByType<BottleneckDetector>();
            workers = FindAnyObjectByType<WorkerAllocation>();
            registry = FindAnyObjectByType<BuildingRegistry>();
            if (detector != null) detector.StatusChanged += HandleStatusChanged;
        }

        private void OnDestroy()
        {
            if (detector != null) detector.StatusChanged -= HandleStatusChanged;
        }

        public BottleneckCause GetCause(GuestNeedType need)
            => workers != null && !workers.IsStaffed(need)
                ? BottleneckCause.MissingWorker
                : BottleneckCause.LowCapacity;

        public string GetCauseText(GuestNeedType need)
        {
            if (GetCause(need) == BottleneckCause.MissingWorker) return "No worker assigned";
            Building b = registry != null ? registry.GetBuildingForNeed(need) : null;
            string name = b != null ? b.Type.ToString() : need.ToString();
            return $"{name} capacity too low";
        }

        private void HandleStatusChanged(GuestNeedType need, NeedStatus status)
        {
            string message;
            if (status == NeedStatus.BOTTLENECK) message = $"{need}: queue is growing. {GetCauseText(need)}";
            else if (status == NeedStatus.RECOVERING) message = $"{need}: recovering. {GetCauseText(need)}";
            else return;
            FeedbackRaised?.Invoke(need, status, message);
        }
    }
}