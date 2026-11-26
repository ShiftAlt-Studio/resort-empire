using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ResortEmpire.Core;

namespace ResortEmpire.UI
{
    public class VipCompletePanel : MonoBehaviour
    {
        [SerializeField] private VipRewardService vipRewardService;
        [SerializeField] private RewardFlyAnimation rewardFlyAnimation;

        [Header("Fonts")]
        [SerializeField] private TMP_FontAsset titleFontAsset;
        [SerializeField] private TMP_FontAsset bodyFontAsset;

        [Header("Sprites")]
        [SerializeField] private Sprite cardSprite;
        [SerializeField] private Sprite pillSprite;
        [SerializeField] private Sprite starFilledSprite;
        [SerializeField] private Sprite starOutlineSprite;
        [SerializeField] private Sprite checkSprite;
        [SerializeField] private Sprite alertSprite;
        [SerializeField] private Sprite reputationSprite;
        [SerializeField] private Sprite coinSprite;
        [SerializeField] private Sprite energySprite;
        [SerializeField] private Sprite giftSprite;
        [SerializeField] private Sprite claimButtonIconSprite;

        [Header("Colors (S8 Layout)")]
        [SerializeField] private Color purpleAccent = new Color(0.49f, 0.23f, 0.93f, 1f);     // #7C3AED
        [SerializeField] private Color filledStarColor = new Color(1.00f, 0.77f, 0.19f, 1f);   // #FFC531
        [SerializeField] private Color reputationColor = new Color(0.76f, 0.25f, 0.05f, 1f);   // #C2410C
        [SerializeField] private Color coinsColor = new Color(0.08f, 0.65f, 0.34f, 1f);        // #14A657
        [SerializeField] private Color energyColor = new Color(0.02f, 0.56f, 0.85f, 1f);       // #058FD9
        [SerializeField] private Color resortValueColor = new Color(0.95f, 0.65f, 0.08f, 1f);  // #F2A614
        [SerializeField] private Color bonusPinkColor = new Color(0.86f, 0.15f, 0.47f, 1f);    // #DB2678
        [SerializeField] private bool showConfetti = true;

        private GameObject panelRoot;
        private GameObject perfectBadgeRoot;
        private TMP_Text titleText;
        private TMP_Text feedbackHeaderText;
        private TMP_Text requestsAnsweredValueText;
        private TMP_Text satisfactionValueText;
        private TMP_Text fulfilledListText;
        private Image quoteIconImg;
        private TMP_Text quoteText;

        private GameObject reputationRow;
        private TMP_Text reputationRewardText;
        private GameObject coinsRow;
        private TMP_Text coinsRewardText;
        private GameObject energyRow;
        private TMP_Text energyRewardText;
        private GameObject resortValueRow;
        private TMP_Text resortValueRewardText;
        private GameObject bonusRow;
        private TMP_Text bonusRewardText;

        private Button claimButton;
        private readonly List<Image> starImages = new List<Image>(5);

        private bool claiming;
        private VipRewardSummary currentSummary;

        private void Awake()
        {
            EnsureUI();

            if (claimButton != null)
                claimButton.onClick.AddListener(OnClaimClicked);

            if (panelRoot != null)
                panelRoot.SetActive(false);
        }

        private void Start()
        {
            if (vipRewardService == null)
                vipRewardService = FindAnyObjectByType<VipRewardService>();
            if (rewardFlyAnimation == null)
                rewardFlyAnimation = GetComponent<RewardFlyAnimation>() ?? FindAnyObjectByType<RewardFlyAnimation>();

            if (vipRewardService != null)
                vipRewardService.RewardsGranted += ShowResults;
        }

        private void OnDestroy()
        {
            if (claimButton != null)
                claimButton.onClick.RemoveListener(OnClaimClicked);

            if (vipRewardService != null)
                vipRewardService.RewardsGranted -= ShowResults;
        }

