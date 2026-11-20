using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ResortEmpire.UI
{
    public enum AreaUnlockViewState { Locked, Available, NotEnoughCoins }

    public class AreaUnlockView : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] private GameObject root;

        [Header("Content")]
        [SerializeField] private Image badgeImage;
        [SerializeField] private TMP_Text badgeText;
        [SerializeField] private Image areaImage;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text line1Text;
        [SerializeField] private TMP_Text line2Text;
        [SerializeField] private TMP_Text costText;
        [SerializeField] private TMP_Text hintText;

        [Header("Button")]
        [SerializeField] private Button unlockButton;
        [SerializeField] private Image buttonImage;
        [SerializeField] private TMP_Text buttonText;

        [Header("Colors")]
        [SerializeField] private Color badgeAvailableColor = Color.white;
        [SerializeField] private Color buttonAvailableColor = Color.white;
        [SerializeField] private Color grey = new Color32(0x9C, 0xA3, 0xAF, 0xFF);
        [SerializeField] private Color green = new Color32(0x10, 0xB9, 0x81, 0xFF);
        [SerializeField] private Color red = new Color32(0xEF, 0x44, 0x44, 0xFF);

        public event Action UnlockClicked;

        private void Awake()
        {
            if (unlockButton != null)
                unlockButton.onClick.AddListener(() => UnlockClicked?.Invoke());
        }

        public void Show(string title, string line1, string line2, Sprite image, int cost,
                         AreaUnlockViewState state, int missingCoins = 0, string lockedHint = "")
        {
            if (titleText != null) titleText.text = title;
            if (line1Text != null) line1Text.text = line1;
            if (line2Text != null) line2Text.text = line2;
            if (areaImage != null && image != null) areaImage.sprite = image;
            if (costText != null) costText.text = $"{cost:N0} Coins";

            SetState(state, missingCoins, lockedHint);
            if (root != null) root.SetActive(true);
        }

        public void Hide()
        {
            if (root != null) root.SetActive(false);
        }

        public void SetState(AreaUnlockViewState state, int missingCoins = 0, string lockedHint = "")
        {
            bool locked = state == AreaUnlockViewState.Locked;
            bool available = state == AreaUnlockViewState.Available;

            if (badgeImage != null) badgeImage.color = locked ? grey : badgeAvailableColor;
            if (badgeText != null) badgeText.text = locked ? "AREA LOCKED" : "NEW AREA AVAILABLE!";

            if (areaImage != null)
                areaImage.color = locked ? new Color(0.45f, 0.45f, 0.45f, 1f) : Color.white;

            if (costText != null) costText.color = available ? green : locked ? grey : red;

            if (hintText != null)
            {
                hintText.gameObject.SetActive(!available);
                hintText.color = locked ? grey : red;
                hintText.text = locked ? lockedHint : "Not enough coins";
            }

            if (unlockButton != null) unlockButton.interactable = available;
            if (buttonImage != null) buttonImage.color = available ? buttonAvailableColor : grey;
            if (buttonText != null)
                buttonText.text = available ? "UNLOCK & EXPAND"
                                : locked ? "LOCKED"
                                : $"NEED {missingCoins:N0} MORE";
        }

        [ContextMenu("Preview/Available")]
        private void PreviewAvailable() => SetState(AreaUnlockViewState.Available);

        [ContextMenu("Preview/Not Enough Coins")]
        private void PreviewNotEnough() => SetState(AreaUnlockViewState.NotEnoughCoins, 300);

        [ContextMenu("Preview/Locked")]
        private void PreviewLocked() => SetState(AreaUnlockViewState.Locked, 0, "Unlocks after wave 4");
    }
}