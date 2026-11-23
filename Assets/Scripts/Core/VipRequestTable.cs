namespace ResortEmpire.Core
{
    public static class VipRequestTable
    {
        public const float VisitSeconds = 300f;

        public const int SpeedUpEnergyCost = 3;
        public const float SpeedUpTimeFactor = 0.5f;

        public const float BonusWeight = 0.1f;

        public static VipRequestDefinition[] GetRequests(VipGuestType type)
        {
            switch (type)
            {
                case VipGuestType.TravelBlogger:
                    return new[]
                    {
                        Req("Tropical Fruit Platter", GuestNeedType.Food, 2, VipBuildingRequirement.None, VipStaffRequirement.None, 5, 45f),
                        Req("Signature Mai Tai", GuestNeedType.Drinks, 1, VipBuildingRequirement.None, VipStaffRequirement.Manager(BuildingType.BeachBar), 4, 60f),
                        Req("Private Pool Session", GuestNeedType.Relax, 0, VipBuildingRequirement.Pool, VipStaffRequirement.None, 3, 50f),
                        Bonus("Photo Shoot Spot", GuestNeedType.Relax, 1, VipBuildingRequirement.None, VipStaffRequirement.OfSpecialist(SpecialistType.Captain), 4, 40f)
                    };

                case VipGuestType.WeddingCouple:
                    return new[]
                    {
                        Req("Romantic Dinner", GuestNeedType.Food, 2, VipBuildingRequirement.None, VipStaffRequirement.Manager(BuildingType.Restaurant), 4, 60f),
                        Req("Honeymoon Suite", GuestNeedType.Rooms, 1, VipBuildingRequirement.Bungalows, VipStaffRequirement.None, 3, 50f),
                        Req("Couples Spa", GuestNeedType.Relax, 0, VipBuildingRequirement.Spa, VipStaffRequirement.None, 3, 70f),
                        Bonus("Sunset Boat Ride", GuestNeedType.OceanView, 1, VipBuildingRequirement.None, VipStaffRequirement.OfSpecialist(SpecialistType.Captain), 4, 45f)
                    };

                case VipGuestType.Investor:
                    return new[]
                    {
                        Req("Executive Briefing Room", GuestNeedType.Rooms, 2, VipBuildingRequirement.None, VipStaffRequirement.Manager(BuildingType.Reception), 4, 50f),
                        Req("Private Cocktail Reception", GuestNeedType.Drinks, 2, VipBuildingRequirement.None, VipStaffRequirement.OfSpecialist(SpecialistType.Butler), 4, 60f),
                        Req("Ocean View Tour", GuestNeedType.OceanView, 1, VipBuildingRequirement.None, VipStaffRequirement.OfSpecialist(SpecialistType.Captain), 3, 55f),
                        Bonus("Investor Gift Basket", GuestNeedType.Food, 3, VipBuildingRequirement.None, VipStaffRequirement.None, 5, 40f)
                    };

                default: // Influencer
                    return new[]
                    {
                        Req("Fresh Juice Shoot", GuestNeedType.Food, 1, VipBuildingRequirement.None, VipStaffRequirement.None, 5, 40f),
                        Req("Wellness Content Shoot", GuestNeedType.Relax, 1, VipBuildingRequirement.Spa, VipStaffRequirement.None, 3, 60f),
                        Req("Butler-Styled Breakfast", GuestNeedType.Food, 2, VipBuildingRequirement.None, VipStaffRequirement.OfSpecialist(SpecialistType.Butler), 4, 50f),
                        Bonus("Sunrise Reel", GuestNeedType.Relax, 1, VipBuildingRequirement.None, VipStaffRequirement.OfSpecialist(SpecialistType.Captain), 4, 45f)
                    };
            }
        }

        public static void RatingFor(float percent, out int stars, out bool perfect)
        {
            int[] t = VipTable.RatingThresholds;
            perfect = percent >= t[3];
            if (percent >= t[2]) stars = 5;
            else if (percent >= t[1]) stars = 4;
            else if (percent >= t[0]) stars = 3;
            else stars = 2;
        }

        private static VipRequestDefinition Req(string name, GuestNeedType need, int materials,
            VipBuildingRequirement building, VipStaffRequirement staff, int capIfMissing, float seconds) =>
            new VipRequestDefinition
            {
                Name = name, Need = need, Materials = materials, Building = building, Staff = staff,
                CapIfMissing = capIfMissing, DurationSeconds = seconds, IsBonus = false
            };

        private static VipRequestDefinition Bonus(string name, GuestNeedType need, int materials,
            VipBuildingRequirement building, VipStaffRequirement staff, int capIfMissing, float seconds)
        {
            VipRequestDefinition d = Req(name, need, materials, building, staff, capIfMissing, seconds);
            d.IsBonus = true;
            return d;
        }
    }
}