        public void ShowResults(VipRewardSummary summary)
        {
            EnsureUI();
            currentSummary = summary;
            claiming = false;

            if (claimButton != null)
                claimButton.interactable = true;

            VipVisitResult visit = summary.VisitResult;

            if (perfectBadgeRoot != null)
                perfectBadgeRoot.SetActive(visit.IsPerfect);

            if (titleText != null)
                titleText.text = "VIP VISIT COMPLETE!";

            UpdateStars(visit.Stars);

            if (feedbackHeaderText != null)
                feedbackHeaderText.text = $"{ExtractFirstName(visit.Guest.GuestName).ToUpperInvariant()}'S FEEDBACK";

            if (requestsAnsweredValueText != null)
            {
                string bonusSuffix = visit.BonusCompleted ? " + Bonus" : string.Empty;
                requestsAnsweredValueText.text = $"{visit.CompletedMain}/{visit.TotalMain} Served{bonusSuffix}";
                requestsAnsweredValueText.color = visit.CompletedMain >= visit.TotalMain ? coinsColor : resortValueColor;
            }

            if (satisfactionValueText != null)
            {
                satisfactionValueText.text = $"{visit.SatisfactionPercent:F0}%";
                satisfactionValueText.color = visit.SatisfactionPercent >= 70f
                    ? coinsColor
                    : (visit.SatisfactionPercent >= 50f ? resortValueColor : new Color(0.88f, 0.25f, 0.25f, 1f));
            }

            if (fulfilledListText != null)
            {
                if (summary.CompletedRequestNames != null && summary.CompletedRequestNames.Length > 0)
                    fulfilledListText.text = $"Fulfilled: {string.Join(", ", summary.CompletedRequestNames)}";
                else
                    fulfilledListText.text = "Fulfilled: None";
            }

            if (quoteIconImg != null)
            {
                bool positive = visit.Stars >= 3;
                Sprite icon = positive ? checkSprite : (alertSprite != null ? alertSprite : checkSprite);
                quoteIconImg.sprite = icon;
                quoteIconImg.color = positive ? coinsColor : resortValueColor;
                quoteIconImg.gameObject.SetActive(icon != null);
            }

            if (quoteText != null)
                quoteText.text = summary.FeedbackQuote;

            if (reputationRewardText != null)
                reputationRewardText.text = $"+{summary.ReputationGranted} Reputation";
            if (reputationRow != null)
                reputationRow.SetActive(summary.ReputationGranted > 0);

            if (coinsRewardText != null)
                coinsRewardText.text = $"+{summary.CoinsGranted:N0} Coins";
            if (coinsRow != null)
                coinsRow.SetActive(summary.CoinsGranted > 0);

            if (energyRewardText != null)
                energyRewardText.text = $"+{summary.EnergyGranted} Energy";
            if (energyRow != null)
                energyRow.SetActive(summary.EnergyGranted > 0);

            if (resortValueRewardText != null)
                resortValueRewardText.text =
                    $"+{summary.ResortValueGranted} Resort Value ({summary.ResortValueBefore} -> {summary.ResortValueAfter})";
            if (resortValueRow != null)
                resortValueRow.SetActive(summary.ResortValueGranted > 0);

            bool hasBonus = !string.IsNullOrEmpty(summary.BonusLabel);
            if (bonusRewardText != null)
                bonusRewardText.text = summary.BonusLabel;
            if (bonusRow != null)
                bonusRow.SetActive(hasBonus);

            if (panelRoot != null)
            {
                panelRoot.transform.SetAsLastSibling();
                panelRoot.SetActive(true);
            }
        }

