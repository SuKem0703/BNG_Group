using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Quests/Quest")]
public class Quest : ScriptableObject
{
    public string questID;
    public string questName;
    public string questTitle;
    public List<QuestObject> questObjects;
    public List<QuestReward> questRewards;

    //private void OnValidate()
    //{
    //    if (string.IsNullOrEmpty(questID))
    //    {
    //        questID = questName + Guid.NewGuid().ToString();
    //    }
    //}
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

    // Runtime-only: baseline counts for collect-item quests at the moment the quest was accepted or loaded
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
                currentAmount = 0
            });
        }

        // baselineCounts will be set by QuestController when the quest is accepted (based on current inventory)
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