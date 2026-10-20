using System;
using System.Collections.Generic;
using UnityEngine;

namespace ResortEmpire.Core
{
    public class ArrivalPhase : MonoBehaviour
    {
        [SerializeField] private float duration = 90f;

        public event Action<GuestNeedType, int> DemandAdded;
        public event Action ArrivalFinished;

        public bool IsRunning { get; private set; }

        private float elapsed;
        private List<NeedDemand> forecast;
        private int[] added;

        private void Start()
        {
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
                StartArrival();
        }

        private void StartArrival()
        {
            forecast = GameState.Instance.GetForecast();
            added = new int[forecast.Count];
            elapsed = 0f;
            IsRunning = true;
            Debug.Log("Arrival phase started");
        }

        private void Update()
        {
            if (!IsRunning) return;

            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);

            for (int i = 0; i < forecast.Count; i++)
            {
                int target = Mathf.FloorToInt(forecast[i].Units * progress);
                int diff = target - added[i];
                if (diff <= 0) continue;

                added[i] = target;
                DemandAdded?.Invoke(forecast[i].Need, diff);
                Debug.Log($"+{diff} {forecast[i].Need} ({added[i]}/{forecast[i].Units})");
            }

            if (progress >= 1f)
            {
                IsRunning = false;
                Debug.Log("Arrival phase finished");
                ArrivalFinished?.Invoke();
                GameState.Instance.FinishArrival();
            }
        }
    }
}