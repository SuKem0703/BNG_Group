using UnityEngine;

public class MaterialItem : Item
{
    public override ItemType ItemType => ItemType.Material;
    public override bool IsStackable => true;

    public override void UseItem()
    {
        Debug.Log("Nguyên liệu để chế tạo, không thể dùng trực tiếp: " + Name);
    }
}