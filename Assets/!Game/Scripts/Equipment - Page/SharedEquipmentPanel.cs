using System.Collections.Generic;
using UnityEngine;

public class SharedEquipmentPanel : MonoBehaviour
{
    [Header("Shared Equipment Slots")]
    public GameObject Legs;
    public GameObject Boots;
    public GameObject Gloves;
    public GameObject Belt;
    public GameObject Ring;
    public GameObject Necklace;

    private void Awake()
    {
        EnsureSlotsInitialized();
    }

    private void EnsureSlotsInitialized()
    {
        if (Legs == null) Legs = transform.FindDeepChild("Legs")?.gameObject ?? GameObject.Find("Legs");
        if (Boots == null) Boots = transform.FindDeepChild("Boots")?.gameObject ?? GameObject.Find("Boots");
        if (Gloves == null) Gloves = transform.FindDeepChild("Gloves")?.gameObject ?? GameObject.Find("Gloves");
        if (Belt == null) Belt = transform.FindDeepChild("Belt")?.gameObject ?? GameObject.Find("Belt");
        if (Ring == null) Ring = transform.FindDeepChild("Ring")?.gameObject ?? GameObject.Find("Ring");
        if (Necklace == null) Necklace = transform.FindDeepChild("Necklace")?.gameObject ?? GameObject.Find("Necklace");
    }

    // Đăng ký lắng nghe thay đổi từ InventoryController để tự động vẽ các ô 2200 - 2299
    private void Start()
    {
        if (InventoryController.Instance != null)
        {
            InventoryController.Instance.OnInventoryChanged -= SyncFromInventory;
            InventoryController.Instance.OnInventoryChanged += SyncFromInventory;
            SyncFromInventory(InventoryController.Instance.GetInventoryItemsData(), InventoryController.Instance.slotCount);
        }
    }

    private void OnEnable()
    {
        if (InventoryController.Instance != null)
        {
            InventoryController.Instance.OnInventoryChanged -= SyncFromInventory;
            InventoryController.Instance.OnInventoryChanged += SyncFromInventory;
            SyncFromInventory(InventoryController.Instance.GetInventoryItemsData(), InventoryController.Instance.slotCount);
        }
    }

    private void OnDestroy()
    {
        if (InventoryController.Instance != null)
        {
            InventoryController.Instance.OnInventoryChanged -= SyncFromInventory;
        }
    }

    public void SyncFromInventory(List<InventorySaveData> inventoryData, int maxSlots = 0)
    {
        EnsureSlotsInitialized();

        List<EquippedSaveData> sharedEquips = new List<EquippedSaveData>();
        if (inventoryData != null)
        {
            foreach (var data in inventoryData)
            {
                if (data != null && data.slotIndex >= 2200 && data.slotIndex < 2300)
                {
                    sharedEquips.Add(new EquippedSaveData
                    {
                        dbID = data.dbID,
                        itemID = data.itemID,
                        slotIndex = data.slotIndex - 2200,
                        quantity = data.quantity,
                        isEquipped = true,
                        rarity = data.rarity,
                        qualityFactor = data.qualityFactor,
                        sourceItemID = -1
                    });
                }
            }
        }

        SetEquipmentItems(sharedEquips);
        InventoryActionManager.Instance?.RefreshPlayerStats();
    }

    public void SetEquipmentItems(List<EquippedSaveData> savedData)
    {
        EnsureSlotsInitialized();

        foreach (var slot in GetAllSlots()) ClearSlot(slot);

        if (savedData == null) return;

        foreach (EquippedSaveData data in savedData)
        {
            if (data == null) continue;

            // Quy đổi Global Index (2200+) về Local Sibling Index
            int localSlotIndex = data.slotIndex >= 2200 ? data.slotIndex - 2200 : data.slotIndex;

            GameObject targetSlot = GetSlotByIndex(localSlotIndex);
            if (targetSlot == null) continue;

            GameObject itemPrefab = ItemDictionary.Instance.GetItemPrefab(data.itemID);
            if (itemPrefab == null) continue;

            GameObject itemGO = Instantiate(itemPrefab, targetSlot.transform);
            itemGO.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

            if (itemGO.GetComponent<Collectible>()) Destroy(itemGO.GetComponent<Collectible>());
            if (itemGO.GetComponent<Monologue>()) Destroy(itemGO.GetComponent<Monologue>());

            Item itemComponent = itemGO.GetComponent<Item>();
            if (itemComponent != null)
            {
                itemComponent.dbID = data.dbID;
                itemComponent.quantity = data.quantity;
                itemComponent.rarity = data.rarity;
                itemComponent.qualityFactor = data.qualityFactor;
                itemComponent.UpdateQuantityDisplay();

                if (itemComponent is EquipmentItem equipComp)
                {
                    equipComp.isEquipped = true;
                    equipComp.isDisplayOnly = false;
                    equipComp.sourceItem = null;
                }
            }

            Slot slotComponent = targetSlot.GetComponent<Slot>();
            if (slotComponent != null)
            {
                slotComponent.isEquipmentSlot = true;
                slotComponent.currentItem = itemGO;
            }
        }
    }

    public List<EquippedSaveData> GetEquipmentItems()
    {
        List<EquippedSaveData> equipmentData = new List<EquippedSaveData>();
        foreach (var slot in GetAllSlots())
        {
            if (slot != null) AddSlotData(slot, slot.transform.GetSiblingIndex(), equipmentData);
        }
        return equipmentData;
    }

    private void AddSlotData(GameObject slotGO, int slotIndex, List<EquippedSaveData> list)
    {
        if (slotGO == null || slotGO.transform.childCount == 0) return;

        Item item = slotGO.transform.GetChild(0).GetComponent<Item>();
        if (item != null)
        {
            list.Add(new EquippedSaveData
            {
                dbID = item.dbID,
                itemID = item.ID,
                slotIndex = slotIndex,
                quantity = item.quantity,
                isEquipped = true,
                rarity = item.rarity,
                qualityFactor = item.qualityFactor,
                sourceItemID = -1
            });
        }
    }

    private void ClearSlot(GameObject slotGO)
    {
        if (slotGO == null) return;
        Slot slotComp = slotGO.GetComponent<Slot>();
        if (slotComp != null) slotComp.currentItem = null;

        for (int i = slotGO.transform.childCount - 1; i >= 0; i--)
        {
            Transform child = slotGO.transform.GetChild(i);
            child.SetParent(null);
            Destroy(child.gameObject);
        }
    }

    private GameObject GetSlotByIndex(int slotIndex)
    {
        foreach (var slot in GetAllSlots())
        {
            if (slot != null && slot.transform.GetSiblingIndex() == slotIndex) return slot;
        }
        return null;
    }

    private GameObject[] GetAllSlots()
    {
        return new GameObject[] { Legs, Boots, Gloves, Belt, Ring, Necklace };
    }
}