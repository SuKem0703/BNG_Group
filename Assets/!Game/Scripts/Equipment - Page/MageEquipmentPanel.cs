using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MageEquipmentPanel : MonoBehaviour
{
    [Header("Mage Equipment Slots")]
    public GameObject Staff;
    public GameObject Catalyst;
    public GameObject Hat;
    public GameObject Robe;

    [Header("Item Display Settings")]
    public GridLayoutGroup scrollViewGrid;

    public static bool HasWeaponEquipped { get; private set; }

    private ItemDictionary itemDictionary;

    private void Awake()
    {
        itemDictionary = Object.FindFirstObjectByType<ItemDictionary>();

        if (itemDictionary == null)
        {
            Debug.LogError("[MageEquipmentPanel] Không tìm thấy ItemDictionary!");
        }

        if (Staff == null) Staff = GameObject.Find("Staff");
        if (Catalyst == null) Catalyst = GameObject.Find("Catalyst");
        if (Hat == null) Hat = GameObject.Find("Hat");
        if (Robe == null) Robe = GameObject.Find("Robe");

        if (scrollViewGrid == null) scrollViewGrid = GameObject.Find("EquipmentList")?.GetComponent<GridLayoutGroup>();

    }
    public void RefreshEquipmentDisplay()
    {
        if (itemDictionary == null) return;

        ClearSlot(Staff);
        ClearSlot(Catalyst);
        ClearSlot(Hat);
        ClearSlot(Robe);

        foreach (Item itemPrefab in itemDictionary.itemPrefabs)
        {
            if (itemPrefab == null) continue;
            if (itemPrefab.classRestriction != ClassRestriction.Mage) continue;
            if (itemPrefab.isEquipped) continue;

            GameObject targetSlot = GetAvailableSlot(itemPrefab.equipSlot);
            if (targetSlot == null) continue;

            Item equippedItem = Instantiate(itemPrefab.gameObject, targetSlot.transform).GetComponent<Item>();
            equippedItem.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

            equippedItem.quantity = 1;
            equippedItem.UpdateQuantityDisplay();

            equippedItem.isEquipped = true;
            equippedItem.sourceItem = itemPrefab;

            Slot slotComponent = targetSlot.GetComponent<Slot>();
            if (slotComponent != null)
            {
                slotComponent.isEquipmentSlot = true;
                slotComponent.currentItem = equippedItem.gameObject;
            }
        }

        UpdateWeaponStatus();
    }

    private GameObject GetAvailableSlot(EquipSlot equipSlot)
    {
        switch (equipSlot)
        {
            case EquipSlot.Staff:
                return Staff.transform.childCount == 0 ? Staff : null;
            case EquipSlot.Catalyst:
                return Catalyst.transform.childCount == 0 ? Catalyst : null;
            case EquipSlot.Hat:
                return Hat.transform.childCount == 0 ? Hat : null;
            case EquipSlot.Robe:
                return Robe.transform.childCount == 0 ? Robe : null;
            default:
                return null;
        }
    }

    private void ClearSlot(GameObject slotGO)
    {
        if (slotGO == null) return;

        foreach (Transform child in slotGO.transform)
        {
            Destroy(child.gameObject);
        }
    }

    public List<EquippedSaveData> GetEquipmentItems()
    {
        List<EquippedSaveData> equipmentData = new List<EquippedSaveData>();

        AddSlotData(Staff, 0, equipmentData);
        AddSlotData(Catalyst, 1, equipmentData);
        AddSlotData(Hat, 2, equipmentData);
        AddSlotData(Robe, 3, equipmentData);

        return equipmentData;
    }

    private void AddSlotData(GameObject slotGO, int slotIndex, List<EquippedSaveData> list)
    {
        if (slotGO == null)
        {
            Debug.LogWarning($"[MageEquipmentPanel] Slot {slotIndex} bị null. Không thể lưu. Hãy gán nó trong Inspector.");
            return;
        }

        if (slotGO.transform.childCount > 0)
        {
            Item item = slotGO.transform.GetChild(0).GetComponent<Item>();
            if (item != null)
            {
                list.Add(new EquippedSaveData
                {
                    itemID = item.ID,
                    slotIndex = slotIndex,
                    quantity = item.quantity,
                    isEquipped = item.isEquipped,
                    rarity = item.rarity,
                    qualityFactor = item.qualityFactor,
                    sourceItemID = item.sourceItem != null ? item.sourceItem.ID : -1
                });
            }
        }
    }

    public void SetEquipmentItems(List<EquippedSaveData> savedData)
    {
        ClearSlot(Staff);
        ClearSlot(Catalyst);
        ClearSlot(Hat);
        ClearSlot(Robe);

        foreach (EquippedSaveData data in savedData)
        {
            GameObject targetSlot = GetSlotByIndex(data.slotIndex);
            if (targetSlot != null)
            {
                GameObject itemPrefab = itemDictionary.GetItemPrefab(data.itemID);
                if (itemPrefab != null)
                {
                    GameObject itemGO = Instantiate(itemPrefab, targetSlot.transform);
                    itemGO.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

                    Item itemComponent = itemGO.GetComponent<Item>();
                    if (itemComponent != null)
                    {
                        itemComponent.quantity = data.quantity;
                        itemComponent.isEquipped = data.isEquipped;
                        itemComponent.rarity = data.rarity;
                        itemComponent.qualityFactor = data.qualityFactor;
                        itemComponent.UpdateQuantityDisplay();

                        Item sourceItemInInventory = FindItemInInventory(itemComponent.ID);
                        if (sourceItemInInventory != null)
                        {
                            itemComponent.sourceItem = sourceItemInInventory;
                            sourceItemInInventory.isEquipped = true;
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
        }

        UpdateWeaponStatus();
    }

    private Item FindItemInInventory(int itemID)
    {
        if (scrollViewGrid == null) return null;

        foreach (Transform child in scrollViewGrid.transform)
        {
            Item item = child.GetComponent<Item>();
            if (item != null && item.ID == itemID)
            {
                return item;
            }
        }

        return null;
    }

    private GameObject GetSlotByIndex(int slotIndex)
    {
        switch (slotIndex)
        {
            case 0: return Staff;
            case 1: return Catalyst;
            case 2: return Hat;
            case 3: return Robe;
            default: return null;
        }
    }
    public void UpdateWeaponStatus()
    {
        HasWeaponEquipped = Staff != null && Staff.transform.childCount > 0;
    }
}
