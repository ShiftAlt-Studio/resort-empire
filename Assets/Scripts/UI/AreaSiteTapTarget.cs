using UnityEngine;
using UnityEngine.EventSystems;
using ResortEmpire.Core;

namespace ResortEmpire.UI
{
    public class AreaSiteTapTarget : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private AreaType area;
        [SerializeField] private AreaUnlockController controller;

        private void Awake()
        {
            if (controller == null)
                controller = FindAnyObjectByType<AreaUnlockController>();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (controller != null)
                controller.Open(area);
        }
    }
}