public static class ItemRarityMultiplier
{
    public static float GetMultiplier(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Rusty: return 0.6f;
            case ItemRarity.Common: return 0.8f;
            case ItemRarity.Refined: return 1.0f;
            case ItemRarity.Rare: return 1.2f;
            case ItemRarity.Relic: return 1.4f;
            case ItemRarity.Glacial: return 1.6f;
            case ItemRarity.Legendary: return 1.8f;
            case ItemRarity.Celestial: return 2.0f;
            case ItemRarity.Mythic: return 2.5f;
            default: return 1.0f;
        }
    }
}
