using UnityEngine;
using UnityEngine.EventSystems;

public class ItemClickHandler : MonoBehaviour, IPointerClickHandler
{
    private float lastClickTime = 0f;
    private float doubleClickThreshold = 0.3f;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (Time.time - lastClickTime < doubleClickThreshold)
        {
            if (!AntiSpam.CanPerformAction()) return;

            Item thisItem = GetComponent<Item>();
            if (thisItem == null) return;

            if (thisItem.dbID == 0)
            {
                GameNotify.Show("Đang đồng bộ...");
                return;
            }

            InventoryActionManager.Instance.ProcessDoubleClick(thisItem);
            lastClickTime = 0;
            return;
        }

        lastClickTime = Time.time;
    }
}