        private void UpdateStars(int earnedStars)
        {
            int lit = Mathf.Clamp(earnedStars, 1, 5);
            for (int i = 0; i < starImages.Count; i++)
            {
                Image img = starImages[i];
                if (img == null) continue;

                bool earned = i < lit;
                if (earned)
                {
                    img.sprite = starFilledSprite != null ? starFilledSprite : starOutlineSprite;
                    img.color = filledStarColor;
                }
                else
                {
                    img.sprite = starOutlineSprite != null ? starOutlineSprite : starFilledSprite;
                    img.color = Color.white;
                }
            }
        }

        private void OnClaimClicked()
        {
            if (claiming) return;

            claiming = true;
            if (claimButton != null)
                claimButton.interactable = false;

            if (rewardFlyAnimation == null)
            {
                FinishClaim();
                return;
            }

            RectTransform coinsOrigin = currentSummary.CoinsGranted > 0 && coinsRewardText != null
                ? coinsRewardText.rectTransform
                : null;
            RectTransform energyOrigin = currentSummary.EnergyGranted > 0 && energyRewardText != null
                ? energyRewardText.rectTransform
                : null;
            RectTransform resortOrigin = currentSummary.ResortValueGranted > 0 && resortValueRewardText != null
                ? resortValueRewardText.rectTransform
                : null;

            rewardFlyAnimation.Play(coinsOrigin, energyOrigin, resortOrigin, FinishClaim);
        }

        private void FinishClaim()
        {
            if (panelRoot != null)
                panelRoot.SetActive(false);

            if (vipRewardService != null)
                vipRewardService.ClaimRewards();
        }

        private static string ExtractFirstName(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName)) return "VIP";
            if (fullName.Contains("&"))
            {
                string[] parts = fullName.Split(' ');
                return parts.Length >= 3 ? $"{parts[0]} {parts[1]} {parts[2]}" : fullName;
            }

