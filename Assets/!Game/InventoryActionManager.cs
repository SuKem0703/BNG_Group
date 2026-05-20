using System.Collections;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;

public class InventoryActionManager : MonoBehaviour
{
    public static InventoryActionManager Instance { get; private set; }

    [Header("Cài đặt thả vật phẩm")]
    public float minDropDistance = 2f;
    public float maxDropDistance = 3f;

    private static float nextEquipTime = 0f;
    private const float EQUIP_COOLDOWN = 0.2f;

    public PlayerStats playerStats
    {
        get
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient && NetworkManager.Singleton.LocalClient.PlayerObject != null)
            {
                PlayerCore localAdapter = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerCore>();
                if (localAdapter != null) return localAdapter.playerStats;
            }

            PlayerCore[] allAdapters = Object.FindObjectsByType<PlayerCore>(FindObjectsSortMode.None);
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

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void ProcessDragDrop(ItemDragHandler dragHandler, PointerEventData eventData)
    {
        Item draggedItem = dragHandler.GetComponent<Item>();
        Slot originalSlot = dragHandler.originalSlot;

        Slot dropSlot = eventData.pointerEnter?.GetComponent<Slot>();
        if (dropSlot == null)
        {
            GameObject dropObj = eventData.pointerEnter;
            if (dropObj != null) dropSlot = dropObj.GetComponentInParent<Slot>();
        }

        if (draggedItem == null || dropSlot == originalSlot) { dragHandler.SnapBack(); return; }

        if (dropSlot == null)
        {
            RectTransform inventoryRect = dragHandler.originalParent.parent.GetComponent<RectTransform>();
            if (!IsWithinInventory(inventoryRect, eventData.position))
            {
                FarmPlot plot = GetFarmPlotAtMouse(eventData);

                if (plot != null)
                {
                    if (draggedItem is SeedItem seedItem)
                    {
                        if (seedItem.TryPlantSeed(plot))
                        {
                            if (draggedItem.quantity <= 0)
                            {
                                originalSlot.currentItem = null;
                                InventoryController.Instance.GetInventoryItemsData().RemoveAll(x => x.dbID == draggedItem.dbID);
                                Destroy(draggedItem.gameObject);
                            }
                            else dragHandler.SnapBack();

                            InventoryController.Instance.ReBuildItemCounts();
                            return;
                        }
                    }
                    else if (draggedItem is ItemTool toolItem)
                    {
                        toolItem.TryUseToolOnPlot(plot);
                        dragHandler.SnapBack();
                        return;
                    }
                }

                bool isEquipped = draggedItem is EquipmentItem eq && eq.isEquipped;
                bool isNeededForQuest = QuestController.Instance != null && QuestController.Instance.IsItemNeededForActiveQuest(draggedItem.ID);

                if (isEquipped || draggedItem is QuestItem || isNeededForQuest)
                {
                    GameNotify.Show("Không thể vứt bỏ vật phẩm này!");
                    dragHandler.SnapBack();
                    return;
                }
                RequestDropItemConfirmation(dragHandler, originalSlot, draggedItem, eventData.position);
            }
            else dragHandler.SnapBack();
            return;
        }

        if (dropSlot.isShopSlot) { dragHandler.SnapBack(); return; }
        if (dropSlot.isHotBarSlot && (draggedItem is EquipmentItem || draggedItem is QuestItem)) { dragHandler.SnapBack(); return; }

        if (dropSlot.isEquipmentSlot)
        {
            if (draggedItem is not EquipmentItem equipItem ||
                equipItem.equipSlot != dropSlot.acceptedEquipSlot ||
                (dropSlot.classRestriction != ClassRestriction.None && equipItem.classRestriction != dropSlot.classRestriction) ||
                (playerStats != null && playerStats.level < equipItem.requiredLevel))
            {
                dragHandler.SnapBack(); return;
            }
        }

        Item targetItem = dropSlot.currentItem != null ? dropSlot.currentItem.GetComponent<Item>() : null;

        if (targetItem != null && draggedItem.ID == targetItem.ID && draggedItem.IsStackable)
        {
            targetItem.AddToStack(draggedItem.quantity);
            originalSlot.currentItem = null;

            var invData = InventoryController.Instance.GetInventoryItemsData();
            var targetRam = invData.Find(x => x.dbID == targetItem.dbID);
            if (targetRam != null) targetRam.quantity = targetItem.quantity;
            invData.RemoveAll(x => x.dbID == draggedItem.dbID);

            Destroy(draggedItem.gameObject);
            InventoryController.Instance.ReBuildItemCounts();

            InventoryService.Instance.ScheduleQuantityUpdate(targetItem.dbID, targetItem.quantity);
            InventoryService.Instance.ScheduleMoveItem(draggedItem.dbID, GetGlobalSlotIndex(dropSlot));
            return;
        }

        int draggedID = draggedItem.ID;
        int draggedDbID = draggedItem.dbID;
        int draggedQuantity = draggedItem.quantity;
        ItemRarity draggedRarity = draggedItem.rarity;
        float draggedQuality = draggedItem.qualityFactor;
        ulong draggedOwner = draggedItem.ownerClientId;

        Item swappedItem = dropSlot.currentItem != null ? dropSlot.currentItem.GetComponent<Item>() : null;
        int swappedID = 0, swappedDbID = 0, swappedQuantity = 0;
        ItemRarity swappedRarity = ItemRarity.Common;
        float swappedQuality = 1f;
        ulong swappedOwner = 999;

        if (swappedItem != null)
        {
            if (swappedItem.dbID == 0)
            {
                GameNotify.Show("Vị trí này đang đồng bộ dữ liệu, vui lòng chờ!");
                dragHandler.SnapBack();
                return;
            }

            swappedID = swappedItem.ID;
            swappedDbID = swappedItem.dbID;
            swappedQuantity = swappedItem.quantity;
            swappedRarity = swappedItem.rarity;
            swappedQuality = swappedItem.qualityFactor;
            swappedOwner = swappedItem.ownerClientId;

            var swapData = InventoryController.Instance.GetInventoryItemsData().Find(x => x.dbID == swappedDbID);
            if (swapData != null)
            {
                swapData.slotIndex = GetGlobalSlotIndex(originalSlot);
                if (swappedItem is EquipmentItem swEq) { swapData.isEquipped = originalSlot.isEquipmentSlot; }
                InventoryService.Instance.ScheduleMoveItem(swappedDbID, swapData.slotIndex);
            }
        }
        else
        {
            originalSlot.currentItem = null;
        }

        var draggedData = InventoryController.Instance.GetInventoryItemsData().Find(x => x.dbID == draggedDbID);
        if (draggedData != null)
        {
            draggedData.slotIndex = GetGlobalSlotIndex(dropSlot);
            if (draggedItem is EquipmentItem eq) { draggedData.isEquipped = dropSlot.isEquipmentSlot; }
            InventoryService.Instance.ScheduleMoveItem(draggedDbID, draggedData.slotIndex);
        }

        if (swappedItem != null) Destroy(swappedItem.gameObject);
        Destroy(draggedItem.gameObject);

        GameObject draggedPrefab = ItemDictionary.Instance.GetItemPrefab(draggedID);
        if (draggedPrefab != null)
        {
            GameObject newDraggedObj = Instantiate(draggedPrefab, dropSlot.transform);
            newDraggedObj.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
            Item newDraggedItem = newDraggedObj.GetComponent<Item>();
            newDraggedItem.dbID = draggedDbID;
            newDraggedItem.quantity = draggedQuantity;
            newDraggedItem.rarity = draggedRarity;
            newDraggedItem.qualityFactor = draggedQuality;
            newDraggedItem.ownerClientId = draggedOwner;
            newDraggedItem.isDisplayOnly = false;
            if (newDraggedItem is EquipmentItem eq) eq.isEquipped = dropSlot.isEquipmentSlot;
            newDraggedItem.UpdateQuantityDisplay();
            dropSlot.currentItem = newDraggedObj;
        }

        if (swappedItem != null)
        {
            GameObject swappedPrefab = ItemDictionary.Instance.GetItemPrefab(swappedID);
            if (swappedPrefab != null)
            {
                GameObject newSwappedObj = Instantiate(swappedPrefab, originalSlot.transform);
                newSwappedObj.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
                Item newSwappedItem = newSwappedObj.GetComponent<Item>();
                newSwappedItem.dbID = swappedDbID;
                newSwappedItem.quantity = swappedQuantity;
                newSwappedItem.rarity = swappedRarity;
                newSwappedItem.qualityFactor = swappedQuality;
                newSwappedItem.ownerClientId = swappedOwner;
                newSwappedItem.isDisplayOnly = false;
                if (newSwappedItem is EquipmentItem eq) eq.isEquipped = originalSlot.isEquipmentSlot;
                newSwappedItem.UpdateQuantityDisplay();
                originalSlot.currentItem = newSwappedObj;
            }
        }


        playerStats?.ApplyEquippedItems();
        InventoryController.Instance.ReBuildItemCounts();

        if (StorageChestController.Instance != null && StorageChestController.Instance.IsViewingChest)
        {
            if (StorageChestController.Instance.storageChestPage != null && dropSlot.transform.IsChildOf(StorageChestController.Instance.storageChestPage.transform))
                StartCoroutine(SyncChestAfterMoveDelay());
        }

        if (TooltipManager.Instance != null) TooltipManager.Instance.gameObject.SetActive(true);
    }

