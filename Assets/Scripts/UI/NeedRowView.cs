using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ResortEmpire.Core
{
    public class NeedRowView : MonoBehaviour
    {
        [SerializeField] private GuestNeedType need;
        [SerializeField] private Image background;
        [SerializeField] private Image bar;
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text counter;
        [SerializeField] private TMP_Text warning;

        public GuestNeedType Need => need;

        private void Awake()
        {
            if (title != null)
                title.text = Regex.Replace(need.ToString(), "(?<=[a-z])(?=[A-Z])", " ");
        }

        public void Refresh(int served, int total, NeedDisplayStatus status)
        {
            bool visible = status != NeedDisplayStatus.LOCKED;
            gameObject.SetActive(visible);
            if (!visible) return;

            counter.text = $"{served}/{total}";
            bar.fillAmount = total > 0 ? Mathf.Clamp01((float)served / total) : 0f;
            bar.color = UIColors.BarColor(status);
            background.color = status == NeedDisplayStatus.BOTTLENECK
                ? UIColors.BottleneckTint
                : UIColors.Cream;

            bool showWarning = status == NeedDisplayStatus.BOTTLENECK
                            || status == NeedDisplayStatus.RECOVERING;
            warning.gameObject.SetActive(showWarning);
            if (showWarning)
                warning.text = status == NeedDisplayStatus.RECOVERING ? "Catching up" : "Queue growing";
        }
    }
}