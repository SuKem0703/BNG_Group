
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

public enum SkillElement
{
    Unassigned = 0,
    ViemHoa = 1,
    HuyenThuy = 2,
    DiaMach = 3,
    ThienTuong = 4
}

[CreateAssetMenu(
    fileName = "new modifier skill",
    menuName = "Skill Tree/Ranked Modifier Skill"
)]
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

    [Header("Element & Class Restriction")]
    public SkillElement skillElement = SkillElement.Unassigned;

    [Tooltip("Cho phép Knight và Mage cùng sử dụng kỹ năng.")]
    public bool isSharedSkill = false;

    [Header("Unlock Requirements")]
    public int lvlReq;
    public int playerAscensionRankReqToUnlock;
    public SkillData skillReqToUnlock;
    public int skillRankReqToUnlock = 10;

    [Header("Stat Scaling Matrix")]
    public int maxRank = 20;
    public float baseValue = 0.05f;
    public float maxRankValue = 1.0f;

    [Header("Runtime Effect")]
    public string effectID;

    [Header("Resources & Cooldown")]
    public float baseManaCost = 20f;
    public float manaCostIncreasePerRank = 2f;
    public float baseCooldown = 10f;
    public float minCooldown = 5f;

    [Header("Secondary Stat Scaling")]
    public float baseSecondaryValue = 0f;
    public float maxRankSecondaryValue = 0f;

    [Header("Effect Duration")]
    [Min(0f)] public float baseEffectDuration = 10f;
    [Min(0f)] public float maxRankEffectDuration = 10f;

    // =========================================
    // CLASS RESTRICTION
    // =========================================

    public bool CanBeUsedBy(PlayerClassType playerClass)
    {
        if (skillElement == SkillElement.Unassigned)
            return false;

        if (isSharedSkill)
        {
            return playerClass == PlayerClassType.Knight ||
                   playerClass == PlayerClassType.Mage;
        }

        switch (skillElement)
        {
            case SkillElement.ViemHoa:
            case SkillElement.HuyenThuy:
                return playerClass == PlayerClassType.Mage;

            case SkillElement.DiaMach:
            case SkillElement.ThienTuong:
                return playerClass == PlayerClassType.Knight;

            default:
                return false;
        }
    }

    public string GetElementName()
    {
        switch (skillElement)
        {
            case SkillElement.ViemHoa:
                return "Viêm Hỏa";

            case SkillElement.HuyenThuy:
                return "Huyền Thủy";

            case SkillElement.DiaMach:
                return "Địa Mạch";

            case SkillElement.ThienTuong:
                return "Thiên Tượng";

            default:
                return "Chưa xác định";
        }
    }

    // =========================================
    // SKILL SCALING
    // =========================================

    public float GetSkillValue(int currentRank)
    {
        if (currentRank <= 0)
            return 0f;

        if (currentRank >= maxRank)
            return maxRankValue;

        float t = (float)(currentRank - 1) / (maxRank - 1);

        return Mathf.Lerp(baseValue, maxRankValue, t);
    }

    public float GetSecondarySkillValue(int currentRank)
    {
        if (currentRank <= 0)
            return 0f;

        if (maxRank <= 1)
            return maxRankSecondaryValue;

        float t = Mathf.Clamp01(
            (float)(currentRank - 1) / (maxRank - 1)
        );

        return Mathf.Lerp(
            baseSecondaryValue,
            maxRankSecondaryValue,
            t
        );
    }

    public float GetEffectDuration(int currentRank)
    {
        if (currentRank <= 0)
            return 0f;

        if (maxRank <= 1)
            return Mathf.Max(0f, maxRankEffectDuration);

        float t = Mathf.Clamp01(
            (float)(currentRank - 1) / (maxRank - 1)
        );

        return Mathf.Max(
            0f,
            Mathf.Lerp(
                baseEffectDuration,
                maxRankEffectDuration,
                t
            )
        );
    }

    public float GetManaCost(int currentRank)
    {
        return baseManaCost +
               ((currentRank - 1) * manaCostIncreasePerRank);
    }

    public float GetCooldown(int currentRank)
    {
        if (currentRank <= 0)
            return baseCooldown;

        if (currentRank >= maxRank)
            return minCooldown;

        float t = (float)(currentRank - 1) / (maxRank - 1);

        return Mathf.Lerp(baseCooldown, minCooldown, t);
    }
}
