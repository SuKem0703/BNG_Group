using UnityEngine;
using UnityEngine.EventSystems;

public class SlotTooltipHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Slot slot;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (slot != null && slot.currentItem != null)
        {
            Item item = slot.currentItem.GetComponent<Item>();
            if (item != null)
            {
                TooltipManager.Instance.Show(item, slot); // ✅ Gọi qua TooltipManager
            }
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        TooltipManager.Instance.HideAll(); // ✅ Ẩn tất cả tooltip
    }
}
