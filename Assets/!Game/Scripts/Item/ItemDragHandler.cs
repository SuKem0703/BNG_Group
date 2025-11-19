using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using static Unity.Cinemachine.Samples.PlatformerCamera2D;
using UnityEngine.UI;

public class ItemDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler//, IPointerClickHandler
{
    Transform originalParent;
    CanvasGroup canvasGroup;

    public float minDropDistance = 2f;
    public float maxDropDistance = 3f;
    [SerializeField] private Slot originalSlot;
    [SerializeField] private InventoryController inventoryController;

    private GameObject confirmUIPrefab;
    private GameObject notifyUIPrefab;
    private PlayerStats playerStats => GameObject.FindGameObjectWithTag("PlayerController").GetComponent<PlayerStats>();
    private KnightEquipmentPanel knightEquipmentPanel => Object.FindFirstObjectByType<KnightEquipmentPanel>();
    private MageEquipmentPanel mageEquipmentPanel => Object.FindFirstObjectByType<MageEquipmentPanel>();

    void Start()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        inventoryController = InventoryController.Instance;

        confirmUIPrefab = Resources.Load<GameObject>("UI/ConfirmUICanvas");
        if (confirmUIPrefab == null)
        {
            Debug.LogError("Không tìm thấy 'ConfirmUICanvas' trong thư mục Resources/UI!");
        }

        notifyUIPrefab = Resources.Load<GameObject>("UI/NotifyUICanvas");
        if (notifyUIPrefab == null)
        {
            Debug.LogError("Không tìm thấy 'NotifyUICanvas' trong thư mục Resources/UI!");
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // 🔒 Kiểm tra parent slot có phải slot trang bị hoặc slot shop không
        Slot parentSlot = transform.parent.GetComponent<Slot>();
        if (parentSlot != null && parentSlot.isEquipmentSlot)
        {
            return;
        }

        if (parentSlot != null && parentSlot.isShopSlot == true)
        {
            return;
        }

        //if (parentSlot != null && parentSlot.isHotBarSlot) 
        //{ 
        //    return; 
        //}

        originalParent = transform.parent; // Save OG parent
        transform.SetParent(transform.root); // Above other canvas'
        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.6f; // semi-transparent during drag

        TooltipManager.Instance.gameObject.SetActive(false);
    }

