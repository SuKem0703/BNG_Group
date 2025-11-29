using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class EquipmentScrollViewController : MonoBehaviour
{
    public GameObject equipmentList;
    public GameObject itemSlotPrefab;

    public GameObject inventoryPanel;

    private void Awake()
    {
        if (equipmentList == null)
            equipmentList = GameObject.Find("EquipmentList");
        if (inventoryPanel == null)
            inventoryPanel = GameObject.Find("InventoryPage");
    }
    private void Start()
    {
        if (inventoryPanel == null)
        {
            var inventoryObj = Resources.FindObjectsOfTypeAll<GameObject>()
                .FirstOrDefault(x => x.name == "InventoryPage");

            if (inventoryObj == null)
            {
                Debug.LogError("Inventory panel not found!");
                return;
            }

            inventoryPanel = inventoryObj;
        }

        if (equipmentList == null)
        {
            var equipmentObj = Resources.FindObjectsOfTypeAll<GameObject>()
                .FirstOrDefault(x => x.name == "EquipmentList");

            if (equipmentObj == null)
            {
                Debug.LogError("Equipment list not found!");
                return;
            }

            equipmentList = equipmentObj;
        }
    }
    public void ShowEquipmentItems()
    {
        foreach (Transform child in equipmentList.transform)
        {
            Destroy(child.gameObject);
        }

        // Duyệt qua tất cả các Slot trong inventoryPanel
        foreach (Transform slotTransform in inventoryPanel.transform)
        {
            Slot slot = slotTransform.GetComponent<Slot>();
            if (slot != null && slot.currentItem != null)
            {
                Item itemInInventory = slot.currentItem.GetComponent<Item>();
                if (itemInInventory != null && itemInInventory.itemType == ItemType.Equipment)
                {
                    // Tạo một slot mới để chứa bản hiển thị
                    GameObject slotGO = Instantiate(itemSlotPrefab, equipmentList.transform);

                    // Clone bản hiển thị từ item thực
                    GameObject itemClone = Instantiate(itemInInventory.gameObject);
                    itemClone.transform.SetParent(slotGO.transform, false);
                    itemClone.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

                    // Đánh dấu bản clone là hiển thị (không phải bản thực)
                    Item displayItem = itemClone.GetComponent<Item>();
                    if (displayItem != null)
                    {
                        displayItem.isDisplayOnly = true;
                        displayItem.isEquipped = false;

                        // 🌟 Gán sourceItem là item thực từ Inventory
                        displayItem.sourceItem = itemInInventory;
                    }


                    // Gán item vào slot nếu slot có component Slot
                    Slot newSlot = slotGO.GetComponent<Slot>();
                    if (newSlot != null)
                    {
                        newSlot.currentItem = itemClone;
                    }
                }
            }
        }
    }
}
