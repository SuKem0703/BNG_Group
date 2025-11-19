using UnityEngine;

public enum TargetType
{
    NPC,
    Item,
    Other
}

public struct TargetInfoData
{
    public string name;
    public Sprite portrait;
    public string actionText;
    public TargetType type;
    public ItemRarity rarity;

    // Constructor without rarity (defaults to Common)
    public TargetInfoData(string name, Sprite portrait, string actionText, TargetType type)
    {
        this.name = name;
        this.portrait = portrait;
        this.actionText = actionText;
        this.type = type;
        this.rarity = ItemRarity.Common;
    }

    // Constructor with rarity
    public TargetInfoData(string name, Sprite portrait, string actionText, TargetType type, ItemRarity rarity)
    {
        this.name = name;
        this.portrait = portrait;
        this.actionText = actionText;
        this.type = type;
        this.rarity = rarity;
    }
}

public interface ITargetableInfo
{
    TargetInfoData GetInfo();
}