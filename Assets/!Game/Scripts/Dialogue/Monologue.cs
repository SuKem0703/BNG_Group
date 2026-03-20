using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Monologue : MonoBehaviour, IInteractable
{
    public event System.Action OnDialogueEndEvent;

    [Header("Data")]
    public MonologueData monologueData;

    [Header("Settings")]
    public bool triggerOnEnter = false;

    [Tooltip("Nếu true, người chơi sẽ không thể Aim hay bấm nút tương tác (dùng cho các object do script khác điều khiển).")]
    public bool disableManualInteraction = false;

    [Tooltip("Nếu true, monologue sẽ tự hủy sau khi hoàn tất và lưu lại trạng thái.")]
    public bool isOneTimeOnly = false;

    [Header("Save Data (Only used if OneTimeOnly is true)")]
    public string uniqueID;
    protected string finalID;

    [SerializeField] protected string characterName = "Elric";
    [SerializeField] protected Sprite characterPortrait;

    protected DialogueController dialogueUI;
    protected int dialogueIndex;
    protected bool isTyping;
    protected bool maintainPauseAfterDialogue = false;
    protected float lastSkipTime = -99f;

    private SceneMapMove mapTransition => GetComponent<SceneMapMove>();

    private enum MonologueQuestState
    {
        NotStarted,
        InProgress,
        Completed,
        NoMoreQuests
    }
    private MonologueQuestState currentQuestState = MonologueQuestState.NotStarted;

    protected virtual void Start()
    {
        dialogueUI = DialogueController.instance;

        if (characterPortrait == null)
            characterPortrait = Resources.Load<Sprite>("Elric_Portrait");

        if (isOneTimeOnly)
        {
            if (!string.IsNullOrEmpty(uniqueID)) finalID = uniqueID;
            else finalID = GlobalHelper.GenerateUniqueID(gameObject);
        }

        if (SaveController.IsDataLoaded) HandleDataLoaded();
        else SaveController.OnDataLoaded += HandleDataLoaded;
    }

    protected virtual void OnDestroy()
    {
        SaveController.OnDataLoaded -= HandleDataLoaded;
    }

    private void HandleDataLoaded()
    {
        if (isOneTimeOnly) CheckIfAlreadyPlayed();
        SaveController.OnDataLoaded -= HandleDataLoaded;
    }

    protected virtual void CheckIfAlreadyPlayed()
    {
        if (SaveController.Instance != null && SaveController.Instance.IsCollected(SceneManager.GetActiveScene().name, finalID))
        {
            Destroy(gameObject);
        }
    }

    public virtual bool CanInteract()
    {
        if (triggerOnEnter) return false;
        if (disableManualInteraction) return false;
        if (!SaveController.IsDataLoaded) return false;
        if (!string.IsNullOrEmpty(SaveController.pendingSceneName)) return false;

        if (mapTransition != null && mapTransition.IsEntryAllowed()) return false;

        return !GameStateManager.IsDialogueActive;
    }

    public virtual void Interact()
    {
        if (!SaveController.IsDataLoaded) return;
        if (!string.IsNullOrEmpty(SaveController.pendingSceneName)) return;
        if (mapTransition != null && mapTransition.IsEntryAllowed()) return;
        if (monologueData == null || (PauseController.IsGamePause && !GameStateManager.IsDialogueActive)) return;

        if (GameStateManager.IsDialogueActive) NextLine();
        else StartDialogue();
    }

    protected virtual void OnTriggerEnter2D(Collider2D collision)
    {
        if (!triggerOnEnter || !collision.CompareTag("Player")) return;
        if (!SaveController.IsDataLoaded) return;
        if (!string.IsNullOrEmpty(SaveController.pendingSceneName)) return;
        if (collision.GetComponent<PlayerItemCollector>() != null) return;
        if (mapTransition != null && mapTransition.IsEntryAllowed()) return;

        OpenDialogOnTrigger();
    }

    public virtual void OpenDialogOnTrigger()
    {
        if (!SaveController.IsDataLoaded) return;
        if (!string.IsNullOrEmpty(SaveController.pendingSceneName)) return;
        if (monologueData == null || (PauseController.IsGamePause && !GameStateManager.IsDialogueActive)) return;

        if (!GameStateManager.IsDialogueActive)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) player.GetComponent<PlayerMovement>()?.LookTowards(transform.position);
            StartDialogue();
        }
    }

    protected virtual void StartDialogue()
    {
        CalculateQuestState();

        if (currentQuestState == MonologueQuestState.NotStarted)
        {
            dialogueIndex = 0;
        }
        else if (currentQuestState == MonologueQuestState.InProgress)
        {
            dialogueIndex = monologueData.questInProgressIndex;
        }
        else if (currentQuestState == MonologueQuestState.Completed)
        {
            dialogueIndex = monologueData.questCompletedIndex;
        }
        else if (currentQuestState == MonologueQuestState.NoMoreQuests)
        {
            dialogueIndex = monologueData.noMoreQuestsIndex;
        }

        if (dialogueIndex >= monologueData.dialogueLines.Length)
        {
            dialogueIndex = 0;
        }

        if (InteractionDetector.Instance != null)
        {
            bool showVisual = !triggerOnEnter;
            InteractionDetector.Instance.ForceSetTarget(this, showVisual);
        }

        lastSkipTime = -99f;
        GameStateManager.IsDialogueActive = true;
        GameStateManager.CanOpenMenu = false;
        CommonUIController.Instance?.SetUIVisible(false);
        dialogueUI.ClearChoices();
        dialogueUI.SetNPCInfo(characterName, characterPortrait);
        dialogueUI.ShowDialogueUI(true);
        PauseController.SetPause(true);

        DisplayCurrentLine();
    }

    private void CalculateQuestState()
    {
        currentQuestState = MonologueQuestState.NotStarted;

        if (monologueData.quest == null) return;

        var qc = QuestController.Instance;
        if (qc == null) return;

        string qID = monologueData.quest.questID;

        if (qc.IsQuestHandedIn(qID))
        {
            currentQuestState = MonologueQuestState.NoMoreQuests;
        }
        else if (qc.IsQuestCompleted(qID))
        {
            currentQuestState = MonologueQuestState.Completed;
        }
        else if (qc.IsQuestActive(qID))
        {
            currentQuestState = MonologueQuestState.InProgress;
        }
        else
        {
            currentQuestState = MonologueQuestState.NotStarted;
        }
    }

    protected virtual void NextLine()
    {
        if (isTyping)
        {
            StopAllCoroutines();
            if (monologueData.dialogueLines.Length > dialogueIndex)
                dialogueUI.SetDialogueText(monologueData.dialogueLines[dialogueIndex]);
            isTyping = false;
            dialogueUI.continueIndicator.gameObject.SetActive(true);
            lastSkipTime = Time.unscaledTime;
            return;
        }
        if (Time.unscaledTime - lastSkipTime < 0.2f) return;

        dialogueUI.continueIndicator.gameObject.SetActive(false);
        dialogueUI.ClearChoices();

        if (monologueData.endDialogueLines != null &&
            dialogueIndex < monologueData.endDialogueLines.Length &&
            monologueData.endDialogueLines[dialogueIndex])
        {
            EndDialogue();
            return;
        }

        if (++dialogueIndex < monologueData.dialogueLines.Length) DisplayCurrentLine();
        else EndDialogue();
    }

    protected virtual void DisplayCurrentLine()
    {
        StopAllCoroutines();
        StartCoroutine(TypeLine());
    }

    protected virtual IEnumerator TypeLine()
    {
        isTyping = true;
        dialogueUI.SetDialogueText("");
        dialogueUI.continueIndicator.gameObject.SetActive(false);

        string currentLine = "";
        if (dialogueIndex < monologueData.dialogueLines.Length)
            currentLine = monologueData.dialogueLines[dialogueIndex];

        foreach (char letter in currentLine)
        {
            dialogueUI.SetDialogueText(dialogueUI.dialogueText.text + letter);
            yield return new WaitForSecondsRealtime(monologueData.typingSpeed);
        }
        isTyping = false;
        dialogueUI.continueIndicator.gameObject.SetActive(true);

        if (monologueData.autoProgressLines != null &&
            monologueData.autoProgressLines.Length > dialogueIndex &&
            monologueData.autoProgressLines[dialogueIndex])
        {
            dialogueUI.continueIndicator.gameObject.SetActive(false);
            yield return new WaitForSecondsRealtime(monologueData.autoProgressDelay);
            NextLine();
        }
    }

    public virtual void EndDialogue()
    {
        if (monologueData.triggerQuestAtEnd && monologueData.quest != null)
        {
            if (QuestController.Instance != null)
            {
                QuestController.Instance.AcceptQuest(monologueData.quest);
            }
        }

        if (monologueData.handleQuestAtEnd && monologueData.quest != null &&
            currentQuestState == MonologueQuestState.Completed)
        {
            if (QuestController.Instance != null && !QuestController.Instance.IsQuestHandedIn(monologueData.quest.questID))
            {
                HandleQuestCompletion(monologueData.quest);
            }
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
        OnDialogueEndEvent?.Invoke();

        if (isOneTimeOnly)
        {
            FinishAndDestroySelf();
        }
        else
        {
            SaveController.Instance?.TriggerAutoSave();
        }
    }

    protected virtual void FinishAndDestroySelf()
    {
        if (!string.IsNullOrEmpty(finalID) && SaveController.Instance != null)
        {
            SaveController.Instance.MarkCollected(SceneManager.GetActiveScene().name, finalID);
            SaveController.Instance.TriggerAutoSave();
        }
        Destroy(gameObject);
    }

    protected void HandleQuestCompletion(Quest quest)
    {
        if (RewardController.Instance != null)
        {
            RewardController.Instance.GiveQuestReward(quest);
        }

        if (QuestController.Instance != null)
        {
            QuestController.Instance.HandInQuest(quest.questID);
        }

        currentQuestState = MonologueQuestState.NoMoreQuests;
    }
}