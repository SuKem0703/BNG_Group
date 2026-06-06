using System;
using System.Collections.Generic;
using UnityEngine;

enum QuestType
{
    Main,
    Side,
    Daily,
    Event
}

[CreateAssetMenu(menuName = "Quests/Quest")]
public class Quest : ScriptableObject
{
    public string questID;
    public string questName;
    [TextArea(2, 10)]
    public string questTitle;
    public bool autoHandInOnComplete = false;
    public List<QuestObject> questObjects;
    public List<QuestReward> questRewards;

    private void Awake()
    {
        if (string.IsNullOrEmpty(questID))
        {
            Debug.LogWarning($"Quest {questName} missing ID. Please assign manually to ensure consistent behavior.");
        }
    }
}

[System.Serializable]
public class QuestObject
{
    public string objectID;
    public string objectTitle;
    public ObjectType objectType;
    public int requiredAmount;
    public int currentAmount;

    [Tooltip("Nếu bật, hệ thống sẽ tính cả những item người chơi đã nhặt TRƯỚC khi nhận quest (Phù hợp cho Item giới hạn).")]
    public bool allowRetroactive;

    [Tooltip("Nếu TẮT, NPC sẽ KHÔNG thu hồi lại item này lúc trả quest (Dùng cho item đã bị tiêu hao lúc làm nhiệm vụ như gieo hạt, chế tạo).")]
    public bool removeItemOnComplete = true;

    public bool IsCompleted => currentAmount >= requiredAmount;
}

public enum ObjectType
{
    CollectItem,
    DefeatEnemy,
    ReachLocation,
    TalkNPC,
    PlantSeed,
    Custom
}

[System.Serializable]
public class QuestProgress
{
    public string questID;
    [System.NonSerialized] public Quest quest;
    public List<QuestObject> questObjects;

    [System.NonSerialized]
    public Dictionary<int, int> baselineCounts = new Dictionary<int, int>();

    public QuestProgress(Quest quest)
    {
        this.quest = quest;
        this.questID = quest.questID;
        questObjects = new List<QuestObject>();

        foreach (var obj in quest.questObjects)
        {
            questObjects.Add(new QuestObject
            {
                objectID = obj.objectID,
                objectTitle = obj.objectTitle,
                objectType = obj.objectType,
                requiredAmount = obj.requiredAmount,
                allowRetroactive = obj.allowRetroactive,
                removeItemOnComplete = obj.removeItemOnComplete,
                currentAmount = 0
            });
        }
    }

    public bool IsCompleted => questObjects.TrueForAll(qo => qo.IsCompleted);

    public string QuestID => quest.questID;
}

[System.Serializable]
public class QuestReward
{
    public RewardType rewardType;
    public int rewardID;
    public int amount = 1;
}

public enum RewardType
{
    Item,
    Coin,
    Gem,
    Experience,
    Custom
}