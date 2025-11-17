using UnityEngine;

[System.Serializable]
public class EquippedSaveData
{
    public int itemID;
    public int slotIndex;
    public int quantity = 1;
    public bool isEquipped = true;
    public ItemRarity rarity;
    public float qualityFactor;

    public int sourceItemID = -1;
}
