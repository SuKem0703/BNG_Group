using UnityEngine;

[System.Serializable]
public class EquippedSaveData
{
    public int dbID;
    public int itemID;
    public int slotIndex;
    public int quantity;
    public bool isEquipped;
    public ItemRarity rarity;
    public float qualityFactor;
    public int sourceItemID;
}