
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ResortEmpire.UI
{
    public class RewardFlyAnimation : MonoBehaviour
    {
        [Header("HUD Targets")]
        [SerializeField] private RectTransform coinsTarget;
        [SerializeField] private RectTransform energyTarget;
        [SerializeField] private RectTransform resortValueTarget;

        [Header("Reward Sprites")]
        [SerializeField] private Sprite coinSprite;
        [SerializeField] private Sprite energySprite;
        [SerializeField] private Sprite starSprite;

        [Header("Motion")]
        [SerializeField, Min(1)] private int particlesPerReward = 12;
        [SerializeField, Min(0.1f)] private float flightDuration = 0.9f;
        [SerializeField, Min(0f)] private float particleDelay = 0.045f;
        [SerializeField, Min(1f)] private float particleSize = 80f;
        [SerializeField] private float arcHeight = 150f;
        [SerializeField, Range(0.1f, 1f)] private float endParticleScale = 0.45f;
        [SerializeField, Min(0f)] private float targetPopDuration = 0.3f;

        [Header("Reward Sounds")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip flySound;
        [SerializeField] private AudioClip collectSound;

        [SerializeField, Range(0f, 1f)]
        private float soundVolume = 0.6f;

        [SerializeField, Min(0f)]
        private float flySoundInterval = 0.12f;

        private readonly List<GameObject> spawned = new List<GameObject>();

        private RectTransform layer;
        private Canvas rootCanvas;
        private Coroutine playing;

        private float lastFlySoundTime = float.NegativeInfinity;

        public bool IsPlaying => playing != null;

        private void Awake()
        {
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();
        }

        public void Play(
            RectTransform coinsOrigin,
            RectTransform energyOrigin,
            RectTransform resortValueOrigin,
            Action onFinished)
        {
            if (playing != null)
                return;

            playing = StartCoroutine(
                PlayRoutine(
                    coinsOrigin,
                    energyOrigin,
                    resortValueOrigin,
                    onFinished));
        }

        private IEnumerator PlayRoutine(
            RectTransform coinsOrigin,
            RectTransform energyOrigin,
            RectTransform resortValueOrigin,
            Action onFinished)
        {
            rootCanvas = GetComponentInParent<Canvas>();

            if (rootCanvas == null)
                rootCanvas = FindAnyObjectByType<Canvas>();

            if (rootCanvas == null)
            {
                Debug.LogWarning(
                    "RewardFlyAnimation: Canvas not found.", this);

                playing = null;
                onFinished?.Invoke();
                yield break;
            }

            rootCanvas = rootCanvas.rootCanvas;
            CreateLayer();

            lastFlySoundTime = float.NegativeInfinity;

            int groups = 0;

            if (coinsOrigin != null && coinsTarget != null)
                groups++;

            if (energyOrigin != null && energyTarget != null)
                groups++;

            if (resortValueOrigin != null && resortValueTarget != null)
                groups++;

            if (groups == 0)
            {
                Debug.LogWarning(
                    "RewardFlyAnimation: Missing reward origins or HUD targets.",
                    this);
            }

            if (coinsOrigin != null && coinsTarget != null)
            {
                StartCoroutine(
                    FlyGroup(
                        coinsOrigin,
                        coinsTarget,
                        coinSprite,
                        "$",
                        new Color(1f, 0.77f, 0.12f)));
            }

            if (energyOrigin != null && energyTarget != null)
            {
                StartCoroutine(
                    FlyGroup(
                        energyOrigin,
                        energyTarget,
                        energySprite,
                        "\u26A1",
                        new Color(0.15f, 0.65f, 1f)));
            }

            if (resortValueOrigin != null && resortValueTarget != null)
            {
                StartCoroutine(
                    FlyGroup(
                        resortValueOrigin,
                        resortValueTarget,
                        starSprite,
                        "\u2605",
                        new Color(1f, 0.69f, 0.12f)));
            }

            float wait =
                flightDuration +
                Mathf.Max(0, particlesPerReward - 1) * particleDelay +
                targetPopDuration +
                0.1f;

            yield return new WaitForSecondsRealtime(
                groups > 0 ? wait : 0.1f);

            ClearLayer();

            playing = null;
            onFinished?.Invoke();
        }

        private void CreateLayer()
        {
            GameObject go = new GameObject(
                "RewardFlightLayer",
                typeof(RectTransform));

            go.transform.SetParent(rootCanvas.transform, false);

            layer = go.GetComponent<RectTransform>();
            layer.anchorMin = Vector2.zero;
            layer.anchorMax = Vector2.one;
            layer.offsetMin = Vector2.zero;
            layer.offsetMax = Vector2.zero;

            layer.SetAsLastSibling();
            spawned.Add(go);
        }

        private IEnumerator FlyGroup(
            RectTransform origin,
            RectTransform target,
            Sprite sprite,
            string fallback,
            Color color)
        {
            for (int i = 0; i < particlesPerReward; i++)
            {
                if (layer == null)
                    yield break;

                StartCoroutine(
                    FlyOne(
                        origin,
                        target,
                        sprite,
                        fallback,
                        color,
                        i));

                TryPlayFlySound();

                yield return new WaitForSecondsRealtime(particleDelay);
            }

            yield return new WaitForSecondsRealtime(flightDuration);

            if (target != null)
            {
                PlayCollectSound();
                StartCoroutine(PopTarget(target));
            }
        }

        private IEnumerator FlyOne(
            RectTransform origin,
            RectTransform target,
            Sprite sprite,
            string fallback,
            Color color,
            int index)
        {
            if (layer == null)
                yield break;

            GameObject go = new GameObject(
                "FlyingReward",
                typeof(RectTransform));

            go.transform.SetParent(layer, false);
            spawned.Add(go);

            RectTransform rt = go.GetComponent<RectTransform>();

            rt.sizeDelta = new Vector2(
                particleSize,
                particleSize);

            Graphic graphic;

            if (sprite != null)
            {
                Image img = go.AddComponent<Image>();
                img.sprite = sprite;
                img.preserveAspect = true;
                img.color = Color.white;
                graphic = img;
            }
            else
            {
                TextMeshProUGUI label =
                    go.AddComponent<TextMeshProUGUI>();

                label.text = fallback;
                label.fontSize = particleSize * 0.8f;
                label.fontStyle = FontStyles.Bold;
                label.alignment = TextAlignmentOptions.Center;
                label.color = color;

                graphic = label;
            }

            graphic.raycastTarget = false;

            Vector2 start = ToLayer(origin);
            Vector2 end = ToLayer(target);

            Vector2 spread = new Vector2(
                UnityEngine.Random.Range(-32f, 32f),
                UnityEngine.Random.Range(-25f, 25f));

            start += spread;

            float elapsed = 0f;

            while (elapsed < flightDuration && rt != null)
            {
                elapsed += Time.unscaledDeltaTime;

                float t = Mathf.Clamp01(
                    elapsed / flightDuration);

                float eased = t * t * (3f - 2f * t);

                Vector2 position =
                    Vector2.LerpUnclamped(start, end, eased);

                position += Vector2.up *
                    Mathf.Sin(t * Mathf.PI) *
                    (arcHeight + index % 3 * 16f);

                rt.anchoredPosition = position;

                float scale = Mathf.Lerp(
                    1f,
                    endParticleScale,
                    eased);

                rt.localScale = Vector3.one * scale;

                Color c = graphic.color;

                c.a = t > 0.8f
                    ? Mathf.InverseLerp(1f, 0.8f, t)
                    : 1f;

                graphic.color = c;

                yield return null;
            }

            if (go != null)
            {
                spawned.Remove(go);
                Destroy(go);
            }
        }

        private Vector2 ToLayer(RectTransform source)
        {
            Camera camera =
                rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                    ? null
                    : rootCanvas.worldCamera;

            Vector2 screen =
                RectTransformUtility.WorldToScreenPoint(
                    camera,
                    source.TransformPoint(source.rect.center));

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                layer,
                screen,
                camera,
                out Vector2 local);

            return local;
        }

        private IEnumerator PopTarget(RectTransform target)
        {
            if (target == null)
                yield break;

            Vector3 initial = target.localScale;
            float elapsed = 0f;

            while (elapsed < targetPopDuration && target != null)
            {
                elapsed += Time.unscaledDeltaTime;

                float t = Mathf.Clamp01(
                    elapsed / Mathf.Max(0.01f, targetPopDuration));

                target.localScale =
                    initial *
                    (1f + 0.22f * Mathf.Sin(t * Mathf.PI));

                yield return null;
            }

            if (target != null)
                target.localScale = initial;
        }

        private void TryPlayFlySound()
        {
            if (audioSource == null || flySound == null)
                return;

            if (Time.unscaledTime - lastFlySoundTime < flySoundInterval)
                return;

            lastFlySoundTime = Time.unscaledTime;

            audioSource.PlayOneShot(
                flySound,
                soundVolume);
        }

        private void PlayCollectSound()
        {
            if (audioSource == null || collectSound == null)
                return;

            audioSource.PlayOneShot(
                collectSound,
                soundVolume);
        }

        private void ClearLayer()
        {
            if (layer != null)
                Destroy(layer.gameObject);

            layer = null;
            spawned.Clear();
        }

        private void OnDisable()
        {
            if (playing != null)
                StopCoroutine(playing);

            playing = null;
            ClearLayer();
        }
    }
}
