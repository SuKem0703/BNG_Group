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
    }

    public void RegisterPlot(FarmPlot plot)
    {
        if (plot != null && !string.IsNullOrEmpty(plot.UniqueID))
        {
            scenePlots[plot.UniqueID] = plot;
        }
    }

    public void UnregisterPlot(FarmPlot plot)
    {
        if (plot != null && !string.IsNullOrEmpty(plot.UniqueID))
        {
            if (scenePlots.ContainsKey(plot.UniqueID))
                scenePlots.Remove(plot.UniqueID);
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

        GameObject seedPrefab = ItemDictionary.Instance.GetItemPrefab(seedItemId);
        if (seedPrefab == null) return;

        SeedItem seedScript = seedPrefab.GetComponent<SeedItem>();
        GameObject cropObj = Instantiate(seedScript.cropPrefab, plot.transform);

        cropObj.transform.localPosition = new Vector3(-0.5f, -0.5f, 0f);

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

        obj.transform.localPosition = new Vector3(-0.5f, -0.5f, 0f);

        Crop crop = obj.GetComponent<Crop>();
        crop.seedItemID = seed.ID;
        crop.plot = plot;

        crop.InitializeGrowth(ServerTimeManager.GetCurrentTime());

        plot.currentCrop = crop;
        plot.isPlanted = true;

        SoundEffectManager.Play("Seeding", true);

        seed.RemoveFromStack(1);

        if (InventoryController.Instance != null)
        {
            var ramData = InventoryController.Instance.GetInventoryItemsData().Find(x => x.dbID == seed.dbID);
            if (ramData != null) ramData.quantity = seed.quantity;

            if (seed.quantity <= 0)
            {
                InventoryController.Instance.GetInventoryItemsData().RemoveAll(x => x.dbID == seed.dbID);
                InventoryService.Instance.CancelQuantityUpdate(seed.dbID);
                InventoryService.Instance.RequestRemoveItem(seed.dbID);
                Destroy(seed.gameObject);
            }
            else
            {
                InventoryService.Instance.ScheduleQuantityUpdate(seed.dbID, seed.quantity);
            }
            InventoryController.Instance.ReBuildItemCounts();
        }

        if (QuestController.Instance != null) QuestController.Instance.MarkCropPlanted(seed.ID);

        FarmService.Instance.RequestPlant(plot.UniqueID, seed.ID);
    }

    public void TryHarvest(FarmPlot plot)
    {
        if (!plot.isPlanted || plot.currentCrop == null || !plot.currentCrop.IsReady()) return;

        Crop crop = plot.currentCrop;
        GameObject itemPrefab = crop.harvestItemPrefab;

        SoundEffectManager.Play("Harvesting", true);

        for (int i = 0; i < crop.harvestAmount; i++)
        {
            Vector3 randomOffset = new Vector3(UnityEngine.Random.Range(-0.2f, 0.2f), UnityEngine.Random.Range(0f, 0.3f), 0);
            GameObject vfxObj = Instantiate(itemPrefab, plot.transform.position + randomOffset, Quaternion.identity);
            if (vfxObj.TryGetComponent(out Item item)) item.ShowPopUp();
            Destroy(vfxObj);
        }

        Item harvestItemData = itemPrefab.GetComponent<Item>();
        if (harvestItemData != null && InventoryController.Instance != null)
        {
            InventoryController.Instance.PredictAddHarvestItem(harvestItemData, crop.harvestAmount);
        }

        if (crop.isRegrowable)
        {
            crop.Regrow();
        }
        else
        {
            Destroy(crop.gameObject);
            plot.currentCrop = null;
            plot.isPlanted = false;
        }

        FarmService.Instance.RequestHarvest(plot.UniqueID);
    }

    public void TryDestroyCrop(FarmPlot plot)
    {
        if (!plot.isPlanted || plot.currentCrop == null) return;

        Destroy(plot.currentCrop.gameObject);

        plot.currentCrop = null;
        plot.isPlanted = false;

        // SoundEffectManager.Play("Destroying", true);

        FarmService.Instance.RequestDestroy(plot.UniqueID);
    }
}