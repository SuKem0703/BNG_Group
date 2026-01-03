using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class StorageChestController : MonoBehaviour
{
    public static StorageChestController Instance { get; private set; }

    [Header("UI References")]
    public GameObject chestPanel;
    public Transform pageContainer;
    public GameObject storageChestPage;

    public GameObject slotPrefab;
    public int slotCount = 20;

    [Header("Borrowed References")]
    public GameObject inventoryPanel;

    [Header("Tab UI")]
    public Image chestTabImage;
    public Image inventoryTabImage;
    public Color activeTabColor = Color.white;
    public Color inactiveTabColor = new Color(0.5f, 0.5f, 0.5f, 1f);

    private ItemDictionary itemDictionary;
    private StorageChest currentActiveChest;
    private Transform originalInventoryParent;
    private int originalInventorySiblingIndex;

    public bool IsViewingChest { get; private set; } = true;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (inventoryPanel == null)
        {
            var foundObj = Resources.FindObjectsOfTypeAll<GameObject>().FirstOrDefault(x => x.name == "InventoryPage");
            if (foundObj != null) { inventoryPanel = foundObj; originalInventoryParent = inventoryPanel.transform.parent; }
        }

        if (chestPanel == null) chestPanel = GameObject.Find("StorageChestPanel");
        if (pageContainer == null && chestPanel != null) pageContainer = chestPanel.transform.Find("PageContainer");
        if (storageChestPage == null && pageContainer != null && pageContainer.childCount > 0)
            storageChestPage = pageContainer.GetChild(0).gameObject;

        itemDictionary = FindFirstObjectByType<ItemDictionary>();
        if (chestPanel != null) chestPanel.SetActive(false);
    }

    void Start() { BuildSlots(); }
    private void OnDestroy() { if (Instance == this) Instance = null; }

    void Update()
    {
        if (chestPanel != null && chestPanel.activeSelf)
        {
            if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Escape)) CloseChest();
            else if (Input.GetKeyDown(KeyCode.Tab)) ToggleView();
        }
    }

    // ============================
    // MỞ / ĐÓNG HỆ THỐNG
    // ============================
    public void OpenChest(StorageChest chest, List<InventoryService.StorageItemDTO> preloadedItems)
    {
        if (chestPanel.activeSelf) return;
        if (!GameStateManager.CanProcessInput()) return;

        currentActiveChest = chest;
        IsViewingChest = true;

        ClearChestUI();

        // Nếu có Cache thì hiển thị ngay
        if (preloadedItems != null)
        {
            PopulateChestUI(preloadedItems);
        }
        else
        {
            // Fallback: Nếu không có cache, gọi API load lẻ
            RefreshChestContent();
        }

        CommonUIController.Instance.SetUIVisible(false, CommonUIController.Instance.hotBar);
        chestPanel.SetActive(true);

        // Mượn Inventory Panel (Code UI cũ giữ nguyên)
        if (inventoryPanel != null && pageContainer != null)
        {
            if (inventoryPanel.transform.parent != pageContainer)
            {
                originalInventoryParent = inventoryPanel.transform.parent;
                originalInventorySiblingIndex = inventoryPanel.transform.GetSiblingIndex();
            }
            inventoryPanel.transform.SetParent(pageContainer);
            RectTransform invRect = inventoryPanel.GetComponent<RectTransform>();
            invRect.localScale = Vector3.one;
            invRect.anchoredPosition = Vector2.zero;
        }

        UpdateViewMode();
        GameStateManager.IsMenuOpen = true;
        MenuStateManager.Instance.OpenMenu(null);
    }

    public void CloseChest()
    {
        currentActiveChest = null;
        if (chestPanel != null) chestPanel.SetActive(false);

        if (inventoryPanel != null && originalInventoryParent != null)
        {
            inventoryPanel.SetActive(false);
            inventoryPanel.transform.SetParent(originalInventoryParent);
            inventoryPanel.transform.SetSiblingIndex(originalInventorySiblingIndex);
        }

        EquipTooltip.Instance?.Hide();
        ConsumableTooltip.Instance?.Hide();
        NecklaceEquipTooltip.Instance?.Hide();

        MenuStateManager.Instance.CloseCurrentMenu();
        CommonUIController.Instance.SetUIVisible(true, CommonUIController.Instance.hotBar);
        InventoryController.Instance.ReBuildItemCounts();
    }

    // Hàm này giữ lại để tương thích SaveController nhưng để trống vì dữ liệu đã lên mây
    public void SyncDataIfOpen(StorageChest chest) { }

    // ============================
    // LOGIC CHUYỂN ĐỒ (ONLINE)
    // ============================

    public void OnItemDoubleClicked(Item item)
    {
        if (currentActiveChest == null) return;
        if (IsViewingChest) WithdrawItem(item);
        else DepositItem(item);
        InventoryController.Instance.RefreshInventory();
    }

    // --- RÚT ĐỒ (CHEST -> INVENTORY) ---
    private void WithdrawItem(Item item)
    {
        if (item == null) return;
        Slot targetSlot = FindEmptySlot(inventoryPanel.transform);
        if (targetSlot == null) { ShowErrorMessage("Túi đồ đã đầy!"); return; }

        int targetSlotIndex = targetSlot.transform.GetSiblingIndex();
        int itemDbId = item.dbID;
        bool isStackable = item.IsStackable;

        item.GetComponent<CanvasGroup>().alpha = 0.5f;
        item.GetComponent<CanvasGroup>().blocksRaycasts = false;

        InventoryService.Instance.RequestWithdraw(itemDbId, targetSlotIndex, isStackable, (success) =>
        {
            if (success)
            {
                if (item != null && item.gameObject != null) Destroy(item.gameObject);
                if (InventoryController.Instance != null) InventoryController.Instance.RefreshInventory();

                // Refresh lại rương để lấy data mới
                RefreshChestContent();
            }
            else
            {
                ShowErrorMessage("Lỗi kết nối Server!");
                if (item != null && item.gameObject != null)
                {
                    item.GetComponent<CanvasGroup>().alpha = 1f;
                    item.GetComponent<CanvasGroup>().blocksRaycasts = true;
                }
            }
        });
    }

    private void DepositItem(Item item)
    {
        if (item == null) return;
        if (item.dbID == 0) { ShowErrorMessage("Lỗi dữ liệu item!"); return; }
        if (item.itemType == ItemType.QuestItem || item.isEquipped) { ShowErrorMessage("Không thể cất!"); return; }

        Slot targetSlot = FindEmptySlot(storageChestPage.transform);
        if (targetSlot == null) { ShowErrorMessage("Rương đầy!"); return; }

        int targetSlotIndex = targetSlot.transform.GetSiblingIndex();
        int itemDbId = item.dbID;
        string chestId = currentActiveChest.chestID;
        bool isStackable = item.IsStackable;

        item.GetComponent<CanvasGroup>().alpha = 0.5f;
        item.GetComponent<CanvasGroup>().blocksRaycasts = false;

        InventoryService.Instance.RequestDeposit(itemDbId, chestId, targetSlotIndex, isStackable, (success) =>
        {
            if (success)
            {
                if (item != null && item.gameObject != null) Destroy(item.gameObject);
                if (InventoryController.Instance != null) InventoryController.Instance.RefreshInventory();

                // Refresh lại rương
                RefreshChestContent();
            }
            else
            {
                ShowErrorMessage("Lỗi kết nối Server!");
                if (item != null && item.gameObject != null)
                {
                    item.GetComponent<CanvasGroup>().alpha = 1f;
                    item.GetComponent<CanvasGroup>().blocksRaycasts = true;
                }
            }
        });
    }

    // ============================
    // UI HELPERS
    // ============================

    private Slot FindEmptySlot(Transform container)
    {
        foreach (Transform t in container)
        {
            Slot s = t.GetComponent<Slot>();
            if (s != null && s.currentItem == null && t.childCount == 0) return s;
        }
        return null;
    }

    private void PopulateChestUI(List<InventoryService.StorageItemDTO> items)
    {
        if (storageChestPage == null) return;
        ClearChestUI();

        if (items == null) return;

        foreach (var data in items)
        {
            if (data.slotIndex >= slotCount) continue;

            Slot slot = storageChestPage.transform.GetChild(data.slotIndex).GetComponent<Slot>();
            GameObject prefab = itemDictionary.GetItemPrefab(data.itemId);

            if (prefab != null)
            {
                GameObject itemObj = Instantiate(prefab, slot.transform);
                itemObj.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

                Item item = itemObj.GetComponent<Item>();

                // --- QUAN TRỌNG: Mapping dữ liệu mới ---
                item.dbID = data.id; // Đây là ID của bảng StorageItems
                item.quantity = data.quantity;
                item.rarity = (ItemRarity)data.rarity;
                item.qualityFactor = data.qualityFactor;
                item.isEquipped = false;
                item.UpdateQuantityDisplay();

                slot.currentItem = itemObj;
            }
        }
    }

    private void BuildSlots()
    {
        if (storageChestPage == null) return;
        foreach (Transform child in storageChestPage.transform) Destroy(child.gameObject);
        for (int i = 0; i < slotCount; i++) Instantiate(slotPrefab, storageChestPage.transform);
    }

    private void ClearChestUI()
    {
        if (storageChestPage == null) return;
        foreach (Transform child in storageChestPage.transform)
        {
            Slot s = child.GetComponent<Slot>();
            if (s != null)
            {
                if (s.currentItem != null) Destroy(s.currentItem);
                s.currentItem = null;
            }
            // Fallback nếu item ko được gán vào currentItem
            if (child.childCount > 0)
            {
                foreach (Transform grandChild in child) Destroy(grandChild.gameObject);
            }
        }
    }

    public void RefreshChestContent()
    {
        if (currentActiveChest == null) return;
        string requestingId = currentActiveChest.chestID;

        // Gọi API load lẻ 1 rương (để lấy ID mới nhất sau khi transaction)
        InventoryService.Instance.RequestLoadSingleChest(requestingId, (items) =>
        {
            if (this == null || currentActiveChest == null) return;
            if (currentActiveChest.chestID != requestingId) return;

            // items lúc này là List<StorageItemDTO>
            PopulateChestUI(items);
            currentActiveChest.SetCache(items);
        });
    }

    // --- TAB SWITCHING ---
    public void SwitchToChestView() { if (!IsViewingChest) { IsViewingChest = true; UpdateViewMode(); } }
    public void SwitchToInventoryView() { if (IsViewingChest) { IsViewingChest = false; UpdateViewMode(); } }
    public void ToggleView() { IsViewingChest = !IsViewingChest; UpdateViewMode(); }

    private void UpdateViewMode()
    {
        if (storageChestPage != null) storageChestPage.SetActive(IsViewingChest);
        if (inventoryPanel != null) inventoryPanel.SetActive(!IsViewingChest);

        if (chestTabImage != null) chestTabImage.color = IsViewingChest ? activeTabColor : inactiveTabColor;
        if (inventoryTabImage != null) inventoryTabImage.color = !IsViewingChest ? activeTabColor : inactiveTabColor;
    }

    private void ShowErrorMessage(string message)
    {
        if (LoadResourceManager.Instance != null && LoadResourceManager.Instance.NotifyUIPrefab != null)
            Instantiate(LoadResourceManager.Instance.NotifyUIPrefab).GetComponent<NotifyUIController>()?.Show(message);
        else Debug.LogWarning(message);
    }
}