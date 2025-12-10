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

[RequireComponent(typeof(CapsuleCollider2D), typeof(CircleCollider2D))]
public class NPC : MonoBehaviour, IInteractable, ITargetableInfo
{
    public event System.Action<QuestState> OnQuestStateUpdated;

    [Header("Danh sách hội thoại (theo thứ tự)")]
    public NPCDialogue[] dialogueDataList;

    [Header("Player Info")]
    [SerializeField] private string playerName = "Elric";
    [SerializeField] private Sprite playerPortrait;

    public NPCDialogue CurrentActiveDialogue { get; private set; }

    private DialogueController dialogueUI;
    private SaveController saveController;

    private int dialogueIndex;
    private bool isTyping;
    private bool isPlayerTalking;
    private System.Action pendingChoiceLogic;
    private string currentTypingText;

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

        if (playerPortrait == null)
        {
            playerPortrait = Resources.Load<Sprite>("Elric_Portrait");
        }
    }

    private void Start()
    {
        dialogueUI = DialogueController.instance;

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

    public bool CanInteract()
    {
        return GameStateManager.CanProcessInput();
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
        if (CurrentActiveDialogue == null || (PauseController.IsGamePause && !GameStateManager.IsDialogueActive))
            return;

        if (!GameStateManager.IsDialogueActive)
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
        if (CurrentActiveDialogue == null || (PauseController.IsGamePause && !GameStateManager.IsDialogueActive))
            return;

        if (GameStateManager.IsDialogueActive)
        {
            NextLine();
        }
        else
        {
            if (triggerOnEnter == true && GameStateManager.IsDialogueActive == false) return;
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

        isPlayerTalking = false;
        pendingChoiceLogic = null;
        currentTypingText = "";

        GameStateManager.IsDialogueActive = true;
        OnQuestStateUpdated?.Invoke(CurrentQuestState);
        GameStateManager.CanOpenMenu = false;
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
        if (isPlayerTalking)
        {
            if (isTyping)
            {
                StopAllCoroutines();
                dialogueUI.SetDialogueText(currentTypingText);
                isTyping = false;
                dialogueUI.continueIndicator.gameObject.SetActive(true);
            }
            else
            {
                pendingChoiceLogic?.Invoke();
            }
            return;
        }

        if (isTyping)
        {
            StopAllCoroutines();

            dialogueUI.SetDialogueText(currentTypingText);
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

        // Kiểm tra Choice
        foreach (DialogueChoice dialogueChoice in CurrentActiveDialogue.choices)
        {
            if (dialogueChoice.dialogueIndex == dialogueIndex)
            {
                DisplayChoices(dialogueChoice);
                return;
            }
        }

        // Nếu không có choice thì tăng dòng
        if (++dialogueIndex < CurrentActiveDialogue.dialogueLines.Length)
        {
            DisplayCurrentLine();
        }
        else
        {
            EndDialogue();
        }
    }

    void CheckForChoices()
    {
        // Hàm này có thể bỏ nếu không dùng trong Auto Progress
        // Hoặc giữ lại nếu bạn muốn Auto Progress tự chuyển sang Choice
        foreach (DialogueChoice dialogueChoice in CurrentActiveDialogue.choices)
        {
            if (dialogueChoice.dialogueIndex == dialogueIndex)
            {
                DisplayChoices(dialogueChoice);
                return;
            }
        }
    }

    IEnumerator TypeLine()
    {
        isTyping = true;
        dialogueUI.SetDialogueText("");
        dialogueUI.continueIndicator.gameObject.SetActive(false);

        currentTypingText = CurrentActiveDialogue.dialogueLines[dialogueIndex];

        if (string.IsNullOrWhiteSpace(currentTypingText))
        {
            isTyping = false;
            CheckForChoices();
            yield break;
        }

        dialogueUI.SetNPCInfo(CurrentActiveDialogue.npcName, CurrentActiveDialogue.npcPortrait);

        foreach (char letter in currentTypingText)
        {
            dialogueUI.SetDialogueText(dialogueUI.dialogueText.text += letter);
            yield return new WaitForSeconds(CurrentActiveDialogue.typingSpeed);
        }

        isTyping = false;
        dialogueUI.continueIndicator.gameObject.SetActive(true);

        // Auto Progress Logic
        if (CurrentActiveDialogue.autoProgressLines.Length > dialogueIndex && CurrentActiveDialogue.autoProgressLines[dialogueIndex])
        {
            dialogueUI.continueIndicator.gameObject.SetActive(false);
            yield return new WaitForSeconds(CurrentActiveDialogue.autoProgressDelay);
            NextLine();
        }
    }

    void DisplayChoices(DialogueChoice choice)
    {
        dialogueUI.continueIndicator.gameObject.SetActive(false);

        // Nếu chỉ có 1 lựa chọn, tự động chọn luôn
        if (choice.choices.Length == 1)
        {
            string choiceText = choice.choices[0];
            int nextIndex = (choice.nextDialogueIndexes != null && 0 < choice.nextDialogueIndexes.Length) ? choice.nextDialogueIndexes[0] : -1;
            bool giveQuest = (choice.giveQuest != null && 0 < choice.giveQuest.Length) ? choice.giveQuest[0] : false;
            SpecialActionType specialAction = (choice.specialActions != null && 0 < choice.specialActions.Length) ? choice.specialActions[0] : SpecialActionType.None;
            Object specialTarget = (choice.specialTargets != null && 0 < choice.specialTargets.Length) ? choice.specialTargets[0] : null;

            OnPlayerSelectedOption(choiceText, nextIndex, giveQuest, specialAction, specialTarget);
            return;
        }

        // Nếu có nhiều lựa chọn, hiển thị nút bấm
        for (int i = 0; i < choice.choices.Length; i++)
        {
            int nextIndex = (choice.nextDialogueIndexes != null && i < choice.nextDialogueIndexes.Length) ? choice.nextDialogueIndexes[i] : -1;
            bool giveQuest = (choice.giveQuest != null && i < choice.giveQuest.Length) ? choice.giveQuest[i] : false;
            SpecialActionType specialAction = (choice.specialActions != null && i < choice.specialActions.Length) ? choice.specialActions[i] : SpecialActionType.None;
            Object specialTarget = (choice.specialTargets != null && i < choice.specialTargets.Length) ? choice.specialTargets[i] : null;
            string choiceText = choice.choices[i];

            dialogueUI.CreateChoiceButton(choiceText, () =>
                OnPlayerSelectedOption(choiceText, nextIndex, giveQuest, specialAction, specialTarget));
        }
    }

    void OnPlayerSelectedOption(string textToSay, int nextIndex, bool giveQuest, SpecialActionType action, Object target)
    {
        dialogueUI.ClearChoices();
        isPlayerTalking = true;
        dialogueUI.SetNPCInfo(playerName, playerPortrait);

        StartCoroutine(TypePlayerLine(textToSay));

        pendingChoiceLogic = () =>
        {
            ExecuteChoiceLogic(nextIndex, giveQuest, action, target);
        };
    }

    IEnumerator TypePlayerLine(string text)
    {
        isTyping = true;
        currentTypingText = text;
        dialogueUI.SetDialogueText("");
        dialogueUI.continueIndicator.gameObject.SetActive(false);

        foreach (char letter in text)
        {
            dialogueUI.SetDialogueText(dialogueUI.dialogueText.text += letter);
            yield return new WaitForSeconds(CurrentActiveDialogue.typingSpeed);
        }

        isTyping = false;

        yield return new WaitForSeconds(1.5f);

        pendingChoiceLogic?.Invoke();
    }

    void ExecuteChoiceLogic(int nextIndex, bool giveQuest, SpecialActionType action, Object target)
    {
        isPlayerTalking = false;
        pendingChoiceLogic = null;

        //if (CurrentActiveDialogue != null)
        //{
        //    dialogueUI.SetNPCInfo(CurrentActiveDialogue.npcName, CurrentActiveDialogue.npcPortrait);
        //}

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
                    if (shop != null) { EndDialogue(); shop.OpenShop(); }
                    else Debug.LogWarning("Target GameObject không chứa NPCShop.");
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
        if (!GameStateManager.IsDialogueActive) return;

        if (CurrentActiveDialogue != null && CurrentActiveDialogue.quest != null &&
            CurrentQuestState == QuestState.Completed && !QuestController.Instance.IsQuestHandedIn(CurrentActiveDialogue.quest.questID))
        {
            HandleQuestCompletion(CurrentActiveDialogue.quest);
        }

        StopAllCoroutines();
        GameStateManager.IsDialogueActive = false;
        GameStateManager.CanOpenMenu = true;
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
        if (justAcceptedQuest) justAcceptedQuest = false;

        isPlayerTalking = false;
        pendingChoiceLogic = null;

        UpdateActiveDialogue();
        SyncQuestState();
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
            return new TargetInfoData(CurrentActiveDialogue.npcName, CurrentActiveDialogue.npcPortrait, "Nói chuyện", TargetType.NPC);
        return new TargetInfoData(gameObject.name, null, "Nói chuyện", TargetType.NPC);
    }
}