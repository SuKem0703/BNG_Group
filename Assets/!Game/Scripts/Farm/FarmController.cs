using System;
using System.Collections.Generic;
using UnityEngine;

public class FarmController : MonoBehaviour
{
    public static FarmController Instance;

    // Thêm Dictionary để quản lý nhanh Plot trong Scene
    private Dictionary<string, FarmPlot> scenePlots = new Dictionary<string, FarmPlot>();

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        Instance = this;

        // Lưu trước toàn bộ ô đất có trong Map vào Dictionary
        FarmPlot[] allPlots = FindObjectsByType<FarmPlot>(FindObjectsSortMode.None);
        foreach (var plot in allPlots)
        {
            scenePlots[plot.UniqueID] = plot;
        }
    }

    // ĐƯỢC GỌI SAU KHI LOAD SCENE (Từ SaveController hoặc tự gọi)
    public void FetchFarmDataFromServer()
    {
        if (FarmService.Instance == null) return;

        FarmService.Instance.SyncFarm((serverPlots) =>
        {
            foreach (var sp in serverPlots)
            {
                if (scenePlots.TryGetValue(sp.plotId, out FarmPlot plot))
                {
                    RespawnCropFromServer(plot, sp.seedItemId, sp.plantedAt);
                }
            }
        });
    }

    private void RespawnCropFromServer(FarmPlot plot, int seedItemId, string plantedAtString)
    {
        if (plot.currentCrop != null) Destroy(plot.currentCrop.gameObject);

        GameObject seedPrefab = InventoryController.Instance.itemDictionary.GetItemPrefab(seedItemId);
        if (seedPrefab == null) return;

        SeedItem seedScript = seedPrefab.GetComponent<SeedItem>();
        GameObject cropObj = Instantiate(seedScript.cropPrefab, plot.transform);
        cropObj.transform.localPosition = Vector3.zero;

        Crop crop = cropObj.GetComponent<Crop>();
        crop.seedItemID = seedItemId;
        crop.plot = plot;

        // Chuyển chuỗi thời gian ISO từ DB thành DateTime
        if (DateTime.TryParse(plantedAtString, out DateTime plantedAt))
        {
            // Quan Trọng: Chuyển về Local Time hoặc để nguyên UTC tùy thuộc ServerTimeManager
            crop.InitializeGrowth(plantedAt.ToLocalTime());
        }
        else
        {
            crop.InitializeGrowth(ServerTimeManager.GetCurrentTime());
        }

        plot.currentCrop = crop;
        plot.isPlanted = true;
    }

    public void TryPlantSeed(FarmPlot plot, SeedItem seed)
    {
        if (plot.isPlanted) return;

        GameObject obj = Instantiate(seed.cropPrefab, plot.transform);
        obj.transform.localPosition = Vector3.zero;

        Crop crop = obj.GetComponent<Crop>();
        crop.seedItemID = seed.ID;
        crop.plot = plot;

        // Bắt đầu tính giờ ngay lập tức trên máy Client
        crop.InitializeGrowth(ServerTimeManager.GetCurrentTime());

        plot.currentCrop = crop;
        plot.isPlanted = true;

        SoundEffectManager.Play("Seeding", true);
        seed.RemoveFromStack(1);

        if (QuestController.Instance != null) QuestController.Instance.MarkCropPlanted(seed.ID);

        // GỌI API LƯU LÊN SERVER THAY VÌ LƯU VÀO JSON
        FarmService.Instance.RequestPlant(plot.UniqueID, seed.ID);
    }

    public void TryHarvest(FarmPlot plot)
    {
        if (!plot.isPlanted || plot.currentCrop == null || !plot.currentCrop.IsReady()) return;

        Crop crop = plot.currentCrop;
        GameObject itemPrefab = crop.harvestItemPrefab;

        bool isFull = false;
        int collectedCount = 0;
        for (int i = 0; i < crop.harvestAmount; i++)
        {
            bool added = InventoryController.Instance.AddItem(itemPrefab);
            if (added)
            {
                collectedCount++;
                Vector3 randomOffset = new Vector3(UnityEngine.Random.Range(-0.2f, 0.2f), UnityEngine.Random.Range(0f, 0.3f), 0);
                GameObject tempObj = Instantiate(itemPrefab, plot.transform.position + randomOffset, Quaternion.identity);
                if (tempObj.TryGetComponent(out Item item)) item.ShowPopUp();
                Destroy(tempObj);
            }
            else { isFull = true; break; }
        }

        if (collectedCount == 0) return;

        SoundEffectManager.Play("Harvesting", true);

        float offsetSeconds = 0f;
        if (crop.isRegrowable)
        {
            offsetSeconds = crop.GetRegrowOffsetSeconds();
            crop.Regrow();
        }
        else
        {
            Destroy(crop.gameObject);
            plot.currentCrop = null;
            plot.isPlanted = false;
        }

        // GỌI API BÁO SERVER THU HOẠCH XONG
        FarmService.Instance.RequestHarvest(plot.UniqueID, crop.isRegrowable, offsetSeconds);
    }

    /*

    // ===== SAVE & LOAD =====
    // --- SAVE ---
    public FarmData GetFarmDataToSave()
    {
        FarmData data = new FarmData();
        FarmPlot[] allPlots = FindObjectsByType<FarmPlot>(FindObjectsSortMode.None);

        foreach (var plot in allPlots)
        {
            FarmPlotSaveData plotData = new FarmPlotSaveData();

            plotData.plotID = plot.PlotID;

            plotData.hasCrop = plot.isPlanted && plot.currentCrop != null;

            if (plotData.hasCrop)
            {
                Crop crop = plot.currentCrop;
                plotData.cropData = new CropSaveData
                {
                    seedItemID = crop.seedItemID,
                    currentStage = crop.stage,
                    currentTimer = crop.timer
                };
            }
            data.plotDataList.Add(plotData);
        }
        return data;
    }

    // --- LOAD ---
    public void LoadFarmData(FarmData data)
    {
        if (data == null || data.plotDataList == null) return;

        Dictionary<string, FarmPlot> scenePlots = new Dictionary<string, FarmPlot>();
        FarmPlot[] allPlots = FindObjectsByType<FarmPlot>(FindObjectsSortMode.None);

        foreach (var plot in allPlots)
        {
            if (!scenePlots.ContainsKey(plot.PlotID))
            {
                scenePlots.Add(plot.PlotID, plot);
            }

            // Dọn dẹp cây cũ
            if (plot.currentCrop != null)
            {
                Destroy(plot.currentCrop.gameObject);
                plot.currentCrop = null;
                plot.isPlanted = false;
            }
        }

        // Khôi phục dữ liệu
        foreach (var plotData in data.plotDataList)
        {
            // Tìm ô đất theo ID đã lưu
            if (scenePlots.TryGetValue(plotData.plotID, out FarmPlot plot))
            {
                if (plotData.hasCrop && plotData.cropData != null)
                {
                    RespawnCrop(plot, plotData.cropData);
                }
            }
        }
    }
    private void RespawnCrop(FarmPlot plot, CropSaveData data)
    {
        GameObject seedPrefab = InventoryController.Instance.itemDictionary.GetItemPrefab(data.seedItemID);
        if (seedPrefab == null) return;

        SeedItem seedScript = seedPrefab.GetComponent<SeedItem>();
        GameObject cropObj = Instantiate(seedScript.cropPrefab, plot.transform);
        cropObj.transform.localPosition = Vector3.zero;

        Crop crop = cropObj.GetComponent<Crop>();
        crop.seedItemID = data.seedItemID;
        crop.plot = plot;
        crop.RestoreState(data.currentStage, data.currentTimer);

        plot.currentCrop = crop;
        plot.isPlanted = true;
    }

    */
}