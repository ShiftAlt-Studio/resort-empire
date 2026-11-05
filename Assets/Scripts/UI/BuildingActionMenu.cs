using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ResortEmpire.Core;

namespace ResortEmpire.UI
{
    public class BuildingActionMenu : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private Button upgradeButton;
        [SerializeField] private Button moveWorkerButton;
        [SerializeField] private Button closeButton;

        public event Action<Building> UpgradeChosen;
        public event Action<Building> MoveWorkerChosen;

        private Building current;

        private void Awake()
        {
            upgradeButton.onClick.AddListener(() => Choose(UpgradeChosen));
            moveWorkerButton.onClick.AddListener(() => Choose(MoveWorkerChosen));
            if (closeButton != null)
                closeButton.onClick.AddListener(Close);
        }

        private void OnEnable()
        {
            if (GameState.Instance != null)
                GameState.Instance.OnStateChanged += HandleStateChanged;
        }

        private void OnDisable()
        {
            if (GameState.Instance != null)
                GameState.Instance.OnStateChanged -= HandleStateChanged;
        }

        public void Open(Building building)
        {
            current = building;
            if (titleText != null) titleText.text = building.Type.ToString();
            upgradeButton.gameObject.SetActive(!building.IsMaxLevel);
            gameObject.SetActive(true);
        }

        public void Close() => gameObject.SetActive(false);

        private void Choose(Action<Building> evt)
        {
            Building b = current;
            Close();
            evt?.Invoke(b);
        }

        private void HandleStateChanged(ResortState state)
        {
            if (state != ResortState.AWAITING_WAVE) Close();
        }
    }
}