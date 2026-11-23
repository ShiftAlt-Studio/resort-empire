using System;
using System.Collections.Generic;
using UnityEngine;

namespace ResortEmpire.Core
{
    public class VipVisitService : MonoBehaviour
    {
        [SerializeField] private VipOfferService offerService;
        [SerializeField] private VipReadinessEvaluator readiness;

        private readonly List<VipRequest> requests = new List<VipRequest>();

        public bool IsActive { get; private set; }
        public VipGuestDefinition Guest { get; private set; }
        public float RemainingSeconds { get; private set; }
        public IReadOnlyList<VipRequest> Requests => requests;

        public event Action VisitStarted;
        public event Action VisitChanged;
        public event Action<VipVisitResult> VisitEnded;

        private void Start()
        {
            if (offerService == null) offerService = FindAnyObjectByType<VipOfferService>();
            if (readiness == null) readiness = FindAnyObjectByType<VipReadinessEvaluator>();

            if (offerService != null)
                offerService.OfferAccepted += HandleOfferAccepted;
        }

        private void OnDestroy()
        {
            if (offerService != null)
                offerService.OfferAccepted -= HandleOfferAccepted;
        }

        private void Update()
        {
            if (!IsActive) return;
            if (GameState.Instance != null && GameState.Instance.CurrentState == ResortState.WAVE_PAUSED) return;

            float dt = Time.deltaTime;
            bool changed = false;

            foreach (VipRequest r in requests)
            {
                if (r.State != VipRequestState.InProgress) continue;

                r.RemainingSeconds -= dt;
                if (r.RemainingSeconds <= 0f)
                {
                    r.RemainingSeconds = 0f;
                    r.State = VipRequestState.Completed;
                    Debug.Log($"[VipVisitService] Completed: {r.Definition.Name}");
                    changed = true;
                }
            }

            RemainingSeconds -= dt;

            if (changed)
                VisitChanged?.Invoke();

            if (AllMainCompleted())
                EndVisit(false);
            else if (RemainingSeconds <= 0f)
                EndVisit(true);
        }

        public float SatisfactionPercent
        {
            get
            {
                float main = 0f;
                int mainCount = 0;
                float bonus = 0f;

                foreach (VipRequest r in requests)
                {
                    if (r.Definition.IsBonus)
                    {
                        if (r.State == VipRequestState.Completed) bonus += r.QualityPercent;
                    }
                    else
                    {
                        mainCount++;
                        if (r.State == VipRequestState.Completed) main += r.QualityPercent;
                    }
                }

                float average = mainCount > 0 ? main / mainCount : 0f;
                return Mathf.Min(100f, average + bonus * VipRequestTable.BonusWeight);
            }
        }

        public VipFixResult Fix(int index)
        {
            if (!IsActive || index < 0 || index >= requests.Count) return VipFixResult.NoVisit;

            VipRequest r = requests[index];
            if (r.State != VipRequestState.Missing) return VipFixResult.NotMissing;
            if (GameState.Instance == null || !GameState.Instance.TrySpendMaterials(r.Definition.Materials))
                return VipFixResult.NotEnoughMaterials;

            r.State = VipRequestState.Ready;
            VisitChanged?.Invoke();
            return VipFixResult.Success;
        }

        public bool StartRequest(int index)
        {
            if (!IsActive || index < 0 || index >= requests.Count) return false;

            VipRequest r = requests[index];
            if (r.State != VipRequestState.Ready) return false;

            r.State = VipRequestState.InProgress;
            r.RemainingSeconds = r.Definition.DurationSeconds;
            VisitChanged?.Invoke();
            return true;
        }

        public bool SpeedUp(int index)
        {
            if (!IsActive || index < 0 || index >= requests.Count) return false;

            VipRequest r = requests[index];
            if (r.State != VipRequestState.InProgress || r.SpeedUpUsed) return false;
            if (GameState.Instance == null || !GameState.Instance.TrySpendEnergy(VipRequestTable.SpeedUpEnergyCost))
                return false;

            r.SpeedUpUsed = true;
            r.RemainingSeconds = Mathf.Max(1f, r.RemainingSeconds * VipRequestTable.SpeedUpTimeFactor);
            VisitChanged?.Invoke();
            return true;
        }

        public int ServeAll()
        {
            if (!IsActive) return 0;

            int started = 0;
            for (int i = 0; i < requests.Count; i++)
            {
                if (requests[i].State == VipRequestState.Missing)
                    Fix(i);
                if (StartRequest(i))
                    started++;
            }
            return started;
        }

        private void HandleOfferAccepted(VipOffer offer)
        {
            Guest = offer.Guest;
            requests.Clear();

            foreach (VipRequestDefinition def in VipRequestTable.GetRequests(offer.Guest.Type))
            {
                int cap = 5;
                string reason = string.Empty;
                if (readiness != null)
                    readiness.EvaluateRequest(def, out cap, out reason);

                requests.Add(new VipRequest(def, cap, reason));
            }

            RemainingSeconds = VipRequestTable.VisitSeconds;
            IsActive = true;
            Debug.Log($"[VipVisitService] Visit started: {Guest.GuestName}, {requests.Count} requests");
            VisitStarted?.Invoke();
            VisitChanged?.Invoke();
        }

        private bool AllMainCompleted()
        {
            foreach (VipRequest r in requests)
                if (!r.Definition.IsBonus && r.State != VipRequestState.Completed)
                    return false;
            return true;
        }

        private void EndVisit(bool timedOut)
        {
            IsActive = false;

            float percent = SatisfactionPercent;
            VipRequestTable.RatingFor(percent, out int stars, out bool perfect);

            int completedMain = 0;
            int totalMain = 0;
            bool bonusDone = false;
            foreach (VipRequest r in requests)
            {
                if (r.Definition.IsBonus)
                {
                    if (r.State == VipRequestState.Completed) bonusDone = true;
                    continue;
                }
                totalMain++;
                if (r.State == VipRequestState.Completed) completedMain++;
            }

            foreach (VipRequest r in requests)
                if (r.State == VipRequestState.Completed && r.IsCapped) perfect = false;

            var result = new VipVisitResult
            {
                Guest = Guest,
                SatisfactionPercent = percent,
                Stars = stars,
                IsPerfect = perfect,
                CompletedMain = completedMain,
                TotalMain = totalMain,
                BonusCompleted = bonusDone,
                TimedOut = timedOut
            };

            Debug.Log($"[VipVisitService] Visit ended: {percent:0}% / {stars}* perfect={perfect} timedOut={timedOut}");
            VisitEnded?.Invoke(result);

            if (offerService != null)
                offerService.FinishVisit();
        }
    }
}