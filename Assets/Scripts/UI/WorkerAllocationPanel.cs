using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ResortEmpire.Core;

namespace ResortEmpire.UI
{
    [Serializable]
    public class WorkerRow
    {
        public GuestNeedType need;
        public GameObject card;
        public Button button;
        public TMP_Text label;
        public GameObject workerIcon;
        public GameObject emptyBadge;
        public CanvasGroup fade;
        public GameObject focusMarker;
    }

    public class WorkerAllocationPanel : MonoBehaviour
    {
        [SerializeField] private WorkerAllocation allocation;
        [SerializeField] private BuildingRegistry buildingRegistry;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private Button closeButton;
        [SerializeField] private WorkerRow[] rows;
        [Header("Speed Up (SS-130)")]
        [SerializeField] private SpeedUpService speedUpService;
        [SerializeField] private Button speedUpButton;
        [SerializeField] private TMP_Text speedUpLabel;

        private GuestNeedType focusNeed;

        private void Awake()
        {
            foreach (WorkerRow row in rows)
            {
                WorkerRow r = row;
                r.button.onClick.AddListener(() => OnRowClicked(r));
            }
            if (closeButton != null)
                closeButton.onClick.AddListener(Close);
            if (speedUpButton != null)
                speedUpButton.onClick.AddListener(OnSpeedUpClicked);
        }

        private void OnEnable()
        {
            allocation.WorkerMoved += HandleWorkerMoved;
            if (GameState.Instance != null)
                GameState.Instance.OnStateChanged += HandleStateChanged;
            if (speedUpService != null)
                speedUpService.SpeedUpChanged += HandleSpeedUpChanged;
            RefreshSpeedUp();
        }

        private void OnDisable()
        {
            allocation.WorkerMoved -= HandleWorkerMoved;
            if (GameState.Instance != null)
                GameState.Instance.OnStateChanged -= HandleStateChanged;
            if (speedUpService != null)
                speedUpService.SpeedUpChanged -= HandleSpeedUpChanged;
        }

        public void Open(Building focus)
        {
            focusNeed = focus.ServedNeed;
            if (titleText != null) titleText.text = $"WORKERS - {focus.Type.ToString().ToUpper()}";
            Refresh();
            gameObject.SetActive(true);
        }

        public void Close() => gameObject.SetActive(false);

        private void Refresh()
        {
            if (statusText != null)
            {
                statusText.text = allocation.IsStaffed(focusNeed)
                    ? "Worker is here. A long queue means the building is too small: upgrade after the wave."
                    : "No Worker here. Move one: free and instant.";
            }

            foreach (WorkerRow row in rows)
            {
                Building b = buildingRegistry.GetBuildingForNeed(row.need);
                bool exists = b != null;
                GameObject root = row.card != null ? row.card : row.button.gameObject;
                root.SetActive(exists);
                if (!exists) continue;

                bool staffed = allocation.IsStaffed(row.need);
                bool isFocus = row.need == focusNeed;
                if (row.focusMarker != null) row.focusMarker.SetActive(isFocus);
                if (row.label != null)
                {
                    row.label.text = (isFocus && row.focusMarker == null) ? $"> {b.Type}" : b.Type.ToString();
                }
                if (row.workerIcon != null) row.workerIcon.SetActive(staffed);
                if (row.emptyBadge != null) row.emptyBadge.SetActive(!staffed);

                if (row.fade != null) row.fade.alpha = staffed ? 1f : 0.6f;

                if (row.card != null) row.button.gameObject.SetActive(staffed);
                else row.button.interactable = staffed;
            }
            RefreshSpeedUp();
        }

        private void Update()
        {
            if (speedUpService != null && speedUpService.GetState(focusNeed) == SpeedUpState.Active)
                RefreshSpeedUp();
        }

        private void RefreshSpeedUp()
        {
            if (speedUpButton == null || speedUpService == null) return;

            SpeedUpState state = speedUpService.GetState(focusNeed);
            bool visible = state != SpeedUpState.Unavailable;
            speedUpButton.gameObject.SetActive(visible);
            if (!visible) return;

            speedUpButton.interactable = state == SpeedUpState.Available;

            if (speedUpLabel == null) return;
            switch (state)
            {
                case SpeedUpState.Available:
                    speedUpLabel.text = $"Speed Up ({speedUpService.EnergyCost} Energy)";
                    break;
                case SpeedUpState.Active:
                    speedUpLabel.text = $"Speed Up {Mathf.CeilToInt(speedUpService.GetRemainingSeconds(focusNeed))}s";
                    break;
                case SpeedUpState.Used:
                    speedUpLabel.text = "Speed Up used";
                    break;
                case SpeedUpState.NotEnoughEnergy:
                    speedUpLabel.text = "Not enough Energy";
                    break;
            }
        }

        private void OnSpeedUpClicked()
        {
            SpeedUpResult result = speedUpService.TrySpeedUp(focusNeed);
            if (result != SpeedUpResult.Success)
                Debug.Log($"[WorkerAllocationPanel] Speed Up refused: {result}");
            RefreshSpeedUp();
        }

        private void HandleSpeedUpChanged(GuestNeedType need)
        {
            if (need == focusNeed) RefreshSpeedUp();
        }

        private void OnRowClicked(WorkerRow row)
        {
            MoveResult result = allocation.TryMoveWorkerFrom(row.need);
            if (result != MoveResult.Success)
                Debug.Log($"[WorkerAllocationPanel] Move refused: {result}");
        }

        private void HandleWorkerMoved(GuestNeedType from, GuestNeedType to) => Refresh();

        private void HandleStateChanged(ResortState state)
        {
            if (state == ResortState.WAVE_COMPLETE || state == ResortState.UPGRADE)
                Close();
        }
    }
}