using UnityEngine;

public class SeedItem : Item
{
    public GameObject cropPrefab;

    [Tooltip("Kích thước vùng trồng")]
    public Vector2Int cropSize = new Vector2Int(1, 1);

    private void OnValidate()
    {
        itemType = ItemType.Seed;
    }
}
