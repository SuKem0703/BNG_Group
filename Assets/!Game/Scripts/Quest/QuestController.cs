using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class QuestController : MonoBehaviour
{
    public static QuestController Instance { get; private set; }
    public List<QuestProgress> activeQuests = new();
    private QuestUI questUI;

    public List<string> handInQuestIDs = new();

    public static event System.Action<string> OnQuestStatusUpdated;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        questUI = FindFirstObjectByType<QuestUI>();

        if (InventoryController.Instance != null)
        {
            InventoryController.Instance.OnInventoryChanged += (data, slotCount) => CheckInventoryForQuest();
        }
        else
        {
            Debug.LogError("QuestController Awake: InventoryController.Instance is null!");
        }
    }

    public void AcceptQuest(Quest quest)
    {
        if (IsQuestActive(quest.questID)) return;

        QuestProgress newQuest = new QuestProgress(quest);

        Dictionary<int, int> currentItemCounts = InventoryController.Instance.GetItemCounts();

        foreach (var questObject in newQuest.questObjects)
        {
            if (questObject.objectType == ObjectType.CollectItem)
            {
                if (int.TryParse(questObject.objectID, out int itemID))
                {
                    int baseline = currentItemCounts.GetValueOrDefault(itemID, 0);
                    newQuest.baselineCounts[itemID] = baseline;
                }
            }
        }

        activeQuests.Add(newQuest);

        CheckInventoryForQuest();

        OnQuestStatusUpdated?.Invoke(quest.questID);
    }

    public bool IsQuestActive(string questID) => activeQuests.Exists(q => q.quest.questID == questID);

    public void CheckInventoryForQuest()
    {
        if (InventoryController.Instance == null) return;

        Dictionary<int, int> itemCounts = InventoryController.Instance.GetItemCounts();
        foreach (QuestProgress quest in activeQuests)
        {
            bool questProgressChanged = false;

            foreach (QuestObject questObject in quest.questObjects)
            {
                if (questObject.objectType != ObjectType.CollectItem) continue;
                if (!int.TryParse(questObject.objectID, out int itemID)) continue;

                int currentTotalCount = itemCounts.GetValueOrDefault(itemID, 0);

                int baselineCount = quest.baselineCounts.GetValueOrDefault(itemID, 0);

                int newAmount = currentTotalCount - baselineCount;

                if (newAmount < 0)
                {
                    newAmount = 0;
                }

                newAmount = Mathf.Min(newAmount, questObject.requiredAmount);

                if (questObject.currentAmount != newAmount)
                {
                    questObject.currentAmount = newAmount;

                    questProgressChanged = true;

                    if (questObject.IsCompleted)
                    {
                        Debug.Log($"Quest object {questObject.objectTitle} completed.");
                    }
                }
            }
            if (questProgressChanged)
            {
                OnQuestStatusUpdated?.Invoke(quest.QuestID);
            }
        }
        if (questUI != null) questUI.UpdateQuestUI();
    }

    public void MarkLocationReached(string locationID)
    {
        bool anyQuestUpdated = false;

        foreach (QuestProgress quest in activeQuests)
        {
            bool questProgressChanged = false;

            foreach (QuestObject questObject in quest.questObjects)
            {
                if (questObject.objectType == ObjectType.ReachLocation &&
                    questObject.objectID == locationID)
                {
                    if (questObject.currentAmount < questObject.requiredAmount)
                    {
                        questObject.currentAmount = questObject.requiredAmount;
                        questProgressChanged = true;

                        Debug.Log($"Location Reached: {questObject.objectTitle}");
                    }
                }
            }

            if (questProgressChanged)
            {
                OnQuestStatusUpdated?.Invoke(quest.QuestID);
                anyQuestUpdated = true;
            }
        }

        if (anyQuestUpdated)
        {
            questUI.UpdateQuestUI();
        }
    }

    public void MarkCropPlanted(int seedItemID)
    {
        bool anyQuestUpdated = false;

        foreach (QuestProgress quest in activeQuests)
        {
            bool questProgressChanged = false;

            foreach (QuestObject questObject in quest.questObjects)
            {
                if (questObject.objectType == ObjectType.PlantSeed)
                {
                    if (questObject.objectID == seedItemID.ToString())
                    {
                        if (questObject.currentAmount < questObject.requiredAmount)
                        {
                            questObject.currentAmount++;
                            questProgressChanged = true;
                        }
                    }
                }
            }

            if (questProgressChanged)
            {
                OnQuestStatusUpdated?.Invoke(quest.QuestID);
                anyQuestUpdated = true;
            }
        }

        if (anyQuestUpdated)
        {
            questUI.UpdateQuestUI();
        }
    }

    public void MarkEnemyDefeated(string enemyID)
    {
        bool anyQuestUpdated = false;

        foreach (QuestProgress quest in activeQuests)
        {
            bool questProgressChanged = false;

            foreach (QuestObject questObject in quest.questObjects)
            {
                if (questObject.objectType == ObjectType.DefeatEnemy &&
                    questObject.objectID == enemyID)
                {
                    if (questObject.currentAmount < questObject.requiredAmount)
                    {
                        questObject.currentAmount++;
                        questProgressChanged = true;

                        Debug.Log($"Enemy Defeated: {questObject.objectTitle} ({questObject.currentAmount}/{questObject.requiredAmount})");
                    }
                }
            }

            if (questProgressChanged)
            {
                OnQuestStatusUpdated?.Invoke(quest.QuestID);
                anyQuestUpdated = true;
            }
        }

        if (anyQuestUpdated)
        {
            questUI.UpdateQuestUI();
        }
    }
    public bool IsQuestCompleted(string questID)
    {
        QuestProgress quest = activeQuests.Find(q => q.QuestID == questID);
        return quest != null && quest.questObjects.TrueForAll(o => o.IsCompleted);
    }
    public void HandInQuest(string questID)
    {
        if (!RemoveRequiredItemsFromInventory(questID))
        {
            Debug.LogWarning($"Failed to remove required items for quest {questID}.");
            return;
        }
        QuestProgress quest = activeQuests.Find(q => q.QuestID == questID);

        if (quest != null)
        {
            handInQuestIDs.Add(questID);
            activeQuests.Remove(quest);
            questUI.UpdateQuestUI();

            OnQuestStatusUpdated?.Invoke(questID);
        }
    }
    public bool IsQuestHandedIn(string questID)
    {
        return handInQuestIDs.Contains(questID);
    }
    public bool RemoveRequiredItemsFromInventory(string questID)
    {
        QuestProgress quest = activeQuests.Find(q => q.QuestID == questID);
        if (quest == null) return false;

        Dictionary<int, int> requiredItems = new();

        foreach (QuestObject questObject in quest.questObjects)
        {
            if (questObject.objectType == ObjectType.CollectItem && int.TryParse(questObject.objectID, out int itemID))
            {
                requiredItems[itemID] = questObject.requiredAmount;
            }
        }

        Dictionary<int, int> itemCounts = InventoryController.Instance.GetItemCounts();
        foreach (var item in requiredItems)
        {
            if (itemCounts.GetValueOrDefault(item.Key) < item.Value)
            {
                Debug.LogWarning($"Not enough items in inventory to remove for quest {questID}. Required: {item.Value}, Available: {itemCounts.GetValueOrDefault(item.Key, 0)}");
                return false;
            }
        }

        foreach (var itemRequirement in requiredItems)
        {
            InventoryController.Instance.RemoveItemsFromInventory(itemRequirement.Key, itemRequirement.Value);
        }

        return true;
    }
    public void LoadQuestProgress(List<QuestProgress> saveQuest)
    {
        activeQuests = new List<QuestProgress>();

        Dictionary<int, int> currentItemCounts = InventoryController.Instance.GetItemCounts();

        if (saveQuest != null)
        {
            foreach (var savedProgress in saveQuest)
            {
                Quest questAsset = FindQuestByID(savedProgress.questID);
                if (questAsset != null)
                {
                    var progress = new QuestProgress(questAsset);
                    for (int i = 0; i < progress.questObjects.Count; i++)
                    {
                        if (i < savedProgress.questObjects.Count)
                        {
                            var savedObj = savedProgress.questObjects[i];
                            progress.questObjects[i].currentAmount = savedObj.currentAmount;

                            if (progress.questObjects[i].objectType == ObjectType.CollectItem)
                            {
                                if (int.TryParse(progress.questObjects[i].objectID, out int itemID))
                                {
                                    int totalCountInInventory = currentItemCounts.GetValueOrDefault(itemID, 0);

                                    int baseline = totalCountInInventory - savedObj.currentAmount;

                                    progress.baselineCounts[itemID] = baseline;
                                }
                            }
                        }
                    }
                    activeQuests.Add(progress);
                }
                else
                {
                    Debug.LogWarning($"Không tìm thấy Quest với ID: {savedProgress.questID}");
                }
            }
        }

        CheckInventoryForQuest();
    }


    public Quest FindQuestByID(string questID)
    {
        var allQuests = Resources.LoadAll<Quest>("Quests").ToList();

        foreach (var quest in allQuests)
        {
            if (quest.questID == questID)
                return quest;
        }
        return null;
    }

    public bool IsItemNeededForActiveQuest(int itemID)
    {
        foreach (QuestProgress quest in activeQuests)
        {
            foreach (QuestObject obj in quest.questObjects)
            {
                if (obj.objectType == ObjectType.CollectItem)
                {
                    if (int.TryParse(obj.objectID, out int questItemID))
                    {
                        if (questItemID == itemID) return true;
                    }
                }
                else if (obj.objectType == ObjectType.PlantSeed)
                {
                    if (int.TryParse(obj.objectID, out int seedItemID))
                    {
                        if (seedItemID == itemID) return true;
                    }
                }
            }
        }
        return false;
    }
}