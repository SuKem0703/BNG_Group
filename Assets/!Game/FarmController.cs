using UnityEngine;

public class FarmController : MonoBehaviour
{
    public static FarmController Instance;
    SaveController saveController;
    private void Awake()
    {
        Instance = this;

        saveController = FindFirstObjectByType<SaveController>();
    }

    public void TryPlantSeed(FarmPlot plot, SeedItem seed)
    {
        if (plot.isPlanted)
        {
            Debug.Log("Ô đất đã trồng rồi");
            return;
        }

        GameObject obj = Instantiate(seed.cropPrefab, plot.transform);
        obj.transform.localPosition = new Vector3(-0.5f, -0.5f, 0f);

        Crop crop = obj.GetComponent<Crop>();

        crop.plot = plot;
        plot.currentCrop = crop;
        plot.isPlanted = true;

        SoundEffectManager.Play("Seeding", true);

        seed.RemoveFromStack(1);
    }

    public void TryHarvest(FarmPlot plot)
    {
        if (!plot.isPlanted) return;
        if (plot.currentCrop == null) return;
        if (!plot.currentCrop.IsReady()) return;

        Crop crop = plot.currentCrop;

        // 1. LẤY PREFAB TỪ ID
        GameObject itemPrefab = InventoryController.Instance
            .itemDictionary
            .GetItemPrefab(crop.harvestItemID);

        if (itemPrefab == null)
        {
            Debug.LogError($"Không tìm thấy prefab item ID {crop.harvestItemID}");
        }
        else
        {
            // 2. ADD ITEM NHIỀU LẦN (VÌ AddItem chỉ add 1 prefab)
            for (int i = 0; i < crop.harvestAmount; i++)
            {
                InventoryController.Instance.AddItem(itemPrefab);
            }
        }

        SoundEffectManager.Play("Harvesting", true);
        saveController.SaveGame();

        // 3. DỌN DẸP CROP
        Destroy(crop.gameObject);
        plot.currentCrop = null;
        plot.isPlanted = false;

        Debug.Log($"Thu hoạch xong! => +{crop.harvestAmount} item ID {crop.harvestItemID}");
    }

}
