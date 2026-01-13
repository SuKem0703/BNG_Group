using UnityEngine;

public static class RarityColorHelper
{
    public static Color GetColorByRarity(ItemRarity rarity)
    {
        return rarity switch
        {
            ItemRarity.Rusty => HexToColor("#fff8d8"),
            ItemRarity.Common => HexToColor("#f4f4f4"),
            ItemRarity.Refined => HexToColor("#71ff65"),
            ItemRarity.Rare => HexToColor("#ff7100"),
            ItemRarity.Relic => HexToColor("#ca97eb"),
            ItemRarity.Glacial => HexToColor("#ffffff"),
            ItemRarity.Legendary => HexToColor("#aedbff"),
            ItemRarity.Celestial => HexToColor("#ffc8ff"),
            ItemRarity.Mythic => HexToColor("#fff4c2"),
            _ => Color.white
        };
    }

    private static Color HexToColor(string hex)
    {
        Color color;
        if (ColorUtility.TryParseHtmlString(hex, out color))
            return color;

        Debug.LogWarning($"Không thể chuyển hex '{hex}' thành màu.");
        return Color.white;
    }
}
