using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace ResortEmpire.Core
{
    public class NeedProgressView : MonoBehaviour
    {
        [Serializable]
        public class Entry
        {
            public GuestNeedType need;
            public TMP_Text label;
        }

        [SerializeField] private NeedAggregator needAggregator;
        [SerializeField] private List<Entry> entries = new List<Entry>();

        private void Start()
        {
            if (needAggregator == null)
                needAggregator = FindAnyObjectByType<NeedAggregator>();
            if (needAggregator != null)
                needAggregator.NeedChanged += HandleNeedChanged;

            if (GameState.Instance != null)
                GameState.Instance.OnStateChanged += HandleStateChanged;

            RefreshAll();
        }

        private void OnDestroy()
        {
            if (needAggregator != null)
                needAggregator.NeedChanged -= HandleNeedChanged;
            if (GameState.Instance != null)
                GameState.Instance.OnStateChanged -= HandleStateChanged;
        }

        private void HandleNeedChanged(GuestNeedType need) => RefreshAll();
        private void HandleStateChanged(ResortState state) => RefreshAll();

        private void RefreshAll()
        {
            if (needAggregator == null || GameState.Instance == null) return;

            foreach (Entry e in entries)
            {
                if (e.label == null) continue;

                int total = TotalFor(e.need);
                bool active = total > 0;
                e.label.gameObject.SetActive(active);
                if (!active) continue;

                e.label.text = $"{e.need}: {needAggregator.GetServed(e.need)} / {total}";
            }
        }

        private static int TotalFor(GuestNeedType need)
        {
            foreach (NeedDemand d in GameState.Instance.GetForecast())
                if (d.Need == need) return d.Units;
            return 0;
        }
    }
}