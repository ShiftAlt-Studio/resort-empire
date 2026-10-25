using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace ResortEmpire.Fx
{
    public static class FxKit
    {
        static Sprite _soft, _ring, _star, _petal;
        static readonly Dictionary<Texture, Material> _mats = new Dictionary<Texture, Material>();

        static Sprite Make(int n, Func<float, float, float> alpha)
        {
            var t = new Texture2D(n, n, TextureFormat.RGBA32, false);
            t.wrapMode = TextureWrapMode.Clamp;
            t.filterMode = FilterMode.Bilinear;
            t.hideFlags = HideFlags.HideAndDontSave;
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float u = (x + 0.5f) / n * 2f - 1f, v = (y + 0.5f) / n * 2f - 1f;
                    px[y * n + x] = (Color32)new Color(1f, 1f, 1f, Mathf.Clamp01(alpha(u, v)));
                }
            t.SetPixels32(px);
            t.Apply();
            var s = Sprite.Create(t, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
            s.hideFlags = HideFlags.HideAndDontSave;
            return s;
        }

        public static Sprite Soft => _soft ? _soft : (_soft = Make(64, (u, v) =>
        {
            float r = Mathf.Sqrt(u * u + v * v);
            float k = 1f - Mathf.Clamp01(r);
            return k * k * (3f - 2f * k);
        }));

        public static Sprite Ring => _ring ? _ring : (_ring = Make(128, (u, v) =>
        {
            float r = Mathf.Sqrt(u * u + v * v);
            return Mathf.Clamp01(1f - Mathf.Abs(r - 0.82f) / 0.13f) * Mathf.Clamp01((1f - r) * 8f + 0.5f);
        }));

        public static Sprite Star => _star ? _star : (_star = Make(96, (u, v) =>
        {
            float r = Mathf.Sqrt(u * u + v * v);
            float a = Mathf.Atan2(v, u) - Mathf.PI / 2f;
            float R = 0.35f + 0.62f * Mathf.Pow(Mathf.Abs(Mathf.Cos(2.5f * a)), 1.6f);
            return Mathf.Clamp01((R - r) / 0.07f);
        }));

        public static Sprite Petal => _petal ? _petal : (_petal = Make(64, (u, v) =>
        {
            float e = (u * u) / 0.2f + (v * v) / 0.85f;
            return Mathf.Clamp01((1f - e) * 3f);
        }));

        static Material MatFor(Sprite s)
        {
            if (!_mats.TryGetValue(s.texture, out var m) || m == null)
            {
                m = new Material(Shader.Find("Sprites/Default")) { mainTexture = s.texture };
                m.hideFlags = HideFlags.HideAndDontSave;
                _mats[s.texture] = m;
            }
            return m;
        }

        public static float InQuad(float t) { return t * t; }
        public static float OutQuad(float t) { return 1f - (1f - t) * (1f - t); }
        public static float OutBack(float t) { const float c1 = 1.70158f, c3 = c1 + 1f; t -= 1f; return 1f + c3 * t * t * t + c1 * t * t; }
        public static float OutElastic(float t)
        {
            if (t <= 0f) return 0f;
            if (t >= 1f) return 1f;
            const float c4 = 2f * Mathf.PI / 3f;
            return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * c4) + 1f;
        }

        public static IEnumerator Tween(float dur, Action<float> step)
        {
            float t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                step(Mathf.Clamp01(t / dur));
                yield return null;
            }
            step(1f);
        }

        public static IEnumerator Wait(float s)
        {
            float t = 0f;
            while (t < s) { t += Time.unscaledDeltaTime; yield return null; }
        }

        public static IEnumerator Shake(float dur, float amp)
        {
            var cam = Camera.main;
            if (cam == null) yield break;
            var tr = cam.transform;
            Vector3 o = tr.position;
            float t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float f = 1f - t / dur;
                tr.position = o + new Vector3((Mathf.PerlinNoise(t * 40f, 0.1f) - 0.5f) * 2f * amp * f,
                                              (Mathf.PerlinNoise(0.3f, t * 40f) - 0.5f) * 2f * amp * f, 0f);
                yield return null;
            }
            tr.position = o;
        }

        public static SpriteRenderer Sprite2D(Sprite s, Vector3 pos, Color c, int layer, int order, Vector3 scale)
        {
            var go = new GameObject("fx_" + s.name);
            go.transform.position = pos;
            go.transform.localScale = scale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = s;
            sr.color = c;
            sr.sharedMaterial = MatFor(s);
            sr.sortingLayerID = layer;
            sr.sortingOrder = order;
            return sr;
        }

        public static ParticleSystem Burst(Vector3 pos, Sprite sprite, int layer, int order, int count,
            float lifeMin, float lifeMax, float speedMin, float speedMax, float sizeMin, float sizeMax,
            Color c0, Color c1, float gravity, ParticleSystemShapeType shape, float radius, Vector3 shapeScale,
            Vector3 eulerRotation, float coneAngle = 25f, float spin = 0f, float delay = 0f, bool shrink = true)
        {
            var go = new GameObject("fx_burst_" + sprite.name);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(eulerRotation);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.loop = false;
            main.playOnAwake = false;
            main.useUnscaledTime = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifeMin, lifeMax);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speedMin, speedMax);
            main.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
            main.startColor = new ParticleSystem.MinMaxGradient(c0, c1);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.gravityModifier = gravity;
            main.maxParticles = count + 8;

            var em = ps.emission;
            em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(delay, (short)count) });

            var sh = ps.shape;
            sh.enabled = true;
            sh.shapeType = shape;
            sh.radius = radius;
            sh.scale = shapeScale;
            if (shape == ParticleSystemShapeType.Cone) sh.angle = coneAngle;

            if (spin != 0f)
            {
                var rot = ps.rotationOverLifetime;
                rot.enabled = true;
                rot.z = new ParticleSystem.MinMaxCurve(-spin * Mathf.Deg2Rad, spin * Mathf.Deg2Rad);
            }

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.08f),
                              new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
            col.color = g;

            if (shrink)
            {
                var sz = ps.sizeOverLifetime;
                sz.enabled = true;
                sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0.25f));
            }

            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = MatFor(sprite);
            r.sortingLayerID = layer;
            r.sortingOrder = order;

            ps.Play();
            go.AddComponent<FxAutoKill>().life = lifeMax + delay + 0.6f;
            return ps;
        }

        public static void Dust(Vector3 ground, float width, int layer, int order, int count)
        {
            Burst(ground, Soft, layer, order, count, 0.5f, 0.9f, 0.4f, 1.6f, width * 0.12f, width * 0.22f,
                  new Color(1f, 0.95f, 0.85f, 0.55f), new Color(0.85f, 0.75f, 0.6f, 0.45f), -0.05f,
                  ParticleSystemShapeType.Circle, width * 0.35f, new Vector3(1f, 0.5f, 1f), Vector3.zero);
        }

        public static IEnumerator RingWave(Vector3 ground, float width, Color c, int layer, int order, float dur, float delay = 0f)
        {
            if (delay > 0f) yield return Wait(delay);
            var sr = Sprite2D(Ring, ground, c, layer, order, new Vector3(width * 0.3f, width * 0.15f, 1f));
            yield return Tween(dur, t =>
            {
                float k = OutQuad(t);
                float w = Mathf.Lerp(width * 0.3f, width * 1.5f, k);
                sr.transform.localScale = new Vector3(w, w * 0.5f, 1f);
                var cc = c; cc.a = c.a * (1f - t);
                sr.color = cc;
            });
            UnityEngine.Object.Destroy(sr.gameObject);
        }

        public static IEnumerator Glow(Vector3 pos, float size, Color c, int layer, int order, float dur)
        {
            var sr = Sprite2D(Soft, pos, c, layer, order, Vector3.one * size * 0.4f);
            yield return Tween(dur, t =>
            {
                float s = size * Mathf.Lerp(0.4f, 1f, OutQuad(t));
                sr.transform.localScale = new Vector3(s, s, 1f);
                var cc = c; cc.a = c.a * (1f - t * t);
                sr.color = cc;
            });
            UnityEngine.Object.Destroy(sr.gameObject);
        }

        public static void SetAlpha(SpriteRenderer sr, float a)
        {
            var c = sr.color; c.a = a; sr.color = c;
        }

        public static Color Hex(string h)
        {
            ColorUtility.TryParseHtmlString(h, out var c);
            return c;
        }
    }
}