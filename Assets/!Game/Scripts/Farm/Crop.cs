using System.Collections.Generic;
using UnityEngine;

public class Crop : MonoBehaviour, IInteractable, ITargetableInfo
{
    [System.Serializable]
    public struct StageData
    {
        public Sprite sprite;
        public float timeToNextStage; // Thời gian cần để lớn lên giai đoạn tiếp theo
    }

    [Header("Save Data")]
    public int seedItemID;

    [Header("Growth Settings")]
    // List chứa thông tin từng giai đoạn (Sprite + Thời gian)
    public List<StageData> cropStages;

    [HideInInspector] public float timer = 0f;
    [HideInInspector] public int stage = 0;

    [Header("Harvest Settings")]
    public GameObject harvestItemPrefab;
    public int harvestAmount = 1;

    [Header("Regrow Settings")]
    public bool isRegrowable = false;
    public int regrowStage = 0;

    private Item _cachedHarvestItemData;
    private SpriteRenderer sr;

    [HideInInspector] public FarmPlot plot;

    private void Start()
    {
        sr = GetComponent<SpriteRenderer>();

        // Cập nhật hiển thị ban đầu
        if (cropStages != null && stage < cropStages.Count)
        {
            sr.sprite = cropStages[stage].sprite;
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
        // Chỉ chạy timer nếu chưa đến giai đoạn cuối cùng
        if (stage < cropStages.Count - 1)
        {
            timer += Time.deltaTime;

            // Kiểm tra thời gian dựa trên cấu hình của giai đoạn hiện tại
            if (timer >= cropStages[stage].timeToNextStage)
            {
                timer = 0f;
                stage++;
                UpdateSprite();
            }
        }
    }

    private void UpdateSprite()
    {
        if (sr != null && stage < cropStages.Count)
        {
            sr.sprite = cropStages[stage].sprite;
        }
    }

    // Cây sẵn sàng khi ở giai đoạn cuối cùng của List
    public bool IsReady() => stage == cropStages.Count - 1;

    public void RestoreState(int loadedStage, float loadedTimer)
    {
        this.stage = loadedStage;
        this.timer = loadedTimer;

        if (sr == null) sr = GetComponent<SpriteRenderer>();
        UpdateSprite();
    }

    public void Regrow()
    {
        // Quay về giai đoạn regrowStage
        if (regrowStage < 0 || regrowStage >= cropStages.Count - 1)
        {
            stage = 0;
        }
        else
        {
            stage = regrowStage;
        }

        timer = 0f;
        UpdateSprite();
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