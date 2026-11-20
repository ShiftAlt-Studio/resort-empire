
using System.Collections;
using UnityEngine;

public class ChefAnimation : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField] private SpriteRenderer visual;

    [Header("Chef Sprites")]
    [SerializeField] private Sprite idleSprite;
    [SerializeField] private Sprite cookingSprite;
    [SerializeField] private Sprite servingSprite;
    [SerializeField] private Sprite happySprite;

    [Header("Animation Timing")]
    [SerializeField, Min(0.1f)]
    private float idleDuration = 3f;

    [SerializeField, Min(0.1f)]
    private float cookingDuration = 0.5f;

    [SerializeField, Min(0.1f)]
    private float servingDuration = 1f;

    [SerializeField, Min(0.1f)]
    private float happyDuration = 0.8f;

    private Coroutine animationRoutine;

    private void Awake()
    {
        if (visual == null)
            visual = GetComponentInChildren<SpriteRenderer>();
    }

    private void OnEnable()
    {
        animationRoutine = StartCoroutine(AnimateChef());
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

    private IEnumerator AnimateChef()
    {
        while (true)
        {
            SetSprite(idleSprite);
            yield return new WaitForSeconds(idleDuration);

            SetSprite(cookingSprite);
            yield return new WaitForSeconds(cookingDuration);

            SetSprite(servingSprite);
            yield return new WaitForSeconds(servingDuration);

            SetSprite(happySprite);
            yield return new WaitForSeconds(happyDuration);
        }
    }

    private void SetSprite(Sprite sprite)
    {
        if (visual != null && sprite != null)
            visual.sprite = sprite;
    }
}
