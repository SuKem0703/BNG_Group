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
        if (!SaveController.IsDataLoaded)
        {
            SaveController.OnDataLoaded += HandleDataLoaded;
        }
    }

    private void OnEnable()
    {
        QuestController.OnQuestStatusUpdated += HandleQuestUpdate;
        
        if (SaveController.IsDataLoaded)
        {
            RefreshState();
        }
    }

    private void OnDisable()
    {
        QuestController.OnQuestStatusUpdated -= HandleQuestUpdate;
    }

    private void OnDestroy()
    {
        SaveController.OnDataLoaded -= HandleDataLoaded;
    }

    private void HandleDataLoaded()
    {
        RefreshState();
        SaveController.OnDataLoaded -= HandleDataLoaded;
    }

    private void HandleQuestUpdate(string updatedQuestID)
    {
        if (npc.CurrentActiveDialogue == null || npc.CurrentActiveDialogue.quest == null) return;
        if (npc.CurrentActiveDialogue.quest.questID == updatedQuestID)
        {
            SyncQuestState();
            UpdateVisibility();
        }
    }

    public void UpdateVisibility()
    {
        if (QuestController.Instance == null || npc.dialogueDataList == null) return;

        bool shouldHide = false;

        for (int i = npc.dialogueDataList.Length - 1; i >= 0; i--)
        {
            NPCDialogueData data = npc.dialogueDataList[i];
            if (data == null || data.quest == null) continue;

            string qID = data.quest.questID;
            bool isHandedIn = QuestController.Instance.IsQuestHandedIn(qID);
            bool isCompleted = QuestController.Instance.IsQuestCompleted(qID);
            bool isActive = QuestController.Instance.IsQuestActive(qID);

            if (isActive)
            {
                if (isCompleted && data.hideWhenCompleted) shouldHide = true;
                else if (!isCompleted && data.hideWhenInProgress) shouldHide = true;
                break;
            }
            else if (isHandedIn)
            {
                if (data.hideWhenHandedIn) shouldHide = true;
                break;
            }
            else
            {
                if (data.hideWhenNotStarted) shouldHide = true;
                break;
            }
        }

        gameObject.SetActive(!shouldHide);
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
            bool needsFading = false;
            Action logicToExecute = null;

            if (CurrentQuestState == QuestState.Completed && !QuestController.Instance.IsQuestHandedIn(npc.CurrentActiveDialogue.quest.questID))
            {
                needsFading = true;
                logicToExecute = () =>
                {
                    RewardController.Instance?.GiveQuestReward(npc.CurrentActiveDialogue.quest);
                    QuestController.Instance?.HandInQuest(npc.CurrentActiveDialogue.quest.questID);

                    CurrentQuestState = QuestState.NoMoreQuests;
                    RefreshState();
                };
            }
            else if (CurrentQuestState == QuestState.NotStarted && npc.CurrentActiveDialogue.autoGiveQuestOnEnd)
            {
                needsFading = true;
                logicToExecute = () =>
                {
                    QuestController.Instance.AcceptQuest(npc.CurrentActiveDialogue.quest);
                    RefreshState();
                };
            }

            if (needsFading && logicToExecute != null)
            {
                ScreenFader.FadeAndExecute(0.5f, logicToExecute);
            }
            else
            {
                RefreshState();
            }
        }
        else
        {
            RefreshState();
        }
    }

    private void RefreshState()
    {
        UpdateActiveDialogue();
        SyncQuestState();
        UpdateVisibility();
    }
}