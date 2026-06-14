using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class InventoryController : MonoBehaviour
{
    public static InventoryController Instance { get; private set; }

    [Header("Config")]
    public int slotCount = 20;

    [SerializeField] private List<InventorySaveData> _inventoryData = new List<InventorySaveData>();
    private readonly Dictionary<int, int> _itemCountCache = new Dictionary<int, int>();

    public event Action<List<InventorySaveData>, int> OnInventoryChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        ReBuildItemCounts();
    }

    #region Public API

    public Dictionary<int, int> GetItemCounts() => _itemCountCache;

    public List<InventorySaveData> GetInventoryItemsData() => _inventoryData;

    public void SetInventoryItems(List<InventorySaveData> newData)
    {
        _inventoryData = newData ?? new List<InventorySaveData>();

        if (_inventoryData.Count > 0)
        {
            int maxSlot = _inventoryData.Where(x => x.slotIndex < 1000).Select(x => x.slotIndex).DefaultIfEmpty(-1).Max();
            if (maxSlot >= slotCount) slotCount = maxSlot + 1;
        }

        ReBuildItemCounts();
    }

    public bool AddItem(Item tempItem, uint validationSeed = 0)
    {
        if (tempItem == null) return false;

        int quantityLeft = tempItem.quantity;

        if (tempItem.IsStackable)
        {
            quantityLeft = TryStackItem(tempItem, quantityLeft);
            if (quantityLeft <= 0) return true;
        }

        return TryCreateNewItem(tempItem, quantityLeft, validationSeed);
    }

    public void RemoveItemsFromInventory(int itemID, int amountToRemove)
    {
        for (int i = _inventoryData.Count - 1; i >= 0; i--)
        {
            if (amountToRemove <= 0) break;

            var data = _inventoryData[i];
            if (data.itemID != itemID) continue;

            int removed = Mathf.Min(amountToRemove, data.quantity);
            data.quantity -= removed;
            amountToRemove -= removed;

            if (data.quantity <= 0)
            {
                InventoryService.Instance.RequestRemoveItem(data.dbID);
                _inventoryData.RemoveAt(i);
            }
        }

        ReBuildItemCounts();
    }

    #endregion

    #region Internal Logic

    private int TryStackItem(Item tempItem, int quantity)
    {
        foreach (var data in _inventoryData)
        {
            if (data.itemID != tempItem.ID) continue;
            if (data.dbID == 0) continue;

            int maxStack = 999;
            int canAdd = Mathf.Min(quantity, maxStack - data.quantity);

            if (canAdd <= 0) continue;

            data.quantity += canAdd;
            quantity -= canAdd;

            InventoryService.Instance.ScheduleQuantityUpdate(data.dbID, data.quantity);

            if (quantity <= 0)
            {
                ReBuildItemCounts();
                return 0;
            }
        }
        return quantity;
    }

    private bool TryCreateNewItem(Item tempItem, int quantity, uint validationSeed)
    {
        int emptySlotIndex = -1;
        var occupiedSlots = _inventoryData.Select(x => x.slotIndex).ToHashSet();

        for (int i = 0; i < slotCount; i++)
        {
            if (!occupiedSlots.Contains(i))
            {
                emptySlotIndex = i;
                break;
            }
        }

        if (emptySlotIndex == -1) return false;

        var newItemData = new InventorySaveData
        {
            dbID = 0,
            itemID = tempItem.ID,
            quantity = tempItem.ItemType == ItemType.Equipment ? 1 : quantity,
            slotIndex = emptySlotIndex,
            isEquipped = false,
            rarity = tempItem.rarity,
            qualityFactor = tempItem.qualityFactor
        };

        _inventoryData.Add(newItemData);
        ReBuildItemCounts();

        InventoryService.Instance.RequestAddItem(
            newItemData.itemID,
            newItemData.quantity,
            newItemData.slotIndex,
            (int)newItemData.rarity,
            newItemData.qualityFactor,
            validationSeed,
            tempItem.IsStackable,
            (dbId, action) =>
            {
                if (action == "stacked")
                {
                    _inventoryData.Remove(newItemData);

                    var existingInRAM = _inventoryData.Find(x => x.dbID == dbId);
                    if (existingInRAM != null)
                    {
                        existingInRAM.quantity += quantity;
                    }
                    else
                    {
                        RefreshInventory();
                        return;
                    }
                }
                else
                {
                    newItemData.dbID = dbId;
                }

                ReBuildItemCounts();
            }
        );

        return true;
    }

    public void PredictAddHarvestItem(Item itemPrefab, int quantity)
    {
        if (itemPrefab == null) return;

        var existingData = _inventoryData.FirstOrDefault(x => x.itemID == itemPrefab.ID && x.slotIndex < 2000);

        if (existingData != null)
        {
            existingData.quantity += quantity;
        }
        else
        {
            int emptySlotIndex = -1;
            var occupiedSlots = _inventoryData.Select(x => x.slotIndex).ToHashSet();

            for (int i = 0; i < slotCount; i++)
            {
                if (!occupiedSlots.Contains(i))
                {
                    emptySlotIndex = i;
                    break;
                }
            }

            if (emptySlotIndex != -1)
            {
                _inventoryData.Add(new InventorySaveData
                {
                    dbID = -1,
                    itemID = itemPrefab.ID,
                    quantity = quantity,
                    slotIndex = emptySlotIndex,
                    isEquipped = false,
                    rarity = itemPrefab.rarity,
                    qualityFactor = itemPrefab.qualityFactor
                });
            }
        }

        ReBuildItemCounts();
    }

    public void RefreshInventory()
    {
        InventoryService.Instance.SyncInventory((serverItems) =>
        {
            if (serverItems == null) return;

            List<InventorySaveData> cleanData = new List<InventorySaveData>();
            List<InventorySaveData> hotBarData = new List<InventorySaveData>();

            foreach (var sItem in serverItems)
            {
                var data = new InventorySaveData
                {
                    dbID = sItem.id,
                    itemID = sItem.itemId,
                    quantity = sItem.quantity,
                    slotIndex = sItem.slotIndex,
                    isEquipped = sItem.slotIndex >= 2000,
                    rarity = (ItemRarity)sItem.rarity,
                    qualityFactor = sItem.qualityFactor
                };

                if (sItem.slotIndex >= 2000) continue;

                cleanData.Add(data);
            }

            SetInventoryItems(cleanData);

            Debug.Log($"[Inventory] Đã làm mới: {cleanData.Count} item trong túi.");
        });
    }

    public void ReBuildItemCounts()
    {
        _itemCountCache.Clear();

        foreach (var data in _inventoryData)
        {
            _itemCountCache[data.itemID] = _itemCountCache.GetValueOrDefault(data.itemID, 0) + data.quantity;
        }

        OnInventoryChanged?.Invoke(_inventoryData, slotCount);
    }

    #endregion
}