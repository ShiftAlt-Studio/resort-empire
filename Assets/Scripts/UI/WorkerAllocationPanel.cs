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
        private GuestNeedType? selectedSource;

        private void Awake()
        {
            if (rows != null)
                foreach (WorkerRow row in rows)
                {
                    WorkerRow r = row;
                    if (r != null && r.button != null)
                        r.button.onClick.AddListener(() => OnRowClicked(r));
                }
            if (closeButton != null)
                closeButton.onClick.AddListener(Close);
            if (speedUpButton != null)
                speedUpButton.onClick.AddListener(OnSpeedUpClicked);
        }

        private void OnEnable()
        {
            if (allocation != null) allocation.WorkerMoved += HandleWorkerMoved;
            if (GameState.Instance != null)
                GameState.Instance.OnStateChanged += HandleStateChanged;
            if (speedUpService != null)
                speedUpService.SpeedUpChanged += HandleSpeedUpChanged;
            RefreshSpeedUp();
        }

        private void OnDisable()
        {
            if (allocation != null) allocation.WorkerMoved -= HandleWorkerMoved;
            if (GameState.Instance != null)
                GameState.Instance.OnStateChanged -= HandleStateChanged;
            if (speedUpService != null)
                speedUpService.SpeedUpChanged -= HandleSpeedUpChanged;
        }

        public void Open(Building focus)
        {
            if (focus == null) return;
            focusNeed = focus.ServedNeed;
            selectedSource = null;
            if (titleText != null) titleText.text = $"WORKERS - {focus.Type.ToString().ToUpper()}";
            Refresh();
            gameObject.SetActive(true);
        }

        public void Close() => gameObject.SetActive(false);

        private void Refresh()
        {
            if (allocation == null || buildingRegistry == null || rows == null) return;

            if (statusText != null)
            {
                if (selectedSource.HasValue)
                {
                    Building source = buildingRegistry.GetBuildingForNeed(selectedSource.Value);
                    string sourceName = source != null ? source.Type.ToString() : selectedSource.Value.ToString();
                    statusText.text = $"Moving 1 Worker from {sourceName}. Select a building with a free slot, or tap {sourceName} again to cancel.";
                }
                else
                {
                    statusText.text = "Select a building with Workers (TAKE WORKER), then select a destination with a free slot (SEND HERE).";
                }
            }

            foreach (WorkerRow row in rows)
            {
                if (row == null) continue;
                Building b = buildingRegistry.GetBuildingForNeed(row.need);
                bool exists = b != null;
                GameObject root = row.card != null ? row.card : (row.button != null ? row.button.gameObject : null);
                if (root != null) root.SetActive(exists);
                if (!exists) continue;

                int count = allocation.GetWorkerCount(row.need);
                int free = allocation.GetFreeSlots(row.need);
                bool isFocus = row.need == focusNeed;
                bool isSource = selectedSource.HasValue && selectedSource.Value == row.need;
                bool selectingDestination = selectedSource.HasValue && !isSource;

                if (row.focusMarker != null) row.focusMarker.SetActive(isSource || (isFocus && !selectedSource.HasValue));
                if (row.label != null)
                {
                    string prefix = isSource ? "> " : (isFocus && row.focusMarker == null ? "> " : "");
                    row.label.text = $"{prefix}{b.Type}";
                }
                if (row.workerIcon != null)
                {
                    row.workerIcon.SetActive(count > 0);
                    TMP_Text workerBadgeText = row.workerIcon.GetComponentInChildren<TMP_Text>(true);
                    if (workerBadgeText != null)
                        workerBadgeText.text = $"WORKERS {count}/{b.WorkerSlots}";
                }
                if (row.emptyBadge != null)
                {
                    row.emptyBadge.SetActive(count == 0);
                    TMP_Text emptyBadgeText = row.emptyBadge.GetComponentInChildren<TMP_Text>(true);
                    if (emptyBadgeText != null)
                        emptyBadgeText.text = $"EMPTY {count}/{b.WorkerSlots}";
                }
                if (row.fade != null) row.fade.alpha = (count > 0 || (selectingDestination && free > 0)) ? 1f : 0.6f;

                if (row.button != null)
                {
                    bool enabled = selectedSource.HasValue ? (isSource || free > 0 || count > 0) : count > 0;
                    row.button.gameObject.SetActive(true);
                    row.button.interactable = enabled;
                    TMP_Text buttonLabel = row.button.GetComponentInChildren<TMP_Text>(true);
                    if (buttonLabel != null)
                    {
                        buttonLabel.text = isSource ? "CANCEL" :
                            selectingDestination && free > 0 ? "SEND HERE" :
                            count > 0 ? "TAKE WORKER" : "FULL";
                    }
                }
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
            if (row == null || allocation == null || buildingRegistry == null) return;
            if (buildingRegistry.GetBuildingForNeed(row.need) == null) return;

            if (!selectedSource.HasValue)
            {
                if (allocation.GetWorkerCount(row.need) <= 0) return;
                selectedSource = row.need;
                Refresh();
                return;
            }

            GuestNeedType from = selectedSource.Value;
            if (row.need == from)
            {
                selectedSource = null;
                Refresh();
                return;
            }

            if (allocation.GetFreeSlots(row.need) <= 0)
            {
                if (allocation.GetWorkerCount(row.need) > 0)
                    selectedSource = row.need;
                Refresh();
                return;
            }

            MoveResult result = allocation.TryMoveWorker(from, row.need);
            if (result != MoveResult.Success)
                Debug.Log($"[WorkerAllocationPanel] Move refused: {result}");
            else
                selectedSource = null;
            Refresh();
        }

        private void HandleWorkerMoved(GuestNeedType from, GuestNeedType to) => Refresh();

        private void HandleStateChanged(ResortState state)
        {
            if (state == ResortState.WAVE_COMPLETE || state == ResortState.UPGRADE)
                Close();
        }
    }
}