    public void ProcessDoubleClick(Item thisItem)
    {
        if (!thisItem.IsOwnedByLocalPlayer()) return;

        if (StorageChestController.Instance != null && StorageChestController.Instance.chestPanel.activeSelf)
        {
            StorageChestController.Instance.OnItemDoubleClicked(thisItem);
            return;
        }

        if (thisItem is EquipmentItem eqItem)
        {
            if (eqItem.isDisplayOnly) TryEquipViaDoubleClick(eqItem);
            else if (eqItem.isEquipped) TryUnequipViaDoubleClick(eqItem);
        }
    }

    private IEnumerator SyncChestAfterMoveDelay()
    {
        yield return new WaitForSeconds(0.2f);
        StorageChestController.Instance.RefreshChestContent();
    }

    public FarmPlot GetFarmPlotAtMouse(PointerEventData eventData)
    {
        Vector2 worldPos = Camera.main.ScreenToWorldPoint(eventData.position);
        Collider2D[] colliders = Physics2D.OverlapPointAll(worldPos);

        foreach (var col in colliders)
        {
            if (col == null || col.CompareTag("PlayerController")) continue;
            FarmPlot plot = col.GetComponent<FarmPlot>();
            if (plot != null) return plot;
        }
        return null;
    }

    public bool IsWithinInventory(RectTransform inventoryRect, Vector2 mousePosition)
    {
        if (inventoryRect == null) return false;
        return RectTransformUtility.RectangleContainsScreenPoint(inventoryRect, mousePosition);
    }

