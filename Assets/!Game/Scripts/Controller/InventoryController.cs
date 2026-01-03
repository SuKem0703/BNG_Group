using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class InventoryController : MonoBehaviour
{
    #region Singleton
    public static InventoryController Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (inventoryPanel == null)
            inventoryPanel = GameObject.Find("InventoryPage");
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
    #endregion

    #region References & Config
    public ItemDictionary itemDictionary;
    public GameObject inventoryPanel;
    public GameObject slotPrefab;
    public int slotCount = 20;
    #endregion

    #region Cache & Events
    private readonly Dictionary<int, int> itemCountCache = new();
    public event Action OnInventoryChanged;
    #endregion

    #region Unity Lifecycle

    private void Start()
    {
        ResolveInventoryPanel();
        EnsureSlotCount(slotCount);

        if (itemDictionary == null)
            itemDictionary = FindFirstObjectByType<ItemDictionary>();

        ReBuildItemCounts();
    }

    #endregion

    #region Initialization

    private void ResolveInventoryPanel()
    {
        if (inventoryPanel != null) return;

        inventoryPanel = Resources.FindObjectsOfTypeAll<GameObject>()
            .FirstOrDefault(x => x.name == "InventoryPage");

        if (inventoryPanel == null)
            Debug.LogError("Inventory panel not found!");
    }

    private void EnsureSlotCount(int neededCount)
    {
        int currentCount = inventoryPanel.transform.childCount;
        if (currentCount < neededCount)
        {
            for (int i = 0; i < (neededCount - currentCount); i++)
            {
                Instantiate(slotPrefab, inventoryPanel.transform);
            }
        }
    }

    #endregion

    #region Inventory Public API

    public Dictionary<int, int> GetItemCounts() => itemCountCache;

    public bool AddItem(GameObject itemPrefab)
    {
        if (itemPrefab == null) return false;

        Item tempItem = itemPrefab.GetComponent<Item>();
        if (tempItem == null) return false;

        int quantityLeft = tempItem.quantity;

        // 1. STACK LOGIC (non-equipment)
        if (tempItem.IsStackable)
        {
            quantityLeft = TryStackItem(tempItem, quantityLeft);
            if (quantityLeft <= 0) return true;
        }

        // 2. CREATE NEW ITEM
        return TryCreateNewItem(itemPrefab, tempItem, quantityLeft);
    }

    public void RemoveItemsFromInventory(int itemID, int amountToRemove)
    {
        foreach (Transform slotTransform in inventoryPanel.transform)
        {
            if (amountToRemove <= 0) break;

            Slot slot = slotTransform.GetComponent<Slot>();
            if (slot == null || slot.currentItem == null) continue;

            Item item = slot.currentItem.GetComponent<Item>();
            if (item == null || item.ID != itemID) continue;

            int removed = Mathf.Min(amountToRemove, item.quantity);
            item.RemoveFromStack(removed);
            amountToRemove -= removed;

            if (item.quantity <= 0)
            {
                Destroy(slot.currentItem);
                slot.currentItem = null;
            }
        }

        ReBuildItemCounts();
    }

    public List<InventorySaveData> GetInventoryItems()
    {
        List<InventorySaveData> data = new();

        foreach (Transform slotTransform in inventoryPanel.transform)
        {
            Slot slot = slotTransform.GetComponent<Slot>();
            if (slot?.currentItem == null) continue;

            Item item = slot.currentItem.GetComponent<Item>();
            if (item == null) continue;

            data.Add(new InventorySaveData
            {
                dbID = item.dbID,
                itemID = item.ID,
                slotIndex = slotTransform.GetSiblingIndex(),
                quantity = item.quantity,
                isEquipped = item.isEquipped,
                rarity = item.rarity,
                qualityFactor = item.qualityFactor
            });
        }

        return data;
    }

    public void SetInventoryItems(List<InventorySaveData> inventorySaveData)
    {
        int maxSlotIndexFromData = 0;
        if (inventorySaveData != null && inventorySaveData.Count > 0)
        {
            maxSlotIndexFromData = inventorySaveData.Max(x => x.slotIndex);
        }

        int requiredSlots = Mathf.Max(slotCount, maxSlotIndexFromData + 1);

        EnsureSlotCount(requiredSlots);

        foreach (Transform slotTrans in inventoryPanel.transform)
        {
            Slot s = slotTrans.GetComponent<Slot>();
            if (s != null && s.currentItem != null)
            {
                Destroy(s.currentItem);
                s.currentItem = null;
            }
        }

        if (inventorySaveData == null || inventorySaveData.Count == 0)
        {
            ReBuildItemCounts();
            return;
        }

        foreach (InventorySaveData data in inventorySaveData)
        {
            if (data.slotIndex >= inventoryPanel.transform.childCount) continue;

            Slot slot = inventoryPanel.transform.GetChild(data.slotIndex).GetComponent<Slot>();
            GameObject prefab = itemDictionary.GetItemPrefab(data.itemID);

            if (prefab == null) continue;

            GameObject itemObj = Instantiate(prefab, slot.transform);
            itemObj.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

            Item item = itemObj.GetComponent<Item>();
            if (item == null) continue;

            item.dbID = data.dbID;
            item.quantity = Mathf.Max(1, data.quantity);
            item.rarity = data.rarity;
            item.qualityFactor = data.qualityFactor;
            item.isEquipped = data.isEquipped;

            item.UpdateQuantityDisplay();
            slot.currentItem = itemObj;
        }

        ReBuildItemCounts();
    }

    #endregion

    #region Internal Logic

    private int TryStackItem(Item tempItem, int quantity)
    {
        foreach (Transform slotTransform in inventoryPanel.transform)
        {
            Slot slot = slotTransform.GetComponent<Slot>();
            if (slot?.currentItem == null) continue;

            Item slotItem = slot.currentItem.GetComponent<Item>();
            if (slotItem == null || slotItem.ID != tempItem.ID) continue;

            int maxStack = 999;
            int canAdd = Mathf.Min(quantity, maxStack - slotItem.quantity);

            if (canAdd <= 0) continue;

            slotItem.AddToStack(canAdd);
            quantity -= canAdd;

            InventoryService.Instance.RequestUpdateQuantity(
                slotItem.dbID,
                slotItem.quantity
            );

            if (quantity <= 0)
            {
                ReBuildItemCounts();
                return 0;
            }
        }

        return quantity;
    }

    private bool TryCreateNewItem(GameObject prefab, Item tempItem, int quantity)
    {
        foreach (Transform slotTransform in inventoryPanel.transform)
        {
            Slot slot = slotTransform.GetComponent<Slot>();
            if (slot == null || slot.currentItem != null) continue;

            GameObject itemObj = Instantiate(prefab, slot.transform);
            itemObj.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

            CleanupWorldComponents(itemObj);

            Item newItem = itemObj.GetComponent<Item>();
            newItem.dbID = 0;
            newItem.quantity = tempItem.itemType == ItemType.Equipment ? 1 : quantity;
            newItem.rarity = tempItem.rarity;
            newItem.qualityFactor = tempItem.qualityFactor;

            newItem.UpdateQuantityDisplay();
            slot.currentItem = itemObj;

            ReBuildItemCounts();

            int slotIndex = slotTransform.GetSiblingIndex();
            InventoryService.Instance.RequestAddItem(
                newItem.ID,
                newItem.quantity,
                slotIndex,
                (int)newItem.rarity,
                newItem.qualityFactor,
                id => newItem.dbID = id
            );

            return true;
        }

        return false;
    }

    private void CleanupWorldComponents(GameObject item)
    {
        if (item.GetComponent<Collectible>()) Destroy(item.GetComponent<Collectible>());
        if (item.GetComponent<Monologue>()) Destroy(item.GetComponent<Monologue>());
    }

    public void ReBuildItemCounts()
    {
        itemCountCache.Clear();

        foreach (Transform slotTransform in inventoryPanel.transform)
        {
            Slot slot = slotTransform.GetComponent<Slot>();
            if (slot?.currentItem == null) continue;

            Item item = slot.currentItem.GetComponent<Item>();
            if (item == null) continue;

            itemCountCache[item.ID] =
                itemCountCache.GetValueOrDefault(item.ID, 0) + item.quantity;
        }

        OnInventoryChanged?.Invoke();
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

            Debug.Log($"[Sync] Đang cập nhật Consumable ID {itemDbId} lên Server. SL còn lại: {finalQty}");

            InventoryService.Instance.RequestUpdateQuantity(itemDbId, finalQty);

            _pendingQuantities.Remove(itemDbId);
        }

        _consumableSyncCoroutines.Remove(itemDbId);
    }

    #endregion
}
