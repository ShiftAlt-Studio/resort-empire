using System;
using System.Collections.Generic;
using UnityEngine;

namespace ResortEmpire.Core
{
    [Serializable]
    public struct SavedBuildingLevel
    {
        public BuildingType buildingType;
        public int level;
    }

    [Serializable]
    public class PersistentSaveData
    {
        public int waveNumber = 1;
        public int coins = 1000;
        public int energy = 45;
        public int resortValue = 700;
        public int materials = 0;
        public int gems = 0;
        public int reputation = 0;
        public bool firstPerfectStayRewarded = false;
        public int availableWorkers = 2;

        public List<AreaType> unlockedAreas = new List<AreaType>();
        public List<GuestNeedType> unlockedNeeds = new List<GuestNeedType>();
        public List<SavedBuildingLevel> buildingLevels = new List<SavedBuildingLevel>();
        public bool[] staffedNeeds = Array.Empty<bool>();
    }


    public class PersistentProgressService : MonoBehaviour
    {
        private const string SaveKey = "ResortEmpire.PersistentProgress.v1";

        [SerializeField] private AreaActivationService areaActivationService;
        [SerializeField] private WorkerAllocation workerAllocation;
        [SerializeField] private BuildingRegistry buildingRegistry;

        private bool isApplyingSave;

        private void Start()
        {
            if (areaActivationService == null)
                areaActivationService = FindAnyObjectByType<AreaActivationService>();
            if (workerAllocation == null)
                workerAllocation = FindAnyObjectByType<WorkerAllocation>();
            if (buildingRegistry == null)
                buildingRegistry = FindAnyObjectByType<BuildingRegistry>();

            SubscribeEvents();
            LoadProgress();
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) SaveProgress();
        }

        private void OnApplicationQuit()
        {
            SaveProgress();
        }

        private void SubscribeEvents()
        {
            if (GameState.Instance != null)
            {
                GameState.Instance.CoinsChanged += OnAnyIntChanged;
                GameState.Instance.EnergyChanged += OnAnyIntChanged;
                GameState.Instance.ResortValueChanged += OnAnyIntChanged;
                GameState.Instance.MaterialsChanged += OnAnyIntChanged;
                GameState.Instance.GemsChanged += OnAnyIntChanged;
                GameState.Instance.ReputationChanged += OnAnyIntChanged;
                GameState.Instance.NeedUnlocked += OnNeedUnlocked;
                GameState.Instance.OnStateChanged += OnStateChanged;
            }

            if (areaActivationService != null)
                areaActivationService.AreaActivated += OnAreaActivated;

            if (workerAllocation != null)
                workerAllocation.WorkerMoved += OnWorkerMoved;

            foreach (Building b in CollectAllBuildings())
            {
                if (b != null) b.LevelChanged += OnBuildingLevelChanged;
            }
        }

        private void UnsubscribeEvents()
        {
            if (GameState.Instance != null)
            {
                GameState.Instance.CoinsChanged -= OnAnyIntChanged;
                GameState.Instance.EnergyChanged -= OnAnyIntChanged;
                GameState.Instance.ResortValueChanged -= OnAnyIntChanged;
                GameState.Instance.MaterialsChanged -= OnAnyIntChanged;
                GameState.Instance.GemsChanged -= OnAnyIntChanged;
                GameState.Instance.ReputationChanged -= OnAnyIntChanged;
                GameState.Instance.NeedUnlocked -= OnNeedUnlocked;
                GameState.Instance.OnStateChanged -= OnStateChanged;
            }

            if (areaActivationService != null)
                areaActivationService.AreaActivated -= OnAreaActivated;

            if (workerAllocation != null)
                workerAllocation.WorkerMoved -= OnWorkerMoved;

            foreach (Building b in CollectAllBuildings())
            {
                if (b != null) b.LevelChanged -= OnBuildingLevelChanged;
            }
        }

        private void OnAnyIntChanged(int _) => SaveProgress();
        private void OnNeedUnlocked(GuestNeedType _) => SaveProgress();
        private void OnAreaActivated(AreaType _) => SaveProgress();
        private void OnWorkerMoved(GuestNeedType _, GuestNeedType __) => SaveProgress();
        private void OnBuildingLevelChanged(Building _, int __) => SaveProgress();

        private void OnStateChanged(ResortState state)
        {
            if (state == ResortState.AWAITING_WAVE)
                SaveProgress();
        }

