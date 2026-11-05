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
        public bool IsFinished { get; private set; }
        public float Elapsed => elapsed;

        private float elapsed;
        private List<NeedDemand> forecast;
        private int[] added;

        private void Start()
        {
            if (GameState.Instance != null)
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
            {
                if (GameState.Instance != null && GameState.Instance.IsResumingFromPause)
                    return;

                StartArrival();
            }
        }

        private void StartArrival()
        {
            forecast = GameState.Instance.GetForecast();
            added = new int[forecast.Count];
            elapsed = 0f;
            IsFinished = false;
            IsRunning = true;
            Debug.Log("Arrival phase started");
        }

        private void Update()
        {
            if (!IsRunning || IsFinished) return;
            if (GameState.Instance != null && GameState.Instance.CurrentState == ResortState.WAVE_PAUSED) return;

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
                StopDemandGeneration();
            }
        }

        public int[] GetAddedSnapshot() => added != null ? (int[])added.Clone() : Array.Empty<int>();

        public void RestoreState(float savedElapsed, bool running, bool finished, int[] savedAdded)
        {
            forecast = GameState.Instance != null ? GameState.Instance.GetForecast() : new List<NeedDemand>();
            added = new int[forecast.Count];
            if (savedAdded != null)
            {
                int copyLen = Mathf.Min(added.Length, savedAdded.Length);
                Array.Copy(savedAdded, added, copyLen);
            }
            elapsed = Mathf.Clamp(savedElapsed, 0f, duration);
            IsRunning = running;
            IsFinished = finished;
        }

        private void StopDemandGeneration()
        {
            if (IsFinished) return;

            IsRunning = false;
            IsFinished = true;
            Debug.Log("Arrival phase finished — demand generation stopped");
            ArrivalFinished?.Invoke();

            if (GameState.Instance != null)
                GameState.Instance.FinishArrival();
        }
    }
}