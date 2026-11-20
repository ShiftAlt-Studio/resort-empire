using UnityEngine;

namespace ResortEmpire.Core
{
    public static class UIColors
    {
        public static readonly Color Cream = Hex("#FFF8E7");
        public static readonly Color Teal = Hex("#0F4C5C");
        public static readonly Color Orange = Hex("#FF7A2F");
        public static readonly Color Green = Hex("#3FB27F");
        public static readonly Color Yellow = Hex("#FFC531");
        public static readonly Color Red = Hex("#E5484D");
        public static readonly Color BottleneckTint = Hex("#FDE3E3");

        public static Color BarColor(NeedDisplayStatus status)
        {
            switch (status)
            {
                case NeedDisplayStatus.BOTTLENECK: return Red;
                case NeedDisplayStatus.RECOVERING: return Yellow;
                default: return Green;
            }
        }

        private static Color Hex(string h)
        {
            ColorUtility.TryParseHtmlString(h, out Color c);
            return c;
        }
    }
}