        public void SaveProgress()
        {
            if (isApplyingSave || GameState.Instance == null) return;

            var data = new PersistentSaveData
            {
                waveNumber = GameState.Instance.CurrentWaveNumber,
                coins = GameState.Instance.Coins,
                energy = GameState.Instance.Energy,
                resortValue = GameState.Instance.ResortValue,
                materials = GameState.Instance.Materials,
                gems = GameState.Instance.Gems,
                reputation = GameState.Instance.Reputation,
                firstPerfectStayRewarded = GameState.Instance.FirstPerfectStayRewarded,
                availableWorkers = GameState.Instance.AvailableWorkers,
                unlockedNeeds = GameState.Instance.GetUnlockedNeedsSnapshot(),
                unlockedAreas = areaActivationService != null
                    ? areaActivationService.GetUnlockedAreasSnapshot()
                    : new List<AreaType>(),
                staffedNeeds = workerAllocation != null
                    ? workerAllocation.GetStaffedSnapshot()
                    : Array.Empty<bool>()
            };

            foreach (Building b in CollectAllBuildings())
            {
                if (b == null) continue;
                data.buildingLevels.Add(new SavedBuildingLevel
                {
                    buildingType = b.Type,
                    level = b.Level
                });
            }

            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }

        public void LoadProgress()
        {
            if (!PlayerPrefs.HasKey(SaveKey) || GameState.Instance == null) return;

            string json = PlayerPrefs.GetString(SaveKey, string.Empty);
            if (string.IsNullOrEmpty(json)) return;

            PersistentSaveData data = JsonUtility.FromJson<PersistentSaveData>(json);
            if (data == null) return;

            isApplyingSave = true;
            try
            {
                if (areaActivationService != null && data.unlockedAreas != null)
                    areaActivationService.RestoreUnlockedAreas(data.unlockedAreas);

                GameState.Instance.RestoreUnlockedNeeds(data.unlockedNeeds);
                GameState.Instance.RestoreMaterials(data.materials);
                GameState.Instance.RestoreGems(data.gems, data.firstPerfectStayRewarded);
                GameState.Instance.RestoreReputation(data.reputation);

                if (data.buildingLevels != null)
                {
                    List<Building> allBuildings = CollectAllBuildings();
                    foreach (SavedBuildingLevel saved in data.buildingLevels)
                    {
                        Building b = allBuildings.Find(x => x != null && x.Type == saved.buildingType);
                        if (b != null)
                            b.SetLevel(saved.level);
                    }
                }

                GameState.Instance.SetWorkers(data.availableWorkers);
                if (workerAllocation != null && data.staffedNeeds != null)
                    workerAllocation.RestoreStaffedSnapshot(data.staffedNeeds);


                if (GameState.Instance.CurrentState != ResortState.WAVE_INCOMING
                    && GameState.Instance.CurrentState != ResortState.WAVE_ACTIVE)
                {
                    GameState.Instance.RestoreSnapshot(
                        data.waveNumber,
                        data.coins,
                        data.energy,
                        data.resortValue,
                        ResortState.AWAITING_WAVE,
                        data.unlockedNeeds
                    );
                }


                Debug.Log($"[PersistentProgressService] Loaded progress: Wave {data.waveNumber}, Coins {data.coins}, Energy {data.energy}, ResortValue {data.resortValue}, Materials {data.materials}, Gems {data.gems}, Reputation {data.reputation}, Workers {data.availableWorkers}.");
            }
            finally
            {
                isApplyingSave = false;
            }
        }

        public void DeleteSavedProgress()
        {
            if (PlayerPrefs.HasKey(SaveKey))
            {
                PlayerPrefs.DeleteKey(SaveKey);
                PlayerPrefs.Save();
            }
        }

        private List<Building> CollectAllBuildings()
        {
            var result = new List<Building>();
            if (buildingRegistry != null && buildingRegistry.AllBuildings != null)
            {
                foreach (Building b in buildingRegistry.AllBuildings)
                {
                    if (b != null && !result.Contains(b))
                        result.Add(b);
                }
            }

            Building[] sceneBuildings = FindObjectsByType<Building>(FindObjectsInactive.Include);
            foreach (Building b in sceneBuildings)
            {
                if (b != null && !result.Contains(b))
                    result.Add(b);
            }
            return result;
        }
    }
}