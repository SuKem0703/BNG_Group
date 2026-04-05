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
        if (!AntiSpam.CanPerformAction())
        {
            eventData.pointerDrag = null;
            return;
        }

        Slot parentSlot = transform.parent.GetComponent<Slot>();
        if (parentSlot != null && (parentSlot.isEquipmentSlot || parentSlot.isShopSlot)) return;

        Item draggedItem = GetComponent<Item>();
        if (draggedItem != null && draggedItem.dbID == 0)
        {
            Debug.LogWarning("Đang đồng bộ dữ liệu vật phẩm, vui lòng chờ...");
            return;
        }

        // [CẬP NHẬT] Type checking Hạt giống
        if (draggedItem is SeedItem && LoadResourceManager.Instance.SelectionBoxPrefab != null)
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

        if (dropSlot == originalSlot) { SnapBack(); return; }

        // 1. Vứt đồ ra ngoài
        if (dropSlot == null)
        {
            if (!IsWithinInventory(eventData.position))
            {
                FarmPlot plot = GetFarmPlotAtMouse(eventData);
                if (plot != null && draggedItem is SeedItem seedItem)
                {
                    if (InteractionDetector.Instance != null && InteractionDetector.Instance.IsPlotInRange(plot))
                    {
                        FarmController.Instance.TryPlantSeed(plot, seedItem);
                        if (InventoryService.Instance != null) InventoryService.Instance.RequestUpdateQuantity(draggedItem.dbID, draggedItem.quantity);

                        if (draggedItem.quantity <= 0)
                        {
                            originalSlot.currentItem = null;
                            Destroy(gameObject);
                        }
                        else SnapBack();

                        return;
                    }
                }

                bool isEquipped = draggedItem is EquipmentItem eq && eq.isEquipped;
                if (isEquipped || draggedItem is QuestItem || QuestController.Instance.IsItemNeededForActiveQuest(draggedItem.ID))
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

        if (dropSlot.isShopSlot) { SnapBack(); return; }

        if (dropSlot.isHotBarSlot)
        {
            if (draggedItem is EquipmentItem || draggedItem is QuestItem)
            {
                SnapBack(); return;
            }
        }

        // --- LOGIC EQUIP (Trang bị) ---
        if (dropSlot.isEquipmentSlot)
        {
            if (draggedItem is not EquipmentItem equipItem ||
                equipItem.equipSlot != dropSlot.acceptedEquipSlot ||
                (dropSlot.classRestriction != ClassRestriction.None && equipItem.classRestriction != dropSlot.classRestriction) ||
                (playerStats != null && playerStats.level < equipItem.requiredLevel))
            {
                SnapBack(); return;
            }
        }

        bool isStackable = draggedItem.IsStackable;
        Item targetItem = dropSlot.currentItem != null ? dropSlot.currentItem.GetComponent<Item>() : null;

        if (targetItem != null && draggedItem.ID == targetItem.ID && draggedItem is not EquipmentItem)
        {
            targetItem.AddToStack(draggedItem.quantity);
            originalSlot.currentItem = null;
            Destroy(gameObject);

            InventoryService.Instance.RequestMoveItem(draggedItem.dbID, GetGlobalSlotIndex(dropSlot), isStackable, (success) =>
            {
                if (success && StorageChestController.Instance != null && StorageChestController.Instance.IsViewingChest)
                {
                    if (dropSlot.transform.IsChildOf(StorageChestController.Instance.storageChestPage.transform))
                        StorageChestController.Instance.RefreshChestContent();
                }
            });
            return;
        }

        // --- Swap UI ---
        if (dropSlot.currentItem != null)
        {
            dropSlot.currentItem.transform.SetParent(originalSlot.transform);
            originalSlot.currentItem = dropSlot.currentItem;
            dropSlot.currentItem.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

            // Đồng bộ isEquipped cho item bị swap ra ngoài
            if (dropSlot.currentItem.GetComponent<Item>() is EquipmentItem targetEquip)
            {
                targetEquip.isEquipped = originalSlot.isEquipmentSlot;
                if (targetEquip.sourceItem is EquipmentItem srcEq) srcEq.isEquipped = targetEquip.isEquipped;
            }
        }
        else
        {
            originalSlot.currentItem = null;
        }

        transform.SetParent(dropSlot.transform);
        dropSlot.currentItem = gameObject;
        GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

        // --- GỌI API CẬP NHẬT DB ---
        if (dropSlot.isEquipmentSlot && draggedItem is EquipmentItem equipToWear)
        {
            UpdateAllEquipmentItems(equipToWear);

            equipToWear.isEquipped = true;
            if (equipToWear.sourceItem is EquipmentItem srcEqWear) srcEqWear.isEquipped = true;

            InventoryService.Instance.RequestEquip(equipToWear.dbID, true);
            playerStats.ApplyEquippedItems();
        }
        else if (originalSlot.isEquipmentSlot && draggedItem is EquipmentItem equipToTakeOff)
        {
            equipToTakeOff.isEquipped = false;
            if (equipToTakeOff.sourceItem is EquipmentItem srcEqTakeOff) srcEqTakeOff.isEquipped = false;

            InventoryService.Instance.RequestEquip(equipToTakeOff.dbID, false);
            playerStats.ApplyEquippedItems();
        }
        else
        {
            InventoryService.Instance.RequestMoveItem(draggedItem.dbID, GetGlobalSlotIndex(dropSlot), isStackable);

            if (StorageChestController.Instance != null && StorageChestController.Instance.IsViewingChest)
            {
                if (dropSlot.transform.IsChildOf(StorageChestController.Instance.storageChestPage.transform))
                    StorageChestController.Instance.StartCoroutine(SyncChestAfterMoveDelay());
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

    private void UpdateAllEquipmentItems(EquipmentItem draggedItem)
    {
        foreach (Transform slotTransform in inventoryController.inventoryPanel.transform)
        {
            Slot slot = slotTransform.GetComponent<Slot>();
            if (slot != null && slot.currentItem != null)
            {
                if (slot.currentItem.GetComponent<Item>() is EquipmentItem eqItem
                    && eqItem != draggedItem
                    && eqItem.isEquipped
                    && eqItem.classRestriction == draggedItem.classRestriction
                    && eqItem.equipSlot == draggedItem.equipSlot)
                {
                    eqItem.isEquipped = false;
                    if (eqItem.sourceItem is EquipmentItem srcEq) srcEq.isEquipped = false;

                    InventoryService.Instance.RequestEquip(eqItem.dbID, false);
                    Debug.Log($"Đã bỏ trang bị: {eqItem.Name}");
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
            SnapBack(); return;
        }

        bool isEquipped = item is EquipmentItem eq && eq.isEquipped;
        if (item.isDisplayOnly || isEquipped)
        {
            SnapBack(); return;
        }

        int dropQuantity = item.quantity;
        int itemDbIdToRemove = item.dbID;

        if (canvasGroup != null) canvasGroup.blocksRaycasts = false;

        InventoryService.Instance.RequestRemoveItem(itemDbIdToRemove, (success) =>
        {
            if (success)
            {
                originalSlot.currentItem = null;

                Transform playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
                if (playerTransform != null)
                {
                    Vector2 playerPosition = (Vector2)playerTransform.position;
                    Vector2 mouseWorldPosition = Camera.main.ScreenToWorldPoint(dragEndMousePosition);
                    Vector2 dropDirection = (mouseWorldPosition - playerPosition).normalized;
                    if (dropDirection == Vector2.zero) dropDirection = Vector2.up;
                    float randomDistance = Random.Range(minDropDistance, maxDropDistance);
                    Vector2 dropPosition = playerPosition + (dropDirection * randomDistance);

                    GameObject dropItem = Instantiate(gameObject, dropPosition, Quaternion.identity);
                    Item droppedItem = dropItem.GetComponent<Item>();

                    if (droppedItem is EquipmentItem dropEq) dropEq.isEquipped = false;
                    droppedItem.isDisplayOnly = false;
                    droppedItem.dbID = 0;
                    droppedItem.quantity = dropQuantity;
                    droppedItem.UpdateQuantityDisplay();

                    dropItem.GetComponent<BounceEffect>()?.StartBounce();
                }

                Destroy(gameObject);
                InventoryController.Instance.ReBuildItemCounts();
            }
            else
            {
                Debug.LogWarning("Vứt đồ thất bại do dữ liệu không tồn tại trên Server.");
                GameNotify.Show("Không thể vứt đồ! Dữ liệu bị lệch.");

                SnapBack();

                InventoryController.Instance.RefreshInventory();
            }
        });
    }

    float lastClickTime = 0f;
    float doubleClickThreshold = 0.3f;

    public void OnPointerClick(PointerEventData eventData)
    {

        if (Time.time - lastClickTime < doubleClickThreshold)
        {
            if (!AntiSpam.CanPerformAction()) return;

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
}