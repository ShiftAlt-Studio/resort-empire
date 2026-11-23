using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ResortEmpire.Core;

namespace ResortEmpire.UI
{
    public class VipRequestBoardController : MonoBehaviour
    {
        [Serializable]
        private class GuestVisual
        {
            public VipGuestType type;
            public Sprite portrait;
        }

        [SerializeField] private VipVisitService visit;
        [SerializeField] private VipRequestBoardView view;

        [Header("Home VIP button (reopens the board during a visit)")]
        [SerializeField] private Button vipButton;
        [SerializeField] private TMP_Text vipButtonTimerText;

        [Header("Portraits")]
        [SerializeField] private GuestVisual[] portraits;

        private const float RefreshInterval = 0.25f;

        private bool isOpen;
        private float refreshTimer;

        private void Start()
        {
            if (visit == null)
                visit = FindAnyObjectByType<VipVisitService>();
            if (view == null)
                view = FindAnyObjectByType<VipRequestBoardView>(FindObjectsInactive.Include);

            if (visit != null)
            {
                visit.VisitStarted += HandleVisitStarted;
                visit.VisitChanged += HandleVisitChanged;
                visit.VisitEnded += HandleVisitEnded;
            }

            if (view != null)
            {
                view.RowActionClicked += OnRowAction;
                view.ServeAllClicked += OnServeAll;
                view.CloseClicked += Close;
                view.Hide();
            }

            if (vipButton != null)
                vipButton.onClick.AddListener(Open);
        }

        private void OnDestroy()
        {
            if (visit != null)
            {
                visit.VisitStarted -= HandleVisitStarted;
                visit.VisitChanged -= HandleVisitChanged;
                visit.VisitEnded -= HandleVisitEnded;
            }

            if (view != null)
            {
                view.RowActionClicked -= OnRowAction;
                view.ServeAllClicked -= OnServeAll;
                view.CloseClicked -= Close;
            }

            if (vipButton != null)
                vipButton.onClick.RemoveListener(Open);
        }

        private void Update()
        {
            if (visit == null || !visit.IsActive) return;

            if (isOpen && view != null)
            {
                refreshTimer += Time.unscaledDeltaTime;
                if (refreshTimer >= RefreshInterval)
                {
                    refreshTimer = 0f;
                    view.Refresh(visit);
                }
            }
        }

        private void LateUpdate()
        {
            if (visit == null || !visit.IsActive) return;

            if (vipButton != null && !vipButton.gameObject.activeSelf)
                vipButton.gameObject.SetActive(true);

            if (vipButtonTimerText != null)
            {
                int seconds = Mathf.CeilToInt(Mathf.Max(0f, visit.RemainingSeconds));
                vipButtonTimerText.text = $"{seconds / 60:00}:{seconds % 60:00}";
            }
        }

        public void Open()
        {
            if (visit == null || view == null || !visit.IsActive) return;

            isOpen = true;
            view.Show(visit.Guest, FindPortrait(visit.Guest.Type));
            view.Refresh(visit);
        }

        public void Close()
        {
            isOpen = false;
            if (view != null) view.Hide();
        }

        private void OnRowAction(int index)
        {
            if (visit == null || index < 0 || index >= visit.Requests.Count) return;

            switch (visit.Requests[index].State)
            {
                case VipRequestState.Missing:
                    if (visit.Fix(index) == VipFixResult.NotEnoughMaterials)
                        Debug.Log("[VipRequestBoard] Not enough Materials");
                    break;
                case VipRequestState.Ready:
                    visit.StartRequest(index);
                    break;
                case VipRequestState.InProgress:
                    if (!visit.SpeedUp(index))
                        Debug.Log("[VipRequestBoard] Speed Up unavailable (Energy or already used)");
                    break;
            }
        }

        private void OnServeAll()
        {
            if (visit != null) visit.ServeAll();
        }

        private void HandleVisitStarted()
        {
            Open();
        }

        private void HandleVisitChanged()
        {
            if (isOpen && view != null)
                view.Refresh(visit);
        }

        private void HandleVisitEnded(VipVisitResult result)
        {
            Close();
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