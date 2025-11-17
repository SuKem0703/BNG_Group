using UnityEngine;

public static class GameFlags
{
    // Equipment Menu First Opened Quest ID
    private const string OpenEquipmentQuestID = "OpenEquipmentQuestID";
    public static bool IsOpenedEquipmentMenu()
    {
        if (QuestController.Instance == null)
        {
            return false;
        }
        return QuestController.Instance.IsQuestHandedIn(OpenEquipmentQuestID);
    }

    // Potential Menu First Opened Quest ID
    private const string OpenPotentialQuestID = "OpenPotentialQuestID";
    public static bool IsOpenedPotentialMenu()
    {
        if (QuestController.Instance == null)
        {
            return false;
        }
        return QuestController.Instance.IsQuestHandedIn(OpenPotentialQuestID);
    }

    // Skill Menu First Opened Quest ID
    private const string OpenSkillQuestID = "OpenSkillQuestID";
    public static bool IsOpenedSkillMenu()
    {
        if (QuestController.Instance == null)
        {
            return false;
        }
        return QuestController.Instance.IsQuestHandedIn(OpenSkillQuestID);
    }

    // Lyria Recruitment Quest ID
    private const string RecruitLyriaQuestID = "QUEST_LYRIA_RECRUIT";
    public static bool HasRecruitedLyria()
    {
        if (QuestController.Instance == null)
        {
            return false;
        }

        return QuestController.Instance.IsQuestHandedIn(RecruitLyriaQuestID);
    }

}
