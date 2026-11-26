using System;
using System.Collections.Generic;
using UnityEngine;

namespace ResortEmpire.Core
{
    [Serializable]
    public struct VipRewardSummary
    {
        public VipVisitResult VisitResult;
        public int ReputationGranted;
        public int CoinsGranted;
        public int EnergyGranted;
        public int ResortValueGranted;
        public int GemsGranted;
        public int ResortValueBefore;
        public int ResortValueAfter;
        public string[] CompletedRequestNames;
        public string FeedbackQuote;
        public string BonusLabel;
    }

    public class VipRewardService : MonoBehaviour
    {
        [SerializeField] private VipVisitService visitService;

        public VipRewardSummary LastSummary { get; private set; }
        public bool HasUnclaimedRewards { get; private set; }

        public event Action<VipRewardSummary> RewardsGranted;
        public event Action<VipRewardSummary> RewardsClaimed;

        private void Start()
        {
            if (visitService == null)
                visitService = FindAnyObjectByType<VipVisitService>();

            if (visitService != null)
                visitService.VisitEnded += HandleVisitEnded;
        }

        private void OnDestroy()
        {
            if (visitService != null)
                visitService.VisitEnded -= HandleVisitEnded;
        }

        private void HandleVisitEnded(VipVisitResult result)
        {
            var completedNames = new List<string>();
            if (visitService != null && visitService.Requests != null)
            {
                foreach (VipRequest req in visitService.Requests)
                {
                    if (req.State == VipRequestState.Completed)
                        completedNames.Add(req.Definition.Name);
                }
            }

            GrantRewards(result, completedNames.ToArray());
        }

        private void GrantRewards(VipVisitResult result, string[] completedRequestNames)
        {
            float factor = GetRatingFactor(result);

            int reputation = CalculateRangeReward(result.Guest.Reputation, factor, minGuaranteed: 1);
            int coins = CalculateRangeReward(result.Guest.Coins, factor, minGuaranteed: 1);
            int energy = CalculateRangeReward(result.Guest.Energy, factor, minGuaranteed: 0);
            int resortValue = CalculateRangeReward(result.Guest.ResortValue, factor, minGuaranteed: 1);

            int resortBefore = GameState.Instance != null ? GameState.Instance.ResortValue : 0;
            int gems = 0;

            if (GameState.Instance != null)
            {
                GameState.Instance.AddReputation(reputation);
                GameState.Instance.AddCoins(coins);
                if (energy > 0)
                    GameState.Instance.AddEnergy(energy);
                GameState.Instance.AddResortValue(resortValue);

                if (result.IsPerfect)
                    gems = GameState.Instance.GrantFirstPerfectStayGems();
            }

            int resortAfter = GameState.Instance != null
                ? GameState.Instance.ResortValue
                : resortBefore + resortValue;

            string bonusLabel = string.Empty;
            if (gems > 0)
                bonusLabel = $"+{gems} Gems (First Perfect Stay!)";
            else if (result.IsPerfect)
                bonusLabel = "Perfect Stay Bonus Achieved!";

            LastSummary = new VipRewardSummary
            {
                VisitResult = result,
                ReputationGranted = reputation,
                CoinsGranted = coins,
                EnergyGranted = energy,
                ResortValueGranted = resortValue,
                GemsGranted = gems,
                ResortValueBefore = resortBefore,
                ResortValueAfter = resortAfter,
                CompletedRequestNames = completedRequestNames ?? Array.Empty<string>(),
                FeedbackQuote = BuildFeedbackQuote(result),
                BonusLabel = bonusLabel
            };

            HasUnclaimedRewards = true;

            Debug.Log(
                $"[VipRewardService] {result.Guest.GuestName} finished ({result.Stars}* / {result.SatisfactionPercent:F0}%, perfect={result.IsPerfect}): " +
                $"+{reputation} Rep, +{coins} Coins, +{energy} Energy, +{resortValue} Resort Value ({resortBefore}->{resortAfter}), +{gems} Gems");

            RewardsGranted?.Invoke(LastSummary);
        }

        public void ClaimRewards()
        {
            if (!HasUnclaimedRewards) return;

            HasUnclaimedRewards = false;
            Debug.Log($"[VipRewardService] Player claimed VIP rewards for {LastSummary.VisitResult.Guest.GuestName}.");
            RewardsClaimed?.Invoke(LastSummary);
        }

        public static float GetRatingFactor(VipVisitResult result)
        {
            if (result.IsPerfect) return 1.0f;

            switch (result.Stars)
            {
                case 5: return 0.85f;
                case 4: return 0.60f;
                case 3: return 0.35f;
                default: return 0.0f;
            }
        }

        private static int CalculateRangeReward(VipRewardRange range, float factor, int minGuaranteed)
        {
            int value = Mathf.RoundToInt(Mathf.Lerp(range.Min, range.Max, Mathf.Clamp01(factor)));
            return Mathf.Max(minGuaranteed, value);
        }

        private static string BuildFeedbackQuote(VipVisitResult result)
        {
            if (result.IsPerfect || result.Stars >= 5)
            {
                switch (result.Guest.Type)
                {
                    case VipGuestType.TravelBlogger:
                        return "“The beach cabana is trending!”";
                    case VipGuestType.WeddingCouple:
                        return "“The most magical honeymoon we could imagine!”";
                    case VipGuestType.Investor:
                        return "“World-class operations. This resort is a winner.”";
                    default:
                        return "“Every corner is pure gold for my feed!”";
                }
            }

            if (result.Stars >= 4)
                return "“Wonderful stay overall, just a tiny detail away from perfection!”";

            if (result.Stars >= 3)
                return "“Nice island vibes, though service could be smoother.”";

            return "“Too many delays this time — I expected more from a VIP stay.”";
        }

        [ContextMenu("Debug/Simulate VIP Complete (Perfect 5★)")]
        private void DebugSimulatePerfect()
        {
            VipGuestDefinition chloe = VipTable.Get(VipGuestType.TravelBlogger);
            var fake = new VipVisitResult
            {
                Guest = chloe,
                SatisfactionPercent = 100f,
                Stars = 5,
                IsPerfect = true,
                CompletedMain = 3,
                TotalMain = 3,
                BonusCompleted = true,
                TimedOut = false
            };

            GrantRewards(fake, new[]
            {
                "Tropical Fruit Platter",
                "Signature Mai Tai",
                "Private Pool Session",
                "Photo Shoot Spot"
            });
        }

        [ContextMenu("Debug/Simulate VIP Complete (3★ Partial)")]
        private void DebugSimulatePartial()
        {
            VipGuestDefinition investor = VipTable.Get(VipGuestType.Investor);
            var fake = new VipVisitResult
            {
                Guest = investor,
                SatisfactionPercent = 62f,
                Stars = 3,
                IsPerfect = false,
                CompletedMain = 2,
                TotalMain = 3,
                BonusCompleted = false,
                TimedOut = true
            };

            GrantRewards(fake, new[]
            {
                "Executive Briefing Room",
                "Private Cocktail Reception"
            });
        }
    }
}