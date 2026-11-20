using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ResortEmpire.Core;

namespace ResortEmpire.UI
{
    public class WaveCompletePanel : MonoBehaviour
    {
        [SerializeField] private WaveRewardService rewardService;
        [SerializeField] private BottleneckDetector bottleneckDetector;
        [SerializeField] private BuildingRegistry buildingRegistry;
        [SerializeField] private WaveSatisfaction waveSatisfaction;

        [Header("Опциональные ссылки на UI (если пустые — создаются автоматически)")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text ratingText;
        [SerializeField] private TMP_Text guestsServedValueText;
        [SerializeField] private TMP_Text satisfactionValueText;
        [SerializeField] private TMP_Text goodNeedsText;
        [SerializeField] private TMP_Text bottleneckHintText;
        [SerializeField] private TMP_Text coinsRewardText;
        [SerializeField] private TMP_Text energyRewardText;
        [SerializeField] private TMP_Text resortValueRewardText;
        [SerializeField] private Button claimRewardsButton;

        [Header("Шрифты (отдельно для заголовков и для текста карточек)")]
        [SerializeField] private TMP_FontAsset titleFontAsset;
        [SerializeField] private TMP_FontAsset bodyFontAsset;

        [Header("Готовые ассеты из проекта (Figma / Sprites)")]
        [SerializeField] private Sprite cardSprite;
        [SerializeField] private Sprite buttonSprite;
        [SerializeField] private bool tintButtonWithOrange = true;
        [SerializeField] private Sprite checkSprite;
        [SerializeField] private Sprite alertSprite;
        [SerializeField] private Sprite coinSprite;
        [SerializeField] private Sprite energySprite;
        [SerializeField] private Sprite starSprite;

        [Header("Цвета иконок")]
        [SerializeField] private Color filledStarColor = new Color(0.98f, 0.78f, 0.10f, 1f);
        [SerializeField] private Color emptyStarColor = new Color(1f, 1f, 1f, 0.35f);
        [SerializeField] private Color checkIconColor = new Color(0.10f, 0.68f, 0.36f, 1f);
        [SerializeField] private Color alertIconColor = new Color(0.96f, 0.65f, 0.08f, 1f);
        [SerializeField] private Color coinIconColor = new Color(0.10f, 0.68f, 0.36f, 1f);
        [SerializeField] private Color energyIconColor = new Color(0.08f, 0.56f, 0.92f, 1f);
        [SerializeField] private Color resortValueIconColor = new Color(0.96f, 0.72f, 0.10f, 1f);

        [Header("Reward flight animation")]
        [SerializeField] private RewardFlyAnimation rewardFlyAnimation;
        private bool claiming;

        private readonly List<Image> starImages = new List<Image>(5);
        private Image goodIconImg;
        private Image alertIconImg;
        private Image coinIconImg;
        private Image energyIconImg;
        private Image resortValueIconImg;

        private static readonly GuestNeedType[] AllNeeds =
            (GuestNeedType[])System.Enum.GetValues(typeof(GuestNeedType));

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
            if (bottleneckDetector == null)
                bottleneckDetector = FindAnyObjectByType<BottleneckDetector>();
            if (buildingRegistry == null)
                buildingRegistry = FindAnyObjectByType<BuildingRegistry>();
            if (waveSatisfaction == null)
                waveSatisfaction = FindAnyObjectByType<WaveSatisfaction>();

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
            claiming = false;
            if (claimRewardsButton != null) claimRewardsButton.interactable = true;

            // Скрываем левый временный текст Wave Satisfaction, если он включился в WaveSatisfaction.Finish()
            if (waveSatisfaction != null)
            {
                var tmp = waveSatisfaction.GetComponentInChildren<TMP_Text>(true);
                if (tmp != null && tmp.gameObject.name.Contains("WaveSatisfaction"))
                    tmp.gameObject.SetActive(false);
            }
            GameObject leftSatObj = GameObject.Find("WaveSatisfactionText");
            if (leftSatObj != null) leftSatObj.SetActive(false);

            if (titleText != null)
                titleText.text = "WAVE COMPLETE!";

            UpdateRatingStars(summary.Rating);

            if (guestsServedValueText != null)
                guestsServedValueText.text = $"{summary.GuestsServed}/{summary.TotalWaveUnits}";

            if (satisfactionValueText != null)
                satisfactionValueText.text = $"{summary.SatisfactionPercent:F0}%";

            BuildPerformanceNotes(out string okSummary, out string issueSummary);

            if (goodNeedsText != null)
                goodNeedsText.text = okSummary;

            if (bottleneckHintText != null)
                bottleneckHintText.text = issueSummary;

            if (coinsRewardText != null)
                coinsRewardText.text = $"+{summary.CoinsGranted:N0} Coins";

            if (energyRewardText != null)
                energyRewardText.text = $"+{summary.EnergyGranted} Energy";

            if (resortValueRewardText != null)
                resortValueRewardText.text = $"+{summary.ResortValueGranted} Resort Value";

            if (panelRoot != null)
                panelRoot.SetActive(true);
        }

