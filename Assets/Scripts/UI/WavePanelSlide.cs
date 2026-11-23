
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class WavePanelSlide : MonoBehaviour
{
    [SerializeField] private RectTransform panel;
    [SerializeField] private RectTransform toggleButton;
    [SerializeField] private Button button;
    [SerializeField] private float duration = 0.35f;

    private Vector2 panelOpenPosition;
    private Vector2 buttonOpenPosition;
    private Vector2 buttonClosedPosition;
    private Coroutine animation;
    private bool isOpen = true;
    public bool IsOpen => isOpen;

    private void Start()
    {
        panelOpenPosition = panel.anchoredPosition;
        buttonOpenPosition = toggleButton.anchoredPosition;

        RectTransform canvasRect =
            panel.GetComponentInParent<Canvas>()
                .rootCanvas.GetComponent<RectTransform>();

        float canvasWidth = canvasRect.rect.width;
        float buttonWidth = toggleButton.rect.width;

        buttonClosedPosition = buttonOpenPosition;
        buttonClosedPosition.x = -10f;

        button.onClick.AddListener(Toggle);
    }

    public void Toggle()
    {
        isOpen = !isOpen;

        if (animation != null)
            StopCoroutine(animation);

        animation = StartCoroutine(Animate());
    }

    private IEnumerator Animate()
    {
        RectTransform visualPanel =
            panel.Find("Panel") as RectTransform;

        float panelWidth = visualPanel != null
            ? visualPanel.rect.width
            : panel.rect.width;

        Vector2 panelTarget = panelOpenPosition +
            (isOpen ? Vector2.zero : Vector2.right * panelWidth);

        Vector2 buttonTarget =
            isOpen ? buttonOpenPosition : buttonClosedPosition;

        Vector2 panelStart = panel.anchoredPosition;
        Vector2 buttonStart = toggleButton.anchoredPosition;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / duration);
            t = t * t * (3f - 2f * t);

            panel.anchoredPosition =
                Vector2.LerpUnclamped(panelStart, panelTarget, t);

            toggleButton.anchoredPosition =
                Vector2.LerpUnclamped(buttonStart, buttonTarget, t);

            yield return null;
        }

        panel.anchoredPosition = panelTarget;
        toggleButton.anchoredPosition = buttonTarget;
        animation = null;
    }


    private void LateUpdate()
    {
        if (panel == null || button == null)
            return;

        bool shouldShow = panel.gameObject.activeInHierarchy;

        if (button.gameObject.activeSelf != shouldShow)
            button.gameObject.SetActive(shouldShow);
    }


    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(Toggle);
    }
}
