using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class HotbarController : MonoBehaviour
{
    public static HotbarController Instance { get; private set; }

    public GameObject hotbarPanel;
    public GameObject slotPrefab;
    public int slotCount = 9;

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

    private void Start()
    {
        if (InventoryController.Instance != null)
        {
            InventoryController.Instance.OnInventoryChanged -= RedrawHotbar;
            InventoryController.Instance.OnInventoryChanged += RedrawHotbar;
        }
    }

    private void OnEnable()
    {
        if (InventoryController.Instance != null)
            InventoryController.Instance.OnInventoryChanged += RedrawHotbar;
    }

    private void OnDisable()
    {
        if (InventoryController.Instance != null)
            InventoryController.Instance.OnInventoryChanged -= RedrawHotbar;
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
            }
        }
    }

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

    private void RedrawHotbar(List<InventorySaveData> inventoryData, int maxSlotCount)
    {
        foreach (Transform slotTrans in hotbarPanel.transform)
        {
            Slot s = slotTrans.GetComponent<Slot>();
            if (s != null && s.currentItem != null)
            {
                Destroy(s.currentItem);
                s.currentItem = null;
            }
        }

        if (inventoryData == null) return;

        foreach (var data in inventoryData)
        {
            if (data.slotIndex >= 1000 && data.slotIndex < 2000)
            {
                int localIndex = data.slotIndex - 1000;

                if (localIndex >= 0 && localIndex < hotbarPanel.transform.childCount)
                {
                    Slot slot = hotbarPanel.transform.GetChild(localIndex).GetComponent<Slot>();
                    GameObject itemPrefab = ItemDictionary.Instance.GetItemPrefab(data.itemID);

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
}