    public void OnDrag(PointerEventData eventData)
    {
        // 🔒 Kiểm tra parent slot có phải slot trang bị hoặc slot shop không
        Slot parentSlot = transform.parent.GetComponent<Slot>();
        if (parentSlot != null && parentSlot.isEquipmentSlot)
        {
            return;
        }

        if (parentSlot != null && parentSlot.isShopSlot == true)
        {
            return;
        }

        //if (parentSlot != null && parentSlot.isHotBarSlot == true) 
        //{ 
        //    return;
        //}

        transform.position = eventData.position; // Follow the mouse
    }
    public void OnEndDrag(PointerEventData eventData)
    {
        // 🔒 Kiểm tra parent slot có phải slot trang bị hoặc slot shop không
        Slot parentSlot = transform.parent.GetComponent<Slot>();
        if (parentSlot != null && parentSlot.isEquipmentSlot)
        {
            return;
        }

        if (parentSlot != null && parentSlot.isShopSlot == true)
        {
            return;
        }

        //if (parentSlot != null && parentSlot.isHotBarSlot == true)
        //{
        //    return;
        //}

        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;

        Slot dropSlot = eventData.pointerEnter?.GetComponent<Slot>();
        if (dropSlot == null)
        {
            GameObject dropItem = eventData.pointerEnter;
            if (dropItem != null)
            {
                dropSlot = dropItem.GetComponentInParent<Slot>();
            }
        }

        originalSlot = originalParent.GetComponent<Slot>();
        Item draggedItem = GetComponent<Item>();

        if (dropSlot == originalSlot)
        {
            SnapBack();
        }

        // Không kéo gì cả hoặc lỗi thì snapback
        if (draggedItem == null)
        {
            SnapBack();
            return;
        }

        // Nếu kéo vào slot nào đó
        if (dropSlot != null)
        {
            if (dropSlot.isShopSlot)
            {
                SnapBack();
                return;
            }

            // Nếu slot đích là Hotbar
            if (dropSlot.isHotBarSlot)
            {
                // Cấm kéo Equipment vào Hotbar
                if (draggedItem.itemType == ItemType.Equipment)
                {
                    Debug.Log("Không thể kéo trang bị vào Hotbar.");
                    SnapBack();
                    return;
                }

                // (Đây là yêu cầu của bạn) Cấm kéo QuestItem vào Hotbar
                if (draggedItem.itemType == ItemType.QuestItem)
                {
                    Debug.Log("Không thể kéo vật phẩm nhiệm vụ vào Hotbar.");
                    SnapBack();
                    return;
                }
            }

            // Nếu item là QuestItem (chúng ta đã biết nó không phải hotbar)
            if (draggedItem.itemType == ItemType.QuestItem)
            {
                // Cấm kéo QuestItem vào EquipmentSlot
                if (dropSlot.isEquipmentSlot)
                {
                    Debug.Log("Không thể trang bị vật phẩm nhiệm vụ.");
                    SnapBack();
                    return;
                }
                // (Nếu nó là inventory slot bình thường, code sẽ chạy tiếp và cho phép di chuyển/swap)
            }

            // Kiểm tra nếu slot là EquipmentSlot và item hợp lệ
            if (dropSlot.isEquipmentSlot)
            {
                // Kiểm tra nếu trang bị hợp lệ
                if (draggedItem.equipSlot != dropSlot.acceptedEquipSlot ||
                    (dropSlot.classRestriction != ClassRestriction.None && draggedItem.classRestriction != dropSlot.classRestriction))
                {
                    SnapBack();
                    return;
                }

                // Kiểm tra cấp độ của người chơi
                if (playerStats != null && playerStats.level < draggedItem.requiredLevel)
                {
                    Debug.Log($"Không thể trang bị {draggedItem.Name}: yêu cầu cấp {draggedItem.requiredLevel}, hiện tại là cấp {playerStats.level}");
                    SnapBack();
                    return;
                }
            }

            // Nếu slot đích có item rồi
            if (dropSlot.currentItem != null)
            {
                Item targetItem = dropSlot.currentItem.GetComponent<Item>();

                bool isFromDisplayToEquip = draggedItem != null
                                            && draggedItem.isDisplayOnly
                                            && dropSlot.isEquipmentSlot;

                // 🚫 Còn lại: nếu item đích bị khóa (displayOnly hoặc equipped) thì snapback
                if (!isFromDisplayToEquip && targetItem != null && (targetItem.isDisplayOnly || targetItem.isEquipped))
                {
                    SnapBack();
                    return;
                }

                // Nếu cùng loại và stackable
                if (draggedItem.ID == targetItem.ID && draggedItem.itemType != ItemType.Equipment && draggedItem.gameObject != targetItem.gameObject)
                {
                    targetItem.AddToStack(draggedItem.quantity);
                    originalSlot.currentItem = null;
                    TooltipManager.Instance.gameObject.SetActive(true);
                    Destroy(gameObject);
                    Debug.Log($"Gộp vào slot {dropSlot.name}, huỷ object {draggedItem.Name}");
                    return;
                }
                else
                {
                    //Slot has an item - swap items
                    dropSlot.currentItem.transform.SetParent(originalSlot.transform);
                    originalSlot.currentItem = dropSlot.currentItem;
                    dropSlot.currentItem.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

                    transform.SetParent(dropSlot.transform);
                    dropSlot.currentItem = gameObject;
                    GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
                }
            }
            else
            {
                originalSlot.currentItem = null;
                transform.SetParent(dropSlot.transform);
                dropSlot.currentItem = gameObject;
                GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
            }

            // Gán item vào slot mới
            transform.SetParent(dropSlot.transform);
            dropSlot.currentItem = gameObject;
            GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

            // Nếu là item trang bị, cập nhật trạng thái
            if (dropSlot.isEquipmentSlot && draggedItem != null)
            {
                UpdateAllEquipmentItems(draggedItem);
                playerStats.ApplyEquippedItems();
                draggedItem.isEquipped = true;
                if (draggedItem.sourceItem != null)
                    draggedItem.sourceItem.isEquipped = true;
            }

            EquipmentScrollViewController equipmentView = Object.FindFirstObjectByType<EquipmentScrollViewController>();
            if (equipmentView != null)
            {
                equipmentView.ShowEquipmentItems();
            }
        }

        // Kéo ra ngoài inventory
        else
        {
            // Nếu thả ra ngoài slot
            if (!IsWithinInventory(eventData.position))
            {
                // Thử gieo hạt nếu có thể
                FarmPlot plot = GetFarmPlotAtMouse(eventData);

                if (plot != null && draggedItem.itemType == ItemType.Seed)
                {
                    FarmController.Instance.TryPlantSeed(plot, (SeedItem)draggedItem);
                    SnapBack();
                    return;
                }

                // Nếu là item đang equipped => không cho vứt
                if (draggedItem.isEquipped)
                {
                    SnapBack();
                    return;
                }

                // Nếu là quest item => không cho vứt
                if (draggedItem.itemType == ItemType.QuestItem)
                {
                    ShowDropErrorMessage("Không thể vứt bỏ vật phẩm nhiệm vụ!");
                    SnapBack();
                    return;
                }

                // Kiểm tra nếu item đang cần cho nhiệm vụ
                if (IsItemNeededForActiveQuest(draggedItem.ID))
                {
                    ShowDropErrorMessage($"<color=yellow>{draggedItem.Name}</color> đang cần cho nhiệm vụ. Không thể vứt bỏ!");
                    SnapBack();
                    return;
                }

                RequestDropItemConfirmation(originalSlot, draggedItem, eventData.position);
            }
            SnapBack();
        }
        TooltipManager.Instance.gameObject.SetActive(true);
    }

