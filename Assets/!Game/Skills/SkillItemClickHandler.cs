using UnityEngine;
using UnityEngine.EventSystems;

public class SkillItemClickHandler : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            Slot parentSlot = GetComponentInParent<Slot>();
            if (parentSlot != null && parentSlot.isHotBarSlot)
            {
                int slotIndex = parentSlot.transform.GetSiblingIndex();
                HotbarController.Instance.RemoveSkillFromSlot(slotIndex);
            }
        }
    }
}