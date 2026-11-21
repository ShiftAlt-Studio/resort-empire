using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class GuestAnimator : MonoBehaviour
{
    public enum State { Hidden, Idle, Walking }

    [Header("Idle Frames (queue_a, queue_b)")]
    [SerializeField] private Sprite[] idleFrames;
    [SerializeField] private float frameInterval = 0.45f;

    [Header("Walk Frames (2 frames, alternating)")]
    [SerializeField] private Sprite[] enterFrames;
    [SerializeField] private Sprite[] leaveFrames;
    [SerializeField] private float walkFrameInterval = 0.18f;

    [Header("Idle Motion (world units)")]
    [SerializeField] private float bobHeight = 0.04f;
    [SerializeField] private float bobSpeed = 3f;
    [SerializeField] private float swayAngle = 3f;

    [Header("Walk Motion")]
    [SerializeField] private float walkSpeed = 1.0f;
    [SerializeField] private float walkBobHeight = 0.07f;
    [SerializeField] private float walkBobSpeed = 10f;

    [Header("Sorting")]
    [SerializeField] private bool ySort = true;
    [SerializeField] private int sortingBase = 10;
    [SerializeField] private float ySortScale = 100f;

    [Header("Fade")]
    [SerializeField] private float fadeTime = 0.3f;

    public static bool Paused;

    private SpriteRenderer sr;
    private Image img;
    private Vector3 basePos;
    private float phase;
    private float animTime;
    private Sprite[] currentWalkFrames;
    private Coroutine routine;

    public State Current { get; private set; } = State.Hidden;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        img = GetComponent<Image>();
        if (img != null) img.raycastTarget = false;
        phase = UnityEngine.Random.value * 10f;
        basePos = transform.localPosition;
    }

    private void OnEnable()
    {
        if (Current == State.Hidden) Current = State.Idle;
    }

    public void Enter(Vector3 from, Vector3 to, Action onDone = null)
    {
        currentWalkFrames = enterFrames;
        basePos = from;
        transform.localPosition = from;
        SetAlpha(0f);
        gameObject.SetActive(true);
        Run(WalkRoutine(to, true, false, onDone));
    }

    public void MoveTo(Vector3 target, Action onDone = null)
    {
        currentWalkFrames = enterFrames;
        Run(WalkRoutine(target, false, false, onDone));
    }

    public void Leave(Vector3 exitPoint, Action onDone = null)
    {
        currentWalkFrames = leaveFrames;
        Run(WalkRoutine(exitPoint, false, true, () =>
        {
            Current = State.Hidden;
            gameObject.SetActive(false);
            onDone?.Invoke();
        }));
    }

    private void Run(IEnumerator r)
    {
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(r);
    }

    private IEnumerator WalkRoutine(Vector3 target, bool fadeIn, bool fadeOut, Action onDone)
    {
        Current = State.Walking;
        float fadeDist = Mathf.Max(0.001f, walkSpeed * fadeTime);
        float t = 0f;

        while ((basePos - target).sqrMagnitude > 0.0001f)
        {
            if (Paused) { yield return null; continue; }

            basePos = Vector3.MoveTowards(basePos, target, walkSpeed * Time.deltaTime);
            t += Time.deltaTime;

            float a = 1f;
            if (fadeIn) a = Mathf.Min(a, Mathf.Clamp01(t / fadeTime));
            if (fadeOut) a = Mathf.Min(a, Mathf.Clamp01(Vector3.Distance(basePos, target) / fadeDist));
            SetAlpha(a);
            yield return null;
        }

        if (!fadeOut) SetAlpha(1f);
        Current = State.Idle;
        routine = null;
        onDone?.Invoke();
    }

    private void Update()
    {
        if (Current == State.Hidden || Paused) return;
        animTime += Time.deltaTime;

        if (ySort && sr != null)
            sr.sortingOrder = sortingBase - Mathf.RoundToInt(transform.position.y * ySortScale);

        float time = animTime + phase;
        bool walking = Current == State.Walking;

        float speed = walking ? walkBobSpeed : bobSpeed;
        float height = walking ? walkBobHeight : bobHeight;

        float wave = walking ? Mathf.Abs(Mathf.Sin(time * speed)) : (Mathf.Sin(time * speed) * 0.5f + 0.5f);
        transform.localPosition = basePos + Vector3.up * (wave * height);

        float sway = Mathf.Sin(time * speed * 0.5f) * (walking ? swayAngle * 1.5f : swayAngle);
        transform.localRotation = Quaternion.Euler(0f, 0f, sway);

        Sprite frame = walking
            ? PickFrame(currentWalkFrames, time, walkFrameInterval)
            : PickFrame(idleFrames, time, frameInterval);

        if (frame != null) SetSprite(frame);
    }

    private static Sprite PickFrame(Sprite[] frames, float time, float interval)
    {
        if (frames == null || frames.Length == 0) return null;
        if (frames.Length == 1) return frames[0];
        int i = (int)(time / interval) % frames.Length;
        return frames[i];
    }

    private void SetSprite(Sprite s)
    {
        if (sr != null && sr.sprite != s) sr.sprite = s;
        if (img != null && img.sprite != s) img.sprite = s;
    }

    private void SetAlpha(float a)
    {
        if (sr != null) { var c = sr.color; c.a = a; sr.color = c; }
        if (img != null) { var c = img.color; c.a = a; img.color = c; }
    }
}