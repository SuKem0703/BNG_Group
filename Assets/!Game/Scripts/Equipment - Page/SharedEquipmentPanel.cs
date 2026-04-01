using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SharedEquipmentPanel : MonoBehaviour
{
    [Header("Shared Equipment Slots")]
    public GameObject Legs;
    public GameObject Boots;
    public GameObject Gloves;
    public GameObject Belt;
    public GameObject Ring;
    public GameObject Necklace;

    [Header("Item Display Settings")]
    public GridLayoutGroup scrollViewGrid;

    private ItemDictionary itemDictionary;

    private void Awake()
    {
        itemDictionary = Object.FindFirstObjectByType<ItemDictionary>();

        if (itemDictionary == null)
            Debug.LogError("[SharedEquipmentPanel] Không tìm thấy ItemDictionary!");

        if (Legs == null) Legs = GameObject.Find("Legs");
        if (Boots == null) Boots = GameObject.Find("Boots");
        if (Gloves == null) Gloves = GameObject.Find("Gloves");
        if (Belt == null) Belt = GameObject.Find("Belt");
        if (Ring == null) Ring = GameObject.Find("Ring");
        if (Necklace == null) Necklace = GameObject.Find("Necklace");

        scrollViewGrid ??= GameObject.Find("EquipmentList")?.GetComponent<GridLayoutGroup>();
    }

    public void RefreshEquipmentDisplay(ClassRestriction classRestriction)
    {
        if (itemDictionary == null) return;

        foreach (var slot in GetAllSlots())
            ClearSlot(slot);

        foreach (Item itemPrefab in itemDictionary.itemPrefabs)
        {
            if (itemPrefab is not EquipmentItem equipPrefab) continue;
            if (equipPrefab.isEquipped) continue;

            if (equipPrefab.classRestriction != ClassRestriction.None &&
                equipPrefab.classRestriction != classRestriction)
                continue;

            GameObject targetSlot = GetAvailableSlot(equipPrefab.equipSlot);
            if (targetSlot == null) continue;

            Item equippedItem = Instantiate(itemPrefab.gameObject, targetSlot.transform).GetComponent<Item>();
            equippedItem.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

            equippedItem.quantity = 1;
            equippedItem.UpdateQuantityDisplay();

            if (equippedItem is EquipmentItem eItem)
            {
                eItem.isEquipped = true;
                eItem.sourceItem = itemPrefab;
            }

            Slot slotComponent = targetSlot.GetComponent<Slot>();
            if (slotComponent != null)
            {
                slotComponent.isEquipmentSlot = true;
                slotComponent.currentItem = equippedItem.gameObject;
            }
        }
    }

    private GameObject GetAvailableSlot(EquipSlot equipSlot)
    {
        GameObject slot = equipSlot switch
        {
            EquipSlot.Legs => Legs,
            EquipSlot.Boots => Boots,
            EquipSlot.Gloves => Gloves,
            EquipSlot.Belt => Belt,
            EquipSlot.Ring => Ring,
            EquipSlot.Necklace => Necklace,
            _ => null
        };

        return (slot != null && slot.transform.childCount == 0) ? slot : null;
    }

    private void ClearSlot(GameObject slotGO)
    {
        if (slotGO == null) return;
        foreach (Transform child in slotGO.transform)
            Destroy(child.gameObject);
    }

    public List<EquippedSaveData> GetEquipmentItems()
    {
        List<EquippedSaveData> equipmentData = new List<EquippedSaveData>();

        GameObject[] allSlots = GetAllSlots();
        for (int i = 0; i < allSlots.Length; i++)
            AddSlotData(allSlots[i], i, equipmentData);

        return equipmentData;
    }

    private void AddSlotData(GameObject slotGO, int slotIndex, List<EquippedSaveData> list)
    {
        if (slotGO == null) return;

        if (slotGO != null && slotGO.transform.childCount > 0)
        {
            Item item = slotGO.transform.GetChild(0).GetComponent<Item>();
            if (item != null)
            {
                bool equippedStatus = item is EquipmentItem eq && eq.isEquipped;
                list.Add(new EquippedSaveData
                {
                    itemID = item.ID,
                    slotIndex = slotIndex,
                    quantity = item.quantity,
                    isEquipped = equippedStatus,
                    rarity = item.rarity,
                    qualityFactor = item.qualityFactor,
                    sourceItemID = item.sourceItem != null ? item.sourceItem.ID : -1
                });
            }
        }
    }

    public void SetEquipmentItems(List<EquippedSaveData> savedData)
    {
        foreach (var slot in GetAllSlots())
            ClearSlot(slot);

        if (savedData == null) return;

        foreach (EquippedSaveData data in savedData)
        {
            GameObject targetSlot = GetSlotByIndex(data.slotIndex);
            if (targetSlot == null) continue;

            GameObject itemPrefab = itemDictionary.GetItemPrefab(data.itemID);
            if (itemPrefab == null) continue;

            GameObject itemGO = Instantiate(itemPrefab, targetSlot.transform);
            itemGO.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

            Item itemComponent = itemGO.GetComponent<Item>();
            if (itemComponent != null)
            {
                itemComponent.quantity = data.quantity;
                itemComponent.rarity = data.rarity;
                itemComponent.qualityFactor = data.qualityFactor;
                itemComponent.UpdateQuantityDisplay();

                if (itemComponent is EquipmentItem equipComp)
                {
                    equipComp.isEquipped = data.isEquipped;

                    Item sourceItemInInventory = FindItemInInventory(itemComponent.ID);
                    if (sourceItemInInventory != null)
                    {
                        equipComp.sourceItem = sourceItemInInventory;
                        if (sourceItemInInventory is EquipmentItem sourceEq)
                            sourceEq.isEquipped = true;
                    }
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

    private Item FindItemInInventory(int itemID)
    {
        if (scrollViewGrid == null) return null;

        foreach (Transform child in scrollViewGrid.transform)
        {
            Item item = child.GetComponent<Item>();
            if (item != null && item.ID == itemID && item is EquipmentItem eq && eq.isEquipped)
                return item;
        }
        return null;
    }

    private GameObject GetSlotByIndex(int slotIndex)
    {
        GameObject[] allSlots = GetAllSlots();
        return (slotIndex >= 0 && slotIndex < allSlots.Length) ? allSlots[slotIndex] : null;
    }

    private GameObject[] GetAllSlots()
    {
        return new GameObject[] { Legs, Boots, Gloves, Belt, Ring, Necklace };
    }
}