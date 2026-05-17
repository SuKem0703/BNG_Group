using UnityEngine;

public class SeedItem : Item
{
    public override ItemType ItemType => ItemType.Seed;
    public override bool IsStackable => true;

    public GameObject cropPrefab;

    [Tooltip("Kích thước vùng trồng")]
    public Vector2Int cropSize = new Vector2Int(1, 1);

    public override void UseItem()
    {
        Debug.Log("Đang gieo hạt: " + Name);
    }

    public bool TryPlantSeed(FarmPlot plot)
    {
        if (InteractionDetector.Instance != null && InteractionDetector.Instance.IsPlotInRange(plot))
        {
            FarmController.Instance.TryPlantSeed(plot, this);

            var ramData = InventoryController.Instance.GetInventoryItemsData().Find(x => x.dbID == dbID);
            if (ramData != null) ramData.quantity = quantity;

            if (InventoryService.Instance != null)
            {
                InventoryService.Instance.ScheduleQuantityUpdate(dbID, quantity);
            }

            return true;
        }
        return false;
    }
}