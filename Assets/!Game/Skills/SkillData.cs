using UnityEngine;

public enum SkillType
{
    Modifier,
    Active,
    Passive
}

public enum SkillTree
{
    Physical,
    Magical,
    Utility
}

[CreateAssetMenu(fileName = "new modifier skill", menuName = "Skill Tree/Ranked Modifier Skill")]
public class SkillData : ScriptableObject
{
    [Header("General Information")]
    public SkillTree skillTree;
    public string skillID;
    public string skillName;
    [TextArea(2, 4)]
    public string description;
    public Sprite skillIcon;
    public SkillType skillType;

    [Header("Unlock Requirements")]
    public int lvlReq;
    public int playerAscensionRankReqToUnlock;
    public SkillData skillReqToUnlock;
    public int skillRankReqToUnlock = 10;

    [Header("Stat Scaling Matrix")]
    public int maxRank = 20;
    public float baseValue = 0.05f;
    public float maxRankValue = 1.0f;

    [Header("Resources & Cooldown")]
    public float baseManaCost = 20f;
    public float manaCostIncreasePerRank = 2f;
    public float baseCooldown = 10f;
    public float minCooldown = 5f;

    public float GetSkillValue(int currentRank)
    {
        if (currentRank <= 0) return 0f;
        if (currentRank >= maxRank) return maxRankValue;

        float t = (float)(currentRank - 1) / (maxRank - 1);
        return Mathf.Lerp(baseValue, maxRankValue, t);
    }

    public float GetManaCost(int currentRank)
    {
        return baseManaCost + ((currentRank - 1) * manaCostIncreasePerRank);
    }

    public float GetCooldown(int currentRank)
    {
        if (currentRank <= 0) return baseCooldown;
        if (currentRank >= maxRank) return minCooldown;

        float t = (float)(currentRank - 1) / (maxRank - 1);
        return Mathf.Lerp(baseCooldown, minCooldown, t);
    }
}