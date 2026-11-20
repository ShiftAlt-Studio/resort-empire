using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ResortEmpire.Core;

public class GuestSpawner : MonoBehaviour
{
    [Serializable]
    public class BuildingQueue
    {
        public GuestNeedType need;
        public Transform[] slots;
    }

    [Header("Guest Prefabs")]
    [SerializeField] private GuestAnimator[] guestPrefabs;

    [Header("Boat Points")]
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform exitPoint;
    [SerializeField] private Transform guestsParent;
    [SerializeField] private BoatController boat;

    [Header("Building Queues (slot 0 nearest entrance)")]
    [SerializeField] private BuildingQueue[] buildingQueues;

    [Header("Wave")]
    [SerializeField] private NeedAggregator needAggregator;
    [Min(1)][SerializeField] private int unitsPerGuest = 1;
    [Min(0.05f)][SerializeField] private float spawnInterval = 0.4f;
    [Min(0.05f)][SerializeField] private float leaveInterval = 0.25f;

    private class ActiveGuest
    {
        public GuestAnimator guest;
        public GuestNeedType need;
        public int slot;
    }

    private readonly List<ActiveGuest> active = new List<ActiveGuest>();
    private readonly Dictionary<GuestNeedType, List<ActiveGuest>> lines = new Dictionary<GuestNeedType, List<ActiveGuest>>();
    private readonly Dictionary<GuestNeedType, int> remaining = new Dictionary<GuestNeedType, int>();
    private readonly Dictionary<GuestNeedType, int> credits = new Dictionary<GuestNeedType, int>();
    private readonly Dictionary<GuestAnimator, Stack<GuestAnimator>> pools = new Dictionary<GuestAnimator, Stack<GuestAnimator>>();
    private readonly Dictionary<GuestAnimator, GuestAnimator> prefabOf = new Dictionary<GuestAnimator, GuestAnimator>();

    private Coroutine spawnRoutine;
    private Coroutine leaveRoutine;
    private int returning;
    private bool waveEnding;

    public bool AllGuestsBoarded => returning == 0 && leaveRoutine == null &&
        spawnRoutine == null && active.Count == 0;

    private void Awake()
    {
        if (guestsParent == null) guestsParent = transform;
        if (needAggregator == null) needAggregator = FindAnyObjectByType<NeedAggregator>();
        if (buildingQueues != null)
        {
            foreach (var queue in buildingQueues)
            {
                if (queue == null) continue;
                if (lines.ContainsKey(queue.need))
                    Debug.LogWarning($"GuestSpawner: Duplicate queue for {queue.need}", this);
                else lines.Add(queue.need, new List<ActiveGuest>());
            }
        }
    }

    private void Start()
    {
        if (needAggregator != null)
            needAggregator.ServiceCompleted += OnServiceCompleted;
        else Debug.LogWarning("GuestSpawner: NeedAggregator missing", this);

        if (GameState.Instance != null)
        {
            GameState.Instance.OnStateChanged += HandleStateChanged;
            GuestAnimator.Paused = GameState.Instance.CurrentState == ResortState.WAVE_PAUSED;
        }
    }

    private void OnDestroy()
    {
        if (needAggregator != null)
            needAggregator.ServiceCompleted -= OnServiceCompleted;
        if (GameState.Instance != null)
            GameState.Instance.OnStateChanged -= HandleStateChanged;
        GuestAnimator.Paused = false;
    }

    private void HandleStateChanged(ResortState state)
    {
        GuestAnimator.Paused = state == ResortState.WAVE_PAUSED;
        if (state == ResortState.WAVE_INCOMING)
        {
            if (GameState.Instance != null && !GameState.Instance.IsResumingFromPause)
                StartWave();
        }
        else if (state == ResortState.WAVE_COMPLETE ||
                 state == ResortState.AWAITING_WAVE || state == ResortState.UPGRADE)
        {
            LeaveAll();
        }
    }

    private BuildingQueue GetQueue(GuestNeedType need)
    {
        if (buildingQueues == null) return null;
        foreach (var queue in buildingQueues)
            if (queue != null && queue.need == need) return queue;
        return null;
    }

    private int Capacity(GuestNeedType need)
    {
        var queue = GetQueue(need);
        if (queue == null || queue.slots == null) return 0;
        int count = 0;
        foreach (var slot in queue.slots) if (slot != null) count++;
        return count;
    }

    private Transform SlotAt(GuestNeedType need, int index)
    {
        var queue = GetQueue(need);
        if (queue == null || queue.slots == null) return null;
        int valid = 0;
        foreach (var slot in queue.slots)
        {
            if (slot == null) continue;
            if (valid == index) return slot;
            valid++;
        }
        return null;
    }

    private void StartWave()
    {
        if (spawnRoutine != null) StopCoroutine(spawnRoutine);
        spawnRoutine = null;
        waveEnding = false;
        remaining.Clear();
        credits.Clear();
        foreach (var line in lines.Values) line.Clear();
        active.Clear();

        if (GameState.Instance == null) return;
        var forecast = GameState.Instance.GetForecast();
        if (forecast == null) return;

        foreach (var demand in forecast)
        {
            if (Capacity(demand.Need) == 0)
            {
                if (demand.Units > 0)
                    Debug.LogWarning($"GuestSpawner: No queue slots for {demand.Need}", this);
                continue;
            }
            int count = Mathf.CeilToInt((float)Mathf.Max(0, demand.Units) / Mathf.Max(1, unitsPerGuest));
            if (!remaining.ContainsKey(demand.Need)) remaining[demand.Need] = 0;
            remaining[demand.Need] += count;
        }

        if (remaining.Count > 0) spawnRoutine = StartCoroutine(SpawnLoop());
    }

