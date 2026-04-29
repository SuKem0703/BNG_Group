using System.Collections.Generic;
using UnityEngine;

public class InventoryUIAdapter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject slotPrefab;

    [SerializeField] private Transform inventoryPanel;

    public Transform InventoryPanel => inventoryPanel;

    private void Start()
    {
        if (InventoryController.Instance != null)
        {
            InventoryController.Instance.OnInventoryChanged += RedrawUI;
            RedrawUI(InventoryController.Instance.GetInventoryItemsData(), InventoryController.Instance.slotCount);
        }
    }

    private void OnDestroy()
    {
        if (InventoryController.Instance != null)
        {
            InventoryController.Instance.OnInventoryChanged -= RedrawUI;
        }
    }

    private void RedrawUI(List<InventorySaveData> currentData, int maxSlots)
    {
        if (inventoryPanel == null || slotPrefab == null) return;

        ItemDictionary dictionary = InventoryController.Instance.itemDictionary;
        if (dictionary == null) return;

        EnsureSlotCount(maxSlots);

        foreach (Transform slotTrans in inventoryPanel)
        {
            Slot s = slotTrans.GetComponent<Slot>();
            if (s != null && s.currentItem != null)
            {
                Destroy(s.currentItem);
                s.currentItem = null;
            }
        }

        if (currentData == null || currentData.Count == 0) return;

        foreach (var data in currentData)
        {
            if (data.slotIndex >= inventoryPanel.childCount) continue;

            Slot slot = inventoryPanel.GetChild(data.slotIndex).GetComponent<Slot>();

            GameObject prefab = dictionary.GetItemPrefab(data.itemID);

            if (prefab == null) continue;

            GameObject itemObj = Instantiate(prefab, slot.transform);
            itemObj.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

            if (itemObj.GetComponent<Collectible>()) Destroy(itemObj.GetComponent<Collectible>());
            if (itemObj.GetComponent<Monologue>()) Destroy(itemObj.GetComponent<Monologue>());

            Item item = itemObj.GetComponent<Item>();
            if (item != null)
            {
                item.dbID = data.dbID;
                item.quantity = Mathf.Max(1, data.quantity);
                item.rarity = data.rarity;
                item.qualityFactor = data.qualityFactor;

                if (item is EquipmentItem equip)
                {
                    equip.isEquipped = data.isEquipped;
                }

                item.UpdateQuantityDisplay();
            }

            slot.currentItem = itemObj;
        }
    }

    private void EnsureSlotCount(int neededCount)
    {
        int currentCount = inventoryPanel.childCount;
        if (currentCount < neededCount)
        {
            for (int i = 0; i < (neededCount - currentCount); i++)
            {
                Instantiate(slotPrefab, inventoryPanel);
            }
        }
    }
}