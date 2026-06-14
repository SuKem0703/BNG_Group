using UnityEngine;
using UnityEngine.EventSystems;

public class Slot : MonoBehaviour, IPointerClickHandler
{
    public bool isEquipmentSlot = false;
    public EquipSlot acceptedEquipSlot = EquipSlot.None;
    public ClassRestriction classRestriction = ClassRestriction.None;
    public GameObject currentItem;

    public bool isShopSlot = false;
    public bool isHotBarSlot = false;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isHotBarSlot && HotbarController.Instance != null)
        {
            int slotIndex = transform.GetSiblingIndex();
            HotbarController.Instance.HandleSlotClick(slotIndex, eventData);
        }
    }
}