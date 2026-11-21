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

        private BottleneckFeedback feedback;

        public GuestNeedType Need => need;

        private void Awake()
        {
            if (title != null)
                title.text = Regex.Replace(need.ToString(), "(?<=[a-z])(?=[A-Z])", " ");

            LayoutElement layoutElement = GetComponent<LayoutElement>();
            if (layoutElement != null)
                layoutElement.preferredHeight = -1f;
        }

        public void Refresh(int served, int total, NeedDisplayStatus status)
        {
            bool visible = status != NeedDisplayStatus.LOCKED;
            gameObject.SetActive(visible);
            if (!visible) return;

            counter.text = $"{served}/{total}";
            bar.fillAmount = total > 0 ? Mathf.Clamp01((float)served / total) : 0f;
            bar.color = UIColors.BarColor(status);
            background.color = Color.white;

            bool showWarning = status == NeedDisplayStatus.BOTTLENECK
                            || status == NeedDisplayStatus.RECOVERING;
            bool warningChanged = warning.gameObject.activeSelf != showWarning;
            warning.gameObject.SetActive(showWarning);

            if (showWarning)
            {
                if (feedback == null) feedback = FindAnyObjectByType<BottleneckFeedback>();
                string cause = feedback != null ? feedback.GetCauseText(need) : "";
                string lead = status == NeedDisplayStatus.RECOVERING ? "Catching up" : "Queue growing";
                warning.text = string.IsNullOrEmpty(cause) ? lead : $"{lead}: {cause}";
            }

            if (warningChanged) RebuildLayout();
        }

        private void RebuildLayout()
        {
            RectTransform root = transform as RectTransform;
            while (root != null && root.parent != null && root.parent.GetComponent<LayoutGroup>() != null)
                root = root.parent as RectTransform;

            if (root != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(root);
        }
    }
}