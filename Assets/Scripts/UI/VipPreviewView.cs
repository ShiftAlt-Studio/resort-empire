using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ResortEmpire.Core;

namespace ResortEmpire.UI
{
    public class VipPreviewView : MonoBehaviour
    {
        [Serializable]
        private class ReadinessChip
        {
            public GameObject root;
            public Image dot;
            public TMP_Text label;
        }

        [Header("Root")]
        [SerializeField] private GameObject root;

        [Header("Guest")]
        [SerializeField] private Image portraitImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private Image[] difficultyStars;
        [SerializeField] private Sprite starFilled;
        [SerializeField] private Sprite starEmpty;
        [SerializeField] private Color starFilledColor = new Color32(0xFB, 0xBF, 0x24, 0xFF);
        [SerializeField] private Color starEmptyColor = new Color32(0xB8, 0xB8, 0xB8, 0xFF);

        [Header("Time")]
        [SerializeField] private TMP_Text arrivingText;
        [SerializeField] private Image timerFill;

        [Header("Promise")]
        [SerializeField] private TMP_Text promiseTitleText;
        [SerializeField] private TMP_Text promiseText;

        [Header("Rewards (range, max = Perfect Stay)")]
        [SerializeField] private TMP_Text coinsText;
        [SerializeField] private TMP_Text resortValueText;

        [Header("Readiness (one chip per service)")]
        [SerializeField] private ReadinessChip[] chips;
        [SerializeField] private Color goodColor = new Color32(0x10, 0xB9, 0x81, 0xFF);
        [SerializeField] private Color riskyColor = new Color32(0xFF, 0x7A, 0x2F, 0xFF);

        [Header("Buttons")]
        [SerializeField] private Button openRequestsButton;
        [SerializeField] private Button backButton;
        [SerializeField] private Button declineButton;

        public event Action AcceptClicked;
        public event Action DeferClicked;
        public event Action DeclineClicked;

        public bool IsVisible => root != null && root.activeSelf;

        private void Awake()
        {
            if (openRequestsButton != null) openRequestsButton.onClick.AddListener(() => AcceptClicked?.Invoke());
            if (backButton != null) backButton.onClick.AddListener(() => DeferClicked?.Invoke());
            if (declineButton != null) declineButton.onClick.AddListener(() => DeclineClicked?.Invoke());
        }

        public void Show(VipOffer offer, Sprite portrait)
        {
            VipGuestDefinition g = offer.Guest;

            if (portraitImage != null && portrait != null) portraitImage.sprite = portrait;
            if (nameText != null) nameText.text = g.GuestName;
            if (titleText != null) titleText.text = g.Title;

            SetDifficulty(g.Difficulty);

            if (promiseTitleText != null) promiseTitleText.text = $"THE {PromiseName(g.Type)} PROMISE";
            if (promiseText != null) promiseText.text = $"\"{g.Promise}\"";

            if (coinsText != null) coinsText.text = $"+{g.Coins.Min:N0}-{g.Coins.Max:N0} Coins";
            if (resortValueText != null) resortValueText.text = $"+{g.ResortValue.Min:N0}-{g.ResortValue.Max:N0} Resort Value";

            SetReadiness(offer);
            SetTime(offer);

            if (root != null) root.SetActive(true);
        }

        public void Hide()
        {
            if (root != null) root.SetActive(false);
        }

        public void SetTime(VipOffer offer)
        {
            int seconds = Mathf.CeilToInt(Mathf.Max(0f, offer.RemainingSeconds));
            if (arrivingText != null)
                arrivingText.text = $"ARRIVING IN {seconds / 60:00}:{seconds % 60:00}";
            if (timerFill != null)
            {
                RectTransform fill = timerFill.rectTransform;
                fill.anchorMin = new Vector2(0f, 0f);
                fill.anchorMax = new Vector2(Mathf.Clamp01(offer.RemainingNormalized), 1f);
                fill.offsetMin = Vector2.zero;
                fill.offsetMax = Vector2.zero;
            }
        }

        private void SetDifficulty(int difficulty)
        {
            if (difficultyStars == null) return;
            for (int i = 0; i < difficultyStars.Length; i++)
            {
                if (difficultyStars[i] == null) continue;
                bool filled = i < difficulty;
                Sprite s = filled ? starFilled : starEmpty;
                if (s != null) difficultyStars[i].sprite = s;
                difficultyStars[i].color = filled ? starFilledColor : starEmptyColor;
            }
        }

        private void SetReadiness(VipOffer offer)
        {
            if (chips == null) return;
            for (int i = 0; i < chips.Length; i++)
            {
                ReadinessChip chip = chips[i];
                if (chip == null || chip.root == null) continue;

                bool used = i < offer.Readiness.Count;
                chip.root.SetActive(used);
                if (!used) continue;

                VipNeedReadiness r = offer.Readiness[i];
                bool good = r.Level == VipReadiness.Good;
                if (chip.dot != null) chip.dot.color = good ? goodColor : riskyColor;
                if (chip.label != null) chip.label.text = $"{NeedName(r.Need)}: {(good ? "Good" : "Risky")}";
            }
        }

        private static string PromiseName(VipGuestType type)
        {
            switch (type)
            {
                case VipGuestType.TravelBlogger: return "TRAVEL BLOGGER";
                case VipGuestType.WeddingCouple: return "WEDDING COUPLE";
                case VipGuestType.Investor: return "INVESTOR";
                default: return "INFLUENCER";
            }
        }

        private static string NeedName(GuestNeedType need)
        {
            switch (need)
            {
                case GuestNeedType.OceanView: return "Ocean View";
                default: return need.ToString();
            }
        }
    }
}