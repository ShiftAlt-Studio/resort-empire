
using System.Collections;
using UnityEngine;

public class BartenderAnimation : MonoBehaviour
{
    [Header("Sprites")]
    [SerializeField] private SpriteRenderer visual;
    [SerializeField] private Sprite idleSprite;
    [SerializeField] private Sprite cheersSprite;

    [Header("Animation")]
    [SerializeField] private float idleTime = 3f;
    [SerializeField] private float cheersTime = 0.7f;

    private Coroutine animationRoutine;

    private void OnEnable()
    {
        animationRoutine = StartCoroutine(Animate());
    }

    private void OnDisable()
    {
        if (animationRoutine != null)
            StopCoroutine(animationRoutine);

        if (visual != null && idleSprite != null)
            visual.sprite = idleSprite;
    }

    private IEnumerator Animate()
    {
        if (visual == null || idleSprite == null ||
            cheersSprite == null)
            yield break;

        while (true)
        {
            visual.sprite = idleSprite;
            yield return new WaitForSeconds(
                Mathf.Max(0.1f, idleTime)
            );

            visual.sprite = cheersSprite;
            yield return new WaitForSeconds(
                Mathf.Max(0.1f, cheersTime)
            );
        }
    }
}
