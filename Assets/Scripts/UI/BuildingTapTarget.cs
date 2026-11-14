using UnityEngine;
using UnityEngine.EventSystems;

namespace ResortEmpire.UI
{
    public class BuildingTapTarget : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private Building building;
        [SerializeField] private BuildingInteractionRouter router;

        private void Awake()
        {
            if (building == null)
                building = GetComponent<Building>();
            if (router == null)
                router = FindAnyObjectByType<BuildingInteractionRouter>();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (router != null && building != null)
                router.OnBuildingTapped(building);
        }
    }
}