using UnityEngine;

public class Crop : MonoBehaviour, IInteractable, ITargetableInfo
{
    public float growTime = 10f;
    public Sprite[] growStages;

    private float timer = 0f;
    private int stage = 0;
    private SpriteRenderer sr;

    public FarmPlot plot;

    public int harvestItemID;
    public int harvestAmount = 1;

    private Item itemDataCache;
    private void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        sr.sprite = growStages[0];

        var itemDictionary = FindFirstObjectByType<ItemDictionary>();
        if (itemDictionary != null)
        {
            GameObject itemPrefab = itemDictionary.GetItemPrefab(harvestItemID);
            if (itemPrefab != null)
            {
                itemDataCache = itemPrefab.GetComponent<Item>();
            }
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

    // ============================
    // IInteractable Implementation
    // ============================
    public void Interact()
    {
        if (!IsReady()) return;

        FarmController.Instance.TryHarvest(plot);
    }

    public bool CanInteract()
    {
        return IsReady();
    }

    public TargetInfoData GetInfo()
    {
        if (itemDataCache == null)
        {
            return new TargetInfoData("Cây trồng", null, "...", TargetType.Item);
        }

        if (IsReady())
        {
            return new TargetInfoData(
                itemDataCache.Name,
                itemDataCache.icon,
                "Thu hoạch",
                TargetType.Item,
                itemDataCache.rarity
            );
        }

        return new TargetInfoData(
            itemDataCache.Name,
            itemDataCache.icon,
            "Đang lớn...",
            TargetType.Item
        );
    }
}
