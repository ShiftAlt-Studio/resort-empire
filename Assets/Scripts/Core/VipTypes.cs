using System.Collections.Generic;

namespace ResortEmpire.Core
{
    public enum VipGuestType
    {
        TravelBlogger,
        WeddingCouple,
        Investor,
        Influencer
    }

    public enum VipReadiness
    {
        Good,
        Risky
    }

    public enum VipOfferStatus
    {
        Available,
        Accepted,
        Declined,
        Expired
    }

    public struct VipNeedReadiness
    {
        public GuestNeedType Need;
        public VipReadiness Level;

        public VipNeedReadiness(GuestNeedType need, VipReadiness level)
        {
            Need = need;
            Level = level;
        }
    }

    public struct VipRewardRange
    {
        public int Min;
        public int Max;

        public VipRewardRange(int min, int max)
        {
            Min = min;
            Max = max;
        }
    }

    public sealed class VipOffer
    {
        public VipGuestDefinition Guest { get; }
        public float TotalSeconds { get; }
        public float RemainingSeconds { get; internal set; }
        public VipOfferStatus Status { get; internal set; }
        public IReadOnlyList<VipNeedReadiness> Readiness { get; internal set; }

        public float RemainingNormalized => TotalSeconds <= 0f ? 0f : RemainingSeconds / TotalSeconds;

        public VipOffer(VipGuestDefinition guest, float totalSeconds)
        {
            Guest = guest;
            TotalSeconds = totalSeconds;
            RemainingSeconds = totalSeconds;
            Status = VipOfferStatus.Available;
            Readiness = new List<VipNeedReadiness>();
        }
    }
}