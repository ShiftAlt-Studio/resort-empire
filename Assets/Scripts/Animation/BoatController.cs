using System.Collections;
using UnityEngine;
using ResortEmpire.Core;

public class BoatController : MonoBehaviour
{
    [Header("Points (empty objects in the scene)")]
    [SerializeField] private Transform seaStart;
    [SerializeField] private Transform dockPoint;
    [SerializeField] private Transform seaExit;

    [Header("Visual (child with SpriteRenderer)")]
    [SerializeField] private Transform visual;

    [Header("Sailing")]
    [SerializeField] private float sailTime = 3f;
    [SerializeField] private AnimationCurve ease = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private float departDelay = 2.5f;

    [Header("Departure Boarding")]
    [Tooltip("Assign the scene object with GuestSpawner. Boat waits for all guests to disappear.")]
    [SerializeField] private GuestSpawner guestSpawner;

    [Header("Bobbing on water")]
    [SerializeField] private float bobHeight = 0.05f;
    [SerializeField] private float bobSpeed = 2f;
    [SerializeField] private float rollAngle = 2f;

    public bool IsDocked { get; private set; }

    private Coroutine routine;
    private float time;

    private void Start()
    {
        if (GameState.Instance == null) { SnapTo(seaStart); return; }

        GameState.Instance.OnStateChanged += HandleStateChanged;

        switch (GameState.Instance.CurrentState)
        {
            case ResortState.WAVE_INCOMING:
            case ResortState.WAVE_ACTIVE:
            case ResortState.WAVE_PAUSED:
            case ResortState.WAVE_RESOLVING:
                SnapTo(dockPoint);
                IsDocked = true;
                break;
            default:
                SnapTo(seaStart);
                break;
        }
    }

    private void OnDestroy()
    {
        if (GameState.Instance != null)
            GameState.Instance.OnStateChanged -= HandleStateChanged;
    }

    private void HandleStateChanged(ResortState state)
    {
        switch (state)
        {
            case ResortState.WAVE_INCOMING:
                if (GameState.Instance.IsResumingFromPause) return;
                Run(ArriveRoutine());
                break;

            case ResortState.WAVE_COMPLETE:
                Run(DepartRoutine());
                break;

            case ResortState.AWAITING_WAVE:
                if (routine == null) { IsDocked = false; SnapTo(seaStart); }
                break;
        }
    }

    private IEnumerator ArriveRoutine()
    {
        IsDocked = false;
        SnapTo(seaStart);
        yield return Sail(seaStart.position, dockPoint.position);
        IsDocked = true;
        routine = null;
    }

    private IEnumerator DepartRoutine()
    {
        if (guestSpawner == null)
        {
            Debug.LogError("BoatController: Assign Guest Spawner in Inspector. Departure cancelled to avoid leaving guests behind.", this);
            routine = null;
            yield break;
        }

        while (!guestSpawner.AllGuestsBoarded)
            yield return null;

        float t = 0f;
        while (t < departDelay)
        {
            if (!GuestAnimator.Paused) t += Time.deltaTime;
            yield return null;
        }

        IsDocked = false;
        Transform exit = seaExit != null ? seaExit : seaStart;
        yield return Sail(dockPoint.position, exit.position);
        SnapTo(seaStart);
        routine = null;
    }

    private IEnumerator Sail(Vector3 from, Vector3 to)
    {
        float t = 0f;
        while (t < sailTime)
        {
            if (!GuestAnimator.Paused)
            {
                t += Time.deltaTime;
                transform.position = Vector3.LerpUnclamped(from, to, ease.Evaluate(Mathf.Clamp01(t / sailTime)));
            }
            yield return null;
        }
        transform.position = to;
    }

    private void Update()
    {
        if (visual == null || GuestAnimator.Paused) return;
        time += Time.deltaTime;
        visual.localPosition = Vector3.up * (Mathf.Sin(time * bobSpeed) * bobHeight);
        visual.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(time * bobSpeed * 0.7f) * rollAngle);
    }

    private void Run(IEnumerator r)
    {
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(r);
    }

    private void SnapTo(Transform t)
    {
        if (t != null) transform.position = t.position;
    }

    [ContextMenu("Debug: Arrive")] private void DebugArrive() => Run(ArriveRoutine());
    [ContextMenu("Debug: Depart")] private void DebugDepart() => Run(DepartRoutine());
}