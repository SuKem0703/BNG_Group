using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum SpecialActionType
{
    None,
    OpenShop,
    OpenUpgrade
}


public class NPC : MonoBehaviour, IInteractable, ITargetableInfo
{
    public event System.Action<QuestState> OnQuestStateUpdated;

    [Header("Danh sách hội thoại (theo thứ tự)")]
    public NPCDialogue[] dialogueDataList;

    public NPCDialogue CurrentActiveDialogue { get; private set; }

    private DialogueController dialogueUI;
    private SaveController saveController;

    private int dialogueIndex;
    private bool isTyping;
    public bool IsDialogueActive { get; private set; }

    public bool triggerOnEnter = false;

    private bool maintainPauseAfterDialogue = false;
    private bool justAcceptedQuest = false;

    public enum QuestState
    {
        NotStarted,
        InProgress,
        Completed,
        NoMoreQuests
    }
    public QuestState CurrentQuestState { get; private set; } = QuestState.NotStarted;


    private void Awake()
    {
        if (dialogueDataList == null) return;

        foreach (NPCDialogue dialogueData in dialogueDataList)
        {
            if (dialogueData == null || dialogueData.choices == null) continue;

            foreach (var choice in dialogueData.choices)
            {
                if (choice.specialActions == null || choice.specialTargetNames == null)
                    continue;

                choice.specialTargets = new Object[choice.specialTargetNames.Length];

                for (int i = 0; i < choice.specialTargetNames.Length; i++)
                {
                    if (!string.IsNullOrEmpty(choice.specialTargetNames[i]))
                    {
                        GameObject go = GameObject.Find(choice.specialTargetNames[i]);
                        if (go != null)
                        {
                            choice.specialTargets[i] = go;
                        }
                        else
                        {
                            Debug.LogWarning($"Không tìm thấy GameObject tên: {choice.specialTargetNames[i]}");
                        }
                    }
                }
            }
        }
    }

    private void Start()
    {
        dialogueUI = DialogueController.instance;

        // Đăng ký sự kiện Load Data
        if (SaveController.IsDataLoaded)
        {
            UpdateActiveDialogue();
            SyncQuestState();
        }
        else
        {
            SaveController.OnDataLoaded += HandleDataLoaded;
        }

        // Đăng ký sự kiện cập nhật trạng thái Quest
        QuestController.OnQuestStatusUpdated += HandleQuestUpdate;
    }
    private void OnDestroy()
    {
        SaveController.OnDataLoaded -= HandleDataLoaded;

        QuestController.OnQuestStatusUpdated -= HandleQuestUpdate;
    }
    private void HandleDataLoaded()
    {
        Debug.Log($"NPC {gameObject.name} nhận được event OnDataLoaded, đang đồng bộ trạng thái Quest.");

        UpdateActiveDialogue();
        SyncQuestState();

        SaveController.OnDataLoaded -= HandleDataLoaded;
    }
    private void HandleQuestUpdate(string updatedQuestID)
    {
        if (CurrentActiveDialogue == null || CurrentActiveDialogue.quest == null)
            return;

        if (CurrentActiveDialogue.quest.questID == updatedQuestID)
        {
            SyncQuestState();
        }
    }
    private void UpdateActiveDialogue()
    {
        if (dialogueDataList == null || dialogueDataList.Length == 0)
        {
            CurrentActiveDialogue = null;
            return;
        }

        foreach (NPCDialogue data in dialogueDataList)
        {
            if (data.quest != null)
            {
                string questID = data.quest.questID;
                if (QuestController.Instance != null && !QuestController.Instance.IsQuestHandedIn(questID))
                {
                    CurrentActiveDialogue = data;
                    return;
                }
            }
            else
            {
                CurrentActiveDialogue = data;
                return;
            }
        }

        CurrentActiveDialogue = dialogueDataList[dialogueDataList.Length - 1];
    }


    private void Update()
    {
        if (!IsDialogueActive)
        {
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (dialogueUI != null && dialogueUI.choiceContainer.childCount > 0)
            {
                return;
            }

            NextLine();
        }
    }

