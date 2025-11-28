using UnityEngine;

[System.Serializable]
public class StorageChestSaveData
{
    public int itemID;
    public int slotIndex;
    public int quantity = 1;
    public ItemRarity rarity;
    public float qualityFactor;
}
