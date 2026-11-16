using ResortEmpire.Core;
using UnityEngine;

namespace ResortEmpire.UI
{
    public class UpgradeSuggestionPresenter : MonoBehaviour
    {
        [SerializeField] private UpgradePanel upgradePanel;

        private UpgradeRecommender recommender;

        private void Start()
        {
            recommender = FindAnyObjectByType<UpgradeRecommender>();
            if (GameState.Instance != null)
                GameState.Instance.OnStateChanged += HandleStateChanged;
        }

        private void OnDestroy()
        {
            if (GameState.Instance != null)
                GameState.Instance.OnStateChanged -= HandleStateChanged;
        }

        private void HandleStateChanged(ResortState state)
        {
            if (state != ResortState.UPGRADE && state != ResortState.AWAITING_WAVE) return;
            if (recommender == null || !recommender.HasRecommendation) return;

            upgradePanel.Open(recommender.RecommendedBuilding, $"{recommender.RecommendedNeed} queue was growing");
            recommender.Consume();
        }
    }
}