    private FarmPlot GetFarmPlotAtMouse(PointerEventData eventData)
    {
        Vector2 worldPos = Camera.main.ScreenToWorldPoint(eventData.position);
        RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero);

        if (hit.collider == null) return null;
        return hit.collider.GetComponent<FarmPlot>();
    }

    // ========== CONFIRMATION & NOTIFY UI ==========
    private void ShowDropErrorMessage(string message)
    {
        if (notifyUIPrefab == null)
        {
            Debug.LogError("NotifyUIPrefab not loaded. Không thể hiển thị thông báo. Canceling drop.");
            SnapBack();
            return;
        }

        MenuController.CanOpenMenu = false;

        GameObject notifyUIObj = Instantiate(notifyUIPrefab);
        NotifyUIController notifyUI = notifyUIObj.GetComponent<NotifyUIController>();

        if (notifyUI == null)
        {
            MenuController.CanOpenMenu = true;
            Debug.LogError("Prefab NotifyUICanvas thiếu script NotifyUIController!");
            Destroy(notifyUIObj);
            SnapBack();
            return;
        }

        UnityEngine.Events.UnityAction onOkAction = () => {
            MenuController.CanOpenMenu = true;
            SnapBack();
        };

        notifyUI.Show(message, onOkAction);
    }
    private void RequestDropItemConfirmation(Slot slotToEmpty, Item itemToDrop, Vector2 dragEndMousePosition)
    {
        if (confirmUIPrefab == null)
        {
            Debug.LogError("ConfirmUIPrefab not loaded. Canceling drop.");
            SnapBack();
            return;
        }

        MenuController.CanOpenMenu = false;

        GameObject confirmUIObj = Instantiate(confirmUIPrefab);
        ConfirmUIController confirmUI = confirmUIObj.GetComponent<ConfirmUIController>();

        if (confirmUI == null)
        {
            MenuController.CanOpenMenu = true;
            Debug.LogError("Prefab ConfirmUICanvas thiếu script ConfirmUIController!");
            Destroy(confirmUIObj);
            SnapBack();
            return;
        }

        UnityEngine.Events.UnityAction onYesAction = () => {
            MenuController.CanOpenMenu = true;
            DropItem(slotToEmpty, dragEndMousePosition);
        };

        UnityEngine.Events.UnityAction onNoAction = () => {
            MenuController.CanOpenMenu = true;
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
            MenuController.CanOpenMenu = true;
            Debug.LogError("Không tìm thấy 'noButton' trên ConfirmUI. Tự động SnapBack.");
            SnapBack();
        }
    }

    private bool IsItemNeededForActiveQuest(int itemID)
    {
        if (QuestController.Instance == null) return false;

        // Duyệt qua tất cả các Quest đang kích hoạt (active)
        foreach (QuestProgress quest in QuestController.Instance.activeQuests)
        {
            foreach (QuestObject obj in quest.questObjects)
            {
                // Nếu nhiệm vụ là CollectItem
                if (obj.objectType == ObjectType.CollectItem)
                {
                    // Parse ID từ string sang int để so sánh
                    if (int.TryParse(obj.objectID, out int questItemID))
                    {
                        if (questItemID == itemID)
                        {
                            // Nếu tìm thấy item này trong 1 quest đang active
                            // Bạn có thể check thêm điều kiện obj.IsCompleted nếu muốn cho phép vứt khi đã gom đủ
                            // Nhưng an toàn nhất là không cho vứt khi chưa trả quest.
                            return true;
                        }
                    }
                }
            }
        }
        return false;
    }

    // ========== HELPER METHODS ==========
    private void SnapBack()
    {
        transform.SetParent(originalParent);
        GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        TooltipManager.Instance.gameObject.SetActive(true);
        playerStats.ApplyEquippedItems();
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
                    item.isEquipped = false; // Set tất cả các item cùng loại là false
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

        if (item != null && (item.isDisplayOnly || item.isEquipped))
        {
            transform.SetParent(originalParent);
            GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
            return;
        }

        int dropQuantity = item.quantity;

        originalSlot.currentItem = null;

        Transform playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (playerTransform == null)
        {
            Debug.LogError("Missing 'Player' tag");
            return;
        }

        Vector2 playerPosition = (Vector2)playerTransform.position;
        Vector2 mouseScreenPosition = dragEndMousePosition;
        Vector2 mouseWorldPosition = Camera.main.ScreenToWorldPoint(mouseScreenPosition);
        Vector2 dropDirection = (mouseWorldPosition - playerPosition).normalized;

        // Xử lý trường hợp chuột ở ngay trên người chơi
        if (dropDirection == Vector2.zero)
        {
            dropDirection = Vector2.up;
        }

        float randomDistance = Random.Range(minDropDistance, maxDropDistance);
        Vector2 dropPosition = playerPosition + (dropDirection * randomDistance);

        //Vector2 dropOffset = Random.insideUnitCircle.normalized * Random.Range(minDropDistance, maxDropDistance);
        //Vector2 dropPosition = (Vector2)playerTransform.position + dropOffset;

        // ✅ Tạo object mới đúng chuẩn:
        GameObject dropItem = Instantiate(gameObject, dropPosition, Quaternion.identity);

        // ✅ Reset các cờ không đúng
        Item droppedItem = dropItem.GetComponent<Item>();
        droppedItem.isEquipped = false;
        droppedItem.isDisplayOnly = false;

        droppedItem.quantity = dropQuantity;
        droppedItem.UpdateQuantityDisplay();

        dropItem.GetComponent<BounceEffect>().StartBounce();

        Destroy(gameObject);
        Debug.Log($"Đã vứt item: {item.Name} ({dropQuantity}) tại vị trí {dropPosition}");

        InventoryController.Instance.ReBuildItemCounts();
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