        private void UpdateRatingStars(WaveRating rating)
        {
            int litCount = StarsForRating(rating);

            if (starImages.Count > 0)
            {
                for (int i = 0; i < starImages.Count; i++)
                {
                    Image starImg = starImages[i];
                    if (starImg == null) continue;

                    if (starSprite != null)
                        starImg.sprite = starSprite;

                    bool earned = i < litCount;
                    starImg.color = earned ? filledStarColor : emptyStarColor;
                    starImg.transform.localScale = earned ? Vector3.one : Vector3.one * 0.9f;
                }
            }

            if (ratingText != null)
                ratingText.text = $"{litCount}/5 ★";
        }

        private static int StarsForRating(WaveRating rating)
        {
            switch (rating)
            {
                case WaveRating.OneStar:    return 1;
                case WaveRating.TwoStars:   return 2;
                case WaveRating.ThreeStars: return 3;
                case WaveRating.FourStars:  return 4;
                case WaveRating.FiveStars:  return 5;
                case WaveRating.Perfect:    return 5;
                default:                    return 1;
            }
        }

        private void BuildPerformanceNotes(out string okLine, out string bottleneckLine)
        {
            var goodNeeds = new List<string>();
            var issueNeeds = new List<string>();

            foreach (GuestNeedType need in AllNeeds)
            {
                if (GameState.Instance != null && !GameState.Instance.IsNeedUnlocked(need))
                    continue;

                bool isBottleneck = bottleneckDetector != null &&
                                    bottleneckDetector.GetStatus(need) == NeedStatus.BOTTLENECK;

                if (isBottleneck)
                {
                    Building b = buildingRegistry != null ? buildingRegistry.GetBuildingForNeed(need) : null;
                    string label = b != null ? b.Type.ToString() : need.ToString();
                    issueNeeds.Add(label);
                }
                else
                {
                    goodNeeds.Add(need.ToString());
                }
            }

            okLine = goodNeeds.Count > 0
                ? $"{string.Join(" & ", goodNeeds)} went great!"
                : "All service lines processed";

            bottleneckLine = issueNeeds.Count > 0
                ? $"{string.Join(", ", issueNeeds)} capacity needs improvement"
                : "No major bottlenecks detected";
        }

        private void OnClaimClicked()
        {
            if (claiming) return;
            if (rewardService != null && !rewardService.HasUnclaimedRewards) return;

            claiming = true;
            if (claimRewardsButton != null) claimRewardsButton.interactable = false;

            if (rewardFlyAnimation == null)
                rewardFlyAnimation = GetComponent<RewardFlyAnimation>();

            if (rewardFlyAnimation == null)
            {
                FinishClaim();
                return;
            }

            rewardFlyAnimation.Play(
                coinsRewardText != null ? coinsRewardText.rectTransform : null,
                energyRewardText != null ? energyRewardText.rectTransform : null,
                resortValueRewardText != null ? resortValueRewardText.rectTransform : null,
                FinishClaim);
        }

