using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class InventoryController : MonoBehaviour
{
    public ItemDictionary itemDictionary;

    public GameObject inventoryPanel;
    public GameObject slotPrefab;
    public int slotCount;
    public GameObject[] itemPrefabs;

    public static InventoryController Instance { get; private set; }
    Dictionary<int, int> itemCountCache = new();
    public event Action OnInventoryChanged;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        inventoryPanel = GameObject.Find("InventoryPage");

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
    void Start()
    {
        if (inventoryPanel == null)
        {
            var inventoryObj = Resources.FindObjectsOfTypeAll<GameObject>()
                .FirstOrDefault(x => x.name == "InventoryPage");

            if (inventoryObj == null)
            {
                Debug.LogError("Inventory panel not found!");
                return;
            }

            inventoryPanel = inventoryObj;
        }

        foreach (Transform child in inventoryPanel.transform)
        {
            Destroy(child.gameObject);
        }

        for (int i = 0; i < slotCount; i++)
        {
            Instantiate(slotPrefab, inventoryPanel.transform);
        }

        itemDictionary = FindFirstObjectByType<ItemDictionary>();
        ReBuildItemCounts();
    }
    public void ReBuildItemCounts()
    {
        itemCountCache.Clear();
        foreach (Transform slotTransform in inventoryPanel.transform)
        {
            Slot slot = slotTransform.GetComponent<Slot>();
            if (slot.currentItem != null)
            {
                Item item = slot.currentItem.GetComponent<Item>();
                if (item != null)
                {
                    itemCountCache[item.ID] = itemCountCache.GetValueOrDefault(item.ID, 0) + item.quantity;
                }
            }
        }
        OnInventoryChanged?.Invoke();
    }
    public Dictionary<int, int> GetItemCounts() => itemCountCache;
    public bool AddItem(GameObject itemPrefab)
    {
        if (itemPrefab == null) return false;

        Item tempItem = itemPrefab.GetComponent<Item>();
        if (tempItem == null) return false;

        int quantity = tempItem.quantity;

        // Nếu là item stackable
        if (tempItem.itemType != ItemType.Equipment)
        {
            foreach (Transform slotTransform in inventoryPanel.transform)
            {
                Slot slot = slotTransform.GetComponent<Slot>();
                if (slot != null && slot.currentItem != null)
                {
                    Item slotItem = slot.currentItem.GetComponent<Item>();
                    if (slotItem != null && slotItem.ID == tempItem.ID)
                    {
                        int maxStack = 999;
                        int canAdd = Mathf.Min(quantity, maxStack - slotItem.quantity);

                        if (canAdd > 0)
                        {
                            slotItem.AddToStack(canAdd);
                            quantity -= canAdd;
                        }

                        if (quantity <= 0)
                        {
                            ReBuildItemCounts();
                            return true;
                        }

                    }
                }
            }
        }

        // Nếu là Equipment hoặc chưa có item cùng loại
        foreach (Transform slotTransform in inventoryPanel.transform)
        {
            Slot slot = slotTransform.GetComponent<Slot>();
            if (slot != null && slot.currentItem == null)
            {
                GameObject newItem = Instantiate(itemPrefab, slot.transform);
                newItem.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

                Collectible collectible = newItem.GetComponent<Collectible>();
                if (collectible != null)
                {
                    Destroy(collectible);
                }

                Monologue monologue = newItem.GetComponent<Monologue>();
                if (monologue != null)
                {
                    Destroy(monologue);
                }

                Item newItemComp = newItem.GetComponent<Item>();
                newItemComp.quantity = (newItemComp.itemType == ItemType.Equipment) ? 1 : quantity;
                newItemComp.UpdateQuantityDisplay();

                slot.currentItem = newItem;
                ReBuildItemCounts();
                return true;
            }
        }

        Debug.Log("Inventory is full!");
        return false;
    }


    public List<InventorySaveData> GetInventoryItems()
    {
        List<InventorySaveData> invData = new List<InventorySaveData>();
        foreach (Transform slotTransform in inventoryPanel.transform)
        {
            Slot slot = slotTransform.GetComponent<Slot>();
            if (slot.currentItem != null)
            {
                Item item = slot.currentItem.GetComponent<Item>();
                invData.Add(new InventorySaveData { 
                    itemID = item.ID,
                    slotIndex = slotTransform.GetSiblingIndex(),
                    quantity = item.quantity,
                    isEquipped = item.isEquipped,
                    rarity = item.rarity,
                    qualityFactor = item.qualityFactor,
                });
            }
        }
        return invData;
    }

    public void SetInventoryItems(List<InventorySaveData> inventorySaveData)
    {
        // Clear inventory panel - avoid duplicates
        foreach (Transform child in inventoryPanel.transform)
        {
            Destroy(child.gameObject);
        }

        // Create new slots
        for (int i = 0; i < slotCount; i++)
        {
            Instantiate(slotPrefab, inventoryPanel.transform);
        }

        // Populate slots with saved items
        foreach (InventorySaveData data in inventorySaveData)
        {
            if (data.slotIndex < slotCount)
            {
                Slot slot = inventoryPanel.transform.GetChild(data.slotIndex).GetComponent<Slot>();
                GameObject itemPrefab = itemDictionary.GetItemPrefab(data.itemID);
                if (itemPrefab != null)
                {
                    GameObject item = Instantiate(itemPrefab, slot.transform);
                    item.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

                    Item itemComponent = item.GetComponent<Item>();
                    if (itemComponent != null)
                    {
                        if (data.quantity > 1)
                        {
                            itemComponent.quantity = data.quantity;
                            itemComponent.UpdateQuantityDisplay();
                        }

                        itemComponent.rarity = data.rarity;

                        itemComponent.qualityFactor = data.qualityFactor;

                        itemComponent.isEquipped = data.isEquipped;

                        slot.currentItem = item;
                    }
                }
            }
        }
        ReBuildItemCounts();
    }

    //public void RemoveItemsFromInventory(int itemID, int amountToRemove)
    //{
    //    foreach (Transform slotTranform in inventoryPanel.transform)
    //    {
    //        if (amountToRemove <= 0) break;

    //        Slot slot = slotTranform.GetComponent<Slot>();

    //        if (slot ?.currentItem?.GetComponent<Item>() is Item item && item.ID == itemID)
    //        {
    //            int removed = Mathf.Min(amountToRemove, item.quantity);
    //            item.RemoveFromStack(removed);
    //            amountToRemove -= removed;

    //            if (item.quantity == 0)
    //            {
    //                Destroy(slot.currentItem);
    //                slot.currentItem = null;
    //            }
    //        }
    //    }
    //    ReBuildItemCounts();
    //}
    public void RemoveItemsFromInventory(int itemID, int amountToRemove)
    {
        foreach (Transform slotTranform in inventoryPanel.transform)
        {
            if (amountToRemove <= 0) break;

            Slot slot = slotTranform.GetComponent<Slot>();

            // ---- BẮT ĐẦU SỬA LỖI ----

            // 1. Kiểm tra tường minh xem slot hoặc 'currentItem' có null không.
            // Phải dùng '== null' vì 'slot.currentItem' có thể là "fake null" (đã bị Destroy)
            // mà toán tử '?.' không bắt được.
            if (slot == null || slot.currentItem == null)
            {
                continue; // Bỏ qua slot này vì nó rỗng
            }

            // 2. Nếu 'currentItem' không null, chúng ta mới lấy Item component
            Item item = slot.currentItem.GetComponent<Item>();

            // 3. Kiểm tra xem component 'Item' có tồn tại và khớp ID không
            if (item != null && item.ID == itemID)
            // ---- KẾT THÚC SỬA LỖI ----
            {
                int removed = Mathf.Min(amountToRemove, item.quantity);
                item.RemoveFromStack(removed);
                amountToRemove -= removed;

                if (item.quantity == 0)
                {
                    Destroy(slot.currentItem);
                    slot.currentItem = null;
                }
            }
        }
        ReBuildItemCounts();
    }
}