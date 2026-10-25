using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace ResortEmpire.Fx
{
    public enum UnlockFlavor { Pool, Bungalows }


    public class BuildingUnlockFx : MonoBehaviour
    {
        public UnlockFlavor flavor = UnlockFlavor.Pool;
        public SpriteRenderer site;
        public SpriteRenderer building;
        public Transform groundPoint;
        public float groundWidth = 5f;
        public bool shakeCamera = true;
        public bool hideBuildingOnStart = true;
        public UnityEvent onImpact;
        public UnityEvent onFinished;

        Vector3 _bs;
        bool _running;

        void Awake()
        {
            _bs = building.transform.localScale;
            if (hideBuildingOnStart) building.enabled = false;
        }

        [ContextMenu("Test Play")]
        void TestPlay() { site.enabled = true; Play(); }

        public void Play(Action done = null)
        {
            if (_running) return;
            StartCoroutine(Run(done));
        }

        IEnumerator Run(Action done)
        {
            _running = true;
            var st = site.transform;
            Vector3 siteScale = st.localScale, sitePos = st.localPosition;
            Color siteCol = site.color;
            var bt = building.transform;
            Vector3 ground = groundPoint ? groundPoint.position : bt.position;
            int layer = building.sortingLayerID, order = building.sortingOrder;
            bool pool = flavor == UnlockFlavor.Pool;

            FxKit.Dust(ground, groundWidth, layer, order + 3, 10);
            yield return FxKit.Tween(0.45f, t =>
            {
                float k = FxKit.InQuad(t);
                st.localPosition = sitePos + new Vector3(Mathf.Sin(t * 70f) * 0.06f * k, 0f, 0f);
                st.localScale = siteScale * (1f + 0.05f * Mathf.Sin(t * 40f) * k);
            });

            FxKit.Dust(ground, groundWidth * 1.3f, layer, order + 3, 26);
            yield return FxKit.Tween(0.22f, t =>
            {
                float k = FxKit.InQuad(t);
                st.localScale = new Vector3(siteScale.x * (1f + 0.2f * k), siteScale.y * (1f - k), siteScale.z);
                FxKit.SetAlpha(site, siteCol.a * (1f - k * k));
            });
            site.enabled = false;
            st.localPosition = sitePos; st.localScale = siteScale; site.color = siteCol;

            building.enabled = true;
            FxKit.SetAlpha(building, 0f);
            bt.localScale = new Vector3(_bs.x * 0.6f, 0.0001f, _bs.z);
            bool impact = false;
            yield return FxKit.Tween(0.6f, t =>
            {
                float ky = FxKit.OutBack(t);
                float kx = Mathf.Lerp(0.6f, 1f, FxKit.OutElastic(Mathf.Clamp01(t * 1.1f)));
                bt.localScale = new Vector3(_bs.x * kx, _bs.y * Mathf.Max(0.0001f, ky), _bs.z);
                FxKit.SetAlpha(building, Mathf.Clamp01(t * 6f));
                if (!impact && t > 0.45f) { impact = true; Impact(ground, layer, order, pool); }
            });

            yield return FxKit.Tween(0.35f, t =>
            {
                float w = Mathf.Sin(t * Mathf.PI * 3f) * (1f - t) * 0.04f;
                bt.localScale = new Vector3(_bs.x * (1f - w), _bs.y * (1f + w), _bs.z);
            });
            bt.localScale = _bs;
            FxKit.SetAlpha(building, 1f);

            yield return FxKit.Wait(0.4f);
            _running = false;
            onFinished?.Invoke();
            done?.Invoke();
        }

        void Impact(Vector3 ground, int layer, int order, bool pool)
        {
            float h = building.sprite ? building.sprite.bounds.max.y * _bs.y : 3f;
            Vector3 center = ground + Vector3.up * h * 0.5f;
            Color accent = pool ? FxKit.Hex("#7FE3FF") : FxKit.Hex("#FFD27A");

            StartCoroutine(FxKit.RingWave(ground, groundWidth, new Color(accent.r, accent.g, accent.b, 0.9f), layer, order - 1, 0.55f));
            StartCoroutine(FxKit.Glow(center, groundWidth * 1.8f, new Color(accent.r, accent.g, accent.b, 0.85f), layer, order - 2, pool ? 0.7f : 1.0f));
            FxKit.Dust(ground, groundWidth * 1.2f, layer, order + 3, 18);
            if (shakeCamera) StartCoroutine(FxKit.Shake(0.3f, pool ? 0.05f : 0.08f));

            if (pool)
            {
                FxKit.Burst(ground, FxKit.Soft, layer, order + 4, 46, 0.7f, 1.1f, 3.5f, 7f, 0.10f, 0.20f,
                    FxKit.Hex("#7FE3FF"), Color.white, 2.2f, ParticleSystemShapeType.Cone, groundWidth * 0.18f,
                    Vector3.one, new Vector3(-90f, 0f, 0f), 28f);
                StartCoroutine(FxKit.RingWave(ground, groundWidth, FxKit.Hex("#BFEFFF"), layer, order - 1, 0.7f, 0.12f));
                StartCoroutine(FxKit.RingWave(ground, groundWidth, FxKit.Hex("#E8FAFF"), layer, order - 1, 0.8f, 0.26f));
                FxKit.Burst(center, FxKit.Star, layer, order + 5, 22, 1.2f, 1.8f, 0.3f, 1.2f, 0.15f, 0.30f,
                    Color.white, FxKit.Hex("#9FE8FF"), -0.05f, ParticleSystemShapeType.Circle, groundWidth * 0.4f,
                    new Vector3(1f, 0.8f, 1f), Vector3.zero, 25f, 120f, 0.1f);
            }
            else
            {
                FxKit.Burst(center, FxKit.Star, layer, order + 5, 30, 1.0f, 1.6f, 1.5f, 4f, 0.18f, 0.35f,
                    FxKit.Hex("#FFD54F"), FxKit.Hex("#FFF1D6"), -0.15f, ParticleSystemShapeType.Circle, 0.3f,
                    Vector3.one, Vector3.zero, 25f, 160f);
                Vector3 top = ground + Vector3.up * (h + 1.2f);
                FxKit.Burst(top, FxKit.Petal, layer, order + 6, 36, 2.0f, 2.8f, 0.2f, 0.8f, 0.2f, 0.34f,
                    FxKit.Hex("#FF8FA3"), FxKit.Hex("#FFB347"), 0.25f, ParticleSystemShapeType.Box, 1f,
                    new Vector3(groundWidth, 0.2f, 1f), new Vector3(90f, 0f, 0f), 25f, 200f, 0.05f, false);
                StartCoroutine(FxKit.RingWave(ground, groundWidth, FxKit.Hex("#FFF1D6"), layer, order - 1, 0.8f, 0.18f));
            }
            onImpact?.Invoke();
        }
    }
}