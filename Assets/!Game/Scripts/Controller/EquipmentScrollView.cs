using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class EquipmentScrollView : MonoBehaviour
{
    public GameObject equipmentList;
    public GameObject itemSlotPrefab;

    private void Start()
    {
        if (InventoryController.Instance != null)
        {
            InventoryController.Instance.OnInventoryChanged -= OnInventoryChanged;
            InventoryController.Instance.OnInventoryChanged += OnInventoryChanged;
        }

        if (gameObject.activeInHierarchy)
        {
            ShowEquipmentItems();
        }
    }

    private void OnDestroy()
    {
        if (InventoryController.Instance != null)
        {
            InventoryController.Instance.OnInventoryChanged -= OnInventoryChanged;
        }
    }

    private void OnEnable()
    {
        ShowEquipmentItems();
    }

    private void OnInventoryChanged(List<InventorySaveData> data, int slotCount)
    {
        if (gameObject.activeInHierarchy)
        {
            ShowEquipmentItems();
        }
    }

    public void ShowEquipmentItems()
    {
        if (InventoryController.Instance == null || ItemDictionary.Instance == null || equipmentList == null || itemSlotPrefab == null)
            return;

        foreach (Transform child in equipmentList.transform)
        {
            Destroy(child.gameObject);
        }

        var sortedItems = InventoryController.Instance.GetInventoryItemsData()
            .Where(data => !data.isEquipped && data.slotIndex < 2000)
            .OrderBy(data => data.slotIndex)
            .ToList();

        foreach (var data in sortedItems)
        {
            GameObject prefab = ItemDictionary.Instance.GetItemPrefab(data.itemID);
            if (prefab != null && prefab.GetComponent<EquipmentItem>() != null)
            {
                EquipmentItem equipPrefab = prefab.GetComponent<EquipmentItem>();

                GameObject slotGO = Instantiate(itemSlotPrefab, equipmentList.transform);
                slotGO.transform.localScale = Vector3.one;

                GameObject itemClone = Instantiate(equipPrefab.gameObject, slotGO.transform);
                itemClone.transform.localScale = Vector3.one;
                itemClone.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

                EquipmentItem displayItem = itemClone.GetComponent<EquipmentItem>();
                if (displayItem != null)
                {
                    displayItem.isDisplayOnly = true;
                    displayItem.isEquipped = false;
                    displayItem.dbID = data.dbID;
                    displayItem.quantity = data.quantity;
                    displayItem.rarity = data.rarity;
                    displayItem.qualityFactor = data.qualityFactor;
                    displayItem.UpdateQuantityDisplay();

                    displayItem.sourceItem = equipPrefab;
                }

                Slot newSlot = slotGO.GetComponent<Slot>();
                if (newSlot != null) newSlot.currentItem = itemClone;
            }
        }
    }
}