
using System.Collections;
using UnityEngine;

public class ReceptionistAnimation : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField] private SpriteRenderer visual;

    [Header("Receptionist Sprites")]
    [SerializeField] private Sprite idleSprite;
    [SerializeField] private Sprite welcomeSprite;

    [Header("Animation Timing")]
    [SerializeField, Min(0.1f)]
    private float idleDuration = 3f;

    [SerializeField, Min(0.1f)]
    private float welcomeDuration = 1f;

    [Header("Random Idle Pause")]
    [SerializeField] private bool randomIdle = true;
    [SerializeField] private float minIdleTime = 2.5f;
    [SerializeField] private float maxIdleTime = 5f;

    private Coroutine animationRoutine;

    private void Awake()
    {
        if (visual == null)
            visual = GetComponentInChildren<SpriteRenderer>();
    }

    private void OnEnable()
    {
        animationRoutine = StartCoroutine(AnimateReceptionist());
    }

    private void OnDisable()
    {
        if (animationRoutine != null)
        {
            StopCoroutine(animationRoutine);
            animationRoutine = null;
        }

        SetSprite(idleSprite);
    }

    private IEnumerator AnimateReceptionist()
    {
        while (true)
        {
            SetSprite(idleSprite);

            float waitTime = randomIdle
                ? Random.Range(
                    Mathf.Min(minIdleTime, maxIdleTime),
                    Mathf.Max(minIdleTime, maxIdleTime))
                : idleDuration;

            yield return new WaitForSeconds(
                Mathf.Max(0.1f, waitTime));

            SetSprite(welcomeSprite);

            yield return new WaitForSeconds(
                Mathf.Max(0.1f, welcomeDuration));
        }
    }

    private void SetSprite(Sprite sprite)
    {
        if (visual != null && sprite != null)
            visual.sprite = sprite;
    }
}
