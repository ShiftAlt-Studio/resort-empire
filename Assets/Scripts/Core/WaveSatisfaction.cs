using System;
using TMPro;
using UnityEngine;

namespace ResortEmpire.Core
{
    public class WaveSatisfaction : MonoBehaviour
    {
        [SerializeField] private NeedAggregator needAggregator;
        [Header("GDD §4.4 (TUNING)")]
        [SerializeField] private float patienceGrace = 10f;
        [SerializeField] private float patienceFloorAfter = 30f;
        [SerializeField, Range(0f, 1f)] private float unitFloor = 0.25f;
        [SerializeField] private float waveFloor = 40f;
        [SerializeField] private float oceanViewWeight = 1.5f;
        [SerializeField] private TMP_Text resultLabel;

        private static readonly GuestNeedType[] AllNeeds =
            (GuestNeedType[])Enum.GetValues(typeof(GuestNeedType));

        private float totalWaitUnitSeconds;
        private float weightedSum;
        private float weightTotal;
        private bool tracking;

        public float LastResult { get; private set; }
        public float TotalWaitUnitSeconds => totalWaitUnitSeconds;
        public bool IsTracking => tracking;

        public event Action<float> ResultReady;

        private void Start()
        {
            if (needAggregator == null)
                needAggregator = FindAnyObjectByType<NeedAggregator>();

            if (needAggregator != null)
                needAggregator.UnitSeated += HandleUnitSeated;

            if (GameState.Instance != null)
                GameState.Instance.OnStateChanged += HandleStateChanged;

            if (resultLabel != null)
                resultLabel.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (needAggregator != null)
                needAggregator.UnitSeated -= HandleUnitSeated;

            if (GameState.Instance != null)
                GameState.Instance.OnStateChanged -= HandleStateChanged;
        }

        private void HandleStateChanged(ResortState state)
        {
            if (state == ResortState.WAVE_INCOMING)
            {
                if (GameState.Instance != null && GameState.Instance.IsResumingFromPause)
                    return;

                totalWaitUnitSeconds = 0f;
                weightedSum = 0f;
                weightTotal = 0f;
                tracking = true;
                if (resultLabel != null)
                    resultLabel.gameObject.SetActive(false);
            }
            else if (state == ResortState.WAVE_COMPLETE && tracking)
            {
                tracking = false;
                Finish();
            }
        }
        
        private void HandleUnitSeated(GuestNeedType need, float waitSeconds)
        {
            if (!tracking) return;
            float weight = need == GuestNeedType.OceanView ? oceanViewWeight : 1f;
            weightedSum += Contribution(waitSeconds) * weight;
            weightTotal += weight;
        }

        private float Contribution(float wait)
        {
            if (wait <= patienceGrace) return 1f;
            if (wait >= patienceFloorAfter) return unitFloor;
            float t = (wait - patienceGrace) / (patienceFloorAfter - patienceGrace);
            return Mathf.Lerp(1f, unitFloor, t);
        }

        private void Update()
        {
            if (!tracking || needAggregator == null) return;
            if (GameState.Instance != null && GameState.Instance.CurrentState == ResortState.WAVE_PAUSED) return;

            foreach (GuestNeedType need in AllNeeds)
                totalWaitUnitSeconds += needAggregator.GetWaiting(need) * Time.deltaTime;
        }

        public void RestoreState(float savedWaitSeconds, bool savedTracking)
        {
            totalWaitUnitSeconds = Mathf.Max(0f, savedWaitSeconds);
            tracking = savedTracking;
        }

        private void Finish()
        {
            int servedTotal = 0;
            foreach (GuestNeedType need in AllNeeds)
                servedTotal += needAggregator.GetServed(need);

            float raw = weightTotal > 0f ? weightedSum / weightTotal : 1f;
            LastResult = Mathf.Max(waveFloor, raw * 100f);
            float avgWait = servedTotal > 0 ? totalWaitUnitSeconds / servedTotal : 0f;

            Debug.Log($"[WaveSatisfaction] avg wait {avgWait:F1}s, satisfaction {LastResult:F0}%");
            ResultReady?.Invoke(LastResult);

            if (resultLabel != null)
            {
                resultLabel.text = $"Wave Satisfaction: {LastResult:F0}%";
                resultLabel.gameObject.SetActive(true);
            }
        }
    }
}