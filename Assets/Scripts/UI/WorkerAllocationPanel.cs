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
        }

        private void OnEnable()
        {
            allocation.WorkerMoved += HandleWorkerMoved;
            if (GameState.Instance != null)
                GameState.Instance.OnStateChanged += HandleStateChanged;
        }

        private void OnDisable()
        {
            allocation.WorkerMoved -= HandleWorkerMoved;
            if (GameState.Instance != null)
                GameState.Instance.OnStateChanged -= HandleStateChanged;
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