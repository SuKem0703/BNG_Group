using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
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

    private static float nextEquipTime = 0f;
    private const float EQUIP_COOLDOWN = 0.2f;

    private PlayerStats playerStats
    {
        get
        {
            if (NetworkManager.Singleton != null &&
                NetworkManager.Singleton.IsConnectedClient &&
                NetworkManager.Singleton.LocalClient.PlayerObject != null)
            {
                LocalPlayerAdapter localAdapter = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<LocalPlayerAdapter>();
                if (localAdapter != null)
                {
                    return localAdapter.playerStats;
                }
            }

            LocalPlayerAdapter[] allAdapters = Object.FindObjectsByType<LocalPlayerAdapter>(FindObjectsSortMode.None);
            foreach (var adapter in allAdapters)
            {
                if (adapter.IsOwner || NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
                {
                    return adapter.playerStats;
                }
            }

            return null;
        }
    }

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

        Item draggedItem = GetComponent<Item>();

        if (draggedItem != null && draggedItem.isDisplayOnly)
        {
            eventData.pointerDrag = null;
            return;
        }

        Slot parentSlot = transform.parent.GetComponent<Slot>();

        if (parentSlot != null && parentSlot.isShopSlot)
        {
            eventData.pointerDrag = null;
            return;
        }

        if (parentSlot != null && parentSlot.isEquipmentSlot && draggedItem is EquipmentItem eq && eq.isEquipped)
        {
            eventData.pointerDrag = null;
            return;
        }

        if (draggedItem != null && draggedItem.dbID == 0)
        {
            Debug.LogWarning("Đang đồng bộ dữ liệu vật phẩm, vui lòng chờ...");
            eventData.pointerDrag = null;
            return;
        }

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

        // --- XỬ LÝ VỨT ĐỒ / TRỒNG CÂY ---
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
                        var ramData = InventoryController.Instance.GetInventoryItemsData().Find(x => x.dbID == draggedItem.dbID);
                        if (ramData != null) ramData.quantity = draggedItem.quantity;

                        if (InventoryService.Instance != null)
                            InventoryService.Instance.ScheduleQuantityUpdate(draggedItem.dbID, draggedItem.quantity);

                        if (draggedItem.quantity <= 0)
                        {
                            originalSlot.currentItem = null;
                            InventoryController.Instance.GetInventoryItemsData().RemoveAll(x => x.dbID == draggedItem.dbID);
                            Destroy(gameObject);
                        }
                        else SnapBack();

                        InventoryController.Instance.ReBuildItemCounts();
                        return;
                    }
                }

                bool isEquipped = draggedItem is EquipmentItem eq && eq.isEquipped;
                bool isNeededForQuest = QuestController.Instance != null && QuestController.Instance.IsItemNeededForActiveQuest(draggedItem.ID);

                if (isEquipped || draggedItem is QuestItem || isNeededForQuest)
                {
                    GameNotify.Show("Không thể vứt bỏ vật phẩm này!");
                    SnapBack();
                    return;
                }
                RequestDropItemConfirmation(originalSlot, draggedItem, eventData.position);
            }
            else SnapBack();
            return;
        }

        if (dropSlot.isShopSlot) { SnapBack(); return; }

        // --- XỬ LÝ DI CHUYỂN / SWAP ---
        if (dropSlot.isHotBarSlot && (draggedItem is EquipmentItem || draggedItem is QuestItem)) { SnapBack(); return; }

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

        Item targetItem = dropSlot.currentItem != null ? dropSlot.currentItem.GetComponent<Item>() : null;

        // Cộng dồn Stack
        if (targetItem != null && draggedItem.ID == targetItem.ID && draggedItem.IsStackable)
        {
            targetItem.AddToStack(draggedItem.quantity);
            originalSlot.currentItem = null;

            var invData = InventoryController.Instance.GetInventoryItemsData();
            var targetRam = invData.Find(x => x.dbID == targetItem.dbID);
            if (targetRam != null) targetRam.quantity = targetItem.quantity;
            invData.RemoveAll(x => x.dbID == draggedItem.dbID);

            Destroy(gameObject);
            InventoryController.Instance.ReBuildItemCounts();

            InventoryService.Instance.ScheduleQuantityUpdate(targetItem.dbID, targetItem.quantity);
            InventoryService.Instance.ScheduleMoveItem(draggedItem.dbID, GetGlobalSlotIndex(dropSlot)); // Để Server xử lý xóa/gộp
            return;
        }

        // Đổi chỗ (Swap)
        if (dropSlot.currentItem != null)
        {
            Item swappedItem = dropSlot.currentItem.GetComponent<Item>();
            if (swappedItem != null && swappedItem.dbID == 0)
            {
                GameNotify.Show("Vị trí này đang đồng bộ dữ liệu, vui lòng chờ!");
                SnapBack();
                return;
            }

            dropSlot.currentItem.transform.SetParent(originalSlot.transform);
            originalSlot.currentItem = dropSlot.currentItem;
            dropSlot.currentItem.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

            if (swappedItem != null)
            {
                var swapData = InventoryController.Instance.GetInventoryItemsData().Find(x => x.dbID == swappedItem.dbID);
                if (swapData != null)
                {
                    swapData.slotIndex = GetGlobalSlotIndex(originalSlot);
                    if (swappedItem is EquipmentItem swEq)
                    {
                        swEq.isEquipped = originalSlot.isEquipmentSlot;
                        swapData.isEquipped = swEq.isEquipped;
                    }
                    InventoryService.Instance.ScheduleMoveItem(swappedItem.dbID, swapData.slotIndex);
                }
            }
        }
        else { originalSlot.currentItem = null; }

        // Cập nhật Item đang kéo
        transform.SetParent(dropSlot.transform);
        dropSlot.currentItem = gameObject;
        GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

        var draggedData = InventoryController.Instance.GetInventoryItemsData().Find(x => x.dbID == draggedItem.dbID);
        if (draggedData != null)
        {
            draggedData.slotIndex = GetGlobalSlotIndex(dropSlot);
            if (draggedItem is EquipmentItem eq)
            {
                eq.isEquipped = dropSlot.isEquipmentSlot;
                draggedData.isEquipped = eq.isEquipped;
            }
            InventoryService.Instance.ScheduleMoveItem(draggedItem.dbID, draggedData.slotIndex);
        }

        playerStats?.ApplyEquippedItems();
        InventoryController.Instance.ReBuildItemCounts();

        if (StorageChestController.Instance != null && StorageChestController.Instance.IsViewingChest)
        {
            if (StorageChestController.Instance.storageChestPage != null && dropSlot.transform.IsChildOf(StorageChestController.Instance.storageChestPage.transform))
                StorageChestController.Instance.StartCoroutine(SyncChestAfterMoveDelay());
        }

        if (TooltipManager.Instance != null) TooltipManager.Instance.gameObject.SetActive(true);
    }

    private IEnumerator SyncChestAfterMoveDelay()
    {
        yield return new WaitForSeconds(0.2f);
        StorageChestController.Instance.RefreshChestContent();
    }

    private FarmPlot GetFarmPlotAtMouse(PointerEventData eventData)
    {
        Vector2 worldPos = Camera.main.ScreenToWorldPoint(eventData.position);

        Collider2D[] colliders = Physics2D.OverlapPointAll(worldPos);

        foreach (var col in colliders)
        {
            if (col == null || col.CompareTag("PlayerController")) continue;

            FarmPlot plot = col.GetComponent<FarmPlot>();
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
        if (slot.isHotBarSlot) return slot.transform.GetSiblingIndex() + 1000;

        if (slot.isEquipmentSlot)
        {
            if (slot.classRestriction == ClassRestriction.Knight) return slot.transform.GetSiblingIndex() + 2000;
            if (slot.classRestriction == ClassRestriction.Mage) return slot.transform.GetSiblingIndex() + 2100;
            return slot.transform.GetSiblingIndex() + 2200;
        }

        return slot.transform.GetSiblingIndex();
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

                InventoryController.Instance.GetInventoryItemsData().RemoveAll(x => x.dbID == itemDbIdToRemove);

                Transform playerTransform = playerStats != null ? playerStats.transform : null;
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

            Item thisItem = GetComponent<Item>();
            if (thisItem == null) return;

            if (thisItem.dbID == 0)
            {
                Debug.LogWarning("Vật phẩm đang được đồng bộ với Server, vui lòng đợi giây lát rồi thử lại.");
                GameNotify.Show("Đang đồng bộ...");
                return;
            }

            // Tương tác với Rương đồ
            if (StorageChestController.Instance != null && StorageChestController.Instance.chestPanel.activeSelf)
            {
                StorageChestController.Instance.OnItemDoubleClicked(thisItem);
                lastClickTime = 0;
                return;
            }

            // Tương tác nhanh với Trang bị (Mặc / Tháo)
            if (thisItem is EquipmentItem eqItem)
            {
                if (eqItem.isDisplayOnly)
                {
                    TryEquipViaDoubleClick(eqItem);
                    lastClickTime = 0;
                    return;
                }
                else if (eqItem.isEquipped)
                {
                    TryUnequipViaDoubleClick(eqItem);
                    lastClickTime = 0;
                    return;
                }
            }
        }

        lastClickTime = Time.time;
    }

    private void TryEquipViaDoubleClick(EquipmentItem sourceEqItem)
    {
        if (Time.time < nextEquipTime) return;
        nextEquipTime = Time.time + EQUIP_COOLDOWN;

        if (playerStats != null && playerStats.level < sourceEqItem.requiredLevel)
        {
            GameNotify.Show("Chưa đủ cấp độ!"); return;
        }

        Slot targetSlot = Object.FindObjectsByType<Slot>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(s => s.isEquipmentSlot && s.acceptedEquipSlot == sourceEqItem.equipSlot &&
            (s.classRestriction == ClassRestriction.None || s.classRestriction == sourceEqItem.classRestriction) && s.gameObject.scene.IsValid());

        if (targetSlot == null) return;

        var invData = InventoryController.Instance.GetInventoryItemsData();
        var sourceRamData = invData.Find(x => x.dbID == sourceEqItem.dbID);
        if (sourceRamData == null) return;

        int targetGlobalIndex = GetGlobalSlotIndex(targetSlot);
        int originalIdx = sourceRamData.slotIndex;

        // Swap nếu có đồ cũ
        if (targetSlot.currentItem != null)
        {
            Item equippedItem = targetSlot.currentItem.GetComponent<Item>();
            var equippedRam = invData.Find(x => x.dbID == equippedItem.dbID);
            if (equippedRam != null)
            {
                equippedRam.slotIndex = originalIdx;
                equippedRam.isEquipped = false;
                InventoryService.Instance.ScheduleMoveItem(equippedRam.dbID, originalIdx);
            }
            Destroy(targetSlot.currentItem);
        }

        sourceRamData.slotIndex = targetGlobalIndex;
        sourceRamData.isEquipped = true;

        // Tạo Visual trên ô trang bị
        GameObject prefab = ItemDictionary.Instance.GetItemPrefab(sourceEqItem.ID);
        if (prefab != null)
        {
            GameObject newObj = Instantiate(prefab, targetSlot.transform);
            newObj.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
            EquipmentItem newEq = newObj.GetComponent<EquipmentItem>();
            newEq.dbID = sourceEqItem.dbID;
            newEq.quantity = sourceEqItem.quantity;
            newEq.rarity = sourceEqItem.rarity;
            newEq.qualityFactor = sourceEqItem.qualityFactor;
            newEq.isEquipped = true;
            newEq.isDisplayOnly = false;
            newEq.UpdateQuantityDisplay();
            targetSlot.currentItem = newObj;
        }

        InventoryService.Instance.ScheduleMoveItem(sourceEqItem.dbID, targetGlobalIndex);
        playerStats?.ApplyEquippedItems();
        InventoryController.Instance.ReBuildItemCounts();
    }

    private void TryUnequipViaDoubleClick(EquipmentItem equippedItem)
    {
        if (Time.time < nextEquipTime) return;
        nextEquipTime = Time.time + EQUIP_COOLDOWN;

        var invData = InventoryController.Instance.GetInventoryItemsData();
        var equippedRamData = invData.Find(x => x.dbID == equippedItem.dbID);
        if (equippedRamData == null) return;

        var occupied = invData.Where(x => x.slotIndex < 1000).Select(x => x.slotIndex).ToHashSet();
        int emptySlot = -1;
        for (int i = 0; i < InventoryController.Instance.slotCount; i++)
            if (!occupied.Contains(i)) { emptySlot = i; break; }

        if (emptySlot == -1) { GameNotify.Show("Túi đầy!"); return; }

        Slot parentSlot = equippedItem.transform.parent?.GetComponent<Slot>();
        if (parentSlot != null) parentSlot.currentItem = null;
        Destroy(equippedItem.gameObject);

        equippedRamData.slotIndex = emptySlot;
        equippedRamData.isEquipped = false;

        InventoryService.Instance.ScheduleMoveItem(equippedItem.dbID, emptySlot);
        playerStats?.ApplyEquippedItems();
        InventoryController.Instance.ReBuildItemCounts();
    }
}