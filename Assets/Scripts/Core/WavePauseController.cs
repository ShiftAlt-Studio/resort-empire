using System;
using UnityEngine;

namespace ResortEmpire.Core
{
    /// <summary>
    /// SS-16: Приостанавливает активную волну при сворачивании/закрытии приложения
    /// и восстанавливает её ровно в том же состоянии после возврата.
    /// </summary>
    public class WavePauseController : MonoBehaviour
    {
        private const string SaveKey = "ResortEmpire.ActiveWaveSnapshot";

        [SerializeField] private ArrivalPhase arrivalPhase;
        [SerializeField] private NeedAggregator needAggregator;
        [SerializeField] private ServiceQueueProcessor queueProcessor;
        [SerializeField] private WorkerAllocation workerAllocation;
        [SerializeField] private WaveSatisfaction waveSatisfaction;
        [SerializeField] private KeyCode editorPauseToggleKey = KeyCode.P;

        [Serializable]
        private class WaveSnapshot
        {
            public bool hasActiveWave;
            public int waveNumber;
            public int coins;
            public int energy;
            public int resortValue;
            public int activeState;

            public float arrivalElapsed;
            public bool arrivalRunning;
            public bool arrivalFinished;
            public int[] arrivalAdded;

            public bool demandClosed;
            public int[] arrived;
            public int[] inService;
            public int[] served;

            public float[] seatsRooms;
            public float[] seatsFood;
            public float[] seatsDrinks;
            public float[] seatsRelax;
            public float[] seatsOceanView;

            public bool[] staffedNeeds;

            public float totalWaitUnitSeconds;
            public bool satisfactionTracking;
        }

        private void Start()
        {
            if (arrivalPhase == null) arrivalPhase = FindAnyObjectByType<ArrivalPhase>();
            if (needAggregator == null) needAggregator = FindAnyObjectByType<NeedAggregator>();
            if (queueProcessor == null) queueProcessor = FindAnyObjectByType<ServiceQueueProcessor>();
            if (workerAllocation == null) workerAllocation = FindAnyObjectByType<WorkerAllocation>();
            if (waveSatisfaction == null) waveSatisfaction = FindAnyObjectByType<WaveSatisfaction>();

            if (GameState.Instance != null)
                GameState.Instance.OnStateChanged += HandleStateChanged;

            TryRestoreSavedWave();
        }

        private void OnDestroy()
        {
            if (GameState.Instance != null)
                GameState.Instance.OnStateChanged -= HandleStateChanged;
        }

