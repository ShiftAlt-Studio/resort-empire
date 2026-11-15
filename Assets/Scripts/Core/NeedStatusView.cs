using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace ResortEmpire.Core
{
    public class NeedStatusView : MonoBehaviour
    {
        [Serializable]
        public class Entry
        {
            public GuestNeedType need;
            public TMP_Text label;
        }

        [SerializeField] private NeedStatusResolver resolver;
        [SerializeField] private List<Entry> entries = new List<Entry>();

        private void Start()
        {
            if (resolver == null)
                resolver = FindAnyObjectByType<NeedStatusResolver>();
            if (resolver == null) return;

            resolver.StatusChanged += HandleStatusChanged;
            foreach (Entry e in entries)
                Apply(e, resolver.GetStatus(e.need));
        }

        private void OnDestroy()
        {
            if (resolver != null)
                resolver.StatusChanged -= HandleStatusChanged;
        }

        private void HandleStatusChanged(GuestNeedType need, NeedDisplayStatus status)
        {
            foreach (Entry e in entries)
                if (e.need == need) Apply(e, status);
        }

       private static void Apply(Entry e, NeedDisplayStatus status)
       {
        if (e.label == null) return;
        string hex = ColorUtility.ToHtmlStringRGB(ColorFor(status));
        e.label.color = Color.white;
        e.label.text = $"{e.need}: <color=#{hex}>{status}</color>";
        }

        private static Color ColorFor(NeedDisplayStatus status)
        {
            switch (status)
            {
                case NeedDisplayStatus.OK: return new Color(0.3f, 0.8f, 0.3f);
                case NeedDisplayStatus.BOTTLENECK: return new Color(0.9f, 0.2f, 0.2f);
                case NeedDisplayStatus.RECOVERING: return new Color(0.95f, 0.65f, 0.1f);
                case NeedDisplayStatus.SATISFIED: return new Color(0.3f, 0.6f, 1f);
                default: return new Color(0.6f, 0.6f, 0.6f);
            }
        }
    }
}