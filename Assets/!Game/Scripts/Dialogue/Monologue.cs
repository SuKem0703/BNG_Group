using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // Cần thêm để lấy Scene Name cho ID

public class Monologue : MonoBehaviour, IInteractable
{
    public event System.Action OnDialogueEndEvent;

    [Header("Data")]
    public MonologueData monologueData;

    [Header("Settings")]
    public bool triggerOnEnter = false;
    [Tooltip("Nếu true, monologue sẽ tự hủy sau khi hoàn tất và lưu lại trạng thái.")]
    public bool isOneTimeOnly = false;

    [Header("Save Data (Only used if OneTimeOnly is true)")]
    [Tooltip("ID để lưu. Nếu trống sẽ tự sinh theo tọa độ.")]
    public string uniqueID;
    protected string finalID;

    [SerializeField] protected string characterName = "Elric";
    [SerializeField] protected Sprite characterPortrait;

    protected DialogueController dialogueUI;
    protected SaveController saveController;
    protected int dialogueIndex;
    protected bool isTyping;
    protected bool maintainPauseAfterDialogue = false;
    protected float lastSkipTime = -99f;

    protected virtual void Start()
    {
        dialogueUI = DialogueController.instance;

        if (characterPortrait == null)
            characterPortrait = Resources.Load<Sprite>("Elric_Portrait");

        // --- LOGIC SAVE/LOAD TÍCH HỢP ---
        if (isOneTimeOnly)
        {
            if (!string.IsNullOrEmpty(uniqueID)) finalID = uniqueID;
            else finalID = GlobalHelper.GenerateUniqueID(gameObject);

            if (SaveController.IsDataLoaded) CheckIfAlreadyPlayed();
            else SaveController.OnDataLoaded += HandleDataLoaded;
        }
        // -------------------------------
    }

    protected virtual void OnDestroy()
    {
        if (isOneTimeOnly) SaveController.OnDataLoaded -= HandleDataLoaded;
    }

    private void HandleDataLoaded()
    {
        if (isOneTimeOnly) CheckIfAlreadyPlayed();
    }

    protected virtual void CheckIfAlreadyPlayed()
    {
        if (SaveController.Instance != null && SaveController.Instance.IsCollected(SceneManager.GetActiveScene().name, finalID))
        {
            Destroy(gameObject);
        }
    }

    // ... (Các hàm CanInteract, Interact, OnTriggerEnter2D, OpenDialogOnTrigger, StartDialogue, NextLine, TypeLine GIỮ NGUYÊN như cũ) ...
    // Copy lại từ code cũ của bạn cho các hàm logic này.

    public virtual bool CanInteract() { return !GameStateManager.IsDialogueActive; }

    public virtual void Interact()
    {
        if (monologueData == null || (PauseController.IsGamePause && !GameStateManager.IsDialogueActive)) return;
        if (GameStateManager.IsDialogueActive) NextLine();
        else StartDialogue();
    }

    protected virtual void OnTriggerEnter2D(Collider2D collision)
    {
        if (!triggerOnEnter || !collision.CompareTag("Player")) return;
        if (collision.GetComponent<PlayerItemCollector>() != null) return;
        OpenDialogOnTrigger();
    }

    public virtual void OpenDialogOnTrigger()
    {
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
        if (InteractionDetector.Instance != null)
        {
            bool showVisual = !triggerOnEnter;
            InteractionDetector.Instance.ForceSetTarget(this, showVisual);
        }

        dialogueIndex = 0;
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
        string currentLine = monologueData.dialogueLines[dialogueIndex];
        foreach (char letter in currentLine)
        {
            dialogueUI.SetDialogueText(dialogueUI.dialogueText.text + letter);
            yield return new WaitForSecondsRealtime(monologueData.typingSpeed);
        }
        isTyping = false;
        dialogueUI.continueIndicator.gameObject.SetActive(true);
        if (monologueData.autoProgressLines.Length > dialogueIndex && monologueData.autoProgressLines[dialogueIndex])
        {
            dialogueUI.continueIndicator.gameObject.SetActive(false);
            yield return new WaitForSecondsRealtime(monologueData.autoProgressDelay);
            NextLine();
        }
    }

    public virtual void EndDialogue()
    {
        // 1. Xử lý Quest trước
        if (monologueData.triggerQuestAtEnd && monologueData.questToGive != null)
        {
            if (QuestController.Instance != null)
            {
                QuestController.Instance.AcceptQuest(monologueData.questToGive);
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

        // 2. Logic Lưu & Tự hủy (nếu là OneTimeOnly)
        if (isOneTimeOnly)
        {
            FinishAndDestroySelf();
        }
        else
        {
            // Nếu không phải one-time thì save game bình thường thôi
            saveController = Object.FindFirstObjectByType<SaveController>();
            if (saveController != null) saveController.SaveGame();
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
}