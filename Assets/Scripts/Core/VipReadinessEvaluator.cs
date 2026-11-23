using System;
using UnityEngine;

namespace ResortEmpire.Core
{
    public class VipReadinessEvaluator : MonoBehaviour, IVipReadinessEvaluator
    {
        [SerializeField] private VipOfferService offerService;
        [SerializeField] private SpecialistService specialistService;
        [SerializeField] private AreaActivationService areaService;

        public Func<BuildingType, bool> HasManager { get; set; } = building => false;
        public Func<bool> SpaBuilt { get; set; } = () => false;

        private void Start()
        {
            if (offerService == null) offerService = FindAnyObjectByType<VipOfferService>();
            if (specialistService == null) specialistService = FindAnyObjectByType<SpecialistService>();
            if (areaService == null) areaService = FindAnyObjectByType<AreaActivationService>();

            if (offerService != null)
            {
                offerService.Evaluator = this;
                offerService.RefreshReadiness();
            }

            if (specialistService != null) specialistService.SpecialistHired += HandleRosterChanged;
            if (areaService != null) areaService.AreaActivated += HandleAreaChanged;
        }

        private void OnDestroy()
        {
            if (specialistService != null) specialistService.SpecialistHired -= HandleRosterChanged;
            if (areaService != null) areaService.AreaActivated -= HandleAreaChanged;
        }

        private void HandleRosterChanged(SpecialistType type) => offerService?.RefreshReadiness();
        private void HandleAreaChanged(AreaType type) => offerService?.RefreshReadiness();

        public VipReadiness Evaluate(VipGuestDefinition guest, GuestNeedType need)
        {
            if (GameState.Instance != null && !GameState.Instance.IsNeedUnlocked(need))
                return VipReadiness.Risky;

            foreach (VipRequestDefinition def in VipRequestTable.GetRequests(guest.Type))
            {
                if (def.IsBonus || def.Need != need) continue;

                EvaluateRequest(def, out int cap, out _);
                if (cap < 5) return VipReadiness.Risky;
            }
            return VipReadiness.Good;
        }

        public void EvaluateRequest(VipRequestDefinition def, out int cap, out string reason)
        {
            cap = 5;
            reason = string.Empty;

            if (def.Building != VipBuildingRequirement.None && !IsBuildingAvailable(def.Building))
            {
                cap = Mathf.Min(cap, def.CapIfMissing);
                reason = $"no {def.Building}";
            }

            if (def.Staff.Kind != VipStaffKind.None && !HasStaff(def.Staff))
            {
                cap = Mathf.Min(cap, def.CapIfMissing);
                string staff = $"no {StaffName(def.Staff)}";
                reason = reason.Length == 0 ? staff : $"{reason}, {staff}";
            }
        }

        private bool IsBuildingAvailable(VipBuildingRequirement building)
        {
            switch (building)
            {
                case VipBuildingRequirement.Pool:
                    return areaService != null && areaService.IsActivated(AreaType.Pool);
                case VipBuildingRequirement.Bungalows:
                    return areaService != null && areaService.IsActivated(AreaType.Bungalows);
                case VipBuildingRequirement.Spa:
                    return SpaBuilt != null && SpaBuilt();
                default:
                    return true;
            }
        }

        private bool HasStaff(VipStaffRequirement staff)
        {
            switch (staff.Kind)
            {
                case VipStaffKind.Specialist:
                    return specialistService != null && specialistService.IsHired(staff.Specialist);
                case VipStaffKind.Manager:
                    return HasManager != null && HasManager(staff.ManagerOf);
                default:
                    return true;
            }
        }

        private static string StaffName(VipStaffRequirement staff)
        {
            return staff.Kind == VipStaffKind.Specialist
                ? SpecialistTable.GetDisplayName(staff.Specialist)
                : $"{staff.ManagerOf} Manager";
        }
    }
}