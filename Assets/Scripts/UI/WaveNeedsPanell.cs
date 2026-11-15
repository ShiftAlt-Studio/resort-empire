using System.Collections.Generic;
using UnityEngine;

namespace ResortEmpire.Core
{
    public class WaveNeedsPanel : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private NeedAggregator needAggregator;
        [SerializeField] private NeedStatusResolver resolver;
        [SerializeField] private List<NeedRowView> rows = new List<NeedRowView>();

        private void Start()
        {
            if (needAggregator == null) needAggregator = FindAnyObjectByType<NeedAggregator>();
            if (resolver == null) resolver = FindAnyObjectByType<NeedStatusResolver>();

            if (needAggregator != null) needAggregator.NeedChanged += HandleNeedChanged;
            if (resolver != null) resolver.StatusChanged += HandleStatusChanged;

            if (GameState.Instance != null)
            {
                GameState.Instance.OnStateChanged += HandleStateChanged;
                ApplyVisibility(GameState.Instance.CurrentState);
            }

            RefreshAll();
        }

        private void OnDestroy()
        {
            if (needAggregator != null) needAggregator.NeedChanged -= HandleNeedChanged;
            if (resolver != null) resolver.StatusChanged -= HandleStatusChanged;
            if (GameState.Instance != null) GameState.Instance.OnStateChanged -= HandleStateChanged;
        }

        private void HandleNeedChanged(GuestNeedType need) => RefreshRow(need);
        private void HandleStatusChanged(GuestNeedType need, NeedDisplayStatus status) => RefreshRow(need);

        private void HandleStateChanged(ResortState state)
        {
            ApplyVisibility(state);
            RefreshAll();
        }

        private void ApplyVisibility(ResortState state)
        {
            bool inWave = state == ResortState.WAVE_INCOMING
                       || state == ResortState.WAVE_ACTIVE
                       || state == ResortState.WAVE_PAUSED
                       || state == ResortState.WAVE_RESOLVING;
            panelRoot.SetActive(inWave);
        }

        private void RefreshAll()
        {
            foreach (NeedRowView row in rows) RefreshRow(row.Need);
        }

        private void RefreshRow(GuestNeedType need)
        {
            if (needAggregator == null || resolver == null || GameState.Instance == null) return;

            foreach (NeedRowView row in rows)
            {
                if (row.Need != need) continue;
                row.Refresh(needAggregator.GetServed(need), TotalFor(need), resolver.GetStatus(need));
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