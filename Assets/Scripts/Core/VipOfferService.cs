using System;
using System.Collections.Generic;
using UnityEngine;

namespace ResortEmpire.Core
{
    public interface IVipReadinessEvaluator
    {
        VipReadiness Evaluate(VipGuestDefinition guest, GuestNeedType need);
    }

    public class VipOfferService : MonoBehaviour
    {
        [SerializeField] private int firstOfferAfterWave = VipTable.FirstOfferAfterWave;
        [SerializeField] private float availableSeconds = VipTable.OfferAvailableSeconds;

        public VipOffer Current { get; private set; }
        public IVipReadinessEvaluator Evaluator { get; set; }

        public event Action<VipOffer> OfferChanged;
        public event Action<VipOffer> OfferAccepted;
        public event Action<VipOffer> OfferDeferred;
        public event Action<VipOffer> OfferDeclined;
        public event Action<VipOffer> OfferExpired;

        public bool HasAvailableOffer => Current != null && Current.Status == VipOfferStatus.Available;

        private int nextGuestIndex;
        private int lastOfferWave = -1;

        private void Start()
        {
            if (GameState.Instance != null)
                GameState.Instance.OnStateChanged += HandleStateChanged;
        }

        private void OnDestroy()
        {
            if (GameState.Instance != null)
                GameState.Instance.OnStateChanged -= HandleStateChanged;
        }

        private void Update()
        {
            if (!HasAvailableOffer) return;
            if (GameState.Instance != null && GameState.Instance.CurrentState == ResortState.WAVE_PAUSED) return;

            Current.RemainingSeconds -= Time.deltaTime;
            if (Current.RemainingSeconds > 0f) return;

            Current.RemainingSeconds = 0f;
            Current.Status = VipOfferStatus.Expired;
            VipOffer expired = Current;
            Current = null;
            Debug.Log($"[VipOfferService] Offer expired: {expired.Guest.GuestName}");
            OfferExpired?.Invoke(expired);
            OfferChanged?.Invoke(expired);
        }

        public bool Accept()
        {
            if (!HasAvailableOffer) return false;

            Current.Status = VipOfferStatus.Accepted;
            Debug.Log($"[VipOfferService] Accepted: {Current.Guest.GuestName}");
            OfferAccepted?.Invoke(Current);
            OfferChanged?.Invoke(Current);
            return true;
        }

        public bool Defer()
        {
            if (!HasAvailableOffer) return false;

            Debug.Log($"[VipOfferService] Deferred: {Current.Guest.GuestName}");
            OfferDeferred?.Invoke(Current);
            return true;
        }

        public bool Decline()
        {
            if (!HasAvailableOffer) return false;

            VipOffer declined = Current;
            declined.Status = VipOfferStatus.Declined;
            Current = null;
            Debug.Log($"[VipOfferService] Declined: {declined.Guest.GuestName}");
            OfferDeclined?.Invoke(declined);
            OfferChanged?.Invoke(declined);
            return true;
        }

        public void FinishVisit()
        {
            if (Current == null || Current.Status != VipOfferStatus.Accepted) return;

            VipOffer finished = Current;
            Current = null;
            OfferChanged?.Invoke(finished);
        }

        public void RefreshReadiness()
        {
            if (Current == null) return;

            Current.Readiness = BuildReadiness(Current.Guest);
            OfferChanged?.Invoke(Current);
        }

        private void HandleStateChanged(ResortState state)
        {
            if (state != ResortState.AWAITING_WAVE) return;
            if (Current != null) return;

            if (GameState.Instance.IsResumingFromPause) return;

            int completed = GameState.Instance.CompletedWaves;
            if (completed < firstOfferAfterWave || completed == lastOfferWave) return;

            lastOfferWave = completed;
            CreateOffer();
        }

        private void CreateOffer()
        {
            VipGuestDefinition guest = VipTable.Get(nextGuestIndex++);
            var offer = new VipOffer(guest, availableSeconds);
            offer.Readiness = BuildReadiness(guest);
            Current = offer;

            Debug.Log($"[VipOfferService] New offer: {guest.GuestName} ({guest.Type})");
            OfferChanged?.Invoke(offer);
        }

        private List<VipNeedReadiness> BuildReadiness(VipGuestDefinition guest)
        {
            var list = new List<VipNeedReadiness>();
            foreach (GuestNeedType need in guest.Needs)
                list.Add(new VipNeedReadiness(need, EvaluateNeed(guest, need)));
            return list;
        }

        private VipReadiness EvaluateNeed(VipGuestDefinition guest, GuestNeedType need)
        {
            if (Evaluator != null)
                return Evaluator.Evaluate(guest, need);

            bool unlocked = GameState.Instance != null && GameState.Instance.IsNeedUnlocked(need);
            return unlocked ? VipReadiness.Good : VipReadiness.Risky;
        }
    }
}