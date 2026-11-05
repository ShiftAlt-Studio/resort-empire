using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ResortEmpire.Core;

namespace ResortEmpire.UI{
    public class StartWavePresenter : MonoBehaviour
    {
        [SerializeField] private Button startWaveButton;
        [SerializeField] private TMP_Text forecastLabel;

        private void Start()
        {
            startWaveButton.onClick.AddListener(OnStartClicked);
            GameState.Instance.OnStateChanged += OnStateChanged;
            Refresh();
        }

        private void OnDestroy()
        {
            if (GameState.Instance != null)
                GameState.Instance.OnStateChanged -= OnStateChanged;
        }

        private void OnStartClicked()
        {
            GameState.Instance.TryStartWave();
        }

        private void OnStateChanged(ResortState state)
        {
            Refresh();
        }

        private void Refresh()
        {
            bool awaiting = GameState.Instance.CurrentState == ResortState.AWAITING_WAVE;
            startWaveButton.gameObject.SetActive(awaiting);

            if (forecastLabel == null) return;
            forecastLabel.gameObject.SetActive(awaiting);
            if (!awaiting) return;

            var sb = new StringBuilder("Next wave: ");
            var forecast = GameState.Instance.GetForecast();
            for (int i = 0; i < forecast.Count; i++)
            {
                if (i > 0) sb.Append(" · ");
                sb.Append(forecast[i].Need).Append(' ').Append(forecast[i].Units);
            }
            forecastLabel.text = sb.ToString();
        }
    }
}