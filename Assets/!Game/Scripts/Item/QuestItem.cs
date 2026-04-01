using UnityEngine;

public class QuestItem : Item
{
    public override ItemType ItemType => ItemType.QuestItem;
    public override bool IsStackable => true;

    public override void UseItem()
    {
        Debug.Log("Vật phẩm nhiệm vụ, sẽ tự động được sử dụng khi trả Quest: " + Name);
    }
}