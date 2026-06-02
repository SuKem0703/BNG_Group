using UnityEngine;
using UnityEngine.SceneManagement;

public class Monologue : MonoBehaviour, IInteractable
{
    public event System.Action OnDialogueEndEvent;

    [Header("Data")]
    public MonologueData monologueData;

    [Header("Settings")]
    public bool triggerOnEnter = false;

    [Tooltip("Nếu true, người chơi sẽ không thể Aim hay bấm nút tương tác.")]
    public bool disableManualInteraction = false;

    [Tooltip("Nếu true, monologue sẽ tự hủy sau khi hoàn tất và lưu lại trạng thái.")]
    public bool isOneTimeOnly = false;

    [Header("Save Data (Only used if OneTimeOnly is true)")]
    public string uniqueID;
    protected string finalID;

    protected string characterName = "Elric";
    protected Sprite characterPortrait => LoadResourceManager.Instance?.ElricPortrait;

    private MapMove mapTransition => GetComponent<MapMove>();

    public enum MonologueQuestState
    {
        NotStarted,
        InProgress,
        Completed,
        NoMoreQuests
    }
    private MonologueQuestState currentQuestState = MonologueQuestState.NotStarted;

    protected virtual void Start()
    {
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

        if (GameStateManager.IsDialogueActive) DialogueController.instance.NextLine();
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
        int startIndex = 0;

        if (currentQuestState == MonologueQuestState.InProgress) startIndex = monologueData.questInProgressIndex;
        else if (currentQuestState == MonologueQuestState.Completed) startIndex = monologueData.questCompletedIndex;
        else if (currentQuestState == MonologueQuestState.NoMoreQuests) startIndex = monologueData.noMoreQuestsIndex;

        if (startIndex >= monologueData.dialogueLines.Length) startIndex = 0;

        if (InteractionDetector.Instance != null)
        {
            InteractionDetector.Instance.ForceSetTarget(this, !triggerOnEnter);
        }

        DialogueController.instance.StartDialogue(
            monologueData,
            startIndex,
            "Elric",
            null,
            characterName,
            characterPortrait,
            EndDialogue
        );
    }

    private void CalculateQuestState()
    {
        currentQuestState = MonologueQuestState.NotStarted;
        if (monologueData.quest == null || QuestController.Instance == null) return;

        string qID = monologueData.quest.questID;
        var qc = QuestController.Instance;

        if (qc.IsQuestHandedIn(qID)) currentQuestState = MonologueQuestState.NoMoreQuests;
        else if (qc.IsQuestCompleted(qID)) currentQuestState = MonologueQuestState.Completed;
        else if (qc.IsQuestActive(qID)) currentQuestState = MonologueQuestState.InProgress;
    }

    public virtual void EndDialogue()
    {
        CommonUIController.Instance?.SetUIVisible(true);
        PauseController.SetPause(false);

        bool needsFading = false;

        if ((monologueData.triggerQuestAtEnd && monologueData.quest != null) ||
            (monologueData.handleQuestAtEnd && monologueData.quest != null && currentQuestState == MonologueQuestState.Completed) ||
            isOneTimeOnly)
        {
            needsFading = true;
        }

        if (needsFading)
        {
            ScreenFader.FadeAndExecute(0.5f, () =>
            {
                ExecutePostDialogueLogic();
            });
        }
        else
        {
            ExecutePostDialogueLogic();
        }
    }

    private void ExecutePostDialogueLogic()
    {
        if (monologueData.triggerQuestAtEnd && monologueData.quest != null)
        {
            QuestController.Instance?.AcceptQuest(monologueData.quest);
        }

        if (monologueData.handleQuestAtEnd && monologueData.quest != null && currentQuestState == MonologueQuestState.Completed)
        {
            if (QuestController.Instance != null && !QuestController.Instance.IsQuestHandedIn(monologueData.quest.questID))
            {
                HandleQuestCompletion(monologueData.quest);
            }
        }

        OnDialogueEndEvent?.Invoke();

        if (isOneTimeOnly) FinishAndDestroySelf();
        else SaveController.Instance?.TriggerAutoSave();
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
        RewardController.Instance?.GiveQuestReward(quest);
        QuestController.Instance?.HandInQuest(quest.questID);
        currentQuestState = MonologueQuestState.NoMoreQuests;
    }
}