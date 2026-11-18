
using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ResortEmpire.Core;

namespace ResortEmpire.UI
{
    public class StartWavePresenter : MonoBehaviour
    {
        [Header("Existing")]
        [SerializeField] private Button startWaveButton;
        [SerializeField] private TMP_Text forecastLabel;

        [Header("Sliding Forecast Panel")]
        [SerializeField] private Button forecastButton;
        [SerializeField] private RectTransform forecastPanel;
        [SerializeField] private CanvasGroup forecastCanvasGroup;
        [SerializeField] private TMP_Text panelForecastText;
        [SerializeField] private Button forecastOverlay;
        [SerializeField] private float slideDistance = 220f;
        [SerializeField, Min(0.05f)] private float animationDuration = 0.28f;
        [SerializeField] private bool closeWhenWaveStarts = true;

        private Vector2 openPosition;
        private Coroutine animationRoutine;
        private bool isOpen;
        private bool ready;

        private void Start()
        {
            if (startWaveButton != null)
                startWaveButton.onClick.AddListener(OnStartClicked);

            if (forecastButton != null)
                forecastButton.onClick.AddListener(ToggleForecast);

            if (forecastOverlay != null)
            {
                forecastOverlay.onClick.AddListener(CloseForecast);
                forecastOverlay.gameObject.SetActive(false);
            }

            if (forecastPanel != null)
            {
                openPosition = forecastPanel.anchoredPosition;

                if (forecastCanvasGroup == null)
                    forecastCanvasGroup = forecastPanel.GetComponent<CanvasGroup>();

                if (forecastCanvasGroup == null)
                    forecastCanvasGroup =
                        forecastPanel.gameObject.AddComponent<CanvasGroup>();

                SetPanelInstant(false);
            }

            if (GameState.Instance != null)
            {
                GameState.Instance.OnStateChanged += OnStateChanged;
                ready = true;
                Refresh();
            }
            else
            {
                Debug.LogWarning(
                    "StartWavePresenter: GameState.Instance is missing.", this);
            }
        }

        private void OnDestroy()
        {
            if (startWaveButton != null)
                startWaveButton.onClick.RemoveListener(OnStartClicked);

            if (forecastButton != null)
                forecastButton.onClick.RemoveListener(ToggleForecast);

            if (forecastOverlay != null)
                forecastOverlay.onClick.RemoveListener(CloseForecast);

            if (GameState.Instance != null)
                GameState.Instance.OnStateChanged -= OnStateChanged;
        }

        private void OnStartClicked()
        {
            if (GameState.Instance != null)
                GameState.Instance.TryStartWave();
        }

        private void OnStateChanged(ResortState state)
        {
            Refresh();
        }

        private void Refresh()
        {
            if (!ready || GameState.Instance == null) return;

            bool awaiting =
                GameState.Instance.CurrentState == ResortState.AWAITING_WAVE;

            if (startWaveButton != null)
                startWaveButton.gameObject.SetActive(awaiting);

            if (forecastButton != null)
                forecastButton.gameObject.SetActive(awaiting);

            var forecast = GameState.Instance.GetForecast();
            var compact = new StringBuilder("Next wave: ");
            var detailed = new StringBuilder();
            int total = 0;

            if (forecast != null)
            {
                for (int i = 0; i < forecast.Count; i++)
                {
                    var item = forecast[i];
                    int units = Mathf.Max(0, item.Units);
                    total += units;

                    if (i > 0)
                        compact.Append(" · ");

                    compact.Append(item.Need)
                        .Append(' ')
                        .Append(units);

                    detailed.Append(item.Need)
                        .Append("   ")
                        .Append(units)
                        .Append('\n');
                }
            }

            detailed.Insert(0, "NEXT WAVE FORECAST\n\n");
            detailed.Append("\nTotal demand: ").Append(total);

            if (forecastLabel != null)
            {
                bool showOldLabel =
                    awaiting && forecastButton == null;

                forecastLabel.gameObject.SetActive(showOldLabel);
                forecastLabel.text = compact.ToString();
            }

            if (panelForecastText != null)
                panelForecastText.text = detailed.ToString();

            if (!awaiting && closeWhenWaveStarts)
                SetPanelInstant(false);
        }

        public void ToggleForecast()
        {
            if (isOpen)
                CloseForecast();
            else
                OpenForecast();
        }

        public void OpenForecast()
        {
            if (forecastPanel == null ||
                GameState.Instance == null ||
                GameState.Instance.CurrentState != ResortState.AWAITING_WAVE)
                return;

            AnimatePanel(true);
        }

        public void CloseForecast()
        {
            if (forecastPanel == null || !isOpen)
                return;

            AnimatePanel(false);
        }

        private void AnimatePanel(bool open)
        {
            isOpen = open;

            if (animationRoutine != null)
                StopCoroutine(animationRoutine);

            if (forecastOverlay != null)
                forecastOverlay.gameObject.SetActive(open);

            animationRoutine = StartCoroutine(AnimateRoutine(open));
        }

        private IEnumerator AnimateRoutine(bool open)
        {
            forecastPanel.gameObject.SetActive(true);

            forecastCanvasGroup.blocksRaycasts = open;
            forecastCanvasGroup.interactable = open;

            Vector2 from = forecastPanel.anchoredPosition;
            Vector2 to = open
                ? openPosition
                : openPosition + Vector2.up * slideDistance;

            float alphaFrom = forecastCanvasGroup.alpha;
            float alphaTo = open ? 1f : 0f;
            float elapsed = 0f;

            while (elapsed < animationDuration)
            {
                elapsed += Time.unscaledDeltaTime;

                float t = Mathf.Clamp01(
                    elapsed / animationDuration);

                t = t * t * (3f - 2f * t);

                forecastPanel.anchoredPosition =
                    Vector2.LerpUnclamped(from, to, t);

                forecastCanvasGroup.alpha =
                    Mathf.Lerp(alphaFrom, alphaTo, t);

                yield return null;
            }

            forecastPanel.anchoredPosition = to;
            forecastCanvasGroup.alpha = alphaTo;

            if (!open)
                forecastPanel.gameObject.SetActive(false);

            animationRoutine = null;
        }

        private void SetPanelInstant(bool open)
        {
            isOpen = open;

            if (animationRoutine != null)
            {
                StopCoroutine(animationRoutine);
                animationRoutine = null;
            }

            if (forecastOverlay != null)
                forecastOverlay.gameObject.SetActive(open);

            if (forecastPanel == null ||
                forecastCanvasGroup == null)
                return;

            forecastPanel.anchoredPosition = open
                ? openPosition
                : openPosition + Vector2.up * slideDistance;

            forecastCanvasGroup.alpha = open ? 1f : 0f;
            forecastCanvasGroup.interactable = open;
            forecastCanvasGroup.blocksRaycasts = open;

            forecastPanel.gameObject.SetActive(open);
        }
    }
}
