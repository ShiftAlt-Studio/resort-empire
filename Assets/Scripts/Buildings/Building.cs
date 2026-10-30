using System;
using UnityEngine;

public class Building : MonoBehaviour
{
    public const int MaxLevel = 3;

    [SerializeField] private BuildingType buildingType;
    [SerializeField] private GuestNeedType servedNeed;

    [Header("Level (Build Doc 4.8, throwaway cisla)")]
    [SerializeField, Range(1, MaxLevel)] private int level = 1;
    [SerializeField] private int[] seatsByLevel = { 3, 5, 7 };
    [SerializeField] private int[] upgradeCosts = { 500, 1200 };

    public event Action<Building, int> LevelChanged;

    public BuildingType Type => buildingType;
    public GuestNeedType ServedNeed => servedNeed;

    public int Level => level;
    public bool IsMaxLevel => level >= MaxLevel;

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

    private void OnValidate()
    {
        if (seatsByLevel == null || seatsByLevel.Length != MaxLevel)
            Array.Resize(ref seatsByLevel, MaxLevel);
        if (upgradeCosts == null || upgradeCosts.Length != MaxLevel - 1)
            Array.Resize(ref upgradeCosts, MaxLevel - 1);
    }
}