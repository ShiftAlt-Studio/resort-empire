using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ResortEmpire.Core;

namespace ResortEmpire.UI
{
    public class VipPreviewController : MonoBehaviour
    {
        [Serializable]
        private class GuestVisual
        {
            public VipGuestType type;
            public Sprite portrait;
        }

        [SerializeField] private VipOfferService service;
        [SerializeField] private VipPreviewView view;

        [Header("Home VIP button (shown only while an offer is available)")]
        [SerializeField] private Button vipButton;
        [SerializeField] private TMP_Text vipButtonTimerText;

        [Header("Portraits")]
        [SerializeField] private GuestVisual[] portraits;

        private bool isOpen;

        private void Start()
        {
            if (service == null)
                service = FindAnyObjectByType<VipOfferService>();
            if (view == null)
                view = FindAnyObjectByType<VipPreviewView>(FindObjectsInactive.Include);

            if (service != null)
            {
                service.OfferChanged += HandleOfferChanged;
                service.OfferAccepted += HandleOfferAccepted;
                service.OfferDeferred += HandleOfferDeferred;
            }

            if (view != null)
            {
                view.AcceptClicked += OnAcceptClicked;
                view.DeferClicked += OnDeferClicked;
                view.DeclineClicked += OnDeclineClicked;
                view.Hide();
            }

            if (vipButton != null)
                vipButton.onClick.AddListener(Open);

            RefreshButton();
        }

        private void OnDestroy()
        {
            if (service != null)
            {
                service.OfferChanged -= HandleOfferChanged;
                service.OfferAccepted -= HandleOfferAccepted;
                service.OfferDeferred -= HandleOfferDeferred;
            }

            if (view != null)
            {
                view.AcceptClicked -= OnAcceptClicked;
                view.DeferClicked -= OnDeferClicked;
                view.DeclineClicked -= OnDeclineClicked;
            }

            if (vipButton != null)
                vipButton.onClick.RemoveListener(Open);
        }

        private void Update()
        {
            if (service == null || !service.HasAvailableOffer) return;

            if (isOpen && view != null)
                view.SetTime(service.Current);

            if (vipButtonTimerText != null)
            {
                int seconds = Mathf.CeilToInt(Mathf.Max(0f, service.Current.RemainingSeconds));
                vipButtonTimerText.text = $"{seconds / 60:00}:{seconds % 60:00}";
            }
        }

        public void Open()
        {
            if (service == null || view == null || !service.HasAvailableOffer) return;

            isOpen = true;
            view.Show(service.Current, FindPortrait(service.Current.Guest.Type));
        }

        public void Close()
        {
            isOpen = false;
            if (view != null) view.Hide();
        }

        private void OnAcceptClicked() => service.Accept();
        private void OnDeferClicked() => service.Defer();
        private void OnDeclineClicked() => service.Decline();

        private void HandleOfferAccepted(VipOffer offer) => Close();
        private void HandleOfferDeferred(VipOffer offer) => Close();

        private void HandleOfferChanged(VipOffer offer)
        {
            RefreshButton();

            if (!service.HasAvailableOffer)
            {
                Close();
                return;
            }

            if (isOpen)
                view.Show(service.Current, FindPortrait(service.Current.Guest.Type));
        }

        private void RefreshButton()
        {
            if (vipButton != null)
                vipButton.gameObject.SetActive(service != null && service.HasAvailableOffer);
        }

        private Sprite FindPortrait(VipGuestType type)
        {
            if (portraits == null) return null;
            foreach (GuestVisual p in portraits)
                if (p.type == type) return p.portrait;
            return null;
        }
    }
}