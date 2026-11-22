using System;
using System.Collections.Generic;
using UnityEngine;

namespace ResortEmpire.Core
{
    public enum SpecialistHireResult
    {
        Success,
        WaveInProgress,
        AlreadyHired,
        NotEnoughCoins
    }

    public class SpecialistService : MonoBehaviour
    {
        [Serializable]
        private class SaveData
        {
            public List<SpecialistType> hired = new List<SpecialistType>();
        }

        private const string SaveKey = "ResortEmpire.Specialists.v1";

        private readonly HashSet<SpecialistType> hired = new HashSet<SpecialistType>();

        public event Action<SpecialistType> SpecialistHired;

        public int HiredCount => hired.Count;

        public bool IsHireWindowOpen =>
            GameState.Instance != null && GameState.Instance.CurrentState == ResortState.AWAITING_WAVE;

        private void Start()
        {
            Load();
        }

        public bool IsHired(SpecialistType type) => hired.Contains(type);

        public int GetCost(SpecialistType type) => SpecialistTable.GetCost(type);

        public bool CanAfford(SpecialistType type) =>
            GameState.Instance != null && GameState.Instance.Coins >= GetCost(type);

        public SpecialistHireResult TryHire(SpecialistType type)
        {
            if (!IsHireWindowOpen) return SpecialistHireResult.WaveInProgress;
            if (hired.Contains(type)) return SpecialistHireResult.AlreadyHired;
            if (!GameState.Instance.TrySpendCoins(GetCost(type))) return SpecialistHireResult.NotEnoughCoins;

            hired.Add(type);
            Save();
            Debug.Log($"[SpecialistService] Hired {SpecialistTable.GetDisplayName(type)}");
            SpecialistHired?.Invoke(type);
            return SpecialistHireResult.Success;
        }

        public List<SpecialistType> GetHiredSnapshot() => new List<SpecialistType>(hired);

        private void Save()
        {
            var data = new SaveData { hired = GetHiredSnapshot() };
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }

        private void Load()
        {
            if (!PlayerPrefs.HasKey(SaveKey)) return;

            SaveData data = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(SaveKey, string.Empty));
            if (data == null || data.hired == null) return;

            hired.Clear();
            foreach (SpecialistType t in data.hired)
                hired.Add(t);
        }
    }
}