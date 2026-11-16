using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ResortEmpire.Core;

namespace ResortEmpire.UI
{
    /// <summary>
    /// SS-37: Отображает результаты волны (rating, Wave Satisfaction, Coins, Energy, Resort Value)
    /// на экране Wave Complete и позволяет игроку забрать награды кнопкой Claim Rewards.
    /// </summary>
    public class WaveCompletePanel : MonoBehaviour
    {
        [SerializeField] private WaveRewardService rewardService;

        [Header("Опциональные ссылки на UI (если пустые — создаются автоматически)")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text ratingText;
        [SerializeField] private TMP_Text satisfactionText;
        [SerializeField] private TMP_Text servedText;
        [SerializeField] private TMP_Text coinsRewardText;
        [SerializeField] private TMP_Text energyRewardText;
        [SerializeField] private TMP_Text resortValueRewardText;
        [SerializeField] private Button claimRewardsButton;

        private void Awake()
        {
            EnsureUI();

            if (claimRewardsButton != null)
                claimRewardsButton.onClick.AddListener(OnClaimClicked);

            if (panelRoot != null)
                panelRoot.SetActive(false);
        }

        private void Start()
        {
            if (rewardService == null)
                rewardService = FindAnyObjectByType<WaveRewardService>();

            if (rewardService != null)
                rewardService.RewardsGranted += ShowWaveResults;

            if (GameState.Instance != null)
                GameState.Instance.OnStateChanged += HandleStateChanged;
        }

        private void OnDestroy()
        {
            if (claimRewardsButton != null)
                claimRewardsButton.onClick.RemoveListener(OnClaimClicked);

            if (rewardService != null)
                rewardService.RewardsGranted -= ShowWaveResults;

            if (GameState.Instance != null)
                GameState.Instance.OnStateChanged -= HandleStateChanged;
        }

        public void ShowWaveResults(WaveRewardSummary summary)
        {
            EnsureUI();

            if (titleText != null)
                titleText.text = "WAVE COMPLETE!";

            if (ratingText != null)
                ratingText.text = FormatRatingStars(summary.Rating);

            if (satisfactionText != null)
                satisfactionText.text = $"Wave Satisfaction: {summary.SatisfactionPercent:F0}%";

            if (servedText != null)
                servedText.text = $"Needs Served: {summary.GuestsServed}/{summary.TotalWaveUnits}";

            if (coinsRewardText != null)
                coinsRewardText.text = $"+{summary.CoinsGranted:N0} Coins";

            if (energyRewardText != null)
                energyRewardText.text = $"+{summary.EnergyGranted} Energy";

            if (resortValueRewardText != null)
                resortValueRewardText.text = $"+{summary.ResortValueGranted} Resort Value";

            if (panelRoot != null)
                panelRoot.SetActive(true);
        }

        private void OnClaimClicked()
        {
            if (panelRoot != null)
                panelRoot.SetActive(false);

            if (rewardService != null)
                rewardService.ClaimRewards();
            else if (GameState.Instance != null)
                GameState.Instance.CompleteClaimAndAdvanceWave();
        }

        private void HandleStateChanged(ResortState state)
        {
            if (state != ResortState.WAVE_COMPLETE && panelRoot != null && panelRoot.activeSelf)
                panelRoot.SetActive(false);
        }

        private static string FormatRatingStars(WaveRating rating)
        {
            switch (rating)
            {
                case WaveRating.OneStar:   return "★☆☆☆☆  (1★)";
                case WaveRating.TwoStars:  return "★★☆☆☆  (2★)";
                case WaveRating.ThreeStars:return "★★★☆☆  (3★)";
                case WaveRating.FourStars: return "★★★★☆  (4★)";
                case WaveRating.FiveStars: return "★★★★★  (5★)";
                case WaveRating.Perfect:   return "★★★★★  PERFECT!";
                default:                   return "★☆☆☆☆";
            }
        }

        private void EnsureUI()
        {
            if (panelRoot != null) return;

            Canvas canvas = GetComponentInParent<Canvas>() ?? FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            TMP_FontAsset sharedFont = null;
            TMP_Text existingText = canvas.GetComponentInChildren<TMP_Text>(true);
            if (existingText != null) sharedFont = existingText.font;

            // Полноэкранный полупрозрачный фон
            panelRoot = new GameObject("WaveCompleteOverlay", typeof(RectTransform), typeof(Image));
            panelRoot.transform.SetParent(canvas.transform, false);
            RectTransform rootRt = panelRoot.GetComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;
            panelRoot.GetComponent<Image>().color = new Color(0.05f, 0.12f, 0.18f, 0.82f);

            // Центральная карточка
            GameObject card = new GameObject("Card", typeof(RectTransform), typeof(Image));
            card.transform.SetParent(panelRoot.transform, false);
            RectTransform cardRt = card.GetComponent<RectTransform>();
            cardRt.anchorMin = new Vector2(0.5f, 0.5f);
            cardRt.anchorMax = new Vector2(0.5f, 0.5f);
            cardRt.sizeDelta = new Vector2(760f, 560f);
            cardRt.anchoredPosition = new Vector2(0f, 20f);
            card.GetComponent<Image>().color = new Color(0.98f, 0.96f, 0.92f, 1f);

            titleText = CreateLabel(card.transform, "Title", "WAVE COMPLETE!", 44, new Vector2(0f, 215f), new Color(0.23f, 0.13f, 0.07f), sharedFont);
            ratingText = CreateLabel(card.transform, "Rating", "★★★★★ (5★)", 38, new Vector2(0f, 150f), new Color(0.92f, 0.62f, 0.08f), sharedFont);

            satisfactionText = CreateLabel(card.transform, "Satisfaction", "Wave Satisfaction: 100%", 30, new Vector2(0f, 80f), new Color(0.18f, 0.55f, 0.28f), sharedFont);
            servedText = CreateLabel(card.transform, "Served", "Needs Served: 22/22", 26, new Vector2(0f, 35f), new Color(0.30f, 0.22f, 0.16f), sharedFont);

            coinsRewardText = CreateLabel(card.transform, "CoinsReward", "+264 Coins", 32, new Vector2(0f, -35f), new Color(0.15f, 0.58f, 0.22f), sharedFont);
            energyRewardText = CreateLabel(card.transform, "EnergyReward", "+5 Energy", 32, new Vector2(0f, -85f), new Color(0.12f, 0.52f, 0.85f), sharedFont);
            resortValueRewardText = CreateLabel(card.transform, "ResortValueReward", "+20 Resort Value", 32, new Vector2(0f, -135f), new Color(0.85f, 0.55f, 0.08f), sharedFont);

            // Кнопка CLAIM REWARDS
            GameObject btnObj = new GameObject("ClaimRewardsButton", typeof(RectTransform), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(card.transform, false);
            RectTransform btnRt = btnObj.GetComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(0.5f, 0.5f);
            btnRt.anchorMax = new Vector2(0.5f, 0.5f);
            btnRt.sizeDelta = new Vector2(380f, 84f);
            btnRt.anchoredPosition = new Vector2(0f, -220f);
            btnObj.GetComponent<Image>().color = new Color(0.95f, 0.43f, 0.24f, 1f);

            claimRewardsButton = btnObj.GetComponent<Button>();
            CreateLabel(btnObj.transform, "ButtonText", "CLAIM REWARDS", 32, Vector2.zero, Color.white, sharedFont);
        }

        private static TMP_Text CreateLabel(Transform parent, string name, string text, int fontSize, Vector2 anchoredPos, Color color, TMP_FontAsset font)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(680f, 54f);
            rt.anchoredPosition = anchoredPos;

            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            if (font != null) tmp.font = font;
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            return tmp;
        }
    }
}