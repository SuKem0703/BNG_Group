using UnityEngine;

public static class GameFlags
{
    private static bool CheckQuestState(string questID, bool includeActive, bool includeHandedIn)
    {
        if (QuestController.Instance == null)
        {
            return false;
        }

        bool isActive = false;
        bool isHandedIn = false;

        if (includeActive)
        {
            isActive = QuestController.Instance.IsQuestActive(questID);
        }

        if (includeHandedIn)
        {
            isHandedIn = QuestController.Instance.IsQuestHandedIn(questID);
        }

        return isActive || isHandedIn;
    }


    // ----- KIỂM TRA CẢ ĐANG LÀM VÀ ĐÃ TRẢ XONG -----

    // Storage Chest example
    private const string InteractChestQuestID = "MQ_C01_P01_01_FetchSeeds";
    public static bool HasInteractedWithStorageChest()
    {
        return CheckQuestState(InteractChestQuestID, includeActive: true, includeHandedIn: true);
    }

    // Equipment Menu example
    private const string OpenEquipmentQuestID = "Thanh Kiếm Bị Bỏ Quên";
    public static bool IsOpenedEquipmentMenu()
    {
        return CheckQuestState(OpenEquipmentQuestID, includeActive: true, includeHandedIn: true);
    }

    // Potential Menu example
    private const string PotentialQuestID = "FirstCombatQuestID";
    public static bool IsOpenedPotentialMenu()
    {
        return CheckQuestState(PotentialQuestID, includeActive: true, includeHandedIn: true);
    }

    // ----- CHỈ kiểm tra ĐANG LÀM (In Progress) -----
    private const string TrainingQuestID = "TrainingQuestID";
    public static bool IsTrainingInProgress()
    {
        return CheckQuestState(TrainingQuestID, includeActive: true, includeHandedIn: false);
    }

    // ------ CHỈ kiểm tra ĐÃ TRẢ XONG (Handed In) ------
    private const string LyriaRecruitedQuestID = "TutorialBossQuestID";
    public static bool HasRecruitedLyria()
    {
        return CheckQuestState(LyriaRecruitedQuestID, includeActive: false, includeHandedIn: true);
    }

    // Skill Menu example
    private const string OpenSkillQuestID = "OpenSkillQuestID";
    public static bool IsOpenedSkillMenu()
    {
        return CheckQuestState(OpenSkillQuestID, true, true);
    }
}