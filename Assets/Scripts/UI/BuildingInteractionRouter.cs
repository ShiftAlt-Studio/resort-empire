using UnityEngine;
using ResortEmpire.Core;

namespace ResortEmpire.UI
{
    public class BuildingInteractionRouter : MonoBehaviour
    {
        [SerializeField] private BuildingActionMenu actionMenu;
        [SerializeField] private UpgradePanel upgradePanel;
        [SerializeField] private WorkerAllocationPanel workerPanel;

        private void Awake()
        {
            actionMenu.UpgradeChosen += b => upgradePanel.Open(b);
            actionMenu.MoveWorkerChosen += b => workerPanel.Open(b);
        }

        private bool AnyPanelOpen =>
            actionMenu.gameObject.activeSelf ||
            upgradePanel.gameObject.activeSelf ||
            workerPanel.gameObject.activeSelf;

        public void OnBuildingTapped(Building building)
        {
            if (GameState.Instance == null || building == null || AnyPanelOpen)
                return;

            switch (GameState.Instance.CurrentState)
            {
                case ResortState.AWAITING_WAVE:
                    actionMenu.Open(building);
                    break;

                case ResortState.WAVE_INCOMING:
                case ResortState.WAVE_ACTIVE:
                case ResortState.WAVE_PAUSED:
                case ResortState.WAVE_RESOLVING:
                    workerPanel.Open(building);
                    break;

            }
        }
    }
}