    private void RequestDropItemConfirmation(ItemDragHandler dragHandler, Slot slotToEmpty, Item itemToDrop, Vector2 dragEndMousePosition)
    {
        GameObject confirmPrefab = LoadResourceManager.Instance.ConfirmUIPrefab;
        if (confirmPrefab == null) { dragHandler.SnapBack(); return; }

        GameStateManager.CanOpenMenu = false;
        GameObject confirmUIObj = Instantiate(confirmPrefab);
        ConfirmUIController confirmUI = confirmUIObj.GetComponent<ConfirmUIController>();

        if (confirmUI == null)
        {
            GameStateManager.CanOpenMenu = true;
            Destroy(confirmUIObj);
            dragHandler.SnapBack();
            return;
        }

        UnityEngine.Events.UnityAction onYesAction = () => {
            GameStateManager.CanOpenMenu = true;
            DropItem(dragHandler, slotToEmpty, itemToDrop, dragEndMousePosition);
        };

        UnityEngine.Events.UnityAction onNoAction = () => {
            GameStateManager.CanOpenMenu = true;
            dragHandler.SnapBack();
        };

        string message = $"Bạn có chắc muốn vứt bỏ <color=yellow>{itemToDrop.Name}</color> (x{itemToDrop.quantity})?";
        confirmUI.Show(message, onYesAction);

        if (confirmUI.noButton != null) confirmUI.noButton.onClick.AddListener(onNoAction);
        else { GameStateManager.CanOpenMenu = true; dragHandler.SnapBack(); }
    }

    private void DropItem(ItemDragHandler dragHandler, Slot originalSlot, Item item, Vector2 dragEndMousePosition)
    {
        if (item == null || item.dbID == 0) { dragHandler.SnapBack(); return; }

        int dropQuantity = item.quantity;
        int itemDbIdToRemove = item.dbID;

        CanvasGroup canvasGroup = item.GetComponent<CanvasGroup>();
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

                    GameObject dropItem = Instantiate(item.gameObject, dropPosition, Quaternion.identity);
                    Item droppedItem = dropItem.GetComponent<Item>();

                    if (droppedItem is EquipmentItem dropEq) dropEq.isEquipped = false;
                    droppedItem.isDisplayOnly = false;
                    droppedItem.dbID = 0;
                    droppedItem.quantity = dropQuantity;
                    droppedItem.UpdateQuantityDisplay();

                    dropItem.GetComponent<BounceEffect>()?.StartBounce();

                    if (dropItem.TryGetComponent(out NetworkObject netObj) && NetworkManager.Singleton.IsServer)
                    {
                        if (!netObj.IsSpawned) netObj.Spawn();
                    }
                }

                Destroy(item.gameObject);
                InventoryController.Instance.ReBuildItemCounts();
            }
            else
            {
                GameNotify.Show("Không thể vứt đồ! Dữ liệu bị lệch.");
                dragHandler.SnapBack();
                InventoryController.Instance.RefreshInventory();
            }
        });
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
            newEq.ownerClientId = sourceEqItem.ownerClientId;
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

    public int GetGlobalSlotIndex(Slot slot)
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

    public void RefreshPlayerStats()
    {
        playerStats?.ApplyEquippedItems();
    }
}