using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ResortEmpire.Core;

namespace ResortEmpire.UI
{
    [Serializable]
    public class BuildingIconEntry
    {
        public BuildingType type;
        public Sprite icon;
    }

    public class StaffHiringPanel : MonoBehaviour
    {
        [Header("Services")]
        [SerializeField] private StaffHiringService hiring;
        [SerializeField] private SpecialistService specialists;
        [SerializeField] private WorkerAllocation allocation;
        [SerializeField] private BuildingRegistry buildingRegistry;

        [Header("UI")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text coinsText;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button backButton;
        [SerializeField] private RectTransform content;
        [SerializeField] private StaffRowView rowPrefab;

        [Header("Pagination")]
        [SerializeField] private Button previousButton;
        [SerializeField] private Button nextButton;
        [SerializeField, Min(1)] private int cardsPerPage = 3;
        [SerializeField] private TMP_Text pageIndicatorText;

        [Header("Ikony (volitelne)")]
        [SerializeField] private Sprite workerIcon;
        [SerializeField] private Sprite managerIcon;
        [SerializeField] private Sprite butlerIcon;
        [SerializeField] private Sprite captainIcon;
        [SerializeField] private List<BuildingIconEntry> buildingIcons = new List<BuildingIconEntry>();

        private readonly List<StaffRowView> rows = new List<StaffRowView>();
        private Building selected;
        private int currentPage;
        private int totalPages = 1;

        private void Awake()
        {
            if (hiring == null) hiring = FindAnyObjectByType<StaffHiringService>();
            if (specialists == null) specialists = FindAnyObjectByType<SpecialistService>();
            if (allocation == null) allocation = FindAnyObjectByType<WorkerAllocation>();
            if (buildingRegistry == null) buildingRegistry = FindAnyObjectByType<BuildingRegistry>();

            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (backButton != null) backButton.onClick.AddListener(Back);
            if (previousButton != null) previousButton.onClick.AddListener(PreviousPage);
            if (nextButton != null) nextButton.onClick.AddListener(NextPage);
        }

        private void OnEnable()
        {
            if (hiring != null) hiring.Changed += Rebuild;
            if (specialists != null) specialists.SpecialistHired += HandleSpecialistHired;
            if (allocation != null) allocation.WorkerMoved += HandleWorkerMoved;
            if (GameState.Instance != null)
                GameState.Instance.OnStateChanged += HandleStateChanged;

            Rebuild();
        }

        private void OnDisable()
        {
            if (hiring != null) hiring.Changed -= Rebuild;
            if (specialists != null) specialists.SpecialistHired -= HandleSpecialistHired;
            if (allocation != null) allocation.WorkerMoved -= HandleWorkerMoved;
            if (GameState.Instance != null)
                GameState.Instance.OnStateChanged -= HandleStateChanged;

            ClearRows();
        }

        public void Open()
        {
            selected = null;
            currentPage = 0;
            if (gameObject.activeSelf) Rebuild();
            else gameObject.SetActive(true);
        }

        public void OpenFor(Building building)
        {
            if (building == null || building.Type == BuildingType.Spa)
                return;

            selected = building;
            if (gameObject.activeSelf) Rebuild();
            else gameObject.SetActive(true);
        }

        public void Close()
        {
            selected = null;
            gameObject.SetActive(false);
        }

        private void Back()
        {
            selected = null;
            Rebuild();
        }

        private void SelectBuilding(Building b)
        {
            selected = b;
            Rebuild();
        }

        private void ClearRows()
        {
            foreach (StaffRowView row in rows)
            {
                if (row == null) continue;
                row.gameObject.SetActive(false);
                Destroy(row.gameObject);
            }
            rows.Clear();
        }

        private void AddRow(string title, string subtitle, string buttonText, bool interactable, Action onClick, Sprite icon = null)
        {
            StaffRowView row = Instantiate(rowPrefab, content);
            row.Set(title, subtitle, buttonText, interactable, onClick, icon);
            rows.Add(row);
        }

        private void Rebuild()
        {
            if (content == null || rowPrefab == null || hiring == null || buildingRegistry == null) return;

            ClearRows();

            if (coinsText != null && GameState.Instance != null)
                coinsText.text = GameState.Instance.Coins.ToString("N0");

            if (backButton != null)
                backButton.gameObject.SetActive(selected != null);

            if (selected == null) BuildPicker();
            else BuildDetail(selected);

            UpdatePaginationUI();
        }

        private void BuildPicker()
        {
            if (titleText != null) titleText.text = "HIRE STAFF";

            List<Building> buildings = new List<Building>();
            foreach (Building building in buildingRegistry.AllBuildings)
                if (building != null && building.Type != BuildingType.Spa)
                    buildings.Add(building);

            List<SpecialistType> specialistTypes = new List<SpecialistType>();
            if (specialists != null)
                foreach (SpecialistType type in Enum.GetValues(typeof(SpecialistType)))
                    specialistTypes.Add(type);

            int totalItems = buildings.Count + specialistTypes.Count;
            int pageSize = Mathf.Max(1, cardsPerPage);
            totalPages = Mathf.Max(1, Mathf.CeilToInt((float)totalItems / pageSize));
            currentPage = Mathf.Clamp(currentPage, 0, totalPages - 1);

            int first = currentPage * pageSize;
            int last = Mathf.Min(first + pageSize, totalItems);
            for (int i = first; i < last; i++)
            {
                if (i < buildings.Count)
                {
                    Building building = buildings[i];
                    int workerCount = allocation != null ? allocation.GetWorkerCount(building.ServedNeed) : 0;
                    AddRow(
                        building.Type.ToString(),
                        $"Workers: {workerCount}/{building.WorkerSlots}  Manager: {(building.HasManager ? "Yes" : "No")}",
                        "Select",
                        true,
                        () => SelectBuilding(building),
                        GetBuildingIcon(building.Type));
                }
                else
                {
                    SpecialistType type = specialistTypes[i - buildings.Count];
                    bool hired = specialists.IsHired(type);
                    bool canHire = !hired && specialists.IsHireWindowOpen && specialists.CanAfford(type);
                    AddRow(
                        SpecialistTable.GetDisplayName(type),
                        hired ? "Hired" : "VIP specialist",
                        hired ? "Hired" : $"Hire <sprite name=\"coin\"> {specialists.GetCost(type):N0}",
                        canHire,
                        () => Report(specialists.TryHire(type)),
                        GetSpecialistIcon(type));
                }
            }
        }

        private void PreviousPage()
        {
            if (selected != null || currentPage <= 0) return;
            currentPage--;
            Rebuild();
        }

        private void NextPage()
        {
            if (selected != null || currentPage >= totalPages - 1) return;
            currentPage++;
            Rebuild();
        }

        private void UpdatePaginationUI()
        {
            bool picker = selected == null;
            if (previousButton != null)
            {
                previousButton.gameObject.SetActive(picker);
                previousButton.interactable = currentPage > 0;
            }
            if (nextButton != null)
            {
                nextButton.gameObject.SetActive(picker);
                nextButton.interactable = currentPage < totalPages - 1;
            }
            if (pageIndicatorText != null)
            {
                pageIndicatorText.gameObject.SetActive(picker);
                pageIndicatorText.text = $"{currentPage + 1} / {totalPages}";
            }
        }

        private void BuildDetail(Building building)
        {
            if (titleText != null)
                titleText.text = $"HIRE STAFF - {building.Type.ToString().ToUpper()}";

            int workerCount = allocation != null ? allocation.GetWorkerCount(building.ServedNeed) : 0;
            bool buildingSlotFree = workerCount < building.WorkerSlots;
            bool slotFree = hiring.HasFreeWorkerSlot;

            string workerSubtitle = $"Workers: {workerCount}/{building.WorkerSlots}";
            string workerButton = !buildingSlotFree ? "Full"
                : (slotFree ? $"Hire <sprite name=\"coin\"> {hiring.WorkerCost:N0}" : "Max");

            AddRow(
                "Worker",
                workerSubtitle,
                workerButton,
                hiring.CanHireWorkerFor(building.ServedNeed),
                () => Report(hiring.TryHireWorker(building.ServedNeed)),
                workerIcon);

            AddRow(
                "Manager",
                building.HasManager ? "Hired" : "Faster service",
                building.HasManager ? "Hired" : $"Hire <sprite name=\"coin\"> {hiring.ManagerCost:N0}",
                hiring.CanHireManager(building),
                () => Report(hiring.TryHireManager(building)),
                managerIcon);
        }

        private Sprite GetBuildingIcon(BuildingType type)
        {
            foreach (BuildingIconEntry e in buildingIcons)
                if (e.type == type) return e.icon;
            return null;
        }

        private Sprite GetSpecialistIcon(SpecialistType type)
        {
            switch (type)
            {
                case SpecialistType.Butler: return butlerIcon;
                case SpecialistType.Captain: return captainIcon;
                default: return null;
            }
        }

        private static void Report(HireResult result)
        {
            if (result != HireResult.Success)
                Debug.Log($"[StaffHiringPanel] Hire refused: {result}");
        }

        private static void Report(SpecialistHireResult result)
        {
            if (result != SpecialistHireResult.Success)
                Debug.Log($"[StaffHiringPanel] Specialist hire refused: {result}");
        }

        private void HandleSpecialistHired(SpecialistType type) => Rebuild();
        private void HandleWorkerMoved(GuestNeedType from, GuestNeedType to) => Rebuild();
        private void HandleStateChanged(ResortState state)
        {
            if (state != ResortState.AWAITING_WAVE)
                Close();
        }
    }
}