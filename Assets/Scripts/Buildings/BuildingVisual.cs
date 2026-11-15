using UnityEngine;
using ResortEmpire.Fx;

[RequireComponent(typeof(Building))]
public class BuildingVisual : MonoBehaviour
{
    [SerializeField] private Building building;
    [SerializeField] private SpriteRenderer targetRenderer;
    [SerializeField] private BuildingLevelUpFx levelUpFx;

    [Tooltip("sprites by levels: [0] = Lv1, [1] = Lv2, [2] = Lv3")]
    [SerializeField] private Sprite[] spritesByLevel = new Sprite[Building.MaxLevel];

    [Tooltip("Size multiplier relative to Lv1: [0] = Lv1 (leave it at 1), [1] = Lv2, [2] = Lv3. If Lv2/Lv3 are too large, set them to 0.8, 0.7, etc.")]
    [SerializeField] private float[] scaleByLevel = { 1f, 1f, 1f };

    private Vector3 baseScale;

    private void Reset()
    {
        building = GetComponent<Building>();
        targetRenderer = GetComponentInChildren<SpriteRenderer>();
        levelUpFx = GetComponentInChildren<BuildingLevelUpFx>();
    }

    private void OnValidate()
    {
        if (spritesByLevel == null || spritesByLevel.Length != Building.MaxLevel)
            System.Array.Resize(ref spritesByLevel, Building.MaxLevel);

        if (scaleByLevel == null || scaleByLevel.Length != Building.MaxLevel)
        {
            int old = scaleByLevel == null ? 0 : scaleByLevel.Length;
            System.Array.Resize(ref scaleByLevel, Building.MaxLevel);
            for (int i = old; i < scaleByLevel.Length; i++) scaleByLevel[i] = 1f;
        }
    }

    private void Awake()
    {
        if (building == null) building = GetComponent<Building>();
        baseScale = targetRenderer.transform.localScale;
    }

    private void OnEnable() { building.LevelChanged += OnLevelChanged; }
    private void OnDisable() { building.LevelChanged -= OnLevelChanged; }

    private void Start()
    {
        ApplySprite(SpriteFor(building.Level));
        targetRenderer.transform.localScale = ScaleFor(building.Level);
    }

    private void OnLevelChanged(Building b, int newLevel)
    {
        Sprite next = SpriteFor(newLevel);
        Vector3 nextScale = ScaleFor(newLevel);

        if (levelUpFx != null && levelUpFx.isActiveAndEnabled)
        {
            levelUpFx.Play(next, newLevel, null, nextScale);
        }
        else
        {
            ApplySprite(next);
            targetRenderer.transform.localScale = nextScale;
        }
    }

    private Sprite SpriteFor(int level)
    {
        return spritesByLevel[Mathf.Clamp(level, 1, Building.MaxLevel) - 1];
    }

    private Vector3 ScaleFor(int level)
    {
        return baseScale * scaleByLevel[Mathf.Clamp(level, 1, Building.MaxLevel) - 1];
    }

    private void ApplySprite(Sprite s)
    {
        if (s != null && targetRenderer != null) targetRenderer.sprite = s;
    }

    [ContextMenu("Test Upgrade (+1 level)")]
    private void TestUpgrade() => building.SetLevel(building.Level + 1);
}