        private void FinishClaim()
        {
            if (panelRoot != null) panelRoot.SetActive(false);

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

        private void ResolveFonts(Canvas canvas, out TMP_FontAsset headerFont, out TMP_FontAsset bodyFont)
        {
            headerFont = titleFontAsset;
            bodyFont = bodyFontAsset;

            if (headerFont == null && canvas != null)
            {
                TMP_Text existingText = canvas.GetComponentInChildren<TMP_Text>(true);
                if (existingText != null && existingText.font != null)
                    headerFont = existingText.font;
            }

            if (bodyFont == null)
                bodyFont = headerFont;

            if (headerFont == null)
                headerFont = bodyFont;
        }

        private void EnsureUI()
        {
            if (panelRoot != null) return;

            Canvas canvas = GetComponentInParent<Canvas>() ?? FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            ResolveFonts(canvas, out TMP_FontAsset headerFont, out TMP_FontAsset bodyFont);

            Color brownOutline = new Color(0.24f, 0.12f, 0.05f, 1f);
            Color cardBg = new Color(0.99f, 0.99f, 0.98f, 1f);
            Color greenText = new Color(0.08f, 0.65f, 0.34f, 1f);
            Color blueText = new Color(0.08f, 0.52f, 0.88f, 1f);
            Color goldText = new Color(0.92f, 0.65f, 0.08f, 1f);
            Color orangeCta = new Color(0.98f, 0.45f, 0.22f, 1f);

            // Затемнённый фон поверх острова
            panelRoot = new GameObject("WaveCompleteOverlay", typeof(RectTransform), typeof(Image));
            panelRoot.transform.SetParent(canvas.transform, false);
            RectTransform rootRt = panelRoot.GetComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;
            panelRoot.GetComponent<Image>().color = new Color(0.04f, 0.14f, 0.20f, 0.48f);

            // Заголовок и 5 звёзд по центру сверху
            titleText = CreateLabel(panelRoot.transform, "Title", "WAVE COMPLETE!", 52, new Vector2(0f, 365f), new Vector2(800f, 68f), Color.white, headerFont, FontStyles.Bold);
            CreateStarsRow(panelRoot.transform, new Vector2(0f, 300f));

            // =========================================================================
            // ЛЕВАЯ КАРТОЧКА: PERFORMANCE (560 x 410), единая левая линия x = -240
            // =========================================================================
            GameObject perfCard = CreateCard(panelRoot.transform, "PerformanceCard", new Vector2(-340f, 20f), new Vector2(560f, 410f), cardBg, brownOutline, cardSprite, true);

            // Заголовок: левый край -240 (центр -240 + 480/2 = 0)
            CreateLabel(perfCard.transform, "PerfHeader", "PERFORMANCE", 32, new Vector2(0f, 145f), new Vector2(480f, 46f), brownOutline, headerFont, FontStyles.Bold, TextAlignmentOptions.Left);

            // Строка 1: Guests Served (левый край -240, правый край +240)
            CreateLabel(perfCard.transform, "ServedLabel", "Guests Served", 27, new Vector2(-70f, 70f), new Vector2(340f, 42f), new Color(0.32f, 0.35f, 0.38f), bodyFont, FontStyles.Normal, TextAlignmentOptions.Left);
            guestsServedValueText = CreateLabel(perfCard.transform, "ServedValue", "12/12", 29, new Vector2(170f, 70f), new Vector2(140f, 42f), greenText, headerFont, FontStyles.Bold, TextAlignmentOptions.Right);

            // Строка 2: Avg. Satisfaction (левый край -240, правый край +240)
            CreateLabel(perfCard.transform, "SatLabel", "Avg. Satisfaction", 27, new Vector2(-70f, 5f), new Vector2(340f, 42f), new Color(0.32f, 0.35f, 0.38f), bodyFont, FontStyles.Normal, TextAlignmentOptions.Left);
            satisfactionValueText = CreateLabel(perfCard.transform, "SatValue", "88%", 29, new Vector2(170f, 5f), new Vector2(140f, 42f), greenText, headerFont, FontStyles.Bold, TextAlignmentOptions.Right);

            // Строка 3: Иконка 38x38 с левым краем -240 (центр -221), текст с левым краем -190 (центр -190 + 430/2 = +25)
            goodIconImg = CreateRowIcon(perfCard.transform, "GoodIcon", checkSprite, new Vector2(-221f, -65f), new Vector2(38f, 38f), checkIconColor);
            goodNeedsText = CreateLabel(perfCard.transform, "GoodNeeds", "Rooms & drinks went great!", 24, new Vector2(25f, -65f), new Vector2(430f, 54f), new Color(0.22f, 0.25f, 0.28f), bodyFont, FontStyles.Normal, TextAlignmentOptions.Left);

            // Строка 4: Иконка 38x38 с левым краем -240 (центр -221), текст с левым краем -190 (центр +25)
            alertIconImg = CreateRowIcon(perfCard.transform, "AlertIcon", alertSprite, new Vector2(-221f, -135f), new Vector2(38f, 38f), alertIconColor);
            bottleneckHintText = CreateLabel(perfCard.transform, "BottleneckHint", "Food capacity needs improvement", 24, new Vector2(25f, -135f), new Vector2(430f, 60f), new Color(0.82f, 0.45f, 0.08f), bodyFont, FontStyles.Normal, TextAlignmentOptions.Left);

            // =========================================================================
            // ПРАВАЯ КАРТОЧКА: REWARDS EARNED (540 x 340), единая левая линия x = -225
            // =========================================================================
            GameObject rewardsCard = CreateCard(panelRoot.transform, "RewardsCard", new Vector2(340f, 20f), new Vector2(540f, 340f), cardBg, brownOutline, cardSprite, true);

            // Заголовок: левый край -225 (центр -225 + 450/2 = 0)
            CreateLabel(rewardsCard.transform, "RewardsHeader", "REWARDS EARNED", 32, new Vector2(0f, 115f), new Vector2(450f, 46f), brownOutline, headerFont, FontStyles.Bold, TextAlignmentOptions.Left);

            // Награды: иконка 44x44 с левым краем -225 (центр -203), текст с левым краем -168 (центр -168 + 395/2 = +29.5)
            coinIconImg = CreateRowIcon(rewardsCard.transform, "CoinIcon", coinSprite, new Vector2(-203f, 32f), new Vector2(44f, 44f), coinIconColor);
            coinsRewardText = CreateLabel(rewardsCard.transform, "CoinsReward", "+120 Coins", 31, new Vector2(30f, 32f), new Vector2(395f, 48f), greenText, bodyFont, FontStyles.Bold, TextAlignmentOptions.Left);

            energyIconImg = CreateRowIcon(rewardsCard.transform, "EnergyIcon", energySprite, new Vector2(-203f, -38f), new Vector2(44f, 44f), energyIconColor);
            energyRewardText = CreateLabel(rewardsCard.transform, "EnergyReward", "+5 Energy", 31, new Vector2(30f, -38f), new Vector2(395f, 48f), blueText, bodyFont, FontStyles.Bold, TextAlignmentOptions.Left);

            resortValueIconImg = CreateRowIcon(rewardsCard.transform, "ResortValueIcon", starSprite, new Vector2(-203f, -108f), new Vector2(44f, 44f), resortValueIconColor);
            resortValueRewardText = CreateLabel(rewardsCard.transform, "ResortValueReward", "+15 Resort Value", 31, new Vector2(30f, -108f), new Vector2(395f, 48f), goldText, bodyFont, FontStyles.Bold, TextAlignmentOptions.Left);

            // Кнопка CLAIM REWARDS внизу по центру
            GameObject btnObj = new GameObject("ClaimRewardsButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(Outline));
            btnObj.transform.SetParent(panelRoot.transform, false);
            RectTransform btnRt = btnObj.GetComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(0.5f, 0.5f);
            btnRt.anchorMax = new Vector2(0.5f, 0.5f);
            btnRt.sizeDelta = new Vector2(430f, 94f);
            btnRt.anchoredPosition = new Vector2(0f, -330f);

            Image btnImg = btnObj.GetComponent<Image>();
            ApplySpriteCleanly(btnImg, buttonSprite, orangeCta, tintButtonWithOrange);

            Outline btnOutline = btnObj.GetComponent<Outline>();
            btnOutline.effectColor = brownOutline;
            btnOutline.effectDistance = new Vector2(4f, -4f);

            claimRewardsButton = btnObj.GetComponent<Button>();
            CreateLabel(btnObj.transform, "ButtonText", "CLAIM REWARDS", 32, Vector2.zero, new Vector2(390f, 64f), Color.white, headerFont, FontStyles.Bold, TextAlignmentOptions.Center);
        }

        private void CreateStarsRow(Transform parent, Vector2 anchoredPos)
        {
            starImages.Clear();

            GameObject row = new GameObject("StarsRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            row.transform.SetParent(parent, false);
            RectTransform rt = row.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(340f, 56f);
            rt.anchoredPosition = anchoredPos;

            HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            for (int i = 0; i < 5; i++)
            {
                GameObject starGo = new GameObject($"Star_{i + 1}", typeof(RectTransform), typeof(Image));
                starGo.transform.SetParent(row.transform, false);
                RectTransform starRt = starGo.GetComponent<RectTransform>();
                starRt.sizeDelta = new Vector2(48f, 48f);

                Image img = starGo.GetComponent<Image>();
                img.preserveAspect = true;
                img.raycastTarget = false;

                if (starSprite != null)
                    img.sprite = starSprite;

                img.color = i < 4 ? filledStarColor : emptyStarColor;
                starImages.Add(img);
            }

            if (ratingText != null)
                ratingText.text = "4/5 ★";
        }

        private static Image CreateRowIcon(Transform parent, string name, Sprite sprite, Vector2 anchoredPos, Vector2 size, Color tintColor)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = anchoredPos;

            Image img = go.GetComponent<Image>();
            img.preserveAspect = true;
            img.raycastTarget = false;

            if (sprite != null)
            {
                img.sprite = sprite;
                img.color = tintColor;
                go.SetActive(true);
            }
            else
            {
                img.color = tintColor;
                go.SetActive(false);
            }
            return img;
        }

        private static void ApplySpriteCleanly(Image img, Sprite sprite, Color fallbackOrTintColor, bool applyTintToSprite)
        {
            if (sprite == null)
            {
                img.sprite = null;
                img.color = fallbackOrTintColor;
                return;
            }

            img.sprite = sprite;
            img.color = applyTintToSprite ? fallbackOrTintColor : Color.white;

            bool hasBorder = sprite.border.sqrMagnitude > 0.001f;
            img.type = hasBorder ? Image.Type.Sliced : Image.Type.Simple;
        }

        private static GameObject CreateCard(Transform parent, string name, Vector2 pos, Vector2 size, Color bgColor, Color outlineColor, Sprite bgSprite, bool tintBgSprite)
        {
            GameObject card = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Outline));
            card.transform.SetParent(parent, false);
            RectTransform rt = card.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;

            Image img = card.GetComponent<Image>();
            ApplySpriteCleanly(img, bgSprite, bgColor, tintBgSprite);

            Outline outline = card.GetComponent<Outline>();
            outline.effectColor = outlineColor;
            outline.effectDistance = new Vector2(4f, -4f);
            return card;
        }

        private static TMP_Text CreateLabel(
            Transform parent,
            string name,
            string text,
            int fontSize,
            Vector2 anchoredPos,
            Vector2 size,
            Color color,
            TMP_FontAsset font,
            FontStyles style,
            TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = anchoredPos;

            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            if (font != null) tmp.font = font;
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.color = color;
            tmp.alignment = align;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.raycastTarget = false;
            return tmp;
        }
    }
}