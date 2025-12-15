using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class StorageChestController : MonoBehaviour
{
    public static StorageChestController Instance { get; private set; }

    [Header("UI References")]
    public GameObject chestPanel;      // Root Panel (Khung)
    public Transform pageContainer;    // Container chứa nội dung (nằm dưới các nút)
    public GameObject storageChestPage;// Trang chứa Grid Slot của Rương

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

    // Khởi tạo Singleton và tìm kiếm các tham chiếu cần thiết
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (inventoryPanel == null)
        {
            var foundObj = Resources.FindObjectsOfTypeAll<GameObject>()
                .FirstOrDefault(x => x.name == "InventoryPage");

            if (foundObj != null)
            {
                inventoryPanel = foundObj;
                originalInventoryParent = inventoryPanel.transform.parent;
            }
        }

        if (chestPanel == null) chestPanel = GameObject.Find("StorageChestPanel");

        if (pageContainer == null && chestPanel != null)
        {
            Transform find = chestPanel.transform.Find("PageContainer");
            if (find != null) pageContainer = find;
        }

        if (storageChestPage == null && pageContainer != null)
        {
            if (pageContainer.childCount > 0)
                storageChestPage = pageContainer.GetChild(0).gameObject;
        }

        itemDictionary = FindFirstObjectByType<ItemDictionary>();

        if (chestPanel != null) chestPanel.SetActive(false);
    }

    // Xây dựng các slot rỗng ban đầu
    void Start()
    {
        BuildSlots();
    }

    // Hủy Singleton
    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
    void Update()
    {
        if (chestPanel != null && chestPanel.activeSelf)
        {
            if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Escape))
            {
                CloseChest();
            }
            else if (Input.GetKeyDown(KeyCode.Tab))
            {
                ToggleView();
            }
        }
    }

    // ============================
    // MỞ / ĐÓNG HỆ THỐNG
    // ============================

    // Mở giao diện rương, load dữ liệu và setup UI
    public void OpenChest(StorageChest chest)
    {
        if (chestPanel.activeSelf) return;
        if (!GameStateManager.CanProcessInput()) return;

        currentActiveChest = chest;
        IsViewingChest = true;

        ClearChestUI();
        PopulateChest(chest.chestData);
        CommonUIController.Instance.SetUIVisible(false, CommonUIController.Instance.hotBar);

        chestPanel.SetActive(true);

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
        CommonUIController.Instance.SetUIVisible(false, CommonUIController.Instance.hotBar);
    }

    // Đóng giao diện, lưu dữ liệu và trả Inventory về chỗ cũ
    public void CloseChest()
    {
        if (currentActiveChest != null) SyncDataToChest();
        currentActiveChest = null;

        if (chestPanel != null) chestPanel.SetActive(false);

        if (inventoryPanel != null && originalInventoryParent != null)
        {
            inventoryPanel.SetActive(false);
            inventoryPanel.transform.SetParent(originalInventoryParent);
            inventoryPanel.transform.SetSiblingIndex(originalInventorySiblingIndex);
        }

        MenuStateManager.Instance.CloseCurrentMenu();
    }

    // Hàm gọi từ bên ngoài để đồng bộ dữ liệu khi Save Game
    public void SyncDataIfOpen(StorageChest chest)
    {
        if (currentActiveChest == chest) SyncDataToChest();
    }

    // ============================
    // VIEW SWITCHING (TAB LOGIC) - CẬP NHẬT
    // ============================

    // Gắn hàm này vào sự kiện OnClick của nút "Tab Rương"
    public void SwitchToChestView()
    {
        // Chỉ chuyển nếu đang không ở chế độ xem Rương
        if (!IsViewingChest)
        {
            IsViewingChest = true;
            UpdateViewMode();

            // Play âm thanh chuyển tab nếu cần
            // SoundEffectManager.PlayVoice(openMenuSound); 
        }
    }

    // Gắn hàm này vào sự kiện OnClick của nút "Tab Túi"
    public void SwitchToInventoryView()
    {
        // Chỉ chuyển nếu đang ở chế độ xem Rương (tức là đang không xem Túi)
        if (IsViewingChest)
        {
            IsViewingChest = false;
            UpdateViewMode();

            // Play âm thanh chuyển tab nếu cần
            // SoundEffectManager.PlayVoice(openMenuSound);
        }
    }

    // Hàm Toggle cũ (giữ lại nếu muốn dùng phím tắt)
    public void ToggleView()
    {
        IsViewingChest = !IsViewingChest;
        UpdateViewMode();
    }

    private void UpdateViewMode()
    {
        if (IsViewingChest)
        {
            if (storageChestPage != null) storageChestPage.SetActive(true);
            if (inventoryPanel != null) inventoryPanel.SetActive(false);
        }
        else
        {
            if (storageChestPage != null) storageChestPage.SetActive(false);
            if (inventoryPanel != null)
            {
                inventoryPanel.SetActive(true);
                InventoryController.Instance.ReBuildItemCounts();
            }
        }

        if (chestTabImage != null)
            chestTabImage.color = IsViewingChest ? activeTabColor : inactiveTabColor;

        if (inventoryTabImage != null)
            inventoryTabImage.color = !IsViewingChest ? activeTabColor : inactiveTabColor;
    }

    // ============================
    // LOGIC CHUYỂN ĐỒ (TRANSFER)
    // ============================

    // Xử lý sự kiện Double Click vào Item
    public void OnItemDoubleClicked(Item item)
    {
        if (currentActiveChest == null) return;

        if (IsViewingChest) WithdrawItem(item);
        else DepositItem(item);
    }

    // Rút đồ từ Rương về Túi
    private void WithdrawItem(Item item)
    {
        GameObject prefab = itemDictionary.GetItemPrefab(item.ID);
        if (prefab == null) return;

        GameObject tempItem = Instantiate(prefab);
        Item tempScript = tempItem.GetComponent<Item>();

        tempScript.quantity = item.quantity;
        tempScript.rarity = item.rarity;
        tempScript.qualityFactor = item.qualityFactor;

        bool success = InventoryController.Instance.AddItem(tempItem);
        Destroy(tempItem);

        if (success)
        {
            Destroy(item.gameObject);
            SyncDataToChest();
        }
        else
        {
            ShowErrorMessage("Túi đồ đã đầy!");
        }
    }

    // Cất đồ từ Túi vào Rương
    private void DepositItem(Item item)
    {
        if (item.itemType == ItemType.QuestItem)
        {
            ShowErrorMessage("Không thể cất vật phẩm nhiệm vụ!");
            return;
        }

        if (item.isEquipped)
        {
            ShowErrorMessage("Vui lòng tháo trang bị trước khi cất!");
            return;
        }

        if (AddItemToChestInternal(item))
        {
            Destroy(item.gameObject);
            InventoryController.Instance.ReBuildItemCounts();
            SyncDataToChest();
        }
        else
        {
            Debug.Log("Rương đầy!");
        }
    }

    // Hiển thị thông báo lỗi sử dụng NotifyUI
    private void ShowErrorMessage(string message)
    {
        // Kiểm tra xem LoadResourceManager có tồn tại không
        if (LoadResourceManager.Instance == null || LoadResourceManager.Instance.NotifyUIPrefab == null)
        {
            Debug.LogWarning("Chưa load NotifyUIPrefab trong LoadResourceManager, dùng Debug.Log thay thế: " + message);
            return;
        }

        // Tạo thông báo từ Prefab
        GameObject notifyUIObj = Instantiate(LoadResourceManager.Instance.NotifyUIPrefab);
        NotifyUIController notifyUI = notifyUIObj.GetComponent<NotifyUIController>();

        if (notifyUI != null)
        {
            notifyUI.Show(message);
        }
        else
        {
            Destroy(notifyUIObj);
        }
    }

    // Logic nội bộ để thêm đồ vào Rương
    private bool AddItemToChestInternal(Item sourceItem)
    {
        if (storageChestPage == null) return false;

        // 1. Stack
        foreach (Transform t in storageChestPage.transform)
        {
            Slot s = t.GetComponent<Slot>();
            if (s.currentItem != null)
            {
                Item target = s.currentItem.GetComponent<Item>();
                if (target.ID == sourceItem.ID && target.itemType != ItemType.Equipment)
                {
                    target.quantity += sourceItem.quantity;
                    target.UpdateQuantityDisplay();
                    return true;
                }
            }
        }

        // 2. Empty Slot
        foreach (Transform t in storageChestPage.transform)
        {
            Slot s = t.GetComponent<Slot>();
            if (s.currentItem == null)
            {
                GameObject prefab = itemDictionary.GetItemPrefab(sourceItem.ID);
                GameObject newObj = Instantiate(prefab, s.transform);
                newObj.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

                Item newItem = newObj.GetComponent<Item>();
                newItem.quantity = sourceItem.quantity;
                newItem.rarity = sourceItem.rarity;
                newItem.qualityFactor = sourceItem.qualityFactor;
                newItem.isEquipped = false;
                newItem.UpdateQuantityDisplay();

                s.currentItem = newObj;
                return true;
            }
        }
        return false;
    }

    // ============================
    // CORE LOGIC (UI BUILDER)
    // ============================

    // Tạo các slot rỗng vào StorageChestPage
    private void BuildSlots()
    {
        if (storageChestPage == null) return;
        foreach (Transform child in storageChestPage.transform) Destroy(child.gameObject);
        for (int i = 0; i < slotCount; i++) Instantiate(slotPrefab, storageChestPage.transform);
    }

    // Xóa item visual trên UI Rương
    private void ClearChestUI()
    {
        if (storageChestPage == null) return;
        foreach (Transform slot in storageChestPage.transform)
        {
            Slot s = slot.GetComponent<Slot>();
            if (s.currentItem != null)
            {
                Destroy(s.currentItem);
                s.currentItem = null;
            }
        }
    }

    // Load dữ liệu từ Data vào UI
    private void PopulateChest(List<StorageChestSaveData> savedData)
    {
        if (savedData == null || storageChestPage == null) return;

        foreach (var data in savedData)
        {
            if (data.slotIndex >= slotCount) continue;

            Transform slotTf = storageChestPage.transform.GetChild(data.slotIndex);
            Slot slot = slotTf.GetComponent<Slot>();

            GameObject prefab = itemDictionary.GetItemPrefab(data.itemID);
            if (prefab == null) continue;

            GameObject itemObj = Instantiate(prefab, slot.transform);
            itemObj.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

            Item item = itemObj.GetComponent<Item>();
            item.quantity = data.quantity;
            item.rarity = data.rarity;
            item.qualityFactor = data.qualityFactor;
            if (item.quantity > 1) item.UpdateQuantityDisplay();

            slot.currentItem = itemObj;
        }
    }

    // Lưu dữ liệu từ UI vào Data
    private void SyncDataToChest()
    {
        if (currentActiveChest == null || storageChestPage == null) return;

        List<StorageChestSaveData> newData = new();

        for (int i = 0; i < storageChestPage.transform.childCount; i++)
        {
            Slot slot = storageChestPage.transform.GetChild(i).GetComponent<Slot>();
            if (slot.currentItem == null) continue;

            Item item = slot.currentItem.GetComponent<Item>();
            if (item == null) continue;

            newData.Add(new StorageChestSaveData
            {
                itemID = item.ID,
                slotIndex = i,
                quantity = item.quantity,
                rarity = item.rarity,
                qualityFactor = item.qualityFactor
            });
        }
        currentActiveChest.chestData = newData;
    }
}