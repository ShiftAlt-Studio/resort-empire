using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace ResortEmpire.Fx
{

    public class BuildingLevelUpFx : MonoBehaviour
    {
        public SpriteRenderer building;
        public float groundWidth = 6f;
        public Color accent = new Color(1f, 0.84f, 0.3f, 1f);
        public bool shakeCamera = true;
        public UnityEvent onPeak;
        public UnityEvent onFinished;

        Vector3 _bs;
        bool _running;

        void Awake() { _bs = building.transform.localScale; }

        [ContextMenu("Test Play")]
        void TestPlay() { Play(building.sprite, 2); }

        public void Play(Sprite newSprite, int newLevel, Action done = null)
        {
            if (_running) return;
            StartCoroutine(Run(newSprite, Mathf.Clamp(newLevel, 1, 5), done));
        }

        IEnumerator Run(Sprite newSprite, int level, Action done)
        {
            _running = true;
            var bt = building.transform;
            Vector3 ground = bt.position;
            int layer = building.sortingLayerID, order = building.sortingOrder;
            _bs = bt.localScale;

            yield return FxKit.Tween(0.14f, t =>
            {
                float k = FxKit.OutQuad(t);
                bt.localScale = new Vector3(_bs.x * (1f + 0.08f * k), _bs.y * (1f - 0.10f * k), _bs.z);
            });
            yield return FxKit.Tween(0.12f, t =>
            {
                float k = FxKit.OutQuad(t);
                bt.localScale = new Vector3(_bs.x * Mathf.Lerp(1.08f, 0.94f, k), _bs.y * Mathf.Lerp(0.90f, 1.14f, k), _bs.z);
            });

            if (newSprite != null) building.sprite = newSprite;
            float h = building.sprite ? building.sprite.bounds.max.y * _bs.y : 3f;
            Vector3 center = ground + Vector3.up * h * 0.5f;
            Color a0 = new Color(accent.r, accent.g, accent.b, 0.9f);

            StartCoroutine(FxKit.Glow(center, Mathf.Max(groundWidth, h) * 1.6f, a0, layer, order + 1, 0.4f));
            StartCoroutine(Beam(ground, h, layer, order));
            StartCoroutine(FxKit.RingWave(ground, groundWidth, a0, layer, order - 1, 0.6f));
            StartCoroutine(FxKit.RingWave(ground, groundWidth, new Color(1f, 1f, 1f, 0.8f), layer, order - 1, 0.7f, 0.12f));
            if (level >= 3)
            {
                StartCoroutine(FxKit.RingWave(ground, groundWidth * 1.2f, a0, layer, order - 1, 0.8f, 0.26f));
                FxKit.Burst(ground + Vector3.up * (h + 1.2f), FxKit.Petal, layer, order + 6, 40, 2.0f, 2.8f, 0.2f, 0.8f, 0.18f, 0.3f,
                    FxKit.Hex("#FF8FA3"), FxKit.Hex("#7FE3FF"), 0.25f, ParticleSystemShapeType.Box, 1f,
                    new Vector3(groundWidth, 0.2f, 1f), new Vector3(90f, 0f, 0f), 25f, 200f, 0.05f, false);
            }
            FxKit.Burst(center, FxKit.Star, layer, order + 5, 10 + 5 * level, 0.9f, 1.5f, 2f, 5f, 0.2f, 0.4f,
                accent, Color.white, -0.1f, ParticleSystemShapeType.Cone, 0.4f, Vector3.one,
                new Vector3(-90f, 0f, 0f), 45f, 160f);
            FxKit.Dust(ground, groundWidth, layer, order + 3, 12);
            if (shakeCamera) StartCoroutine(FxKit.Shake(0.25f, 0.05f));
            StartCoroutine(Pips(ground + Vector3.up * (h + 0.8f), level, layer, order + 7));
            onPeak?.Invoke();

            // 4) land with an elastic bounce
            yield return FxKit.Tween(0.55f, t =>
            {
                float k = FxKit.OutElastic(t);
                bt.localScale = new Vector3(_bs.x * Mathf.LerpUnclamped(0.94f, 1f, k), _bs.y * Mathf.LerpUnclamped(1.14f, 1f, k), _bs.z);
            });
            bt.localScale = _bs;

            yield return FxKit.Wait(1.2f);
            _running = false;
            onFinished?.Invoke();
            done?.Invoke();
        }

        // vertical light beam
        IEnumerator Beam(Vector3 ground, float h, int layer, int order)
        {
            var sr = FxKit.Sprite2D(FxKit.Soft, ground + Vector3.up * h * 0.6f, new Color(accent.r, accent.g, accent.b, 0.55f),
                layer, order - 2, new Vector3(groundWidth * 0.4f, h * 2.2f, 1f));
            yield return FxKit.Tween(0.6f, t =>
            {
                sr.transform.localScale = new Vector3(groundWidth * Mathf.Lerp(0.5f, 0.15f, t), h * 2.2f, 1f);
                var c = accent; c.a = 0.55f * (1f - t);
                sr.color = c;
            });
            Destroy(sr.gameObject);
        }

        IEnumerator Pips(Vector3 top, int level, int layer, int order)
        {
            var list = new List<SpriteRenderer>();
            for (int i = 0; i < level; i++)
            {
                float x = (i - (level - 1) * 0.5f) * 0.75f;
                var sr = FxKit.Sprite2D(FxKit.Star, top + new Vector3(x, 0f, 0f), accent, layer, order, Vector3.zero);
                list.Add(sr);
                StartCoroutine(Pop(sr, i * 0.12f));
            }
            yield return FxKit.Wait(0.12f * level + 0.3f + 1.0f);
            yield return FxKit.Tween(0.3f, t =>
            {
                foreach (var sr in list)
                    if (sr) { var c = accent; c.a = 1f - t; sr.color = c; sr.transform.position += Vector3.up * Time.unscaledDeltaTime * 0.6f; }
            });
            foreach (var sr in list) if (sr) Destroy(sr.gameObject);
        }

        IEnumerator Pop(SpriteRenderer sr, float delay)
        {
            yield return FxKit.Wait(delay);
            yield return FxKit.Tween(0.3f, t =>
            {
                if (sr) sr.transform.localScale = Vector3.one * 0.7f * FxKit.OutBack(t);
            });
        }
    }
}