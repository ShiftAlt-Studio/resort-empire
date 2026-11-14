using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SplashSparks : MonoBehaviour
{
    [SerializeField] RectTransform area;
    [SerializeField] Sprite sparkSprite;
    [SerializeField] int count = 70;
    [SerializeField] Color color = new Color(1f, 0.85f, 0.3f, 1f);
    [SerializeField] Vector2 sizeRange = new Vector2(3f, 7f);
    [SerializeField] Vector2 lifeRange = new Vector2(1.5f, 3f);
    [SerializeField] float speed = 25f;
    [SerializeField] float fadeSpeed = 2f;

    class Spark { public RectTransform rt; public Image img; public Vector2 vel; public float life, age; }

    readonly List<Spark> sparks = new List<Spark>();
    bool emitting;
    float globalAlpha;

    void Awake()
    {
        for (int i = 0; i < count; i++)
        {
            var go = new GameObject("Spark", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(area, false);
            var img = go.GetComponent<Image>();
            img.sprite = sparkSprite;
            img.raycastTarget = false;
            var s = new Spark { rt = go.GetComponent<RectTransform>(), img = img };
            Respawn(s, true);
            sparks.Add(s);
        }
    }

    public void Play() { emitting = true; }
    public void Stop() { emitting = false; }

    void Update()
    {
        globalAlpha = Mathf.MoveTowards(globalAlpha, emitting ? 1f : 0f, fadeSpeed * Time.deltaTime);

        foreach (var s in sparks)
        {
            s.age += Time.deltaTime;
            if (s.age >= s.life) Respawn(s, false);

            s.rt.anchoredPosition += s.vel * Time.deltaTime;

            float t = s.age / s.life;
            float a = Mathf.Sin(t * Mathf.PI) * globalAlpha;
            var c = color; c.a = a;
            s.img.color = c;
        }
    }

    void Respawn(Spark s, bool randomAge)
    {
        Rect r = area.rect;
        s.rt.anchoredPosition = new Vector2(
            Random.Range(r.xMin, r.xMax),
            Random.Range(r.yMin, r.yMax));
        float size = Random.Range(sizeRange.x, sizeRange.y);
        s.rt.sizeDelta = new Vector2(size, size);
        s.vel = Random.insideUnitCircle * speed;
        s.life = Random.Range(lifeRange.x, lifeRange.y);
        s.age = randomAge ? Random.Range(0f, s.life) : 0f;
    }
}