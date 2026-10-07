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
        EnsureSlotsInitialized();
    }

    private void EnsureSlotsInitialized()
    {
        if (Staff == null) Staff = transform.FindDeepChild("Staff")?.gameObject ?? GameObject.Find("Staff");
        if (Catalyst == null) Catalyst = transform.FindDeepChild("Catalyst")?.gameObject ?? GameObject.Find("Catalyst");
        if (Hat == null) Hat = transform.FindDeepChild("Hat")?.gameObject ?? GameObject.Find("Hat");
        if (Robe == null) Robe = transform.FindDeepChild("Robe")?.gameObject ?? GameObject.Find("Robe");
    }

    // Đăng ký lắng nghe thay đổi từ InventoryController để tự động vẽ các ô 2100 - 2199
    private void Start()
    {
        if (InventoryController.Instance != null)
        {
            InventoryController.Instance.OnInventoryChanged -= SyncFromInventory;
            InventoryController.Instance.OnInventoryChanged += SyncFromInventory;
            SyncFromInventory(InventoryController.Instance.GetInventoryItemsData(), InventoryController.Instance.slotCount);
        }
    }

    private void OnEnable()
    {
        if (InventoryController.Instance != null)
        {
            InventoryController.Instance.OnInventoryChanged -= SyncFromInventory;
            InventoryController.Instance.OnInventoryChanged += SyncFromInventory;
            SyncFromInventory(InventoryController.Instance.GetInventoryItemsData(), InventoryController.Instance.slotCount);
        }
    }

    private void OnDestroy()
    {
        if (InventoryController.Instance != null)
        {
            InventoryController.Instance.OnInventoryChanged -= SyncFromInventory;
        }
    }

    public void SyncFromInventory(List<InventorySaveData> inventoryData, int maxSlots = 0)
    {
        EnsureSlotsInitialized();

        List<EquippedSaveData> mageEquips = new List<EquippedSaveData>();
        if (inventoryData != null)
        {
            foreach (var data in inventoryData)
            {
                if (data != null && data.slotIndex >= 2100 && data.slotIndex < 2200)
                {
                    mageEquips.Add(new EquippedSaveData
                    {
                        dbID = data.dbID,
                        itemID = data.itemID,
                        slotIndex = data.slotIndex - 2100,
                        quantity = data.quantity,
                        isEquipped = true,
                        rarity = data.rarity,
                        qualityFactor = data.qualityFactor,
                        sourceItemID = -1
                    });
                }
            }
        }

        SetEquipmentItems(mageEquips);
        InventoryActionManager.Instance?.RefreshPlayerStats();
    }

    public void SetEquipmentItems(List<EquippedSaveData> savedData)
    {
        EnsureSlotsInitialized();

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

            // Quy đổi Global Index (2100+) về Local Sibling Index
            int localSlotIndex = data.slotIndex >= 2100 ? data.slotIndex - 2100 : data.slotIndex;

            GameObject targetSlot = GetSlotByIndex(localSlotIndex);
            if (targetSlot != null)
            {
                GameObject itemPrefab = ItemDictionary.Instance.GetItemPrefab(data.itemID);
                if (itemPrefab != null)
                {
                    GameObject itemGO = Instantiate(itemPrefab, targetSlot.transform);
                    itemGO.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

                    if (itemGO.GetComponent<Collectible>()) Destroy(itemGO.GetComponent<Collectible>());
                    if (itemGO.GetComponent<Monologue>()) Destroy(itemGO.GetComponent<Monologue>());

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
        Slot slotComp = slotGO.GetComponent<Slot>();
        if (slotComp != null) slotComp.currentItem = null;

        for (int i = slotGO.transform.childCount - 1; i >= 0; i--)
        {
            Transform child = slotGO.transform.GetChild(i);
            child.SetParent(null);
            Destroy(child.gameObject);
        }
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