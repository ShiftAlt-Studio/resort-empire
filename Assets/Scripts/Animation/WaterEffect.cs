
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class WaterEffect : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField]
    private Vector2 driftDirection =
        new Vector2(1f, 0.2f);

    [SerializeField] private float driftDistance = 0.25f;
    [SerializeField] private float driftSpeed = 0.6f;

    [Header("Floating")]
    [SerializeField] private float floatHeight = 0.06f;
    [SerializeField] private float floatSpeed = 1.2f;

    [Header("Fade")]
    [SerializeField, Range(0f, 1f)]
    private float maxAlpha = 0.45f;

    [SerializeField] private float fadeSpeed = 1f;

    [Header("Scale Pulse")]
    [SerializeField] private float scalePulse = 0.08f;
    [SerializeField] private float pulseSpeed = 1f;

    [Header("Randomize")]
    [SerializeField] private bool randomPhase = true;

    private SpriteRenderer spriteRenderer;
    private Vector3 startPosition;
    private Vector3 startScale;
    private Color originalColor;
    private float phase;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        startPosition = transform.localPosition;
        startScale = transform.localScale;
        originalColor = spriteRenderer.color;

        phase = randomPhase
            ? Random.Range(0f, Mathf.PI * 2f)
            : 0f;
    }

    private void Update()
    {
        float t = Time.time;

        Vector2 direction = driftDirection.normalized;

        float drift = Mathf.Sin(
            t * driftSpeed + phase
        ) * driftDistance;

        float floating = Mathf.Sin(
            t * floatSpeed + phase
        ) * floatHeight;

        transform.localPosition = startPosition +
            new Vector3(
                direction.x * drift,
                direction.y * drift + floating,
                0f
            );

        float fade = (
            Mathf.Sin(t * fadeSpeed + phase) + 1f
        ) * 0.5f;

        Color color = originalColor;
        color.a = originalColor.a * maxAlpha * fade;

        spriteRenderer.color = color;

        float scale = 1f +
            Mathf.Sin(t * pulseSpeed + phase)
            * scalePulse;

        transform.localScale = startScale * scale;
    }

    private void OnDisable()
    {
        transform.localPosition = startPosition;
        transform.localScale = startScale;

        if (spriteRenderer != null)
            spriteRenderer.color = originalColor;
    }
}