            int spaceIdx = fullName.IndexOf(' ');
            return spaceIdx > 0 ? fullName.Substring(0, spaceIdx) : fullName;
        }

        private void ResolveFonts(Canvas canvas, out TMP_FontAsset headerFont, out TMP_FontAsset bodyFont)
        {
            headerFont = titleFontAsset;
            bodyFont = bodyFontAsset;

            if (headerFont == null && canvas != null)
            {
                TMP_Text existing = canvas.GetComponentInChildren<TMP_Text>(true);
                if (existing != null && existing.font != null)
                    headerFont = existing.font;
            }

            if (bodyFont == null) bodyFont = headerFont;
            if (headerFont == null) headerFont = bodyFont;
        }

        private void EnsureUI()
        {
            if (panelRoot != null) return;

            Canvas canvas = GetComponentInParent<Canvas>() ?? FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            ResolveFonts(canvas, out TMP_FontAsset headerFont, out TMP_FontAsset bodyFont);

            // Full-screen dimmed overlay
            panelRoot = new GameObject("VipCompleteOverlay", typeof(RectTransform), typeof(Image));
            panelRoot.transform.SetParent(canvas.transform, false);
            RectTransform rootRt = panelRoot.GetComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;
            panelRoot.GetComponent<Image>().color = new Color(0.04f, 0.14f, 0.20f, 0.58f);

            if (showConfetti)
                BuildConfettiLayer(panelRoot.transform);

            // Top badge: PERFECT STAY BONUS UNLOCKED
            perfectBadgeRoot = CreateBorderedBox(
                panelRoot.transform,
                "PerfectStayBadge",
                new Vector2(0f, 412f),
                new Vector2(560f, 52f),
                Color.white,
                purpleAccent,
                4f,
                pillSprite != null ? pillSprite : cardSprite);

            BuildPerfectBadgeContent(perfectBadgeRoot.transform.Find("InnerFill"), headerFont);

            // Title and 5-star row
            titleText = CreateLabel(
                panelRoot.transform,
                "Title",
                "VIP VISIT COMPLETE!",
                54,
                new Vector2(0f, 344f),
                new Vector2(860f, 70f),
                Color.white,
                headerFont,
                FontStyles.Bold,
                TextAlignmentOptions.Center);

            CreateStarsRow(panelRoot.transform, new Vector2(0f, 274f));

            // =========================================================================
            // LEFT CARD: {NAME}'S FEEDBACK (620 x 355)
            // =========================================================================
            GameObject feedbackCard = CreateBorderedBox(
                panelRoot.transform,
                "FeedbackCard",
                new Vector2(-360f, 15f),
                new Vector2(620f, 355f),
                purpleAccent,
                new Color(0.99f, 0.99f, 0.99f, 1f),
                5f,
                cardSprite);

            Transform fbInner = feedbackCard.transform.Find("InnerFill");

            feedbackHeaderText = CreateLabel(
                fbInner,
                "FeedbackHeader",
                "CHLOE'S FEEDBACK",
                30,
                new Vector2(0f, 122f),
                new Vector2(540f, 44f),
                purpleAccent,
                headerFont,
                FontStyles.Bold,
                TextAlignmentOptions.Left);

            TMP_Text reqLabel = CreateLabel(
                fbInner,
                "ReqLabel",
                "Requests Answered",
                24,
                new Vector2(-145f, 58f),
                new Vector2(250f, 40f),
                new Color(0.35f, 0.37f, 0.40f, 1f),
                bodyFont,
                FontStyles.Normal,
                TextAlignmentOptions.Left);
            reqLabel.textWrappingMode = TextWrappingModes.NoWrap;
            reqLabel.enableAutoSizing = true;
            reqLabel.fontSizeMin = 18f;
            reqLabel.fontSizeMax = 24f;

            requestsAnsweredValueText = CreateLabel(
                fbInner,
                "ReqValue",
                "3/3 Served",
                24,
                new Vector2(135f, 58f),
                new Vector2(270f, 40f),
                coinsColor,
                headerFont,
                FontStyles.Bold,
                TextAlignmentOptions.Right);
            requestsAnsweredValueText.textWrappingMode = TextWrappingModes.NoWrap;
            requestsAnsweredValueText.enableAutoSizing = true;
            requestsAnsweredValueText.fontSizeMin = 18f;
            requestsAnsweredValueText.fontSizeMax = 24f;

            CreateLabel(
                fbInner,
                "SatLabel",
                "VIP Guest Satisfaction",
                24,
                new Vector2(-80f, 4f),
                new Vector2(380f, 40f),
                new Color(0.35f, 0.37f, 0.40f, 1f),
                bodyFont,
                FontStyles.Normal,
                TextAlignmentOptions.Left);

            satisfactionValueText = CreateLabel(
                fbInner,
                "SatValue",
                "100%",
                26,
                new Vector2(195f, 4f),
                new Vector2(150f, 40f),
                coinsColor,
                headerFont,
                FontStyles.Bold,
                TextAlignmentOptions.Right);

            fulfilledListText = CreateLabel(
                fbInner,
                "FulfilledList",
                "Fulfilled: Tropical Fruit Platter, Signature Mai Tai, Private Pool Session",
                19,
                new Vector2(0f, -54f),
                new Vector2(540f, 54f),
                new Color(0.42f, 0.44f, 0.48f, 1f),
                bodyFont,
                FontStyles.Normal,
                TextAlignmentOptions.Left);

            quoteIconImg = CreateIcon(
                fbInner,
                "QuoteIcon",
                checkSprite,
                new Vector2(-252f, -120f),
                new Vector2(36f, 36f),
                coinsColor);

            quoteText = CreateLabel(
                fbInner,
                "QuoteText",
                "\"The beach cabana is trending!\"",
                22,
                new Vector2(23f, -120f),
                new Vector2(494f, 54f),
                new Color(0.32f, 0.34f, 0.38f, 1f),
                bodyFont,
                FontStyles.Italic,
                TextAlignmentOptions.Left);

                
            // =========================================================================
            // RIGHT CARD: PREMIUM REWARDS (670 x 435)
            // =========================================================================
            GameObject rewardsCard = CreateBorderedBox(
                panelRoot.transform,
                "PremiumRewardsCard",
                new Vector2(345f, 5f),
                new Vector2(670f, 435f),
                purpleAccent,
                new Color(0.99f, 0.99f, 0.99f, 1f),
                5f,
                cardSprite);

            Transform rwInner = rewardsCard.transform.Find("InnerFill");

            CreateLabel(
                rwInner,
                "RewardsHeader",
                "PREMIUM REWARDS",
                30,
                new Vector2(0f, 162f),
                new Vector2(590f, 44f),
                purpleAccent,
                headerFont,
                FontStyles.Bold,
                TextAlignmentOptions.Left);

            // Row container with VerticalLayoutGroup so hidden rows leave no empty gaps
            GameObject rowsHolder = new GameObject("RewardRows", typeof(RectTransform), typeof(VerticalLayoutGroup));
            rowsHolder.transform.SetParent(rwInner, false);
            RectTransform rowsRt = rowsHolder.GetComponent<RectTransform>();
            rowsRt.anchorMin = new Vector2(0.5f, 0.5f);
            rowsRt.anchorMax = new Vector2(0.5f, 0.5f);
            rowsRt.sizeDelta = new Vector2(590f, 310f);
            rowsRt.anchoredPosition = new Vector2(0f, -26f);

            VerticalLayoutGroup vlg = rowsHolder.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 12f;
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            reputationRow = CreateRewardRow(
                rowsHolder.transform, "ReputationRow", reputationSprite, reputationColor,
                "+40 Reputation", bodyFont, out reputationRewardText);

            coinsRow = CreateRewardRow(
                rowsHolder.transform, "CoinsRow", coinSprite, coinsColor,
                "+120 Coins", bodyFont, out coinsRewardText);

            energyRow = CreateRewardRow(
                rowsHolder.transform, "EnergyRow", energySprite, energyColor,
                "+15 Energy", bodyFont, out energyRewardText);

            resortValueRow = CreateRewardRow(
                rowsHolder.transform, "ResortValueRow", starOutlineSprite != null ? starOutlineSprite : starFilledSprite, resortValueColor,
                "+50 Resort Value (700 -> 750)", bodyFont, out resortValueRewardText);

            bonusRow = CreateRewardRow(
                rowsHolder.transform, "BonusRow", giftSprite, bonusPinkColor,
                "+10 Gems (First Perfect Stay!)", bodyFont, out bonusRewardText);

            // =========================================================================
            // CLAIM REWARDS BUTTON (purple fill with white border)
            // =========================================================================
            GameObject btnBox = CreateBorderedBox(
                panelRoot.transform,
                "ClaimRewardsButton",
                new Vector2(0f, -328f),
                new Vector2(450f, 96f),
                Color.white,
                purpleAccent,
                6f,
                pillSprite != null ? pillSprite : cardSprite);

            claimButton = btnBox.AddComponent<Button>();
            claimButton.targetGraphic = btnBox.transform.Find("InnerFill").GetComponent<Image>();

            Transform btnInner = btnBox.transform.Find("InnerFill");
            if (claimButtonIconSprite != null)
            {
                CreateIcon(btnInner, "BtnIcon", claimButtonIconSprite, new Vector2(-148f, 0f), new Vector2(44f, 44f), Color.white);
                CreateLabel(btnInner, "BtnText", "CLAIM REWARDS", 31, new Vector2(24f, 0f), new Vector2(320f, 60f), Color.white, headerFont, FontStyles.Bold, TextAlignmentOptions.Center);
            }
            else
            {
                CreateLabel(btnInner, "BtnText", "CLAIM REWARDS", 32, Vector2.zero, new Vector2(390f, 60f), Color.white, headerFont, FontStyles.Bold, TextAlignmentOptions.Center);
            }
        }

        private void BuildPerfectBadgeContent(Transform badgeInner, TMP_FontAsset headerFont)
        {
            HorizontalLayoutGroup hlg = badgeInner.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 12f;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = true;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            Sprite badgeStar = starFilledSprite != null ? starFilledSprite : starOutlineSprite;

            if (badgeStar != null)
                CreateBadgeStar(badgeInner, "LeftStar", badgeStar);

            GameObject textGo = new GameObject("BadgeText", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(badgeInner, false);
            textGo.GetComponent<RectTransform>().sizeDelta = new Vector2(420f, 40f);

            TextMeshProUGUI tmp = textGo.GetComponent<TextMeshProUGUI>();
            if (headerFont != null) tmp.font = headerFont;
            tmp.text = "PERFECT STAY BONUS UNLOCKED";
            tmp.fontSize = 22f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.raycastTarget = false;

            if (badgeStar != null)
                CreateBadgeStar(badgeInner, "RightStar", badgeStar);
        }

        private static void CreateBadgeStar(Transform parent, string name, Sprite starSprite)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(20f, 20f);

            LayoutElement le = go.GetComponent<LayoutElement>();
            le.minWidth = 20f;
            le.preferredWidth = 20f;
            le.minHeight = 20f;
            le.preferredHeight = 20f;

            Image img = go.GetComponent<Image>();
            img.sprite = starSprite;
            img.color = Color.white;
            img.preserveAspect = true;
            img.raycastTarget = false;
        }

        private void CreateStarsRow(Transform parent, Vector2 anchoredPos)
        {
            starImages.Clear();

            GameObject row = new GameObject("StarsRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            row.transform.SetParent(parent, false);
            RectTransform rt = row.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(360f, 60f);
            rt.anchoredPosition = anchoredPos;

            HorizontalLayoutGroup hlg = row.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 12f;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            for (int i = 0; i < 5; i++)
            {
                GameObject starGo = new GameObject($"Star_{i + 1}", typeof(RectTransform), typeof(Image));
                starGo.transform.SetParent(row.transform, false);
                starGo.GetComponent<RectTransform>().sizeDelta = new Vector2(54f, 54f);

                Image img = starGo.GetComponent<Image>();
                img.sprite = starFilledSprite != null ? starFilledSprite : starOutlineSprite;
                img.preserveAspect = true;
                img.raycastTarget = false;
                img.color = filledStarColor;
                starImages.Add(img);
            }
        }

        private static GameObject CreateRewardRow(
            Transform parent,
            string name,
            Sprite iconSprite,
            Color textColor,
            string defaultText,
            TMP_FontAsset font,
            out TMP_Text valueText)
        {
            GameObject row = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup));
            row.transform.SetParent(parent, false);
            row.GetComponent<RectTransform>().sizeDelta = new Vector2(590f, 50f);

            HorizontalLayoutGroup hlg = row.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 16f;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            GameObject iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(row.transform, false);
            iconGo.GetComponent<RectTransform>().sizeDelta = new Vector2(42f, 42f);

            Image iconImg = iconGo.GetComponent<Image>();
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;
            if (iconSprite != null)
            {
                iconImg.sprite = iconSprite;
                iconImg.color = textColor;
            }
            else
            {
                iconImg.color = Color.clear;
            }

            GameObject textGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(row.transform, false);
            textGo.GetComponent<RectTransform>().sizeDelta = new Vector2(532f, 48f);

            TextMeshProUGUI tmp = textGo.GetComponent<TextMeshProUGUI>();
            if (font != null) tmp.font = font;
            tmp.text = defaultText;
            tmp.fontSize = 26f;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 19f;
            tmp.fontSizeMax = 26f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = textColor;
            tmp.alignment = TextAlignmentOptions.Left;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.raycastTarget = false;
            valueText = tmp;

            return row;
        }

        private void BuildConfettiLayer(Transform parent)
        {
            GameObject layer = new GameObject("ConfettiLayer", typeof(RectTransform));
            layer.transform.SetParent(parent, false);
            RectTransform rt = layer.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            Color[] palette =
            {
                new Color(0.99f, 0.78f, 0.16f, 0.85f),
                new Color(0.93f, 0.28f, 0.60f, 0.85f),
                new Color(0.22f, 0.74f, 0.97f, 0.85f),
                new Color(0.55f, 0.36f, 0.96f, 0.85f),
                new Color(0.29f, 0.87f, 0.50f, 0.85f),
                new Color(0.98f, 0.45f, 0.09f, 0.85f)
            };

            System.Random rng = new System.Random(42);
            for (int i = 0; i < 42; i++)
            {
                GameObject piece = new GameObject($"Confetti_{i}", typeof(RectTransform), typeof(Image));
                piece.transform.SetParent(layer.transform, false);
                RectTransform pRt = piece.GetComponent<RectTransform>();
                pRt.anchorMin = new Vector2(0.5f, 0.5f);
                pRt.anchorMax = new Vector2(0.5f, 0.5f);

                float x = (float)(rng.NextDouble() * 1760.0 - 880.0);
                float y = (float)(rng.NextDouble() * 940.0 - 470.0);
                pRt.anchoredPosition = new Vector2(x, y);

                bool isStar = i % 3 == 0 && starFilledSprite != null;
                pRt.sizeDelta = isStar
                    ? new Vector2(26f, 26f)
                    : new Vector2((float)(10 + rng.Next(12)), (float)(22 + rng.Next(26)));
                pRt.localRotation = Quaternion.Euler(0f, 0f, rng.Next(0, 360));

                Image img = piece.GetComponent<Image>();
                if (isStar) img.sprite = starFilledSprite;
                img.preserveAspect = isStar;
                img.color = palette[i % palette.Length];
                img.raycastTarget = false;
            }
        }

        private static GameObject CreateBorderedBox(
            Transform parent,
            string name,
            Vector2 anchoredPos,
            Vector2 size,
            Color borderColor,
            Color fillColor,
            float borderThickness,
            Sprite sprite)
        {
            GameObject outer = new GameObject(name, typeof(RectTransform), typeof(Image));
            outer.transform.SetParent(parent, false);
            RectTransform outerRt = outer.GetComponent<RectTransform>();
            outerRt.anchorMin = new Vector2(0.5f, 0.5f);
            outerRt.anchorMax = new Vector2(0.5f, 0.5f);
            outerRt.sizeDelta = size;
            outerRt.anchoredPosition = anchoredPos;

            ApplySprite(outer.GetComponent<Image>(), sprite, borderColor);

            GameObject inner = new GameObject("InnerFill", typeof(RectTransform), typeof(Image));
            inner.transform.SetParent(outer.transform, false);
            RectTransform innerRt = inner.GetComponent<RectTransform>();
            innerRt.anchorMin = Vector2.zero;
            innerRt.anchorMax = Vector2.one;
            innerRt.offsetMin = new Vector2(borderThickness, borderThickness);
            innerRt.offsetMax = new Vector2(-borderThickness, -borderThickness);

            ApplySprite(inner.GetComponent<Image>(), sprite, fillColor);
            return outer;
        }

        private static void ApplySprite(Image img, Sprite sprite, Color color)
        {
            img.sprite = sprite;
            img.color = color;
            img.type = sprite != null && sprite.border.sqrMagnitude > 0.001f
                ? Image.Type.Sliced
                : Image.Type.Simple;
        }

        private static Image CreateIcon(
            Transform parent,
            string name,
            Sprite sprite,
            Vector2 anchoredPos,
            Vector2 size,
            Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = anchoredPos;

            Image img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.preserveAspect = true;
            img.raycastTarget = false;
            go.SetActive(sprite != null);
            return img;
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
            TextAlignmentOptions align)
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