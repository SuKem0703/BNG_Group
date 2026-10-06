using System.Collections.Generic;
using UnityEngine;

public class InventoryUIAdapter : MonoBehaviour
{
    public static InventoryUIAdapter Instance;

    [Header("References")]
    [SerializeField] private GameObject slotPrefab;

    [SerializeField] private Transform inventoryPanel;

    public Transform InventoryPanel => inventoryPanel;

    private void Start()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        Instance = this;

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

        ItemDictionary dictionary = ItemDictionary.Instance;
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

        for (int i = currentData.Count - 1; i >= 0; i--)
        {
            var data = currentData[i];

            if (data.slotIndex >= 1000) continue;
            if (data.slotIndex >= inventoryPanel.childCount) continue;

            // Kiểm tra tính hợp lệ của itemID và Prefab trước khi tạo UI
            GameObject prefab = data.itemID > 0 ? dictionary.GetItemPrefab(data.itemID) : null;
            if (prefab == null)
            {
                Debug.LogWarning($"[InventoryUIAdapter] ItemID không hợp lệ ({data.itemID}, dbID: {data.dbID}) tại slot {data.slotIndex}. Tiến hành xóa khỏi RAM và Database!");
                if (data.dbID > 0 && InventoryService.Instance != null)
                {
                    InventoryService.Instance.RequestRemoveItem(data.dbID);
                }
                currentData.RemoveAt(i);
                continue;
            }

            Slot slot = inventoryPanel.GetChild(data.slotIndex).GetComponent<Slot>();

            GameObject itemObj = Instantiate(prefab, slot.transform);
            itemObj.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

            if (itemObj.GetComponent<Collectible>()) Destroy(itemObj.GetComponent<Collectible>());
            if (itemObj.GetComponent<Monologue>()) Destroy(itemObj.GetComponent<Monologue>());

            Item item = itemObj.GetComponent<Item>();
            if (item != null)
            {
                // Nếu Prefab gắn script Item nhưng ID cấu hình không hợp lệ thì Destroy ngay
                if (item.ID <= 0)
                {
                    Debug.LogWarning($"[InventoryUIAdapter] Prefab {prefab.name} có Item.ID không hợp lệ ({item.ID}). Tiến hành Destroy UI Object!");
                    Destroy(itemObj);
                    if (data.dbID > 0 && InventoryService.Instance != null)
                    {
                        InventoryService.Instance.RequestRemoveItem(data.dbID);
                    }
                    currentData.RemoveAt(i);
                    continue;
                }

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
            else
            {
                Debug.LogWarning($"[InventoryUIAdapter] Prefab {prefab.name} thiếu component Item. Tiến hành Destroy!");
                Destroy(itemObj);
                continue;
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