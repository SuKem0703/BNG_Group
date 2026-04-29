using System;
using System.Collections.Generic;
using UnityEngine;

public class FarmController : MonoBehaviour
{
    public static FarmController Instance;

    private Dictionary<string, FarmPlot> scenePlots = new Dictionary<string, FarmPlot>();

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        Instance = this;

        FarmPlot[] allPlots = FindObjectsByType<FarmPlot>(FindObjectsSortMode.None);
        foreach (var plot in allPlots)
        {
            scenePlots[plot.UniqueID] = plot;
        }
    }

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

        if (DateTime.TryParse(plantedAtString, out DateTime plantedAt))
        {
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

        crop.InitializeGrowth(ServerTimeManager.GetCurrentTime());

        plot.currentCrop = crop;
        plot.isPlanted = true;

        SoundEffectManager.Play("Seeding", true);
        seed.RemoveFromStack(1);

        if (QuestController.Instance != null) QuestController.Instance.MarkCropPlanted(seed.ID);

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
            bool added = InventoryController.Instance.AddItem(itemPrefab.GetComponent<Item>());
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

        FarmService.Instance.RequestHarvest(plot.UniqueID, crop.isRegrowable, offsetSeconds);
    }
}