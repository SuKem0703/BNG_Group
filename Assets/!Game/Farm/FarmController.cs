using System.Collections.Generic;
using UnityEngine;

public class FarmController : MonoBehaviour
{
    public static FarmController Instance;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void TryPlantSeed(FarmPlot plot, SeedItem seed)
    {
        if (plot.isPlanted)
        {
            Debug.Log("Ô đất đã trồng rồi");
            return;
        }

        GameObject obj = Instantiate(seed.cropPrefab, plot.transform);
        obj.transform.localPosition = Vector3.zero;

        Crop crop = obj.GetComponent<Crop>();
        crop.seedItemID = seed.ID;

        crop.plot = plot;
        plot.currentCrop = crop;
        plot.isPlanted = true;

        SoundEffectManager.Play("Seeding", true);

        seed.RemoveFromStack(1);

        SaveController.Instance.SaveGame();
    }

    public void TryHarvest(FarmPlot plot)
    {
        if (!plot.isPlanted) return;
        if (plot.currentCrop == null) return;
        if (!plot.currentCrop.IsReady()) return;

        Crop crop = plot.currentCrop;

        GameObject itemPrefab = crop.harvestItemPrefab;

        if (itemPrefab == null)
        {
            Debug.LogError($"Crop {crop.name} bị thiếu harvestItemPrefab!");
        }
        else
        {
            for (int i = 0; i < crop.harvestAmount; i++)
            {
                bool added = InventoryController.Instance.AddItem(itemPrefab);

                if (added)
                {
                    Vector3 randomOffset = new Vector3(Random.Range(-0.2f, 0.2f), Random.Range(0f, 0.3f), 0);
                    GameObject tempObj = Instantiate(itemPrefab, plot.transform.position + randomOffset, Quaternion.identity);

                    if (tempObj.TryGetComponent(out Item item))
                    {
                        item.ShowPopUp();
                    }
                    Destroy(tempObj);
                }
            }
        }

        SoundEffectManager.Play("Harvesting", true);

        // DỌN DẸP CROP
        Destroy(crop.gameObject);
        plot.currentCrop = null;
        plot.isPlanted = false;

        SaveController.Instance.SaveGame();
    }

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
            // [UPDATE] Dùng PlotID có sẵn để map
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
        // 1. Tìm Prefab hạt giống dựa vào ID đã lưu
        if (InventoryController.Instance == null) return;

        GameObject seedPrefab = InventoryController.Instance.itemDictionary.GetItemPrefab(data.seedItemID);
        if (seedPrefab == null) return;

        SeedItem seedScript = seedPrefab.GetComponent<SeedItem>();
        if (seedScript == null || seedScript.cropPrefab == null) return;

        // 2. Instantiate Cây
        GameObject cropObj = Instantiate(seedScript.cropPrefab, plot.transform);
        cropObj.transform.localPosition = Vector3.zero;

        Crop crop = cropObj.GetComponent<Crop>();

        // 3. Restore dữ liệu
        crop.seedItemID = data.seedItemID;
        crop.plot = plot;
        crop.RestoreState(data.currentStage, data.currentTimer);

        plot.currentCrop = crop;
        plot.isPlanted = true;
    }
}
