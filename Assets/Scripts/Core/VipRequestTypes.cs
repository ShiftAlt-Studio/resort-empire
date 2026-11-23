namespace ResortEmpire.Core
{
    public enum VipRequestState
    {
        Missing,
        Ready,
        InProgress,
        Completed
    }

    public enum VipBuildingRequirement
    {
        None,
        Pool,
        Bungalows,
        Spa
    }

    public enum VipStaffKind
    {
        None,
        Manager,
        Specialist
    }

    public struct VipStaffRequirement
    {
        public VipStaffKind Kind;
        public BuildingType ManagerOf;
        public SpecialistType Specialist;

        public static VipStaffRequirement None => default;

        public static VipStaffRequirement Manager(BuildingType building) =>
            new VipStaffRequirement { Kind = VipStaffKind.Manager, ManagerOf = building };

        public static VipStaffRequirement OfSpecialist(SpecialistType type) =>
            new VipStaffRequirement { Kind = VipStaffKind.Specialist, Specialist = type };
    }

    public struct VipRequestDefinition
    {
        public string Name;
        public GuestNeedType Need;
        public int Materials;
        public VipBuildingRequirement Building;
        public VipStaffRequirement Staff;
        public int CapIfMissing;
        public float DurationSeconds;
        public bool IsBonus;
    }

    // One request of an accepted VIP visit (runtime state).
    public sealed class VipRequest
    {
        public VipRequestDefinition Definition { get; }
        public VipRequestState State { get; internal set; }
        public float RemainingSeconds { get; internal set; }
        public bool SpeedUpUsed { get; internal set; }

        public int QualityCap { get; }
        public string CapReason { get; }

        public bool IsCapped => QualityCap < 5;
        public int QualityPercent => QualityCap * 20;

        public VipRequest(VipRequestDefinition definition, int qualityCap, string capReason)
        {
            Definition = definition;
            QualityCap = qualityCap;
            CapReason = capReason ?? string.Empty;
            State = definition.Materials > 0 ? VipRequestState.Missing : VipRequestState.Ready;
        }
    }

    public enum VipFixResult
    {
        Success,
        NoVisit,
        NotMissing,
        NotEnoughMaterials
    }

    public struct VipVisitResult
    {
        public VipGuestDefinition Guest;
        public float SatisfactionPercent;
        public int Stars;
        public bool IsPerfect;
        public int CompletedMain;
        public int TotalMain;
        public bool BonusCompleted;
        public bool TimedOut;
    }
}