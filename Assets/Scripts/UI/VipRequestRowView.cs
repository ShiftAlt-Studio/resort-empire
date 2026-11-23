using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ResortEmpire.Core;

namespace ResortEmpire.UI
{
    public class VipRequestRowView : MonoBehaviour
    {
        [Header("Parts")]
        [SerializeField] private Image background;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private Button actionButton;
        [SerializeField] private Image actionImage;
        [SerializeField] private TMP_Text actionLabel;

        [Header("Button sprites")]
        [SerializeField] private Sprite redButton;
        [SerializeField] private Sprite orangeButton;
        [SerializeField] private Sprite greenButton;

        [Header("Icons (optional)")]
        [SerializeField] private Sprite missingIcon;
        [SerializeField] private Sprite readyIcon;
        [SerializeField] private Sprite progressIcon;
        [SerializeField] private Sprite doneIcon;
        [SerializeField] private Sprite bonusIcon;

        [Header("Colors")]
        [SerializeField] private Color normalBackground = Color.white;
        [SerializeField] private Color bonusBackground = new Color32(0xF3, 0xE8, 0xFF, 0xFF);
        [SerializeField] private Color missingColor = new Color32(0xEF, 0x44, 0x44, 0xFF);
        [SerializeField] private Color readyColor = new Color32(0x16, 0xA3, 0x4A, 0xFF);
        [SerializeField] private Color progressColor = new Color32(0xEA, 0x58, 0x0C, 0xFF);
        [SerializeField] private Color doneColor = new Color32(0x6B, 0x72, 0x80, 0xFF);
        [SerializeField] private Color bonusTextColor = new Color32(0x7C, 0x3A, 0xED, 0xFF);

        [Header("Bonus row (optional sprite with purple outline)")]
        [SerializeField] private Sprite bonusSprite;

        public event Action<int> ActionClicked;

        private int index;
        private bool isBonus;
        private Sprite normalSprite;
        private Color nameColor = Color.white;

        private void Awake()
        {
            if (background != null) normalSprite = background.sprite;
            if (nameText != null) nameColor = nameText.color;

            if (actionButton != null)
                actionButton.onClick.AddListener(OnActionClicked);
        }

        private void OnDestroy()
        {
            if (actionButton != null)
                actionButton.onClick.RemoveListener(OnActionClicked);
        }

        public void Bind(int rowIndex, VipRequest request)
        {
            index = rowIndex;
            isBonus = request.Definition.IsBonus;
            gameObject.SetActive(true);

            VipRequestDefinition def = request.Definition;

            if (nameText != null)
            {
                nameText.text = def.IsBonus ? "BONUS: " + def.Name : def.Name;
                nameText.color = def.IsBonus ? bonusTextColor : nameColor;
            }

            if (background != null)
            {
                bool useBonusSprite = def.IsBonus && bonusSprite != null;
                background.sprite = useBonusSprite ? bonusSprite : normalSprite;
                background.color = useBonusSprite ? Color.white : def.IsBonus ? bonusBackground : normalBackground;
            }

            string cap = string.Empty;
            if (request.IsCapped)
            {
                cap = string.IsNullOrEmpty(request.CapReason)
                    ? $" - max {request.QualityCap} stars"
                    : $" - max {request.QualityCap} stars ({request.CapReason})";
            }

            switch (request.State)
            {
                case VipRequestState.Missing:
                    string missing = def.Materials > 0 ? $"MISSING: {def.Materials} Materials" : "MISSING";
                    SetStatus(missing + cap, missingColor);
                    SetAction(true, "FIX", redButton, true);
                    SetIcon(missingIcon);
                    break;

                case VipRequestState.Ready:
                    SetStatus("READY" + cap, readyColor);
                    SetAction(true, "SERVE", greenButton, true);
                    SetIcon(readyIcon);
                    break;

                case VipRequestState.InProgress:
                    int seconds = Mathf.CeilToInt(Mathf.Max(0f, request.RemainingSeconds));
                    SetStatus($"IN PROGRESS ({seconds / 60:00}:{seconds % 60:00} left){cap}", progressColor);
                    SetAction(true, "SPEED UP", orangeButton, !request.SpeedUpUsed);
                    SetIcon(progressIcon);
                    break;

                default:
                    SetStatus("DONE" + cap, doneColor);
                    SetAction(false, string.Empty, null, false);
                    SetIcon(doneIcon);
                    break;
            }
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void SetStatus(string text, Color color)
        {
            if (statusText == null) return;
            statusText.text = text;
            statusText.color = color;
        }

        private void SetAction(bool visible, string label, Sprite sprite, bool interactable)
        {
            if (actionButton == null) return;
            actionButton.gameObject.SetActive(visible);
            actionButton.interactable = interactable;
            if (actionLabel != null) actionLabel.text = label;
            if (actionImage != null && sprite != null) actionImage.sprite = sprite;
        }

        private void SetIcon(Sprite sprite)
        {
            if (isBonus && bonusIcon != null)
                sprite = bonusIcon;

            if (icon != null && sprite != null)
                icon.sprite = sprite;
        }

        private void OnActionClicked()
        {
            ActionClicked?.Invoke(index);
        }
    }
}