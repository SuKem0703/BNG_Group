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
        EnsureSlotsInitialized();
    }

    // Hỗ trợ tìm kiếm Slot kể cả khi Panel đang bị ẩn lúc khởi chạy
    private void EnsureSlotsInitialized()
    {
        if (Swords == null) Swords = transform.FindDeepChild("Swords")?.gameObject ?? GameObject.Find("Swords");
        if (Shield == null) Shield = transform.FindDeepChild("Shield")?.gameObject ?? GameObject.Find("Shield");
        if (Helmet == null) Helmet = transform.FindDeepChild("Helmet")?.gameObject ?? GameObject.Find("Helmet");
        if (Armor == null) Armor = transform.FindDeepChild("Armor")?.gameObject ?? GameObject.Find("Armor");
    }

    // Đăng ký lắng nghe thay đổi từ InventoryController để tự động vẽ các ô 2000 - 2099
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

    // Lọc các trang bị của Knight (slotIndex từ 2000 đến 2099) và đưa vào ô tương ứng
    public void SyncFromInventory(List<InventorySaveData> inventoryData, int maxSlots = 0)
    {
        EnsureSlotsInitialized();

        List<EquippedSaveData> knightEquips = new List<EquippedSaveData>();
        if (inventoryData != null)
        {
            foreach (var data in inventoryData)
            {
                if (data != null && data.slotIndex >= 2000 && data.slotIndex < 2100)
                {
                    knightEquips.Add(new EquippedSaveData
                    {
                        dbID = data.dbID,
                        itemID = data.itemID,
                        slotIndex = data.slotIndex - 2000,
                        quantity = data.quantity,
                        isEquipped = true,
                        rarity = data.rarity,
                        qualityFactor = data.qualityFactor,
                        sourceItemID = -1
                    });
                }
            }
        }

        SetEquipmentItems(knightEquips);
        InventoryActionManager.Instance?.RefreshPlayerStats();
    }

    public void SetEquipmentItems(List<EquippedSaveData> savedData)
    {
        EnsureSlotsInitialized();

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

            // Quy đổi Global Index (2000+) về Local Sibling Index nếu dữ liệu truyền vào chưa trừ
            int localSlotIndex = data.slotIndex >= 2000 ? data.slotIndex - 2000 : data.slotIndex;

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

    // Tách child khỏi Slot trước khi Destroy để childCount cập nhật ngay lập tức trong cùng frame
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