    public bool CanInteract()
    {
        return !IsDialogueActive;
    }


    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!triggerOnEnter || !collision.CompareTag("Player"))
            return;

        UpdateActiveDialogue();
        SyncQuestState();

        if (CurrentActiveDialogue == null) return;

        if (CurrentActiveDialogue.triggerOnEnter_NotStarted && CurrentQuestState == QuestState.NotStarted)
        {
            OpenDialogOnTrigger();
        }
        else if (CurrentActiveDialogue.triggerOnEnter_InProgress && CurrentQuestState == QuestState.InProgress)
        {
            OpenDialogOnTrigger();
        }
        else if (CurrentActiveDialogue.triggerOnEnter_Completed && CurrentQuestState == QuestState.Completed)
        {
            OpenDialogOnTrigger();
        }
        else if (CurrentActiveDialogue.triggerOnEnter_NoMoreQuests && CurrentQuestState == QuestState.NoMoreQuests)
        {
            OpenDialogOnTrigger();
        }
    }
    public void OpenDialogOnTrigger()
    {
        if (CurrentActiveDialogue == null || (PauseController.IsGamePause && !IsDialogueActive))
            return;

        if (!IsDialogueActive)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                PlayerMovement playerMovement = player.GetComponent<PlayerMovement>();
                if (playerMovement != null)
                {
                    playerMovement.LookTowards(transform.position);
                }
            }

            StartDialogue();
            Debug.Log("Hội thoại bắt đầu khi vào vùng kích hoạt.");
        }
    }
    public void Interact()
    {
        UpdateActiveDialogue();
        if (CurrentActiveDialogue == null || (PauseController.IsGamePause && !IsDialogueActive))
            return;

        if (IsDialogueActive)
        {
            NextLine();
        }
        else
        {
            if (triggerOnEnter == true && IsDialogueActive == false) return;

            Debug.Log("Hội thoại bắt đầu khi tương tác.");
            StartDialogue();
        }
    }

    void StartDialogue()
    {
        SyncQuestState();

        if (CurrentQuestState == QuestState.NotStarted)
        {
            dialogueIndex = 0;
        }
        else if (CurrentQuestState == QuestState.InProgress)
        {
            dialogueIndex = CurrentActiveDialogue.questInProgressIndex;
        }
        else if (CurrentQuestState == QuestState.Completed)
        {
            dialogueIndex = CurrentActiveDialogue.questCompletedIndex;
        }
        else if (CurrentQuestState == QuestState.NoMoreQuests)
        {
            dialogueIndex = CurrentActiveDialogue.noMoreQuestsIndex;
        }

        IsDialogueActive = true;

        OnQuestStateUpdated?.Invoke(CurrentQuestState);

        MenuController.CanOpenMenu = false;

        CommonUIController.Instance?.SetUIVisible(false);
        dialogueUI.ClearChoices();
        dialogueUI.SetNPCInfo(CurrentActiveDialogue.npcName, CurrentActiveDialogue.npcPortrait);
        dialogueUI.ShowDialogueUI(true);
        PauseController.SetPause(true);

        DisplayCurrentLine();
    }

    private void SyncQuestState()
    {
        if (CurrentActiveDialogue == null)
            return;

        var qc = QuestController.Instance;
        var quest = CurrentActiveDialogue.quest;

        if (quest == null)
        {
            CurrentQuestState = QuestState.NoMoreQuests;
            OnQuestStateUpdated?.Invoke(CurrentQuestState);
            return;
        }

        string id = quest.questID;

        if (qc.IsQuestHandedIn(id))
        {
            CurrentQuestState = QuestState.NoMoreQuests;
        }

        else if (!qc.IsQuestActive(id))
        {
            CurrentQuestState = QuestState.NotStarted;
        }

        else
        {
            if (qc.IsQuestCompleted(id))
                CurrentQuestState = QuestState.Completed;
            else
                CurrentQuestState = QuestState.InProgress;
        }

        OnQuestStateUpdated?.Invoke(CurrentQuestState);
    }


    void NextLine()
    {
        if (isTyping)
        {
            StopAllCoroutines();
            dialogueUI.SetDialogueText(CurrentActiveDialogue.dialogueLines[dialogueIndex]);
            isTyping = false;
            dialogueUI.continueIndicator.gameObject.SetActive(true);
            return;
        }

        dialogueUI.continueIndicator.gameObject.SetActive(false);
        dialogueUI.ClearChoices();

        if (CurrentActiveDialogue.dialogueLines.Length > dialogueIndex && CurrentActiveDialogue.endDialogueLines[dialogueIndex])
        {
            EndDialogue();
            return;
        }

        foreach (DialogueChoice dialogueChoice in CurrentActiveDialogue.choices)
        {
            if (dialogueChoice.dialogueIndex == dialogueIndex)
            {
                DisplayChoices(dialogueChoice);
                return;
            }
        }

        if (++dialogueIndex < CurrentActiveDialogue.dialogueLines.Length)
        {
            DisplayCurrentLine();
        }
        else
        {
            EndDialogue();
        }
    }

    IEnumerator TypeLine()
    {
        isTyping = true;
        dialogueUI.SetDialogueText("");
        dialogueUI.continueIndicator.gameObject.SetActive(false);

        foreach (char letter in CurrentActiveDialogue.dialogueLines[dialogueIndex])
        {
            dialogueUI.SetDialogueText(dialogueUI.dialogueText.text += letter);
            yield return new WaitForSeconds(CurrentActiveDialogue.typingSpeed);
        }

        isTyping = false;

        dialogueUI.continueIndicator.gameObject.SetActive(true);

        if (CurrentActiveDialogue.autoProgressLines.Length > dialogueIndex && CurrentActiveDialogue.autoProgressLines[dialogueIndex])
        {
            dialogueUI.continueIndicator.gameObject.SetActive(false);
            yield return new WaitForSeconds(CurrentActiveDialogue.autoProgressDelay);
            NextLine();
        }
    }

    void DisplayChoices(DialogueChoice choice)
    {
        for (int i = 0; i < choice.choices.Length; i++)
        {
            int nextIndex = (choice.nextDialogueIndexes != null && i < choice.nextDialogueIndexes.Length)
                ? choice.nextDialogueIndexes[i] : -1;

            bool giveQuest = (choice.giveQuest != null && i < choice.giveQuest.Length)
                ? choice.giveQuest[i] : false;

            SpecialActionType specialAction = (choice.specialActions != null && i < choice.specialActions.Length)
                ? choice.specialActions[i] : SpecialActionType.None;

            Object specialTarget = (choice.specialTargets != null && i < choice.specialTargets.Length)
                ? choice.specialTargets[i] : null;

            dialogueUI.CreateChoiceButton(choice.choices[i], () =>
                ChooseOption(nextIndex, giveQuest, specialAction, specialTarget));
        }

    }
    void ChooseOption(int nextIndex, bool giveQuest, SpecialActionType action, Object target)
    {
        if (action != SpecialActionType.None)
        {
            maintainPauseAfterDialogue = true;
            TriggerSpecialAction(action, target);
        }

        if (giveQuest)
        {
            QuestController.Instance.AcceptQuest(CurrentActiveDialogue.quest);
            justAcceptedQuest = true;
            EndDialogue();
            Debug.Log($"Đã nhận nhiệm vụ: {CurrentActiveDialogue.quest.questName}");
        }

        if (nextIndex == -1)
        {
            EndDialogue();
            return;
        }

        dialogueIndex = nextIndex;
        dialogueUI.ClearChoices();
        DisplayCurrentLine();
    }
    void TriggerSpecialAction(SpecialActionType action, Object target)
    {
        switch (action)
        {
            case SpecialActionType.OpenShop:
                if (target is GameObject go)
                {
                    NPCShop shop = go.GetComponent<NPCShop>();
                    if (shop != null)
                    {
                        EndDialogue();
                        shop.OpenShop();
                    }
                    else
                    {
                        Debug.LogWarning("Target GameObject không chứa NPCShop.");
                    }
                }
                break;

            case SpecialActionType.OpenUpgrade:
                EndDialogue();
                Debug.Log("Open Upgrade UI");
                break;
        }
    }
    void DisplayCurrentLine()
    {
        StopAllCoroutines();
        StartCoroutine(TypeLine());
    }

    public void EndDialogue()
    {
        if (CurrentActiveDialogue != null && CurrentActiveDialogue.quest != null &&
            CurrentQuestState == QuestState.Completed && !QuestController.Instance.IsQuestHandedIn(CurrentActiveDialogue.quest.questID))
        {
            HandleQuestCompletion(CurrentActiveDialogue.quest);
        }

        StopAllCoroutines();
        IsDialogueActive = false;
        MenuController.CanOpenMenu = true;
        dialogueUI.continueIndicator.gameObject.SetActive(false);

        dialogueUI.SetDialogueText("");
        dialogueUI.ShowDialogueUI(false);
        dialogueUI.ClearChoices();

        if (!maintainPauseAfterDialogue)
        {
            CommonUIController.Instance?.SetUIVisible(true);
            PauseController.SetPause(false);
        }

        maintainPauseAfterDialogue = false;

        if (justAcceptedQuest)
        {
            justAcceptedQuest = false;
        }

        UpdateActiveDialogue();
        SyncQuestState(); // Cập nhật lại trạng thái (và indicator)

        saveController = Object.FindFirstObjectByType<SaveController>();
        saveController.SaveGame();
    }
    void HandleQuestCompletion(Quest quest)
    {
        RewardController.Instance.GiveQuestReward(quest);
        QuestController.Instance.HandInQuest(quest.questID);
        CurrentQuestState = QuestState.NoMoreQuests;
    }

    public TargetInfoData GetInfo()
    {
        if (CurrentActiveDialogue != null)
        {
            return new TargetInfoData(
                CurrentActiveDialogue.npcName,
                CurrentActiveDialogue.npcPortrait,
                "Nói chuyện",
                TargetType.NPC
            );
        }

        return new TargetInfoData(
            gameObject.name,
            null,
            "Nói chuyện",
            TargetType.NPC
        );
    }
}