using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ResortEmpire.Core;

namespace ResortEmpire.UI
{
    [Serializable]
    public class BuildingIcon
    {
        public BuildingType type;
        public Sprite sprite;
        public Color color = Color.white;
    }

    public class UpgradePanel : MonoBehaviour
    {
        [SerializeField] private BuildingUpgradeService upgradeService;

        [Header("Budova")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text buildingNameText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private BuildingIcon[] icons;

        [Header("Capacity pred -> po")]
        [SerializeField] private TMP_Text capacityLabelText;
        [SerializeField] private TMP_Text capacityBeforeText;
        [SerializeField] private TMP_Text capacityAfterText;

        [Header("Cena a hodnota")]
        [SerializeField] private TMP_Text costText;
        [SerializeField] private TMP_Text resortValueText;
        [SerializeField] private int resortValueGain = 20;

        [Header("Stav a tlacidla")]
        [SerializeField] private TMP_Text feedbackText;
        [SerializeField] private Button upgradeNowButton;
        [SerializeField] private Button laterButton;

        private Building current;

        private void Awake()
        {
            upgradeNowButton.onClick.AddListener(OnUpgradeNow);
            laterButton.onClick.AddListener(Close);
        }

        private void OnEnable()
        {
            if (GameState.Instance == null) return;
            GameState.Instance.OnStateChanged += HandleStateChanged;
            GameState.Instance.CoinsChanged += HandleCoinsChanged;
        }

        private void OnDisable()
        {
            if (GameState.Instance == null) return;
            GameState.Instance.OnStateChanged -= HandleStateChanged;
            GameState.Instance.CoinsChanged -= HandleCoinsChanged;
        }

        public void Open(Building building)
        {
            current = building;
            Refresh();
            gameObject.SetActive(true);
        }

        public void Close() => gameObject.SetActive(false);

        private void Refresh()
        {
            if (current == null) return;

            if (buildingNameText != null) buildingNameText.text = current.Type.ToString();
            if (descriptionText != null) descriptionText.text = DescriptionFor(current.ServedNeed);
            if (capacityLabelText != null) capacityLabelText.text = $"{current.ServedNeed} Capacity";

            if (iconImage != null)
            {
                foreach (BuildingIcon i in icons)
                    if (i.type == current.Type)
                    {
                        iconImage.sprite = i.sprite;
                        iconImage.color = i.color.a > 0f ? i.color : Color.white;
                        break;
                    }
            }

            if (capacityBeforeText != null) capacityBeforeText.text = current.Capacity.ToString();

            if (current.IsMaxLevel)
            {
                if (capacityAfterText != null) capacityAfterText.text = "MAX";
                if (costText != null) costText.text = "-";
                if (resortValueText != null) resortValueText.text = "";
                if (feedbackText != null) feedbackText.text = "Max level";
                upgradeNowButton.gameObject.SetActive(false);
                return;
            }

            int cost = current.NextUpgradeCost;
            if (capacityAfterText != null) capacityAfterText.text = current.NextCapacity.ToString();
            if (costText != null) costText.text = cost.ToString("N0");
            if (resortValueText != null) resortValueText.text = $"+{resortValueGain} Resort Value";
            upgradeNowButton.gameObject.SetActive(true);

            int coins = GameState.Instance != null ? GameState.Instance.Coins : 0;
            if (feedbackText != null)
                feedbackText.text = coins < cost ? $"Missing: {cost - coins:N0} Coins" : "";
        }

        private void OnUpgradeNow()
        {
            UpgradeResult result = upgradeService.TryUpgrade(current);

            switch (result)
            {
                case UpgradeResult.Success:
                    Close();
                    break;
                case UpgradeResult.NotEnoughCoins:
                case UpgradeResult.MaxLevel:
                    Refresh();
                    break;
                default:
                    Close();
                    break;
            }
        }

        private static string DescriptionFor(GuestNeedType need)
        {
            switch (need)
            {
                case GuestNeedType.Food: return "Level up to unlock more tables!";
                case GuestNeedType.Drinks: return "Level up to serve more drinks!";
                case GuestNeedType.Rooms: return "Level up to check in more guests!";
                default: return "Level up to serve more guests!";
            }
        }

        private void HandleStateChanged(ResortState state)
        {
            if (upgradeService != null && !upgradeService.IsUpgradeWindowOpen)
                Close();
        }

        private void HandleCoinsChanged(int coins) => Refresh();
    }
}