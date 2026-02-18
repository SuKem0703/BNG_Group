using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ItemDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    Transform originalParent;
    CanvasGroup canvasGroup;

    public float minDropDistance = 2f;
    public float maxDropDistance = 3f;
    [SerializeField] private Slot originalSlot;
    [SerializeField] private InventoryController inventoryController;

    [Header("Selection Highlight")]
    private GameObject currentSelectionBox;
    private SpriteRenderer selectionBoxRenderer;

    private Color invalidColor = new Color(1, 0, 0, 0.5f);
    private PlayerStats playerStats => GameObject.FindGameObjectWithTag("PlayerController").GetComponent<PlayerStats>();
    private KnightEquipmentPanel knightEquipmentPanel => Object.FindFirstObjectByType<KnightEquipmentPanel>();
    private MageEquipmentPanel mageEquipmentPanel => Object.FindFirstObjectByType<MageEquipmentPanel>();

    void Start()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        inventoryController = InventoryController.Instance;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        Slot parentSlot = transform.parent.GetComponent<Slot>();
        if (parentSlot != null && (parentSlot.isEquipmentSlot || parentSlot.isShopSlot)) return;

        // Selection box logic
        Item draggedItem = GetComponent<Item>();
        if (draggedItem != null && draggedItem.dbID == 0)
        {
            Debug.LogWarning("Đang đồng bộ dữ liệu vật phẩm, vui lòng chờ...");
            return;
        }

        if (draggedItem != null && draggedItem.itemType == ItemType.Seed && LoadResourceManager.Instance.SelectionBoxPrefab != null)
        {
            currentSelectionBox = Instantiate(LoadResourceManager.Instance.SelectionBoxPrefab);
            selectionBoxRenderer = currentSelectionBox.GetComponent<SpriteRenderer>();
            currentSelectionBox.SetActive(false);
        }

        originalParent = transform.parent;
        originalSlot = originalParent.GetComponent<Slot>();

        Canvas mainCanvas = GetComponentInParent<Canvas>();
        if (mainCanvas != null)
        {
            transform.SetParent(mainCanvas.rootCanvas != null ? mainCanvas.rootCanvas.transform : mainCanvas.transform, true);
        }

        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.6f;
        TooltipManager.Instance.gameObject.SetActive(false);
    }

    public void OnDrag(PointerEventData eventData)
    {
        Slot parentSlot = transform.parent.GetComponent<Slot>();
        if (parentSlot != null && (parentSlot.isEquipmentSlot || parentSlot.isShopSlot)) return;

        if (currentSelectionBox != null) UpdateSelectionBoxPosition(eventData);
        transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        Slot parentSlot = transform.parent.GetComponent<Slot>();
        if (parentSlot != null && (parentSlot.isEquipmentSlot || parentSlot.isShopSlot)) return;

        if (currentSelectionBox != null) { Destroy(currentSelectionBox); currentSelectionBox = null; }

        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;

        Slot dropSlot = eventData.pointerEnter?.GetComponent<Slot>();
        if (dropSlot == null)
        {
            GameObject dropItem = eventData.pointerEnter;
            if (dropItem != null) dropSlot = dropItem.GetComponentInParent<Slot>();
        }

        Item draggedItem = GetComponent<Item>();
        if (draggedItem == null) { SnapBack(); return; }

        // 1. Kéo vào chính nó -> Snapback
        if (dropSlot == originalSlot) { SnapBack(); return; }

        // 2. Kéo ra ngoài Inventory -> Vứt đồ
        if (dropSlot == null)
        {
            if (!IsWithinInventory(eventData.position))
            {
                // Logic gieo hạt
                FarmPlot plot = GetFarmPlotAtMouse(eventData);
                if (plot != null && draggedItem.itemType == ItemType.Seed)
                {
                    if (InteractionDetector.Instance != null && InteractionDetector.Instance.IsPlotInRange(plot))
                    {
                        FarmController.Instance.TryPlantSeed(plot, (SeedItem)draggedItem);

                        if (InventoryService.Instance != null)
                        {
                            InventoryService.Instance.RequestUpdateQuantity(draggedItem.dbID, draggedItem.quantity);
                        }

                        if (draggedItem.quantity <= 0)
                        {
                            originalSlot.currentItem = null;
                            Destroy(gameObject);
                        }
                        else
                        {
                            SnapBack();
                        }
                        return;
                    }
                }

                if (draggedItem.isEquipped || draggedItem.itemType == ItemType.QuestItem || QuestController.Instance.IsItemNeededForActiveQuest(draggedItem.ID))
                {
                    GameNotify.Show("Không thể vứt bỏ vật phẩm này!");
                    SnapBack();
                    return;
                }
                RequestDropItemConfirmation(originalSlot, draggedItem, eventData.position);
            }
            else SnapBack();
            TooltipManager.Instance.gameObject.SetActive(true);
            return;
        }

        // 3. Kéo vào Slot hợp lệ
        if (dropSlot.isShopSlot) { SnapBack(); return; }

        // --- LOGIC HOTBAR ---
        if (dropSlot.isHotBarSlot)
        {
            if (draggedItem.itemType == ItemType.Equipment || draggedItem.itemType == ItemType.QuestItem)
            {
                SnapBack(); return;
            }
        }

        // --- LOGIC EQUIP (Trang bị) ---
        if (dropSlot.isEquipmentSlot)
        {
            if (draggedItem.itemType == ItemType.QuestItem ||
                draggedItem.equipSlot != dropSlot.acceptedEquipSlot ||
                (dropSlot.classRestriction != ClassRestriction.None && draggedItem.classRestriction != dropSlot.classRestriction) ||
                (playerStats != null && playerStats.level < draggedItem.requiredLevel))
            {
                SnapBack(); return;
            }
        }

        // --- XỬ LÝ SWAP HOẶC MOVE ---
        bool isStackable = draggedItem.IsStackable;
        Item targetItem = dropSlot.currentItem != null ? dropSlot.currentItem.GetComponent<Item>() : null;

        // Logic Stack (Gộp)
        if (targetItem != null && draggedItem.ID == targetItem.ID && draggedItem.itemType != ItemType.Equipment)
        {
            // 1. Client Update (Visual - Mượt ngay lập tức)
            targetItem.AddToStack(draggedItem.quantity);
            originalSlot.currentItem = null;
            Destroy(gameObject);

            // 2. Gọi API Move (Kèm Callback)
            InventoryService.Instance.RequestMoveItem(
                draggedItem.dbID,
                GetGlobalSlotIndex(dropSlot),
                isStackable,
                (success) => 
                {
                    if (success)
                    {
                        if (StorageChestController.Instance != null && StorageChestController.Instance.IsViewingChest)
                        {
                            // Kiểm tra xem slot đích có nằm trong rương không
                            if (dropSlot.transform.IsChildOf(StorageChestController.Instance.storageChestPage.transform))
                            {
                                StorageChestController.Instance.RefreshChestContent();
                            }
                        }
                    }
                }
            );
            return;
        }

        // Thực hiện Swap UI
        if (dropSlot.currentItem != null)
        {
            // Move target về slot cũ
            dropSlot.currentItem.transform.SetParent(originalSlot.transform);
            originalSlot.currentItem = dropSlot.currentItem;
            dropSlot.currentItem.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        }
        else
        {
            originalSlot.currentItem = null;
        }

        // Move dragged đến slot mới
        transform.SetParent(dropSlot.transform);
        dropSlot.currentItem = gameObject;
        GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

        // --- GỌI API CẬP NHẬT DB ---

        // A. Xử lý Equip/Unequip
        if (dropSlot.isEquipmentSlot)
        {
            // Đeo vào -> Gọi Equip TRUE
            UpdateAllEquipmentItems(draggedItem);
            draggedItem.isEquipped = true;
            InventoryService.Instance.RequestEquip(draggedItem.dbID, true);
            playerStats.ApplyEquippedItems();
        }
        else if (originalSlot.isEquipmentSlot)
        {
            // Tháo ra -> Gọi Equip FALSE
            draggedItem.isEquipped = false;
            InventoryService.Instance.RequestEquip(draggedItem.dbID, false);
            playerStats.ApplyEquippedItems();
        }
        else
        {
            InventoryService.Instance.RequestMoveItem(draggedItem.dbID, GetGlobalSlotIndex(dropSlot), isStackable);

            if (StorageChestController.Instance != null && StorageChestController.Instance.IsViewingChest)
            {
                if (dropSlot.transform.IsChildOf(StorageChestController.Instance.storageChestPage.transform))
                {
                    StorageChestController.Instance.StartCoroutine(SyncChestAfterMoveDelay());
                }
            }
        }

        TooltipManager.Instance.gameObject.SetActive(true);
    }

    private IEnumerator SyncChestAfterMoveDelay()
    {
        yield return new WaitForSeconds(0.2f);
        StorageChestController.Instance.RefreshChestContent();
    }

    private FarmPlot GetFarmPlotAtMouse(PointerEventData eventData)
    {
        Vector2 worldPos = Camera.main.ScreenToWorldPoint(eventData.position);

        RaycastHit2D[] hits = Physics2D.RaycastAll(worldPos, Vector2.zero);

        foreach (var hit in hits)
        {
            if (hit.collider == null) continue;

            if (hit.collider.CompareTag("Player")) continue;

            FarmPlot plot = hit.collider.GetComponent<FarmPlot>();

            if (plot != null)
            {
                return plot;
            }
        }

        return null;
    }
    private void UpdateSelectionBoxPosition(PointerEventData eventData)
    {
        if (IsWithinInventory(eventData.position))
        {
            currentSelectionBox.SetActive(false);
            return;
        }

        FarmPlot plot = GetFarmPlotAtMouse(eventData);

        if (plot != null)
        {
            currentSelectionBox.SetActive(true);

            currentSelectionBox.transform.position = plot.transform.position;

            bool isPlotInRange = InteractionDetector.Instance != null && InteractionDetector.Instance.IsPlotInRange(plot);
            bool isPlanted = plot.isPlanted;

            if (isPlotInRange && !isPlanted)
            {
                selectionBoxRenderer.color = Color.white;
            }
            else
            {
                selectionBoxRenderer.color = invalidColor;
            }
        }
        else
        {
            currentSelectionBox.SetActive(false);
        }
    }
    // ========== CONFIRMATION & NOTIFY UI ==========
    //private void ShowDropErrorMessage(string message)
    //{
    //    GameObject notifyPrefab = LoadResourceManager.Instance.NotifyUIPrefab;

    //    if (notifyPrefab == null)
    //    {
    //        Debug.LogError("NotifyUIPrefab not loaded in LoadResourceManager. Không thể hiển thị thông báo. Canceling drop.");
    //        SnapBack();
    //        return;
    //    }

    //    GameObject notifyUIObj = Instantiate(notifyPrefab);
    //    NotifyUIController notifyUI = notifyUIObj.GetComponent<NotifyUIController>();

    //    if (notifyUI == null)
    //    {
    //        Debug.LogError("Prefab NotifyUICanvas thiếu script NotifyUIController!");
    //        Destroy(notifyUIObj);
    //        SnapBack();
    //        return;
    //    }

    //    notifyUI.Show(message);
    //}
    private void RequestDropItemConfirmation(Slot slotToEmpty, Item itemToDrop, Vector2 dragEndMousePosition)
    {
        GameObject confirmPrefab = LoadResourceManager.Instance.ConfirmUIPrefab;

        if (confirmPrefab == null)
        {
            Debug.LogError("ConfirmUIPrefab not loaded in LoadResourceManager. Canceling drop.");
            SnapBack();
            return;
        }

        GameStateManager.CanOpenMenu = false;

        GameObject confirmUIObj = Instantiate(confirmPrefab);
        ConfirmUIController confirmUI = confirmUIObj.GetComponent<ConfirmUIController>();

        if (confirmUI == null)
        {
            GameStateManager.CanOpenMenu = true;
            Debug.LogError("Prefab ConfirmUICanvas thiếu script ConfirmUIController!");
            Destroy(confirmUIObj);
            SnapBack();
            return;
        }

        UnityEngine.Events.UnityAction onYesAction = () => {
            GameStateManager.CanOpenMenu = true;
            DropItem(slotToEmpty, dragEndMousePosition);
        };

        UnityEngine.Events.UnityAction onNoAction = () => {
            GameStateManager.CanOpenMenu = true;
            SnapBack();
        };

        string itemName = itemToDrop.Name;
        string message = $"Bạn có chắc muốn vứt bỏ <color=yellow>{itemName}</color> (x{itemToDrop.quantity})?";

        confirmUI.Show(message, onYesAction);

        if (confirmUI.noButton != null)
        {
            confirmUI.noButton.onClick.AddListener(onNoAction);
        }
        else
        {
            GameStateManager.CanOpenMenu = true;
            Debug.LogError("Không tìm thấy 'noButton' trên ConfirmUI. Tự động SnapBack.");
            SnapBack();
        }
    }
    //private bool IsItemNeededForActiveQuest(int itemID)
    //{
    //    if (QuestController.Instance == null) return false;

    //    // Duyệt qua tất cả các Quest đang kích hoạt (active)
    //    foreach (QuestProgress quest in QuestController.Instance.activeQuests)
    //    {
    //        foreach (QuestObject obj in quest.questObjects)
    //        {
    //            // Nếu nhiệm vụ là CollectItem
    //            if (obj.objectType == ObjectType.CollectItem)
    //            {
    //                // Parse ID từ string sang int để so sánh
    //                if (int.TryParse(obj.objectID, out int questItemID))
    //                {
    //                    if (questItemID == itemID)
    //                    {
    //                        // Nếu tìm thấy item này trong 1 quest đang active
    //                        // Bạn có thể check thêm điều kiện obj.IsCompleted nếu muốn cho phép vứt khi đã gom đủ
    //                        // Nhưng an toàn nhất là không cho vứt khi chưa trả quest.
    //                        return true;
    //                    }
    //                }
    //            }
    //        }
    //    }
    //    return false;
    //}

    // ========== HELPER METHODS ==========
    private void SnapBack()
    {
        transform.SetParent(originalParent);
        GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        TooltipManager.Instance.gameObject.SetActive(true);
        playerStats.ApplyEquippedItems();
    }

    private int GetGlobalSlotIndex(Slot slot)
    {
        int index = slot.transform.GetSiblingIndex();
        if (slot.isHotBarSlot) index += 1000;
        return index;
    }

    private void UpdateAllEquipmentItems(Item draggedItem)
    {
        foreach (Transform slotTransform in inventoryController.inventoryPanel.transform)
        {
            Slot slot = slotTransform.GetComponent<Slot>();
            if (slot != null && slot.currentItem != null)
            {
                Item item = slot.currentItem.GetComponent<Item>();
                if (item != null
                    && item.itemType == draggedItem.itemType
                    && item.classRestriction == draggedItem.classRestriction
                    && item.equipSlot == draggedItem.equipSlot)
                {
                    item.isEquipped = false;
                    Debug.Log($"Đã bỏ trang bị: {item.Name}");
                }
            }
        }
        knightEquipmentPanel.UpdateWeaponStatus();
        mageEquipmentPanel.UpdateWeaponStatus();
    }

    bool IsWithinInventory(Vector2 mousePosition)
    {
        RectTransform inventoryRect = originalParent.parent.GetComponent<RectTransform>();
        return RectTransformUtility.RectangleContainsScreenPoint(inventoryRect, mousePosition);
    }
    void DropItem(Slot originalSlot, Vector2 dragEndMousePosition)
    {
        Item item = GetComponent<Item>();

        if (item == null || item.dbID == 0)
        {
            Debug.LogWarning("Item chưa đồng bộ, không thể vứt.");
            SnapBack();
            return;
        }

        if (item.isDisplayOnly || item.isEquipped)
        {
            SnapBack(); return;
        }

        int dropQuantity = item.quantity;
        int itemDbIdToRemove = item.dbID;

        originalSlot.currentItem = null;

        Transform playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (playerTransform == null) return;

        Vector2 playerPosition = (Vector2)playerTransform.position;
        Vector2 mouseScreenPosition = dragEndMousePosition;
        Vector2 mouseWorldPosition = Camera.main.ScreenToWorldPoint(mouseScreenPosition);
        Vector2 dropDirection = (mouseWorldPosition - playerPosition).normalized;
        if (dropDirection == Vector2.zero) dropDirection = Vector2.up;
        float randomDistance = Random.Range(minDropDistance, maxDropDistance);
        Vector2 dropPosition = playerPosition + (dropDirection * randomDistance);

        GameObject dropItem = Instantiate(gameObject, dropPosition, Quaternion.identity);
        Item droppedItem = dropItem.GetComponent<Item>();

        droppedItem.isEquipped = false;
        droppedItem.isDisplayOnly = false;
        droppedItem.dbID = 0;
        droppedItem.quantity = dropQuantity;
        droppedItem.UpdateQuantityDisplay();

        dropItem.GetComponent<BounceEffect>()?.StartBounce();

        InventoryService.Instance.RequestRemoveItem(itemDbIdToRemove);

        Destroy(gameObject);
        Debug.Log($"Đã vứt item: {item.Name} (ID: {itemDbIdToRemove})");

        InventoryController.Instance.ReBuildItemCounts();
    }

    float lastClickTime = 0f;
    float doubleClickThreshold = 0.3f;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (Time.time - lastClickTime < doubleClickThreshold)
        {
            if (StorageChestController.Instance != null &&
                StorageChestController.Instance.chestPanel.activeSelf)
            {
                Item thisItem = GetComponent<Item>();

                if (thisItem == null) return;

                if (thisItem.dbID == 0)
                {
                    Debug.LogWarning("Vật phẩm đang được đồng bộ với Server, vui lòng đợi giây lát rồi thử lại.");
                    GameNotify.Show("Đang đồng bộ...");
                    return;
                }

                StorageChestController.Instance.OnItemDoubleClicked(thisItem);

                lastClickTime = 0;
                return;
            }
        }

        lastClickTime = Time.time;
    }

    //public void OnPointerClick(PointerEventData eventData)
    //{
    //    if (eventData.button == PointerEventData.InputButton.Right)
    //    {
    //        DropItem(originalSlot);
    //    }
    //}
    //private void SplitStack()
    //{
    //    if (isFromShop) return; // 🚫 Không cho chia nếu là từ shop

    //    Item item = GetComponent<Item>();
    //    if (item == null || item.quantity <= 1 || item.itemType == ItemType.Equipment) return;

    //    int splitAmount = item.quantity / 2;
    //    if (splitAmount <= 0) return;

    //    item.RemoveFromStack(splitAmount);
    //    GameObject newItem = item.CloneItem(splitAmount);

    //    if (inventoryController == null || newItem == null) return;

    //    foreach (Transform slotTransform in inventoryController.inventoryPanel.transform)
    //    {
    //        Slot slot = slotTransform.GetComponent<Slot>();
    //        if (slot != null && slot.currentItem == null)
    //        {
    //            slot.currentItem = newItem;
    //            newItem.transform.SetParent(slot.transform);
    //            newItem.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
    //            return;
    //        }
    //    }

    //    //Không có Slot trống thì không Stack
    //    item.AddToStack(splitAmount);
    //    Destroy(newItem);
    //}
}