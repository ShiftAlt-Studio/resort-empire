namespace ResortEmpire.Core
{
    public struct VipGuestDefinition
    {
        public VipGuestType Type;
        public string GuestName;
        public string Title;
        public string Promise;
        public int Difficulty;           
        public GuestNeedType[] Needs;
        public VipRewardRange Coins;
        public VipRewardRange ResortValue;
        public VipRewardRange Reputation;
        public VipRewardRange Energy;
    }

    public static class VipTable
    {
        public const int FirstOfferAfterWave = 3;

        public const float OfferAvailableSeconds = 180f;

        public static readonly int[] RatingThresholds = { 50, 70, 90, 100 };

        private static readonly VipGuestDefinition[] Guests =
        {
            new VipGuestDefinition
            {
                Type = VipGuestType.TravelBlogger,
                GuestName = "Chloe Vance",
                Title = "Lifestyle & Luxury Travel Blogger",
                Promise = "I'm writing a piece on the ultimate island stay. Give me exquisite meals, pool relaxation, and smooth drinks!",
                Difficulty = 4,
                Needs = new[] { GuestNeedType.Food, GuestNeedType.Drinks, GuestNeedType.Relax },
                Coins = new VipRewardRange(60, 120),
                ResortValue = new VipRewardRange(15, 50),
                Reputation = new VipRewardRange(10, 40),
                Energy = new VipRewardRange(0, 15)
            },
            new VipGuestDefinition
            {
                Type = VipGuestType.WeddingCouple,
                GuestName = "Emma & Lucas Hart",
                Title = "Newlyweds on Honeymoon",
                Promise = "We want a romantic stay: a quiet room, a special dinner and a spa day for two.",
                Difficulty = 3,
                Needs = new[] { GuestNeedType.Rooms, GuestNeedType.Food, GuestNeedType.Relax },
                Coins = new VipRewardRange(50, 100),
                ResortValue = new VipRewardRange(12, 40),
                Reputation = new VipRewardRange(8, 30),
                Energy = new VipRewardRange(0, 10)
            },
            new VipGuestDefinition
            {
                Type = VipGuestType.Investor,
                GuestName = "Marcus Bell",
                Title = "Hospitality Investor",
                Promise = "I'm scouting resorts to back. Impress me with service speed and a view worth paying for.",
                Difficulty = 5,
                Needs = new[] { GuestNeedType.Rooms, GuestNeedType.Drinks, GuestNeedType.OceanView },
                Coins = new VipRewardRange(80, 160),
                ResortValue = new VipRewardRange(20, 60),
                Reputation = new VipRewardRange(12, 50),
                Energy = new VipRewardRange(0, 20)
            },
            new VipGuestDefinition
            {
                Type = VipGuestType.Influencer,
                GuestName = "Zoe Rivera",
                Title = "Wellness Influencer",
                Promise = "My followers want fresh food, a wellness shoot and a picture-perfect spot. Make it shine!",
                Difficulty = 2,
                Needs = new[] { GuestNeedType.Food, GuestNeedType.Relax },
                Coins = new VipRewardRange(40, 80),
                ResortValue = new VipRewardRange(10, 30),
                Reputation = new VipRewardRange(6, 25),
                Energy = new VipRewardRange(0, 8)
            }
        };

        public static int Count => Guests.Length;

        public static VipGuestDefinition Get(int index) => Guests[((index % Guests.Length) + Guests.Length) % Guests.Length];

        public static VipGuestDefinition Get(VipGuestType type)
        {
            foreach (VipGuestDefinition g in Guests)
                if (g.Type == type) return g;
            return Guests[0];
        }
    }
}