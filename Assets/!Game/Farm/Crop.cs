using UnityEngine;

public class Crop : MonoBehaviour, IInteractable, ITargetableInfo
{
    [Header("Save Data")]
    public int seedItemID;

    [Header("Growth Settings")]
    public float growTime = 10f;
    public Sprite[] growStages;

    [HideInInspector] public float timer = 0f;
    [HideInInspector] public int stage = 0;

    [Header("Harvest Settings")]
    public GameObject harvestItemPrefab;
    public int harvestAmount = 1;

    private Item _cachedHarvestItemData;
    private SpriteRenderer sr;

    [HideInInspector] public FarmPlot plot;

    private void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        if (stage == 0 && growStages.Length > 0)
        {
            sr.sprite = growStages[0];
        }

        if (harvestItemPrefab != null)
        {
            _cachedHarvestItemData = harvestItemPrefab.GetComponent<Item>();
        }
        else
        {
            Debug.LogError($"Crop '{gameObject.name}' chưa được gán Harvest Item Prefab!");
        }
    }

    private void Update()
    {
        if (stage < growStages.Length - 1)
        {
            timer += Time.deltaTime;
            if (timer >= growTime)
            {
                timer = 0f;
                stage++;
                sr.sprite = growStages[stage];
            }
        }
    }

    public bool IsReady() => stage == growStages.Length - 1;

    public void RestoreState(int loadedStage, float loadedTimer)
    {
        this.stage = loadedStage;
        this.timer = loadedTimer;

        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (growStages != null && stage < growStages.Length)
        {
            sr.sprite = growStages[stage];
        }
    }

    // ============================
    // Interactions
    // ============================
    public void Interact()
    {
        if (!IsReady()) return;
        FarmController.Instance.TryHarvest(plot);
    }

    public bool CanInteract() => IsReady();

    public TargetInfoData GetInfo()
    {
        if (_cachedHarvestItemData == null)
            return new TargetInfoData("Cây trồng (Lỗi Data)", null, "...", TargetType.Item);

        if (IsReady())
        {
            return new TargetInfoData(
                _cachedHarvestItemData.Name,
                _cachedHarvestItemData.icon,
                "Thu hoạch",
                TargetType.Item,
                _cachedHarvestItemData.rarity
            );
        }

        return new TargetInfoData(
            _cachedHarvestItemData.Name,
            _cachedHarvestItemData.icon,
            "Đang lớn...",
            TargetType.Item
        );
    }
}