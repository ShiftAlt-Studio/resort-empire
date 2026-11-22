namespace ResortEmpire.Core
{
    public enum SpecialistType
    {
        Butler,
        Captain
    }

    public static class SpecialistTable
    {
        public static int GetCost(SpecialistType type)
        {
            switch (type)
            {
                case SpecialistType.Butler: return 1000;
                case SpecialistType.Captain: return 1000;
                default: return 1000;
            }
        }

        public static string GetDisplayName(SpecialistType type)
        {
            switch (type)
            {
                case SpecialistType.Butler: return "Butler";
                case SpecialistType.Captain: return "Captain";
                default: return type.ToString();
            }
        }
    }
}