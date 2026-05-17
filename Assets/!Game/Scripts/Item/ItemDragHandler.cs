using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ItemDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public Transform originalParent { get; private set; }
    public Slot originalSlot { get; private set; }

    private CanvasGroup canvasGroup;
    private GameObject currentSelectionBox;
    private SpriteRenderer selectionBoxRenderer;
    private Color invalidColor = new Color(1, 0, 0, 0.5f);

    private GameObject dummyDragIcon;

    void Start()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!AntiSpam.CanPerformAction())
        {
            eventData.pointerDrag = null;
            return;
        }

        Item draggedItem = GetComponent<Item>();

        if (draggedItem != null && !draggedItem.IsOwnedByLocalPlayer())
        {
            eventData.pointerDrag = null;
            return;
        }

        if (draggedItem != null && draggedItem.isDisplayOnly) { eventData.pointerDrag = null; return; }

        Slot parentSlot = transform.parent.GetComponent<Slot>();
        if (parentSlot != null && parentSlot.isShopSlot) { eventData.pointerDrag = null; return; }
        if (parentSlot != null && parentSlot.isEquipmentSlot && draggedItem is EquipmentItem eq && eq.isEquipped) { eventData.pointerDrag = null; return; }
        if (draggedItem != null && draggedItem.dbID == 0) { eventData.pointerDrag = null; return; }

        CleanupDragObjects();

        if (draggedItem is SeedItem && LoadResourceManager.Instance.SelectionBoxPrefab != null)
        {
            currentSelectionBox = Instantiate(LoadResourceManager.Instance.SelectionBoxPrefab);
            selectionBoxRenderer = currentSelectionBox.GetComponent<SpriteRenderer>();
            currentSelectionBox.SetActive(false);
        }

        originalParent = transform.parent;
        originalSlot = originalParent.GetComponent<Slot>();

        Canvas mainCanvas = GetComponentInParent<Canvas>();
        dummyDragIcon = new GameObject("DummyDragIcon");

        if (mainCanvas != null)
        {
            dummyDragIcon.transform.SetParent(mainCanvas.rootCanvas != null ? mainCanvas.rootCanvas.transform : mainCanvas.transform, false);
        }

        Image iconImg = dummyDragIcon.AddComponent<Image>();
        iconImg.sprite = draggedItem.icon;
        iconImg.raycastTarget = false;

        RectTransform rect = dummyDragIcon.GetComponent<RectTransform>();
        rect.sizeDelta = GetComponent<RectTransform>().sizeDelta;
        rect.position = eventData.position;

        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.3f;
        TooltipManager.Instance.gameObject.SetActive(false);
    }

    public void OnDrag(PointerEventData eventData)
    {
        Slot parentSlot = transform.parent.GetComponent<Slot>();
        if (parentSlot != null && (parentSlot.isEquipmentSlot || parentSlot.isShopSlot)) return;

        if (currentSelectionBox != null) UpdateSelectionBoxPosition(eventData);

        if (dummyDragIcon != null)
        {
            dummyDragIcon.transform.position = eventData.position;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        CleanupDragObjects();

        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;

        InventoryActionManager.Instance.ProcessDragDrop(this, eventData);
    }

    public void SnapBack()
    {
        GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        TooltipManager.Instance.gameObject.SetActive(true);
        InventoryActionManager.Instance.RefreshPlayerStats();
    }

    private void UpdateSelectionBoxPosition(PointerEventData eventData)
    {
        if (InventoryActionManager.Instance == null || originalParent == null || originalParent.parent == null) return;

        RectTransform inventoryRect = originalParent.parent.GetComponent<RectTransform>();
        if (InventoryActionManager.Instance.IsWithinInventory(inventoryRect, eventData.position))
        {
            if (currentSelectionBox != null) currentSelectionBox.SetActive(false);
            return;
        }

        FarmPlot plot = InventoryActionManager.Instance.GetFarmPlotAtMouse(eventData);

        if (plot != null && currentSelectionBox != null)
        {
            currentSelectionBox.SetActive(true);
            currentSelectionBox.transform.position = plot.transform.position;

            bool isPlotInRange = InteractionDetector.Instance != null && InteractionDetector.Instance.IsPlotInRange(plot);
            bool isPlanted = plot.isPlanted;

            if (isPlotInRange && !isPlanted) selectionBoxRenderer.color = Color.white;
            else selectionBoxRenderer.color = invalidColor;
        }
        else if (currentSelectionBox != null)
        {
            currentSelectionBox.SetActive(false);
        }
    }

    private void CleanupDragObjects()
    {
        if (currentSelectionBox != null)
        {
            Destroy(currentSelectionBox);
            currentSelectionBox = null;
        }
        if (dummyDragIcon != null)
        {
            Destroy(dummyDragIcon);
            dummyDragIcon = null;
        }
    }

    void OnDisable()
    {
        CleanupDragObjects();
    }

    void OnDestroy()
    {
        CleanupDragObjects();
    }
}