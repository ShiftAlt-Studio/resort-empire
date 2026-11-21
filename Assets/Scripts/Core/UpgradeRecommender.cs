using System;
using UnityEngine;

namespace ResortEmpire.Core
{
    public class UpgradeRecommender : MonoBehaviour
    {
        [SerializeField] private float minBottleneckSeconds = 1f;

        private BottleneckDetector detector;
        private BuildingRegistry registry;
        private readonly float[] seconds = new float[Enum.GetValues(typeof(GuestNeedType)).Length];

        public bool HasRecommendation { get; private set; }
        public Building RecommendedBuilding { get; private set; }
        public GuestNeedType RecommendedNeed { get; private set; }
        public float RecommendedSeconds { get; private set; }
        public event Action RecommendationReady;

        private void Start()
        {
            detector = FindAnyObjectByType<BottleneckDetector>();
            registry = FindAnyObjectByType<BuildingRegistry>();
            if (GameState.Instance != null)
                GameState.Instance.OnStateChanged += HandleStateChanged;
        }

        private void OnDestroy()
        {
            if (GameState.Instance != null)
                GameState.Instance.OnStateChanged -= HandleStateChanged;
        }

        private void Update()
        {
            var s = GameState.Instance;
            if (s == null || detector == null) return;
            if (s.CurrentState != ResortState.WAVE_INCOMING &&
                s.CurrentState != ResortState.WAVE_ACTIVE) return;

            foreach (GuestNeedType need in Enum.GetValues(typeof(GuestNeedType)))
                if (detector.GetStatus(need) == NeedStatus.BOTTLENECK)
                    seconds[(int)need] += Time.deltaTime;
        }

        private void HandleStateChanged(ResortState state)
        {
            if (state == ResortState.WAVE_INCOMING)
            {
                if (GameState.Instance.IsResumingFromPause) return;
                Array.Clear(seconds, 0, seconds.Length);
                HasRecommendation = false;
                RecommendedBuilding = null;
            }
            else if (state == ResortState.WAVE_COMPLETE)
            {
                Evaluate();
            }
        }

        private void Evaluate()
        {
            HasRecommendation = false;
            RecommendedBuilding = null;
            float best = minBottleneckSeconds;

            foreach (GuestNeedType need in Enum.GetValues(typeof(GuestNeedType)))
            {
                float t = seconds[(int)need];
                if (t <= best) continue;
                Building b = registry.GetBuildingForNeed(need);
                if (b == null || b.IsMaxLevel) continue;
                best = t;
                RecommendedNeed = need;
                RecommendedBuilding = b;
                RecommendedSeconds = t;
                HasRecommendation = true;
            }
            if (HasRecommendation) RecommendationReady?.Invoke();
        }
        
        public void Consume() => HasRecommendation = false;
    }
}