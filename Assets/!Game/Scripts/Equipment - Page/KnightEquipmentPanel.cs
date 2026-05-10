using System.Collections.Generic;
using UnityEngine;

public class KnightEquipmentPanel : MonoBehaviour
{
    [Header("Knight Equipment Slots")]
    public GameObject Swords;
    public GameObject Shield;
    public GameObject Helmet;
    public GameObject Armor;
    public static bool HasWeaponEquipped { get; private set; }

    private void Awake()
    {
        if (Swords == null) Swords = GameObject.Find("Swords");
        if (Shield == null) Shield = GameObject.Find("Shield");
        if (Helmet == null) Helmet = GameObject.Find("Helmet");
        if (Armor == null) Armor = GameObject.Find("Armor");
    }

    public void SetEquipmentItems(List<EquippedSaveData> savedData)
    {
        ClearSlot(Swords);
        ClearSlot(Shield);
        ClearSlot(Helmet);
        ClearSlot(Armor);

        if (savedData == null)
        {
            UpdateWeaponStatus();
            return;
        }

        foreach (EquippedSaveData data in savedData)
        {
            if (data == null) continue;

            GameObject targetSlot = GetSlotByIndex(data.slotIndex);
            if (targetSlot != null)
            {
                GameObject itemPrefab = ItemDictionary.Instance.GetItemPrefab(data.itemID);
                if (itemPrefab != null)
                {
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
        }
        UpdateWeaponStatus();
    }

    public List<EquippedSaveData> GetEquipmentItems()
    {
        List<EquippedSaveData> equipmentData = new List<EquippedSaveData>();
        if (Swords != null) AddSlotData(Swords, Swords.transform.GetSiblingIndex(), equipmentData);
        if (Shield != null) AddSlotData(Shield, Shield.transform.GetSiblingIndex(), equipmentData);
        if (Helmet != null) AddSlotData(Helmet, Helmet.transform.GetSiblingIndex(), equipmentData);
        if (Armor != null) AddSlotData(Armor, Armor.transform.GetSiblingIndex(), equipmentData);
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
        GameObject[] allSlots = { Swords, Shield, Helmet, Armor };
        foreach (var slot in allSlots)
        {
            if (slot != null && slot.transform.GetSiblingIndex() == slotIndex) return slot;
        }
        return null;
    }

    public void UpdateWeaponStatus()
    {
        HasWeaponEquipped = Swords != null && Swords.transform.childCount > 0;
    }
}