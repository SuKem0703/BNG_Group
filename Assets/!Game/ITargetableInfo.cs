using UnityEngine;

public enum TargetType
{
    NPC,
    Item,
    Enemy,
    Other
}

public struct TargetInfoData
{
    public string name;
    public Sprite portrait;
    public string actionText;
    public TargetType type;

    public int currentHP;
    public int maxHP;

    public TargetInfoData(string name, Sprite portrait, string actionText, TargetType type)
    {
        this.name = name;
        this.portrait = portrait;
        this.actionText = actionText;
        this.type = type;
        this.currentHP = 0;
        this.maxHP = 0;
    }
}

public interface ITargetableInfo
{
    TargetInfoData GetInfo();
}