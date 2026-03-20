using System.Collections.Generic;
using UnityEngine;

public class StorageChest : MonoBehaviour, IInteractable
{
    [Header("Settings")]
    public string chestID;

    private List<InventoryService.StorageItemDTO> cachedItems = new List<InventoryService.StorageItemDTO>();

    void Awake()
    {
        if (string.IsNullOrEmpty(chestID))
        {
            chestID = GlobalHelper.GenerateUniqueID(gameObject);
        }
    }
    public void ClearCache()
    {
        cachedItems.Clear();
    }

    public void AddToCache(InventoryService.StorageItemDTO item)
    {
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
        //return GameFlags.HasInteractedWithStorageChest();
    }
}