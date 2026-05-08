using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class LootDropItem
{
    public int itemID;
    public int weight;
}

[CreateAssetMenu(fileName = "NewLootTable", menuName = "ScriptableObjects/LootTable")]
public class LootTable : ScriptableObject
{
    public List<LootDropItem> lootItems;
    public int emptyDropWeight = 0;

    public int GetRandomDrop(SeededRandom rng)
    {
        int totalWeight = emptyDropWeight;
        foreach (var item in lootItems)
        {
            totalWeight += item.weight;
        }

        if (totalWeight <= 0) return 0;

        int randomValue = rng.NextInt(0, totalWeight);
        int currentWeight = 0;

        foreach (var item in lootItems)
        {
            currentWeight += item.weight;
            if (randomValue < currentWeight)
            {
                return item.itemID;
            }
        }

        return 0;
    }
}