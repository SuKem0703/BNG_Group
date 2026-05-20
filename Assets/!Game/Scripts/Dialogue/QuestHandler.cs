using System;
using UnityEngine;

public enum QuestState
{
    NotStarted,
    InProgress,
    Completed,
    NoMoreQuests
}

[RequireComponent(typeof(NPC))]
public class QuestHandler : MonoBehaviour
{
    public QuestState CurrentQuestState { get; private set; } = QuestState.NotStarted;

    public event Action<QuestState> OnQuestStateUpdated;

    private NPC npc;

    private void Awake()
    {
        npc = GetComponent<NPC>();
    }

    private void Start()
    {
        if (SaveController.IsDataLoaded)
        {
            UpdateActiveDialogue();
            SyncQuestState();
        }
        else
        {
            SaveController.OnDataLoaded += HandleDataLoaded;
        }

        QuestController.OnQuestStatusUpdated += HandleQuestUpdate;
    }

    private void OnDestroy()
    {
        SaveController.OnDataLoaded -= HandleDataLoaded;
        QuestController.OnQuestStatusUpdated -= HandleQuestUpdate;
    }

    private void HandleDataLoaded()
    {
        UpdateActiveDialogue();
        SyncQuestState();
        SaveController.OnDataLoaded -= HandleDataLoaded;
    }

    private void HandleQuestUpdate(string updatedQuestID)
    {
        if (npc.CurrentActiveDialogue == null || npc.CurrentActiveDialogue.quest == null) return;
        if (npc.CurrentActiveDialogue.quest.questID == updatedQuestID) SyncQuestState();
    }

    public void UpdateActiveDialogue()
    {
        if (npc.dialogueDataList == null || npc.dialogueDataList.Length == 0)
        {
            npc.CurrentActiveDialogue = null;
            return;
        }

        foreach (NPCDialogueData data in npc.dialogueDataList)
        {
            if (data.quest != null)
            {
                if (QuestController.Instance != null && !QuestController.Instance.IsQuestHandedIn(data.quest.questID))
                {
                    npc.CurrentActiveDialogue = data;
                    return;
                }
            }
            else
            {
                npc.CurrentActiveDialogue = data;
                return;
            }
        }
        npc.CurrentActiveDialogue = npc.dialogueDataList[npc.dialogueDataList.Length - 1];
    }

    public void SyncQuestState()
    {
        if (npc.CurrentActiveDialogue == null) return;

        var qc = QuestController.Instance;
        var quest = npc.CurrentActiveDialogue.quest;

        if (quest == null)
        {
            CurrentQuestState = QuestState.NoMoreQuests;
        }
        else
        {
            string id = quest.questID;
            if (qc.IsQuestHandedIn(id)) CurrentQuestState = QuestState.NoMoreQuests;
            else if (!qc.IsQuestActive(id)) CurrentQuestState = QuestState.NotStarted;
            else if (qc.IsQuestCompleted(id)) CurrentQuestState = QuestState.Completed;
            else CurrentQuestState = QuestState.InProgress;
        }

        OnQuestStateUpdated?.Invoke(CurrentQuestState);
    }

    public int GetStartingDialogueIndex()
    {
        if (npc.CurrentActiveDialogue == null) return 0;

        switch (CurrentQuestState)
        {
            case QuestState.InProgress: return npc.CurrentActiveDialogue.questInProgressIndex;
            case QuestState.Completed: return npc.CurrentActiveDialogue.questCompletedIndex;
            case QuestState.NoMoreQuests: return npc.CurrentActiveDialogue.noMoreQuestsIndex;
            default: return 0;
        }
    }

    public void OnDialogueEnded()
    {
        if (npc.CurrentActiveDialogue != null && npc.CurrentActiveDialogue.quest != null)
        {
            if (CurrentQuestState == QuestState.Completed && !QuestController.Instance.IsQuestHandedIn(npc.CurrentActiveDialogue.quest.questID))
            {
                RewardController.Instance.GiveQuestReward(npc.CurrentActiveDialogue.quest);
                QuestController.Instance.HandInQuest(npc.CurrentActiveDialogue.quest.questID);
                CurrentQuestState = QuestState.NoMoreQuests;
            }
            else if (CurrentQuestState == QuestState.NotStarted && npc.CurrentActiveDialogue.autoGiveQuestOnEnd)
            {
                QuestController.Instance.AcceptQuest(npc.CurrentActiveDialogue.quest);
                Debug.Log($"[NPC] Đã tự động nhận nhiệm vụ: {npc.CurrentActiveDialogue.quest.questName}");
            }
        }

        UpdateActiveDialogue();
        SyncQuestState();
    }
}