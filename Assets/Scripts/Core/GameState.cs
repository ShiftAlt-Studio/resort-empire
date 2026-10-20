using UnityEngine;

namespace ResortEmpire.Core
{
    public class GameState : MonoBehaviour
    {
        public static GameState Instance { get; private set; }

        [SerializeField] private int startingWorkers = 2;

        public ResortState CurrentState { get; private set; }
        public int AvailableWorkers { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            CurrentState = ResortState.AWAITING_WAVE;
            AvailableWorkers = startingWorkers;

            Debug.Log($"GameState: {CurrentState}, workers: {AvailableWorkers}");
        }
    }
}