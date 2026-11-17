using System;
using UnityEngine;
using UnityEngine.UI;
using ResortEmpire.Core;

namespace ResortEmpire.UI
{
    public class AreaUnlockController : MonoBehaviour
    {
        [Serializable]
        private class AreaInfo
        {
            public AreaType type;
            public string title;
            public string line1;
            public string line2;
            public Sprite image;
        }

        [SerializeField] private AreaActivationService service;
        [SerializeField] private AreaUnlockView view;
        [SerializeField] private Button closeButton;
        [SerializeField] private AreaInfo[] areas;

        private AreaType current;
        private bool isOpen;

        private void Start()
        {
            if (service == null)
                service = FindAnyObjectByType<AreaActivationService>();
            if (view == null)
                view = FindAnyObjectByType<AreaUnlockView>(FindObjectsInactive.Include);

            view.UnlockClicked += OnUnlockClicked;
            if (closeButton != null)
                closeButton.onClick.AddListener(Close);

            if (GameState.Instance != null)
            {
                GameState.Instance.CoinsChanged += HandleCoinsChanged;
                GameState.Instance.OnStateChanged += HandleStateChanged;
            }

            view.Hide();
        }

        private void OnDestroy()
        {
            if (view != null)
                view.UnlockClicked -= OnUnlockClicked;

            if (GameState.Instance != null)
            {
                GameState.Instance.CoinsChanged -= HandleCoinsChanged;
                GameState.Instance.OnStateChanged -= HandleStateChanged;
            }
        }

        public void Open(AreaType type)
        {
            if (!service.IsPurchaseWindowOpen || service.IsActivated(type))
                return;

            current = type;
            isOpen = true;
            Refresh();
        }

        public void Close()
        {
            isOpen = false;
            view.Hide();
        }

        private void Refresh()
        {
            AreaInfo info = Find(current);
            if (info == null)
            {
                Debug.LogError($"[AreaUnlockController] Area {current} is not configured.");
                Close();
                return;
            }

            int cost = AreaActivationService.GetCost(current);

            switch (service.GetState(current))
            {
                case AreaState.Locked:
                    view.Show(info.title, info.line1, info.line2, info.image, cost,
                        AreaUnlockViewState.Locked, 0,
                        $"Complete Wave {AreaActivationService.RequiredWave(current)}");
                    break;

                case AreaState.NotAffordable:
                    view.Show(info.title, info.line1, info.line2, info.image, cost,
                        AreaUnlockViewState.NotEnoughCoins, cost - GameState.Instance.Coins);
                    break;

                case AreaState.Available:
                    view.Show(info.title, info.line1, info.line2, info.image, cost,
                        AreaUnlockViewState.Available);
                    break;

                case AreaState.Unlocked:
                    Close();
                    break;
            }
        }

        private void OnUnlockClicked()
        {
            if (!isOpen) return;

            PurchaseResult result = service.TryPurchase(current);
            if (result == PurchaseResult.Success)
                Close();
            else
                Refresh();
        }

        private void HandleCoinsChanged(int coins)
        {
            if (isOpen) Refresh();
        }

        private void HandleStateChanged(ResortState state)
        {
            if (isOpen && !service.IsPurchaseWindowOpen)
                Close();
        }

        private AreaInfo Find(AreaType type)
        {
            if (areas == null) return null;
            foreach (AreaInfo a in areas)
                if (a.type == type) return a;
            return null;
        }
    }
}