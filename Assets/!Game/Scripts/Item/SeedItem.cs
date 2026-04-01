using UnityEngine;

public class SeedItem : Item
{
    public override ItemType ItemType => ItemType.Seed;
    public override bool IsStackable => true;

    public GameObject cropPrefab;

    [Tooltip("Kích thước vùng trồng")]
    public Vector2Int cropSize = new Vector2Int(1, 1);

    public override void UseItem()
    {
        Debug.Log("Đang gieo hạt: " + Name);
    }
}
