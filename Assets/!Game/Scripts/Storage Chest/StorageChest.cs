using System.Collections.Generic;
using UnityEngine;

public class StorageChest : AutoIDBehaviour, IInteractable
{
    private List<InventoryService.StorageItemDTO> cachedItems = null;

    public void InitChunkData(string customID)
    {
        UniqueID = customID;
    }

    public void ClearCache()
    {
        if (cachedItems != null) cachedItems.Clear();
    }

    public void AddToCache(InventoryService.StorageItemDTO item)
    {
        if (cachedItems == null) cachedItems = new List<InventoryService.StorageItemDTO>();
        cachedItems.Add(item);
    }

    public void SetCache(List<InventoryService.StorageItemDTO> items)
    {
        cachedItems = items;
    }

    public List<InventoryService.StorageItemDTO> GetItems() => cachedItems;

    public void Interact()
    {
        StorageChestController.Instance.OpenChest(this, cachedItems);
    }

    public bool CanInteract()
    {
        if (!GameStateManager.CanProcessInput()) return false;
        return true;
    }
}