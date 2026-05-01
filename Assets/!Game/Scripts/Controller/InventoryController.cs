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

    private List<InventorySaveData> _inventoryData = new List<InventorySaveData>();
    private readonly Dictionary<int, int> _itemCountCache = new Dictionary<int, int>();

    public event Action<List<InventorySaveData>, int> OnInventoryChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
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
            int maxSlot = _inventoryData.Max(x => x.slotIndex);
            if (maxSlot >= slotCount) slotCount = maxSlot + 1;
        }

        ReBuildItemCounts();
    }

    public bool AddItem(Item tempItem)
    {
        if (tempItem == null) return false;

        int quantityLeft = tempItem.quantity;

        if (tempItem.IsStackable)
        {
            quantityLeft = TryStackItem(tempItem, quantityLeft);
            if (quantityLeft <= 0) return true;
        }

        return TryCreateNewItem(tempItem, quantityLeft);
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

            int maxStack = 999;
            int canAdd = Mathf.Min(quantity, maxStack - data.quantity);

            if (canAdd <= 0) continue;

            data.quantity += canAdd;
            quantity -= canAdd;

            InventoryService.Instance.RequestUpdateQuantity(data.dbID, data.quantity);

            if (quantity <= 0)
            {
                ReBuildItemCounts();
                return 0;
            }
        }
        return quantity;
    }

    private bool TryCreateNewItem(Item tempItem, int quantity)
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
            dbId =>
            {
                newItemData.dbID = dbId;
            }
        );

        return true;
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
                    isEquipped = sItem.isEquipped,
                    rarity = (ItemRarity)sItem.rarity,
                    qualityFactor = sItem.qualityFactor
                };

                if (sItem.slotIndex >= 1000)
                {
                    data.slotIndex -= 1000;
                    hotBarData.Add(data);
                }
                else
                {
                    cleanData.Add(data);
                }
            }

            SetInventoryItems(cleanData);

            if (HotbarController.Instance != null)
                HotbarController.Instance.SetHotbarItems(hotBarData);

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

    #region Sync Queue (Debounce Logic)
    private Dictionary<int, Coroutine> _consumableSyncCoroutines = new Dictionary<int, Coroutine>();
    private Dictionary<int, int> _pendingQuantities = new Dictionary<int, int>();

    public void ScheduleConsumableSync(int itemDbId, int currentQuantity)
    {
        if (itemDbId == 0) return;

        if (_pendingQuantities.ContainsKey(itemDbId))
            _pendingQuantities[itemDbId] = currentQuantity;
        else
            _pendingQuantities.Add(itemDbId, currentQuantity);

        if (_consumableSyncCoroutines.ContainsKey(itemDbId))
        {
            if (_consumableSyncCoroutines[itemDbId] != null)
                StopCoroutine(_consumableSyncCoroutines[itemDbId]);

            _consumableSyncCoroutines.Remove(itemDbId);
        }

        Coroutine newCoroutine = StartCoroutine(SyncConsumableDelay(itemDbId, 3));
        _consumableSyncCoroutines.Add(itemDbId, newCoroutine);
    }

    private IEnumerator SyncConsumableDelay(int itemDbId, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (_pendingQuantities.ContainsKey(itemDbId))
        {
            int finalQty = _pendingQuantities[itemDbId];
            InventoryService.Instance.RequestUpdateQuantity(itemDbId, finalQty);
            _pendingQuantities.Remove(itemDbId);
        }

        _consumableSyncCoroutines.Remove(itemDbId);
    }
    #endregion
}