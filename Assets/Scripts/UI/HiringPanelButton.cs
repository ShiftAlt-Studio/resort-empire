using UnityEngine;
using UnityEngine.UI;
using ResortEmpire.Core;
using ResortEmpire.UI;

[RequireComponent(typeof(Button))]
[RequireComponent(typeof(CanvasGroup))]
public class HiringPanelButton : MonoBehaviour
{
    [SerializeField] private StaffHiringPanel panel;

    private Button button;
    private CanvasGroup group;

    private void Awake()
    {
        button = GetComponent<Button>();
        group = GetComponent<CanvasGroup>();
        button.onClick.AddListener(OnClick);
    }

    private void Start()
    {
        if (panel == null) panel = FindAnyObjectByType<StaffHiringPanel>(FindObjectsInactive.Include);

        if (GameState.Instance != null)
        {
            GameState.Instance.OnStateChanged += ApplyState;
            ApplyState(GameState.Instance.CurrentState);
        }
    }

    private void OnDestroy()
    {
        if (GameState.Instance != null)
            GameState.Instance.OnStateChanged -= ApplyState;
    }

    private void ApplyState(ResortState state)
    {
        bool visible = state == ResortState.AWAITING_WAVE;
        group.alpha = visible ? 1f : 0f;
        group.interactable = visible;
        group.blocksRaycasts = visible;
    }

    private void OnClick()
    {
        if (panel != null) panel.Open();
    }
}
