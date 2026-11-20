using UnityEngine;

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
        public static WaveRating FromSatisfaction(float percent, bool hadPlayerAction = true)
        {
            WaveRating rating;

            if (percent >= 100f) rating = WaveRating.Perfect;
            else if (percent >= 90f) rating = WaveRating.FiveStars;
            else if (percent >= 80f) rating = WaveRating.FourStars;
            else if (percent >= 70f) rating = WaveRating.ThreeStars;
            else if (percent >= 55f) rating = WaveRating.TwoStars;
            else rating = WaveRating.OneStar;

            // SS-140 / SS-141: vlna bez akcie hráča nesmie dosiahnuť 3★ a viac
            if (!hadPlayerAction && ToStarCount(rating) >= 3)
                rating = WaveRating.TwoStars;

            return rating;
        }

        public static int ToStarCount(WaveRating rating)
        {
            switch (rating)
            {
                case WaveRating.OneStar:    return 1;
                case WaveRating.TwoStars:   return 2;
                case WaveRating.ThreeStars: return 3;
                case WaveRating.FourStars:  return 4;
                case WaveRating.FiveStars:  return 5;
                case WaveRating.Perfect:    return 5;
                default:                    return 1;
            }
        }
    }
}