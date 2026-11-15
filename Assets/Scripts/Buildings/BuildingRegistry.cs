using System.Collections.Generic;
using UnityEngine;

public class BuildingRegistry : MonoBehaviour
{
    [SerializeField] private List<Building> buildings = new();

    public Building GetBuildingForNeed(GuestNeedType need)
    {
        return buildings.Find(b => b.ServedNeed == need);
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