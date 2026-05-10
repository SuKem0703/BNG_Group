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
        if (Legs == null) Legs = GameObject.Find("Legs");
        if (Boots == null) Boots = GameObject.Find("Boots");
        if (Gloves == null) Gloves = GameObject.Find("Gloves");
        if (Belt == null) Belt = GameObject.Find("Belt");
        if (Ring == null) Ring = GameObject.Find("Ring");
        if (Necklace == null) Necklace = GameObject.Find("Necklace");
    }

    public void SetEquipmentItems(List<EquippedSaveData> savedData)
    {
        foreach (var slot in GetAllSlots()) ClearSlot(slot);

        if (savedData == null) return;

        foreach (EquippedSaveData data in savedData)
        {
            GameObject targetSlot = GetSlotByIndex(data.slotIndex);
            if (targetSlot == null) continue;

            GameObject itemPrefab = ItemDictionary.Instance.GetItemPrefab(data.itemID);
            if (itemPrefab == null) continue;

            GameObject itemGO = Instantiate(itemPrefab, targetSlot.transform);
            itemGO.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

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
        foreach (Transform child in slotGO.transform) Destroy(child.gameObject);
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