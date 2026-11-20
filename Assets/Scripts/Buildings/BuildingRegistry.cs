using System.Collections.Generic;
using UnityEngine;

public class BuildingRegistry : MonoBehaviour
{
    [SerializeField] private List<Building> buildings = new();

    public IReadOnlyList<Building> AllBuildings => buildings;

    public Building GetBuildingForNeed(GuestNeedType need)
    {
        return buildings.Find(b => b.ServedNeed == need);
    }

    public Building GetBuildingByType(BuildingType type)
    {
        return buildings.Find(b => b.Type == type);
    }

    public void Register(Building building)
    {
        if (building == null || buildings.Contains(building)) return;
        buildings.Add(building);
        Debug.Log($"[BuildingRegistry] Registered {building.Type} for {building.ServedNeed}");
    }

    private void Start()
    {
        foreach (GuestNeedType need in System.Enum.GetValues(typeof(GuestNeedType)))
        {
            var b = GetBuildingForNeed(need);
            Debug.Log($"{need} -> {(b != null ? b.Type.ToString() : "NOT FOUND")}");
        }
    }
}