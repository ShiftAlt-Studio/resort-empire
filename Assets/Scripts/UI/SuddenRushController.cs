using System;
using UnityEngine;
using ResortEmpire.Core;

namespace ResortEmpire.UI
{
    public class SuddenRushController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SuddenRushView view;
        [SerializeField] private WorkerAllocationPanel workerPanel;
        [SerializeField] private ArrivalPhase arrivalPhase;
        [SerializeField] private WorkerAllocation workers;
        [SerializeField] private BuildingRegistry registry;
        [SerializeField] private SpeedUpService speedUp;

        [Header("Tuning (SS-134)")]
        [SerializeField] private int waveNumber = 2;
        [SerializeField] private float startAfterSeconds = 20f;
        [SerializeField] private int extraUnits = 6;
        [SerializeField] private float rushDuration = 10f;

        private static readonly GuestNeedType[] AllNeeds =
            (GuestNeedType[])Enum.GetValues(typeof(GuestNeedType));

        private const float ArrivalSafetyMargin = 0.9f;

        private bool triggered;
        private bool rushing;
        private float rushElapsed;
        private float currentRushDuration;
        private int rushAdded;
        private float lastArrivalElapsed = -1f;
        private GuestNeedType rushNeed;
        private Building rushBuilding;
        private bool windowDismissed;

        [Serializable]
        public class RushSnapshot
        {
            public bool triggered;
            public bool rushing;
            public float rushElapsed;
            public float currentRushDuration;
            public int rushAdded;
            public GuestNeedType rushNeed;
            public float lastArrivalElapsed;
            public bool windowDismissed;
        }

        public RushSnapshot GetRushSnapshot()
        {
            return new RushSnapshot
            {
                triggered = triggered,
                rushing = rushing,
                rushElapsed = rushElapsed,
                currentRushDuration = currentRushDuration,
                rushAdded = rushAdded,
                rushNeed = rushNeed,
                lastArrivalElapsed = lastArrivalElapsed,
                windowDismissed = windowDismissed
            };
        }


        public void RestoreRushSnapshot(RushSnapshot snapshot)
        {
            if (snapshot == null) return;

            triggered = snapshot.triggered;
            rushing = snapshot.rushing;
            rushElapsed = snapshot.rushElapsed;
            currentRushDuration = snapshot.currentRushDuration;
            rushAdded = snapshot.rushAdded;
            rushNeed = snapshot.rushNeed;
            lastArrivalElapsed = snapshot.lastArrivalElapsed;
            windowDismissed = snapshot.windowDismissed;

            rushBuilding = registry != null
                ? registry.GetBuildingForNeed(rushNeed)
                : null;

            if (view != null)
            {
                if (triggered && !windowDismissed && rushBuilding != null)
                {
                    bool canUseEnergy = speedUp != null
                        && speedUp.GetState(rushNeed) == SpeedUpState.Available;

                    bool canMoveWorker = workers != null
                        && workers.StaffedCount > 0;

                    int cost = speedUp != null ? speedUp.EnergyCost : 0;

                    view.Show(
                        rushBuilding.Type.ToString(),
                        cost,
                        canUseEnergy,
                        canMoveWorker
                    );
                }
                else
                {
                    view.Hide();
                }
            }
        }


        private void Awake()
        {
            if (arrivalPhase == null)
                arrivalPhase = FindAnyObjectByType<ArrivalPhase>();

            if (workers == null)
                workers = FindAnyObjectByType<WorkerAllocation>();

            if (registry == null)
                registry = FindAnyObjectByType<BuildingRegistry>();

            if (speedUp == null)
                speedUp = FindAnyObjectByType<SpeedUpService>();
        }

        private void Start()
        {
            if (arrivalPhase == null) arrivalPhase = FindAnyObjectByType<ArrivalPhase>();
            if (workers == null) workers = FindAnyObjectByType<WorkerAllocation>();
            if (registry == null) registry = FindAnyObjectByType<BuildingRegistry>();
            if (speedUp == null) speedUp = FindAnyObjectByType<SpeedUpService>();

            if (view != null)
            {
                view.MoveWorkerClicked += OnMoveWorker;
                view.UseEnergyClicked += OnUseEnergy;
                view.IgnoreClicked += OnIgnore;
            }

            if (GameState.Instance != null)
                GameState.Instance.OnStateChanged += HandleStateChanged;
        }

