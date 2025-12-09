using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class KnightEquipmentPanel : MonoBehaviour
{
    [Header("Knight Equipment Slots")]
    public GameObject Swords;
    public GameObject Shield;
    public GameObject Helmet;
    public GameObject Armor;
    public static bool HasWeaponEquipped { get; private set; }

    [Header("Item Display Settings")]
    public GridLayoutGroup scrollViewGrid;

    private ItemDictionary itemDictionary;

    private void Awake()
    {
        itemDictionary = Object.FindFirstObjectByType<ItemDictionary>();

        if (itemDictionary == null)
        {
            Debug.LogError("[KnightEquipmentPanel] Không tìm thấy ItemDictionary!");
        }

        if (Swords == null) Swords = GameObject.Find("Swords");
        if (Shield == null) Shield = GameObject.Find("Shield");
        if (Helmet == null) Helmet = GameObject.Find("Helmet");
        if (Armor == null) Armor = GameObject.Find("Armor");

        if (scrollViewGrid == null) scrollViewGrid = GameObject.Find("EquipmentList")?.GetComponent<GridLayoutGroup>();

    }

    public void RefreshEquipmentDisplay()
    {
        if (itemDictionary == null) return;

        ClearSlot(Swords);
        ClearSlot(Shield);
        ClearSlot(Helmet);
        ClearSlot(Armor);

        foreach (Item itemPrefab in itemDictionary.itemPrefabs)
        {
            if (itemPrefab == null) continue;
            if (itemPrefab.classRestriction != ClassRestriction.Knight) continue;
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
            case EquipSlot.Swords:
                return Swords.transform.childCount == 0 ? Swords : null;
            case EquipSlot.Shield:
                return Shield.transform.childCount == 0 ? Shield : null;
            case EquipSlot.Helmet:
                return Helmet.transform.childCount == 0 ? Helmet : null;
            case EquipSlot.Armor:
                return Armor.transform.childCount == 0 ? Armor : null;
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

        AddSlotData(Swords, 0, equipmentData);
        AddSlotData(Shield, 1, equipmentData);
        AddSlotData(Helmet, 2, equipmentData);
        AddSlotData(Armor, 3, equipmentData);

        return equipmentData;
    }

    private void AddSlotData(GameObject slotGO, int slotIndex, List<EquippedSaveData> list)
    {
        if (slotGO == null)
        {
            Debug.LogWarning($"[KnightEquipmentPanel] Slot {slotIndex} bị null. Không thể lưu. Hãy gán nó trong Inspector.");
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
        if (itemDictionary == null)
        {
            itemDictionary = Object.FindFirstObjectByType<ItemDictionary>();
            if (itemDictionary == null)
            {
                Debug.LogError("[KnightEquipmentPanel] ItemDictionary is missing! Cannot load equipment.");
                return;
            }
        }

        if (savedData == null)
        {
            ClearSlot(Swords);
            ClearSlot(Shield);
            ClearSlot(Helmet);
            ClearSlot(Armor);
            UpdateWeaponStatus();
            return;
        }

        ClearSlot(Swords);
        ClearSlot(Shield);
        ClearSlot(Helmet);
        ClearSlot(Armor);

        foreach (EquippedSaveData data in savedData)
        {
            if (data == null) continue;

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
            case 0: return Swords;
            case 1: return Shield;
            case 2: return Helmet;
            case 3: return Armor;
            default: return null;
        }
    }
    public void UpdateWeaponStatus()
    {
        HasWeaponEquipped = Swords != null && Swords.transform.childCount > 0;
    }

}