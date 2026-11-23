using System;
using UnityEngine;

public class Building : MonoBehaviour
{
    public const int MaxLevel = 5;

    [SerializeField] private BuildingType buildingType;
    [SerializeField] private GuestNeedType servedNeed;

    [Header("Level (Build Doc 4.8, throwaway cisla)")]
    [SerializeField, Range(1, MaxLevel)] private int level = 1;
    [SerializeField] private int[] seatsByLevel = { 3, 5, 7, 7, 10 };
    [SerializeField] private int[] upgradeCosts = { 500, 1200, 2000, 3500 };

    [Header("Service quality (SS-86, tuning)")]
    [SerializeField] private int[] baseQualityByLevel = { 0, 0, 0, 5, 10 };

    [Header("Worker slots (SS-85)")]
    [SerializeField] private int[] workerSlotsByLevel = { 1, 1, 1, 2, 3 };

    [Header("Manager (SS-126/127, throwaway cisla)")]
    [SerializeField] private float managerSpeedBonus = 0.2f;

    public event Action<Building, int> LevelChanged;
    public event Action<Building, bool> ManagerChanged;

    public BuildingType Type => buildingType;
    public GuestNeedType ServedNeed => servedNeed;

    public bool HasManager { get; private set; }
    public float ServiceSpeedMultiplier => HasManager ? 1f + managerSpeedBonus : 1f;

    public int Level => level;
    public bool IsMaxLevel => level >= MaxLevel;

    public int WorkerSlots => workerSlotsByLevel != null && workerSlotsByLevel.Length >= level
        ? Mathf.Max(1, workerSlotsByLevel[level - 1])
        : (level >= 5 ? 3 : level >= 4 ? 2 : 1);

    public int BaseQuality => baseQualityByLevel != null && baseQualityByLevel.Length >= level
        ? Mathf.Max(0, baseQualityByLevel[level - 1])
        : (level >= 5 ? 10 : level >= 4 ? 5 : 0);
    public int NextBaseQuality => IsMaxLevel ? BaseQuality : QualityAt(level + 1);
    private int QualityAt(int lvl) => baseQualityByLevel[Mathf.Clamp(lvl, 1, MaxLevel) - 1];

    public int Capacity => SeatsAt(level);
    public int NextCapacity => IsMaxLevel ? Capacity : SeatsAt(level + 1);
    public int NextUpgradeCost => IsMaxLevel ? 0 : upgradeCosts[level - 1];

    private int SeatsAt(int lvl) => seatsByLevel[Mathf.Clamp(lvl, 1, MaxLevel) - 1];

    public void SetLevel(int newLevel)
    {
        newLevel = Mathf.Clamp(newLevel, 1, MaxLevel);
        if (newLevel == level) return;
        level = newLevel;
        LevelChanged?.Invoke(this, level);
    }

    public void SetManager(bool value)
    {
        if (HasManager == value) return;
        HasManager = value;
        ManagerChanged?.Invoke(this, value);
    }

    private void OnValidate()
    {
        if (seatsByLevel == null || seatsByLevel.Length != MaxLevel)
            Array.Resize(ref seatsByLevel, MaxLevel);
        if (baseQualityByLevel == null || baseQualityByLevel.Length != MaxLevel)
            baseQualityByLevel = new[] { 0, 0, 0, 5, 10 };
        if (workerSlotsByLevel == null || workerSlotsByLevel.Length != MaxLevel)
            workerSlotsByLevel = new[] { 1, 1, 1, 2, 3 };
        if (upgradeCosts == null || upgradeCosts.Length != MaxLevel - 1)
            Array.Resize(ref upgradeCosts, MaxLevel - 1);
    }
}