        private void OnDestroy()
        {
            if (view != null)
            {
                view.MoveWorkerClicked -= OnMoveWorker;
                view.UseEnergyClicked -= OnUseEnergy;
                view.IgnoreClicked -= OnIgnore;
            }

            if (GameState.Instance != null)
                GameState.Instance.OnStateChanged -= HandleStateChanged;
        }


        private void HandleStateChanged(ResortState state)
        {
            if (GameState.Instance != null && GameState.Instance.IsResumingFromPause)
            {
                if (GameState.Instance.PreviousState != ResortState.WAVE_PAUSED
                    && lastArrivalElapsed < 0f
                    && arrivalPhase != null)
                {
                    lastArrivalElapsed = arrivalPhase.Elapsed;
                }

                return;
            }

            if (state == ResortState.WAVE_INCOMING)
            {
                triggered = false;
                rushing = false;
                lastArrivalElapsed = 0f;
            }
            else if (state == ResortState.WAVE_COMPLETE || state == ResortState.AWAITING_WAVE)
            {
                rushing = false;
                if (view != null) view.Hide();
            }
        }


        private void Update()
        {
            if (GameState.Instance == null || arrivalPhase == null) return;

            ResortState state = GameState.Instance.CurrentState;
            if (state != ResortState.WAVE_INCOMING && state != ResortState.WAVE_ACTIVE) return;

            float elapsed = arrivalPhase.Elapsed;
            bool crossedStart = lastArrivalElapsed >= 0f
                                && elapsed >= startAfterSeconds
                                && (lastArrivalElapsed < startAfterSeconds
                                    || (startAfterSeconds <= 0f && lastArrivalElapsed == 0f));
            lastArrivalElapsed = elapsed;

            if (!triggered && crossedStart && arrivalPhase.IsRunning
                && GameState.Instance.CurrentWaveNumber == waveNumber)
            {
                TryStartRush();
            }

            if (rushing) AddRushDemand(Time.deltaTime);
        }

        private void TryStartRush()
        {
            triggered = true;
            windowDismissed = false;

            rushBuilding = null;
            foreach (GuestNeedType need in AllNeeds)
            {
                Building b = registry != null ? registry.GetBuildingForNeed(need) : null;
                if (b == null) continue;
                if (workers != null && workers.IsStaffed(need)) continue;

                rushNeed = need;
                rushBuilding = b;
                break;
            }

            if (rushBuilding == null)
            {
                Debug.Log("[SuddenRush] Every building has a Worker, event skipped.");
                return;
            }

            currentRushDuration = Mathf.Min(rushDuration, arrivalPhase.Remaining * ArrivalSafetyMargin);

            rushing = true;
            rushElapsed = 0f;
            rushAdded = 0;
            Debug.Log($"[SuddenRush] Started at {rushBuilding.Type}: +{extraUnits} {rushNeed} over {currentRushDuration:0.0}s");

            if (view != null)
            {
                bool canUseEnergy = speedUp != null && speedUp.GetState(rushNeed) == SpeedUpState.Available;
                bool canMoveWorker = workers != null && workers.StaffedCount > 0;
                int cost = speedUp != null ? speedUp.EnergyCost : 0;
                view.Show(rushBuilding.Type.ToString(), cost, canUseEnergy, canMoveWorker);
            }

            if (currentRushDuration <= 0f) AddRushDemand(0f);
        }

        private void AddRushDemand(float deltaTime)
        {
            rushElapsed += deltaTime;
            float progress = currentRushDuration > 0f ? Mathf.Clamp01(rushElapsed / currentRushDuration) : 1f;
            int target = Mathf.FloorToInt(extraUnits * progress);
            int diff = target - rushAdded;

            if (diff > 0)
            {
                rushAdded = target;
                arrivalPhase.AddExtraDemand(rushNeed, diff);
            }

            if (progress >= 1f) rushing = false;
        }

        private void OnMoveWorker()
        {
            windowDismissed = true;
            Debug.Log("[SuddenRush] Player chose: Move Worker");
            if (workerPanel != null && rushBuilding != null)
                workerPanel.Open(rushBuilding);
        }

        private void OnUseEnergy()
        {
            windowDismissed = true;
            Debug.Log("[SuddenRush] Player chose: Use Energy");
            if (speedUp == null) return;
            SpeedUpResult result = speedUp.TrySpeedUp(rushNeed);
            if (result != SpeedUpResult.Success)
                Debug.Log($"[SuddenRush] Speed Up refused: {result}");
        }

        private void OnIgnore()
        {
            windowDismissed = true;
            Debug.Log("[SuddenRush] Player chose: Ignore");
        }
    }
}