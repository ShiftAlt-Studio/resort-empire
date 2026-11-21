using ResortEmpire.Core;
using TMPro;
using UnityEngine;

namespace ResortEmpire.UI
{
    public class BottleneckToastView : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private float showSeconds = 4f;

        private BottleneckFeedback feedback;
        private float hideAt;

        private void Start()
        {
            feedback = FindAnyObjectByType<BottleneckFeedback>();
            if (feedback != null) feedback.FeedbackRaised += HandleFeedback;
            if (GameState.Instance != null) GameState.Instance.OnStateChanged += HandleStateChanged;
            Hide();
        }

        private void OnDestroy()
        {
            if (feedback != null) feedback.FeedbackRaised -= HandleFeedback;
            if (GameState.Instance != null) GameState.Instance.OnStateChanged -= HandleStateChanged;
        }

        private void HandleFeedback(GuestNeedType need, NeedStatus status, string message)
        {
            messageText.text = message;
            root.SetActive(true);
            hideAt = Time.unscaledTime + showSeconds;
        }

        private void Update()
        {
            if (root.activeSelf && Time.unscaledTime >= hideAt) Hide();
        }

        private void HandleStateChanged(ResortState state)
        {
            if (state == ResortState.WAVE_COMPLETE || state == ResortState.AWAITING_WAVE) Hide();
        }

        private void Hide() => root.SetActive(false);
    }
}