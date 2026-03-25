using System;
using System.Collections.Generic;
using UnityEngine;

public class Crop : MonoBehaviour, IInteractable, ITargetableInfo
{
    [System.Serializable]
    public struct StageData
    {
        public Sprite sprite;
        public float timeToNextStage;
    }

    public int seedItemID;
    public List<StageData> cropStages;

    [Header("Harvest Settings")]
    public GameObject harvestItemPrefab;
    public int harvestAmount = 1;

    [Header("Regrow Settings")]
    public bool isRegrowable = false;
    public int regrowStage = 0;

    [HideInInspector] public FarmPlot plot;

    public DateTime PlantedAt { get; private set; }
    public int stage = 0;

    private Item _cachedHarvestItemData;
    private SpriteRenderer sr;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (harvestItemPrefab != null) _cachedHarvestItemData = harvestItemPrefab.GetComponent<Item>();
    }

    private void Update()
    {
        if (PlantedAt != default && stage < cropStages.Count - 1)
        {
            CalculateCurrentStage();
        }
    }

    public void InitializeGrowth(DateTime plantedAt)
    {
        this.PlantedAt = plantedAt;
        CalculateCurrentStage();
    }

    private void CalculateCurrentStage()
    {
        if (cropStages == null || cropStages.Count == 0) return;

        double totalSecondsElapsed = (ServerTimeManager.GetCurrentTime() - PlantedAt).TotalSeconds;

        double accumulatedTime = 0;
        int newStage = 0;

        for (int i = 0; i < cropStages.Count - 1; i++)
        {
            accumulatedTime += cropStages[i].timeToNextStage;
            if (totalSecondsElapsed >= accumulatedTime)
            {
                newStage = i + 1;
            }
            else
            {
                break;
            }
        }

        // Cập nhật lại nếu stage thay đổi HOẶC ảnh hiện tại chưa khớp với cấu hình
        if (newStage != stage || sr.sprite != cropStages[newStage].sprite)
        {
            stage = newStage;
            UpdateSprite();
        }
    }

    private void UpdateSprite()
    {
        if (sr != null && stage < cropStages.Count)
            sr.sprite = cropStages[stage].sprite;
    }

    public bool IsReady() => stage >= cropStages.Count - 1;

    public float GetRegrowOffsetSeconds()
    {
        float offset = 0f;
        for (int i = 0; i < regrowStage; i++)
        {
            offset += cropStages[i].timeToNextStage;
        }
        return offset;
    }

    public void Regrow()
    {
        float offset = GetRegrowOffsetSeconds();
        PlantedAt = ServerTimeManager.GetCurrentTime().AddSeconds(-offset);
        CalculateCurrentStage();
    }

    public void Interact()
    {
        if (!IsReady()) return;
        FarmController.Instance.TryHarvest(plot);
    }

    public bool CanInteract() => IsReady();

    public TargetInfoData GetInfo()
    {
        if (_cachedHarvestItemData == null) return new TargetInfoData("Lỗi", null, "...", TargetType.Item);
        if (IsReady()) return new TargetInfoData(_cachedHarvestItemData.Name, _cachedHarvestItemData.icon, "Thu hoạch", TargetType.Item, _cachedHarvestItemData.rarity);

        return new TargetInfoData(_cachedHarvestItemData.Name, _cachedHarvestItemData.icon, "Đang lớn...", TargetType.Item);
    }
}