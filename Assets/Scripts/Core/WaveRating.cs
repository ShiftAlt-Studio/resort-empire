namespace ResortEmpire.Core
{
    public enum WaveRating
    {
        OneStar,
        TwoStars,
        ThreeStars,
        FourStars,
        FiveStars,
        Perfect
    }

    public static class WaveRatingCalculator
    {
        public static WaveRating FromSatisfaction(float percent)
        {
            if (percent >= 100f) return WaveRating.Perfect;
            if (percent >= 90f) return WaveRating.FiveStars;
            if (percent >= 80f) return WaveRating.FourStars;
            if (percent >= 70f) return WaveRating.ThreeStars;
            if (percent >= 55f) return WaveRating.TwoStars;
            return WaveRating.OneStar;
        }
    }
}