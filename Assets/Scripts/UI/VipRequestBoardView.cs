using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ResortEmpire.Core;

namespace ResortEmpire.UI
{
    public class VipRequestBoardView : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] private GameObject root;
        [SerializeField] private Button dimButton;

        [Header("Header")]
        [SerializeField] private Image avatarImage;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text timerText;

        [Header("Satisfaction")]
        [SerializeField] private RectTransform satisfactionFill;
        [SerializeField] private TMP_Text mark3Text;
        [SerializeField] private TMP_Text mark4Text;
        [SerializeField] private TMP_Text mark5Text;
        [SerializeField] private Image[] stars;
        [SerializeField] private Sprite starFilled;
        [SerializeField] private Sprite starEmpty;
        [SerializeField] private Color starFilledColor = new Color32(0xFB, 0xBF, 0x24, 0xFF);
        [SerializeField] private Color starEmptyColor = new Color32(0xB8, 0xB8, 0xB8, 0xFF);

        [Header("Requests")]
        [SerializeField] private VipRequestRowView[] rows;

        [Header("Quote")]
        [SerializeField] private TMP_Text quoteText;
        [SerializeField] private string[] quotes =
        {
            "Let's see what this resort can do.",
            "That was lovely! What's next?",
            "Almost perfect. Keep it up!"
        };

        [Header("Rewards")]
        [SerializeField] private TMP_Text reputationText;
        [SerializeField] private TMP_Text coinsText;
        [SerializeField] private TMP_Text energyText;
        [SerializeField] private TMP_Text resortValueText;

        [Header("Buttons")]
        [SerializeField] private Button serveAllButton;

        public event Action<int> RowActionClicked;
        public event Action ServeAllClicked;
        public event Action CloseClicked;

        public bool IsVisible => root != null && root.activeSelf;

        private void Awake()
        {
            if (serveAllButton != null)
                serveAllButton.onClick.AddListener(OnServeAll);
            if (dimButton != null)
                dimButton.onClick.AddListener(OnClose);

            if (rows != null)
                foreach (VipRequestRowView row in rows)
                    if (row != null) row.ActionClicked += OnRowAction;
        }

        private void OnDestroy()
        {
            if (serveAllButton != null)
                serveAllButton.onClick.RemoveListener(OnServeAll);
            if (dimButton != null)
                dimButton.onClick.RemoveListener(OnClose);

            if (rows != null)
                foreach (VipRequestRowView row in rows)
                    if (row != null) row.ActionClicked -= OnRowAction;
        }

        public void Show(VipGuestDefinition guest, Sprite portrait)
        {
            if (root != null) root.SetActive(true);

            if (titleText != null)
            {
                string name = guest.GuestName;
                if (name.Contains("&"))
                {
                    int lastSpace = name.LastIndexOf(' ');
                    string pair = lastSpace > 0 ? name.Substring(0, lastSpace) : name;
                    titleText.text = $"{pair.ToUpper()}' DEMANDS";
                }
                else
                {
                    titleText.text = $"{name.Split(' ')[0].ToUpper()}'S DEMANDS";
                }
            }

            if (avatarImage != null && portrait != null)
                avatarImage.sprite = portrait;

            int[] t = VipTable.RatingThresholds;
            if (mark3Text != null) mark3Text.text = $"3 stars ({t[0]}%)";
            if (mark4Text != null) mark4Text.text = $"4 stars ({t[1]}%)";
            if (mark5Text != null) mark5Text.text = $"5 stars ({t[2]}%)";

            if (reputationText != null) reputationText.text = $"+{guest.Reputation.Min}-{guest.Reputation.Max} Reputation";
            if (coinsText != null) coinsText.text = $"+{guest.Coins.Min}-{guest.Coins.Max} Coins";
            if (energyText != null) energyText.text = $"+{guest.Energy.Min}-{guest.Energy.Max} Energy";
            if (resortValueText != null) resortValueText.text = $"+{guest.ResortValue.Min}-{guest.ResortValue.Max} Resort Value";
        }

        public void Hide()
        {
            if (root != null) root.SetActive(false);
        }

        public void Refresh(VipVisitService visit)
        {
            if (visit == null || !visit.IsActive) return;

            int seconds = Mathf.CeilToInt(Mathf.Max(0f, visit.RemainingSeconds));
            if (timerText != null)
                timerText.text = $"TIME LEFT: {seconds / 60:00}:{seconds % 60:00}";

            float percent = visit.SatisfactionPercent;
            if (satisfactionFill != null)
                satisfactionFill.anchorMax = new Vector2(Mathf.Clamp01(percent / 100f), satisfactionFill.anchorMax.y);

            int[] t = VipTable.RatingThresholds;
            int filled = percent >= t[2] ? 5 : percent >= t[1] ? 4 : percent >= t[0] ? 3 : 0;
            if (stars != null)
            {
                for (int i = 0; i < stars.Length; i++)
                {
                    if (stars[i] == null) continue;
                    bool on = i < filled;
                    if (on && starFilled != null) stars[i].sprite = starFilled;
                    if (!on && starEmpty != null) stars[i].sprite = starEmpty;
                    stars[i].color = on ? starFilledColor : starEmptyColor;
                }
            }

            int completedMain = 0;
            int totalMain = 0;
            bool canServe = false;

            for (int i = 0; i < rows.Length; i++)
            {
                if (rows[i] == null) continue;

                if (i < visit.Requests.Count)
                    rows[i].Bind(i, visit.Requests[i]);
                else
                    rows[i].Hide();
            }

            for (int i = 0; i < visit.Requests.Count; i++)
            {
                VipRequest r = visit.Requests[i];
                if (!r.Definition.IsBonus)
                {
                    totalMain++;
                    if (r.State == VipRequestState.Completed) completedMain++;
                }
                if (r.State == VipRequestState.Missing || r.State == VipRequestState.Ready)
                    canServe = true;
            }

            if (serveAllButton != null)
                serveAllButton.interactable = canServe;

            if (quoteText != null && quotes != null && quotes.Length > 0)
            {
                int q = completedMain == 0 ? 0 : completedMain >= totalMain ? quotes.Length - 1 : Mathf.Min(1, quotes.Length - 1);
                quoteText.text = quotes[q];
            }
        }


        private void OnRowAction(int index) => RowActionClicked?.Invoke(index);
        private void OnServeAll() => ServeAllClicked?.Invoke();
        private void OnClose() => CloseClicked?.Invoke();
    }
}