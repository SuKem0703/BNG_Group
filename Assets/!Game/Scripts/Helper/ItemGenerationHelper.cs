using UnityEngine;

public static class ItemGenerationHelper
{
    public static ItemRarity GetRandomRarity()
    {
        float roll = Random.Range(0f, 100f);
        if (roll < 40f) return ItemRarity.Rusty;
        if (roll < 70f) return ItemRarity.Common;
        if (roll < 85f) return ItemRarity.Refined;
        if (roll < 93f) return ItemRarity.Rare;
        if (roll < 97f) return ItemRarity.Relic;
        if (roll < 99f) return ItemRarity.Glacial;
        if (roll < 99.7f) return ItemRarity.Legendary;
        if (roll < 99.95f) return ItemRarity.Celestial;
        return ItemRarity.Mythic;
    }

    public static float GetWeightedQualityFactor()
    {
        // Càng gần 1 càng hiếm
        float raw = Mathf.Pow(Random.Range(0f, 1f), 4); // Phân phối nghiêng về 0
        return Mathf.Lerp(0.5f, 1.0f, raw);
    }
}
