using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SplashSequence : MonoBehaviour
{
    [Header("Objects")]
    [SerializeField] Image logoOutline;
    [SerializeField] Image logoFill;
    [SerializeField] CanvasGroup logoText;
    [SerializeField] CanvasGroup screenFade;

    [Header("Sparks / outline colour")]
    [SerializeField] SplashSparks sparks;
    [SerializeField] Color outlineDrawColor = Color.white;
    [SerializeField] Color outlineRestColor = new Color(0.6f, 0.05f, 0.05f, 1f);

    [Header("Time (sec)")]
    [SerializeField] float outlineDrawTime = 1.4f;
    [SerializeField] float fillDelay = 0.15f;
    [SerializeField] float fillTime = 0.8f;
    [SerializeField] float outlineFadeTime = 0.3f;
    [SerializeField] float textDelay = 0.2f;
    [SerializeField] float textFadeTime = 0.9f;
    [SerializeField] float holdTime = 1.0f;
    [SerializeField] float fadeOutTime = 0.5f;

    [Header("After the splash screen")]
    [SerializeField] GameObject splashPanel;
    [SerializeField] GameObject menuPanel;

    IEnumerator Start()
    {
        logoOutline.fillAmount = 0f;
        logoOutline.color = outlineDrawColor;
        SetAlpha(logoOutline, 1f);
        SetAlpha(logoFill, 0f);
        logoText.alpha = 0f;
        screenFade.alpha = 0f;
        menuPanel.SetActive(false);
        splashPanel.SetActive(true);

        if (sparks != null) sparks.Play();

        yield return Animate(outlineDrawTime, t =>
        {
            logoOutline.fillAmount = t;
            logoOutline.color = Color.Lerp(outlineDrawColor, outlineRestColor, t);
        });
        yield return new WaitForSeconds(fillDelay);

        yield return Animate(fillTime, t => SetAlpha(logoFill, t));

        yield return Animate(outlineFadeTime, t => SetAlpha(logoOutline, 1f - t));

        yield return new WaitForSeconds(textDelay);
        yield return Animate(textFadeTime, t => logoText.alpha = t);

        yield return new WaitForSeconds(holdTime);

        if (sparks != null) sparks.Stop();
        yield return Animate(fadeOutTime, t => screenFade.alpha = t);

        splashPanel.SetActive(false);
        menuPanel.SetActive(true);
        screenFade.alpha = 0f;
    }

    static void SetAlpha(Image img, float a)
    {
        Color c = img.color;
        c.a = a;
        img.color = c;
    }

    static IEnumerator Animate(float duration, Action<float> apply)
    {
        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            apply(Mathf.SmoothStep(0f, 1f, time / duration));
            yield return null;
        }
        apply(1f);
    }
}