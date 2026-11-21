
using UnityEngine;

public class LockAnimation : MonoBehaviour
{
    [Header("Wiggle")]
    [SerializeField] private float wiggleAngle = 7f;
    [SerializeField] private float wiggleSpeed = 2f;

    [Header("Pulse")]
    [SerializeField] private float pulseAmount = 0.07f;
    [SerializeField] private float pulseSpeed = 2f;

    [Header("Sparkle")]
    [SerializeField] private SpriteRenderer sparkle;
    [SerializeField] private float sparkleInterval = 3f;
    [SerializeField] private float sparkleDuration = 0.6f;

    private Vector3 originalScale;
    private Quaternion originalRotation;
    private float sparkleTimer;

    private void Awake()
    {
        originalScale = transform.localScale;
        originalRotation = transform.localRotation;

        if (sparkle != null)
            sparkle.enabled = false;
    }

    private void Update()
    {
        float t = Time.time;

        float angle = Mathf.Sin(t * wiggleSpeed)
                      * wiggleAngle;

        transform.localRotation =
            originalRotation *
            Quaternion.Euler(0f, 0f, angle);

        float pulse = 1f +
            Mathf.Sin(t * pulseSpeed) * pulseAmount;

        transform.localScale = originalScale * pulse;

        if (sparkle == null) return;

        sparkleTimer += Time.deltaTime;

        float interval = Mathf.Max(0.1f, sparkleInterval);
        float duration = Mathf.Clamp(
            sparkleDuration, 0.01f, interval);

        float phase = sparkleTimer % interval;

        if (phase < duration)
        {
            sparkle.enabled = true;

            float progress = phase / duration;

            Color c = sparkle.color;
            c.a = Mathf.Sin(progress * Mathf.PI);
            sparkle.color = c;

            sparkle.transform.localPosition =
                new Vector3(
                    Mathf.Lerp(-0.25f, 0.25f, progress),
                    Mathf.Lerp(0.25f, -0.25f, progress),
                    0f
                );

            sparkle.transform.localRotation =
                Quaternion.Euler(
                    0f, 0f, progress * 180f);
        }
        else
        {
            sparkle.enabled = false;
        }
    }
}
