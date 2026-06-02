using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueController : MonoBehaviour
{
    public static DialogueController instance { get; private set; }

    [Header("UI References")]
    public GameObject dialoguePanel;
    public TMP_Text dialogueText, nameText;
    public Image portraitImage;
    public Transform choiceContainer;
    public Image continueIndicator;
    public GameObject choiceButtonPrefab;

    private BaseDialogueData currentDialogue;
    private int dialogueIndex;

    private string playerName;
    private Sprite playerPortrait;

    private string currentSpeakerName;
    private Sprite currentSpeakerPortrait;

    private bool isTyping;
    private bool isPlayerTalking;
    private string currentTypingText;

    private Action pendingChoiceLogic;
    private Action onDialogueEnded;

    void Awake()
    {
        if (continueIndicator != null) continueIndicator.gameObject.SetActive(false);

        if (instance == null) { instance = this; }
        else { Destroy(gameObject); }
    }

    private void Start()
    {
        dialoguePanel.SetActive(false);
    }

    public void StartDialogue(BaseDialogueData dialogue, int startIndex, string pName, Sprite pPortrait, string speakerName, Sprite speakerPortrait, Action onEnded)
    {
        currentDialogue = dialogue;
        dialogueIndex = startIndex;

        playerName = pName;
        playerPortrait = pPortrait;

        currentSpeakerName = speakerName;
        currentSpeakerPortrait = speakerPortrait;

        onDialogueEnded = onEnded;

        isPlayerTalking = false;
        pendingChoiceLogic = null;
        currentTypingText = "";

        GameStateManager.IsDialogueActive = true;
        GameStateManager.CanOpenMenu = false;
        CommonUIController.Instance?.SetUIVisible(false);
        PauseController.SetPause(true);

        ClearChoices();
        SetNPCInfo(currentSpeakerName, currentSpeakerPortrait);
        dialoguePanel.SetActive(true);

        DisplayCurrentLine();
    }

    public void EndDialogue()
    {
        if (!GameStateManager.IsDialogueActive) return;

        StopAllCoroutines();

        if (continueIndicator != null) continueIndicator.gameObject.SetActive(false);
        if (dialogueText != null) dialogueText.text = "";
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        ClearChoices();

        isPlayerTalking = false;
        pendingChoiceLogic = null;

        GameStateManager.IsDialogueActive = false;
        GameStateManager.CanOpenMenu = true;
        CommonUIController.Instance?.SetUIVisible(true);
        PauseController.SetPause(false);

        onDialogueEnded?.Invoke();
    }

    public void NextLine()
    {
        if (isPlayerTalking)
        {
            if (isTyping)
            {
                StopAllCoroutines();
                dialogueText.text = currentTypingText;
                isTyping = false;
                if (continueIndicator != null) continueIndicator.gameObject.SetActive(true);
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
            dialogueText.text = currentTypingText;
            isTyping = false;
            if (continueIndicator != null) continueIndicator.gameObject.SetActive(true);
            return;
        }

        if (continueIndicator != null) continueIndicator.gameObject.SetActive(false);
        ClearChoices();

        if (currentDialogue.endDialogueLines != null && currentDialogue.dialogueLines.Length > dialogueIndex && currentDialogue.endDialogueLines[dialogueIndex])
        {
            EndDialogue();
            return;
        }

        foreach (var choice in currentDialogue.GetChoices())
        {
            if (choice.dialogueIndex == dialogueIndex)
            {
                DisplayChoices(choice);
                return;
            }
        }

        if (++dialogueIndex < currentDialogue.dialogueLines.Length)
        {
            DisplayCurrentLine();
        }
        else
        {
            EndDialogue();
        }
    }

    private void DisplayCurrentLine()
    {
        StopAllCoroutines();
        StartCoroutine(TypeLine());
    }

    private IEnumerator TypeLine()
    {
        isTyping = true;
        dialogueText.text = "";
        if (continueIndicator != null) continueIndicator.gameObject.SetActive(false);
        currentTypingText = currentDialogue.dialogueLines[dialogueIndex];

        if (string.IsNullOrWhiteSpace(currentTypingText))
        {
            isTyping = false;
            CheckForChoices();
            yield break;
        }

        SetNPCInfo(currentSpeakerName, currentSpeakerPortrait);

        foreach (char letter in currentTypingText)
        {
            dialogueText.text += letter;
            yield return new WaitForSecondsRealtime(currentDialogue.typingSpeed);
        }

        isTyping = false;
        if (continueIndicator != null) continueIndicator.gameObject.SetActive(true);

        if (currentDialogue.autoProgressLines != null && currentDialogue.autoProgressLines.Length > dialogueIndex && currentDialogue.autoProgressLines[dialogueIndex])
        {
            if (continueIndicator != null) continueIndicator.gameObject.SetActive(false);
            yield return new WaitForSecondsRealtime(currentDialogue.autoProgressDelay);
            NextLine();
        }
    }

    private IEnumerator TypePlayerLine(string text)
    {
        isTyping = true;
        currentTypingText = text;
        dialogueText.text = "";
        if (continueIndicator != null) continueIndicator.gameObject.SetActive(false);

        foreach (char letter in text)
        {
            dialogueText.text += letter;
            yield return new WaitForSecondsRealtime(currentDialogue.typingSpeed);
        }

        isTyping = false;
        yield return new WaitForSecondsRealtime(1.5f);
        pendingChoiceLogic?.Invoke();
    }

    private void CheckForChoices()
    {
        foreach (var choice in currentDialogue.GetChoices())
        {
            if (choice.dialogueIndex == dialogueIndex)
            {
                DisplayChoices(choice);
                return;
            }
        }
    }

    private void DisplayChoices(DialogueChoice choice)
    {
        if (continueIndicator != null) continueIndicator.gameObject.SetActive(false);

        if (choice.choices.Length == 1)
        {
            string choiceText = choice.choices[0];
            int nextIndex = (choice.nextDialogueIndexes != null && 0 < choice.nextDialogueIndexes.Length) ? choice.nextDialogueIndexes[0] : -1;
            bool giveQuest = (choice.giveQuest != null && 0 < choice.giveQuest.Length) ? choice.giveQuest[0] : false;
            SpecialActionType specialAction = (choice.specialActions != null && 0 < choice.specialActions.Length) ? choice.specialActions[0] : SpecialActionType.None;
            UnityEngine.Object specialTarget = (choice.specialTargets != null && 0 < choice.specialTargets.Length) ? choice.specialTargets[0] : null;

            OnPlayerSelectedOption(choiceText, nextIndex, giveQuest, specialAction, specialTarget);
            return;
        }

        for (int i = 0; i < choice.choices.Length; i++)
        {
            int nextIndex = (choice.nextDialogueIndexes != null && i < choice.nextDialogueIndexes.Length) ? choice.nextDialogueIndexes[i] : -1;
            bool giveQuest = (choice.giveQuest != null && i < choice.giveQuest.Length) ? choice.giveQuest[i] : false;
            SpecialActionType specialAction = (choice.specialActions != null && i < choice.specialActions.Length) ? choice.specialActions[i] : SpecialActionType.None;
            UnityEngine.Object specialTarget = (choice.specialTargets != null && i < choice.specialTargets.Length) ? choice.specialTargets[i] : null;
            string choiceText = choice.choices[i];

            CreateChoiceButton(choiceText, () =>
                OnPlayerSelectedOption(choiceText, nextIndex, giveQuest, specialAction, specialTarget));
        }
    }

    private void OnPlayerSelectedOption(string textToSay, int nextIndex, bool giveQuest, SpecialActionType action, UnityEngine.Object target)
    {
        ClearChoices();
        isPlayerTalking = true;
        SetNPCInfo(playerName, playerPortrait);

        StartCoroutine(TypePlayerLine(textToSay));

        pendingChoiceLogic = () => ExecuteChoiceLogic(nextIndex, giveQuest, action, target);
    }

    private void ExecuteChoiceLogic(int nextIndex, bool giveQuest, SpecialActionType action, UnityEngine.Object target)
    {
        isPlayerTalking = false;
        pendingChoiceLogic = null;

        if (action != SpecialActionType.None)
        {
            TriggerSpecialAction(action, target);
        }

        if (giveQuest)
        {
            QuestController.Instance.AcceptQuest(currentDialogue.quest);
            Debug.Log($"Đã nhận nhiệm vụ (Qua Choice): {currentDialogue.quest.questName}");
            EndDialogue();
            return;
        }

        if (nextIndex == -1)
        {
            EndDialogue();
            return;
        }

        dialogueIndex = nextIndex;
        ClearChoices();
        DisplayCurrentLine();
    }

    private void TriggerSpecialAction(SpecialActionType action, UnityEngine.Object target)
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

    public void SetNPCInfo(string npcName, Sprite portrait)
    {
        if (nameText != null) nameText.text = npcName;
        if (portraitImage != null) portraitImage.sprite = portrait;
    }

    public void ClearChoices()
    {
        if (choiceContainer == null) return;
        foreach (Transform child in choiceContainer)
        {
            Destroy(child.gameObject);
        }
    }

    private GameObject CreateChoiceButton(string choiceText, UnityEngine.Events.UnityAction onClick)
    {
        GameObject choiceButton = Instantiate(choiceButtonPrefab, choiceContainer);
        choiceButton.GetComponentInChildren<TMP_Text>().text = choiceText;
        choiceButton.GetComponent<Button>().onClick.AddListener(onClick);
        return choiceButton;
    }
}