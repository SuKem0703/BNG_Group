using System.Collections.Generic;
using UnityEngine;

public class MageEquipmentPanel : MonoBehaviour
{
    [Header("Mage Equipment Slots")]
    public GameObject Staff;
    public GameObject Catalyst;
    public GameObject Hat;
    public GameObject Robe;
    public static bool HasWeaponEquipped { get; private set; }

    private void Awake()
    {
        if (Staff == null) Staff = GameObject.Find("Staff");
        if (Catalyst == null) Catalyst = GameObject.Find("Catalyst");
        if (Hat == null) Hat = GameObject.Find("Hat");
        if (Robe == null) Robe = GameObject.Find("Robe");
    }

    public void SetEquipmentItems(List<EquippedSaveData> savedData)
    {
        ClearSlot(Staff);
        ClearSlot(Catalyst);
        ClearSlot(Hat);
        ClearSlot(Robe);

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
        if (Staff != null) AddSlotData(Staff, Staff.transform.GetSiblingIndex(), equipmentData);
        if (Catalyst != null) AddSlotData(Catalyst, Catalyst.transform.GetSiblingIndex(), equipmentData);
        if (Hat != null) AddSlotData(Hat, Hat.transform.GetSiblingIndex(), equipmentData);
        if (Robe != null) AddSlotData(Robe, Robe.transform.GetSiblingIndex(), equipmentData);
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
        GameObject[] allSlots = { Staff, Catalyst, Hat, Robe };
        foreach (var slot in allSlots)
        {
            if (slot != null && slot.transform.GetSiblingIndex() == slotIndex) return slot;
        }
        return null;
    }

    public void UpdateWeaponStatus()
    {
        HasWeaponEquipped = Staff != null && Staff.transform.childCount > 0;
    }
}