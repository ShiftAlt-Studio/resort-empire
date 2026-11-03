using System;
using TMPro;
using UnityEngine;

namespace ResortEmpire.Core
{
    public class WaveSatisfaction : MonoBehaviour
    {
        [SerializeField] private NeedAggregator needAggregator;
        [SerializeField] private float maxAcceptableWait = 30f;
        [SerializeField] private TMP_Text resultLabel;

        private static readonly GuestNeedType[] AllNeeds =
            (GuestNeedType[])Enum.GetValues(typeof(GuestNeedType));

        private float totalWaitUnitSeconds;
        private bool tracking;

        public float LastResult { get; private set; }
        public event Action<float> ResultReady;

        private void Start()
        {
            if (needAggregator == null)
                needAggregator = FindAnyObjectByType<NeedAggregator>();

            if (GameState.Instance != null)
                GameState.Instance.OnStateChanged += HandleStateChanged;

            if (resultLabel != null)
                resultLabel.gameObject.SetActive(false);
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
                totalWaitUnitSeconds = 0f;
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

        private void Update()
        {
            if (!tracking || needAggregator == null) return;

            foreach (GuestNeedType need in AllNeeds)
                totalWaitUnitSeconds += needAggregator.GetWaiting(need) * Time.deltaTime;
        }

        private void Finish()
        {
            int servedTotal = 0;
            foreach (GuestNeedType need in AllNeeds)
                servedTotal += needAggregator.GetServed(need);

            float avgWait = servedTotal > 0 ? totalWaitUnitSeconds / servedTotal : 0f;
            LastResult = Mathf.Clamp(100f * (1f - avgWait / maxAcceptableWait), 0f, 100f);

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