using UnityEngine;

public class Building : MonoBehaviour
{
    [SerializeField] private BuildingType buildingType;
    [SerializeField] private GuestNeedType servedNeed;

    public BuildingType Type => buildingType;
    public GuestNeedType ServedNeed => servedNeed;
}