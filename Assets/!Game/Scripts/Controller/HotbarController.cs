using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class HotbarController : MonoBehaviour
{
    public static HotbarController Instance { get; private set; } // Singleton

    public GameObject hotbarPanel;
    public GameObject slotPrefab;
    public int slotCount = 9;

    private ItemDictionary itemDictionary;
    private Key[] hotbarKeys;

    private void Awake()
    {
        // Init Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        itemDictionary = FindFirstObjectByType<ItemDictionary>();

        hotbarKeys = new Key[slotCount];
        for (int i = 0; i < slotCount; i++)
        {
            hotbarKeys[i] = i < 9 ? (Key)((int)Key.Digit1 + i) : Key.Digit0;
        }

        if (hotbarPanel.transform.childCount < slotCount)
        {
            for (int i = 0; i < slotCount; i++)
            {
                GameObject slotObj = Instantiate(slotPrefab, hotbarPanel.transform);
                Slot slot = slotObj.GetComponent<Slot>();
                slot.isHotBarSlot = true;
            }
        }
    }

    void Update()
    {
        if (PauseController.IsGamePause)
            return;

        for (int i = 0; i < slotCount; i++)
        {
            if (Keyboard.current[hotbarKeys[i]].wasPressedThisFrame)
            {
                UseItemInSlot(i);
            }
        }
    }

    void UseItemInSlot(int index)
    {
        if (index >= hotbarPanel.transform.childCount) return;

        Slot slot = hotbarPanel.transform.GetChild(index).GetComponent<Slot>();
        if (slot.currentItem != null)
        {
            Item item = slot.currentItem.GetComponent<Item>();

            if (item is ConsumableItem consumable)
            {
                if (consumable.dbID == 0)
                {
                    GameNotify.Show("Vật phẩm đang đồng bộ, vui lòng chờ!");
                    return;
                }

                consumable.UseItem();

                if (consumable.quantity <= 0)
                {
                    InventoryService.Instance.RequestRemoveItem(consumable.dbID);
                    Destroy(slot.currentItem);
                    slot.currentItem = null;
                }
                else
                {
                    InventoryService.Instance.RequestUpdateQuantity(consumable.dbID, consumable.quantity);
                }
            }
        }
    }

    // Lấy dữ liệu để Save (Gửi lên Server Inventory Table)
    public List<InventorySaveData> GetHotbarItems()
    {
        List<InventorySaveData> hotData = new List<InventorySaveData>();
        foreach (Transform slotTransform in hotbarPanel.transform)
        {
            Slot slot = slotTransform.GetComponent<Slot>();
            if (slot.currentItem != null)
            {
                Item item = slot.currentItem.GetComponent<Item>();
                hotData.Add(new InventorySaveData
                {
                    dbID = item.dbID,
                    itemID = item.ID,
                    // QUAN TRỌNG: Cộng 1000 để Server biết đây là Hotbar
                    slotIndex = slotTransform.GetSiblingIndex() + 1000,
                    quantity = item.quantity,
                    isEquipped = false,
                    rarity = item.rarity,
                    qualityFactor = item.qualityFactor
                });
            }
        }
        return hotData;
    }

    // Hiển thị dữ liệu từ Server
    public void SetHotbarItems(List<InventorySaveData> inventorySaveData)
    {
        int currentCount = hotbarPanel.transform.childCount;
        if (currentCount < slotCount)
        {
            for (int i = 0; i < (slotCount - currentCount); i++)
            {
                GameObject slotObj = Instantiate(slotPrefab, hotbarPanel.transform);
                Slot slot = slotObj.GetComponent<Slot>();
                slot.isHotBarSlot = true;
            }
        }

        // Dọn dẹp Item cũ trong các Slot
        foreach (Transform slotTrans in hotbarPanel.transform)
        {
            Slot s = slotTrans.GetComponent<Slot>();
            if (s != null && s.currentItem != null)
            {
                Destroy(s.currentItem);
                s.currentItem = null;
            }
        }

        if (inventorySaveData == null || inventorySaveData.Count == 0) return;

        // Điền Item mới vào
        foreach (InventorySaveData data in inventorySaveData)
        {
            // Kiểm tra index hợp lệ
            if (data.slotIndex >= 0 && data.slotIndex < hotbarPanel.transform.childCount)
            {
                Slot slot = hotbarPanel.transform.GetChild(data.slotIndex).GetComponent<Slot>();
                GameObject itemPrefab = itemDictionary.GetItemPrefab(data.itemID);

                if (itemPrefab != null)
                {
                    GameObject itemObj = Instantiate(itemPrefab, slot.transform);
                    itemObj.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

                    Item itemComponent = itemObj.GetComponent<Item>();
                    if (itemComponent != null)
                    {
                        itemComponent.dbID = data.dbID;
                        itemComponent.quantity = data.quantity;
                        itemComponent.rarity = data.rarity;
                        itemComponent.qualityFactor = data.qualityFactor;

                        itemComponent.UpdateQuantityDisplay();
                    }
                    slot.currentItem = itemObj;
                }
            }
        }
    }
}