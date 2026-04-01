using UnityEngine;

public class TooltipManager : MonoBehaviour
{
    public static TooltipManager Instance;

    private void Awake()
    {
        Instance = this;
    }

    public void Show(Item item, Slot slot)
    {
        HideAll();

        if (slot.isShopSlot == true) return;

        // Nếu là trang bị (vũ khí, giáp, mũ, v.v…)
        if (item is EquipmentItem)
        {
            EquipTooltip.Instance.Show(item, slot);
        }
        // Nếu là vật phẩm tiêu hao hoặc vật phẩm nhiệm vụ
        else if (item is ConsumableItem || item is QuestItem)
        {
            ConsumableTooltip.Instance.Show(item);
        }
    }

    public void HideAll()
    {
        EquipTooltip.Instance.Hide();
        ConsumableTooltip.Instance.Hide();
    }
}
