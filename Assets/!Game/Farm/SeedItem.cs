using UnityEngine;

public class SeedItem : Item
{
    public GameObject cropPrefab;

    private void OnValidate()
    {
        itemType = ItemType.Seed;
    }
}