    private IEnumerator SpawnLoop()
    {
        float wait = 0f;
        while (!waveEnding && HasPending())
        {
            if (GuestAnimator.Paused || (boat != null && !boat.IsDocked))
            {
                yield return null;
                continue;
            }

            if (wait > 0f)
            {
                wait -= Time.deltaTime;
                yield return null;
                continue;
            }

            bool spawned = false;
            GuestNeedType selectedNeed = default;
            int selectedSlot = -1;
            foreach (var pair in remaining)
            {
                if (pair.Value <= 0) continue;
                if (!lines.TryGetValue(pair.Key, out var line)) continue;
                if (line.Count >= Capacity(pair.Key)) continue;
                selectedNeed = pair.Key;
                selectedSlot = line.Count;
                break;
            }

            if (selectedSlot >= 0)
            {
                if (guestPrefabs == null || guestPrefabs.Length == 0 || spawnPoint == null)
                {
                    Debug.LogWarning("GuestSpawner: Missing prefabs or spawnPoint", this);
                    waveEnding = true;
                }
                else
                {
                    SpawnGuest(selectedNeed, selectedSlot);
                    remaining[selectedNeed]--;
                    spawned = true;
                    wait = spawnInterval;
                }
            }

            if (!spawned) yield return null;
        }
        spawnRoutine = null;
    }

    private bool HasPending()
    {
        foreach (var pair in remaining)
            if (pair.Value > 0) return true;
        return false;
    }

    private void SpawnGuest(GuestNeedType need, int slot)
    {
        var prefab = guestPrefabs[UnityEngine.Random.Range(0, guestPrefabs.Length)];
        var guest = Get(prefab);
        var ag = new ActiveGuest { guest = guest, need = need, slot = slot };
        active.Add(ag);
        lines[need].Add(ag);
        guest.Enter(ToLocal(spawnPoint.position), ToLocal(SlotAt(need, slot).position), ApplyCredits);
    }

    private void OnServiceCompleted(GuestNeedType need, int count)
    {
        if (waveEnding || count <= 0) return;
        if (!credits.ContainsKey(need)) credits[need] = 0;
        credits[need] += count;
        ApplyCredits();
    }

    private void Update()
    {
        if (!GuestAnimator.Paused && !waveEnding) ApplyCredits();
    }

    private void ApplyCredits()
    {
        if (waveEnding || GuestAnimator.Paused) return;
        foreach (var pair in lines)
        {
            GuestNeedType need = pair.Key;
            var line = pair.Value;
            if (!credits.TryGetValue(need, out int available)) continue;
            while (available >= Mathf.Max(1, unitsPerGuest) && line.Count > 0)
            {
                if (line[0].guest.Current != GuestAnimator.State.Idle) break;
                available -= Mathf.Max(1, unitsPerGuest);
                SendFirstToBoat(need);
            }
            credits[need] = available;
        }
    }

    private void SendFirstToBoat(GuestNeedType need)
    {
        var line = lines[need];
        if (line.Count == 0) return;
        var first = line[0];
        line.RemoveAt(0);
        active.Remove(first);
        SendToBoat(first.guest);

        for (int i = 0; i < line.Count; i++)
        {
            line[i].slot = i;
            Transform slot = SlotAt(need, i);
            if (slot != null) line[i].guest.MoveTo(ToLocal(slot.position));
        }
    }

    private void SendToBoat(GuestAnimator guest)
    {
        returning++;
        Vector3 exit = exitPoint != null ? exitPoint.position :
            (spawnPoint != null ? spawnPoint.position : transform.position);
        guest.Leave(ToLocal(exit), () =>
        {
            Release(guest);
            returning--;
        });
    }

    public void LeaveAll()
    {
        waveEnding = true;
        remaining.Clear();
        credits.Clear();
        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }
        if (leaveRoutine == null && active.Count > 0)
            leaveRoutine = StartCoroutine(LeaveAllRoutine());
    }

    private IEnumerator LeaveAllRoutine()
    {
        var leaving = new List<ActiveGuest>(active);
        active.Clear();
        foreach (var line in lines.Values) line.Clear();
        foreach (var ag in leaving)
        {
            while (GuestAnimator.Paused) yield return null;
            SendToBoat(ag.guest);
            float timer = 0f;
            while (timer < leaveInterval)
            {
                if (!GuestAnimator.Paused) timer += Time.deltaTime;
                yield return null;
            }
        }
        leaveRoutine = null;
    }

    private Vector3 ToLocal(Vector3 world) => guestsParent.InverseTransformPoint(world);

    private GuestAnimator Get(GuestAnimator prefab)
    {
        if (!pools.TryGetValue(prefab, out var stack))
        {
            stack = new Stack<GuestAnimator>();
            pools[prefab] = stack;
        }
        var guest = stack.Count > 0 ? stack.Pop() : Instantiate(prefab, guestsParent);
        prefabOf[guest] = prefab;
        return guest;
    }

    private void Release(GuestAnimator guest)
    {
        if (prefabOf.TryGetValue(guest, out var prefab))
            pools[prefab].Push(guest);
    }
}