        private void Update()
        {
            if (Input.GetKeyDown(editorPauseToggleKey) && GameState.Instance != null)
            {
                if (GameState.Instance.CurrentState == ResortState.WAVE_PAUSED)
                    ResumeFromPause();
                else if (IsWaveActiveState(GameState.Instance.CurrentState))
                    PauseAndSaveWave();
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
                PauseAndSaveWave();
            else
                ResumeFromPause();
        }

        private void OnApplicationQuit()
        {
            PauseAndSaveWave();
        }

        public void PauseAndSaveWave()
        {
            if (GameState.Instance == null) return;

            ResortState current = GameState.Instance.CurrentState;
            if (!IsWaveActiveState(current) && current != ResortState.WAVE_PAUSED)
                return;

            ResortState activeState = current == ResortState.WAVE_PAUSED
                ? GameState.Instance.PreviousState
                : current;

            if (current != ResortState.WAVE_PAUSED)
                GameState.Instance.PauseWave();

            SaveSnapshot(activeState);
            Debug.Log("[WavePauseController] Active wave paused and state saved.");
        }

        public void ResumeFromPause()
        {
            if (GameState.Instance == null) return;
            if (GameState.Instance.CurrentState == ResortState.WAVE_PAUSED)
            {
                GameState.Instance.ResumeWave();
                Debug.Log("[WavePauseController] Active wave resumed in the exact same state.");
            }
        }

        private void HandleStateChanged(ResortState state)
        {
            if (state == ResortState.WAVE_COMPLETE || state == ResortState.AWAITING_WAVE)
            {
                if (PlayerPrefs.HasKey(SaveKey))
                {
                    PlayerPrefs.DeleteKey(SaveKey);
                    PlayerPrefs.Save();
                }
            }
        }

        private void SaveSnapshot(ResortState activeWaveState)
        {
            var snap = new WaveSnapshot
            {
                hasActiveWave = true,
                waveNumber = GameState.Instance.CurrentWaveNumber,
                coins = GameState.Instance.Coins,
                energy = GameState.Instance.Energy,
                resortValue = GameState.Instance.ResortValue,
                activeState = (int)activeWaveState
            };

            if (arrivalPhase != null)
            {
                snap.arrivalElapsed = arrivalPhase.Elapsed;
                snap.arrivalRunning = arrivalPhase.IsRunning;
                snap.arrivalFinished = arrivalPhase.IsFinished;
                snap.arrivalAdded = arrivalPhase.GetAddedSnapshot();
            }

            if (needAggregator != null)
            {
                snap.demandClosed = needAggregator.IsDemandClosed;
                needAggregator.GetSnapshots(out snap.arrived, out snap.inService, out snap.served);
            }

            if (queueProcessor != null)
            {
                snap.seatsRooms = queueProcessor.GetSeatProgressForNeed(GuestNeedType.Rooms);
                snap.seatsFood = queueProcessor.GetSeatProgressForNeed(GuestNeedType.Food);
                snap.seatsDrinks = queueProcessor.GetSeatProgressForNeed(GuestNeedType.Drinks);
                snap.seatsRelax = queueProcessor.GetSeatProgressForNeed(GuestNeedType.Relax);
                snap.seatsOceanView = queueProcessor.GetSeatProgressForNeed(GuestNeedType.OceanView);
            }

            if (workerAllocation != null)
            {
                GuestNeedType[] allNeeds = (GuestNeedType[])Enum.GetValues(typeof(GuestNeedType));
                snap.staffedNeeds = new bool[allNeeds.Length];
                for (int i = 0; i < allNeeds.Length; i++)
                    snap.staffedNeeds[i] = workerAllocation.IsStaffed(allNeeds[i]);
            }

            if (waveSatisfaction != null)
            {
                snap.totalWaitUnitSeconds = waveSatisfaction.TotalWaitUnitSeconds;
                snap.satisfactionTracking = waveSatisfaction.IsTracking;
            }

            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(snap));
            PlayerPrefs.Save();
        }

        private void TryRestoreSavedWave()
        {
            if (!PlayerPrefs.HasKey(SaveKey) || GameState.Instance == null)
                return;

            string json = PlayerPrefs.GetString(SaveKey, string.Empty);
            if (string.IsNullOrEmpty(json)) return;

            WaveSnapshot snap = JsonUtility.FromJson<WaveSnapshot>(json);
            if (snap == null || !snap.hasActiveWave) return;

            ResortState savedState = (ResortState)snap.activeState;
            if (!IsWaveActiveState(savedState)) return;

            if (arrivalPhase != null)
                arrivalPhase.RestoreState(snap.arrivalElapsed, snap.arrivalRunning, snap.arrivalFinished, snap.arrivalAdded);

            if (needAggregator != null)
                needAggregator.RestoreState(snap.demandClosed, snap.arrived, snap.inService, snap.served);

            if (queueProcessor != null)
            {
                queueProcessor.RestoreSeatProgressForNeed(GuestNeedType.Rooms, snap.seatsRooms);
                queueProcessor.RestoreSeatProgressForNeed(GuestNeedType.Food, snap.seatsFood);
                queueProcessor.RestoreSeatProgressForNeed(GuestNeedType.Drinks, snap.seatsDrinks);
                queueProcessor.RestoreSeatProgressForNeed(GuestNeedType.Relax, snap.seatsRelax);
                queueProcessor.RestoreSeatProgressForNeed(GuestNeedType.OceanView, snap.seatsOceanView);
            }

            if (waveSatisfaction != null)
                waveSatisfaction.RestoreState(snap.totalWaitUnitSeconds, snap.satisfactionTracking);

            GameState.Instance.RestoreSnapshot(snap.waveNumber, snap.coins, snap.energy, snap.resortValue, savedState);
            Debug.Log($"[WavePauseController] Restored interrupted wave {snap.waveNumber} in state {savedState} (elapsed {snap.arrivalElapsed:F1}s).");
        }

        private static bool IsWaveActiveState(ResortState state)
        {
            return state == ResortState.WAVE_INCOMING || state == ResortState.WAVE_ACTIVE;
        }
    }
}