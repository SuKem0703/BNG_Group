using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Monologue : MonoBehaviour, IInteractable
{
    public event System.Action OnDialogueEndEvent;

    [Header("Data")]
    public MonologueData monologueData;

    [Header("Settings")]
    public bool triggerOnEnter = false;

    [SerializeField] private string characterName = "Elric";

    private DialogueController dialogueUI;
    private SaveController saveController;

    private int dialogueIndex;
    private bool isTyping;

    private Sprite characterPortrait;
    private bool maintainPauseAfterDialogue = false;

    private void Start()
    {
        dialogueUI = DialogueController.instance;

        characterPortrait = Resources.Load<Sprite>("Elric_Portrait");
        if (characterPortrait == null)
        {
            Debug.LogWarning($"Monologue: Không tìm thấy 'Elric_Portrait' trong Resources.");
        }

        if (!SaveController.IsDataLoaded)
        {
            SaveController.OnDataLoaded += HandleDataLoaded;
        }
    }

    private void OnDestroy()
    {
        SaveController.OnDataLoaded -= HandleDataLoaded;
    }

    private void HandleDataLoaded()
    {
        SaveController.OnDataLoaded -= HandleDataLoaded;
    }

    private void Update()
    {
        if (!GameStateManager.IsDialogueActive)
        {
            return;
        }
        if (GameStateManager.IsMenuOpen) return;

        if (Input.GetMouseButtonDown(0))
        {
            if (dialogueUI != null && dialogueUI.choiceContainer.childCount > 0)
            {
                return;
            }

            Interact();
        }
    }

    public bool CanInteract()
    {
        return !GameStateManager.IsDialogueActive;
    }

    public void Interact()
    {
        if (monologueData == null || (PauseController.IsGamePause && !GameStateManager.IsDialogueActive))
            return;

        if (GameStateManager.IsDialogueActive)
        {
            NextLine();
        }
        else
        {
            if (triggerOnEnter == true && GameStateManager.IsDialogueActive == false)
            {
                // Có thể return nếu muốn bắt buộc phải đi vào vùng mới kích hoạt
                // return; 
            }

            Debug.Log("Độc thoại bắt đầu khi tương tác.");
            StartDialogue();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!triggerOnEnter || !collision.CompareTag("Player"))
            return;

        if (collision.GetComponent<PlayerItemCollector>() != null)
            return;

        OpenDialogOnTrigger();
    }

    public void OpenDialogOnTrigger()
    {
        if (monologueData == null || (PauseController.IsGamePause && !GameStateManager.IsDialogueActive))
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
            Debug.Log("Độc thoại bắt đầu khi vào vùng kích hoạt.");
        }
    }
    void StartDialogue()
    {
        dialogueIndex = 0;

        GameStateManager.IsDialogueActive = true;
        GameStateManager.CanOpenMenu = false; // Khóa menu

        // Ẩn UI Gameplay
        CommonUIController.Instance?.SetUIVisible(false);

        dialogueUI.ClearChoices();
        // Set thông tin người nói (Mặc định là nhân vật chính tự nghĩ)
        dialogueUI.SetNPCInfo(characterName, characterPortrait);
        dialogueUI.ShowDialogueUI(true);

        // Pause game logic
        PauseController.SetPause(true);

        DisplayCurrentLine();
    }

    void NextLine()
    {
        // Logic skip typing (giống NPC.cs)
        if (isTyping)
        {
            StopAllCoroutines();
            if (monologueData.dialogueLines.Length > dialogueIndex)
            {
                dialogueUI.SetDialogueText(monologueData.dialogueLines[dialogueIndex]);
            }
            isTyping = false;
            dialogueUI.continueIndicator.gameObject.SetActive(true);
            return;
        }

        dialogueUI.continueIndicator.gameObject.SetActive(false);
        dialogueUI.ClearChoices();

        // Tăng index
        if (++dialogueIndex < monologueData.dialogueLines.Length)
        {
            DisplayCurrentLine();
        }
        else
        {
            EndDialogue();
        }
    }

    void DisplayCurrentLine()
    {
        StopAllCoroutines();
        StartCoroutine(TypeLine());
    }

    IEnumerator TypeLine()
    {
        isTyping = true;
        dialogueUI.SetDialogueText("");
        dialogueUI.continueIndicator.gameObject.SetActive(false);

        string currentLine = monologueData.dialogueLines[dialogueIndex];

        foreach (char letter in currentLine)
        {
            dialogueUI.SetDialogueText(dialogueUI.dialogueText.text + letter);
            yield return new WaitForSeconds(monologueData.typingSpeed);
        }

        isTyping = false;
        dialogueUI.continueIndicator.gameObject.SetActive(true);

        // Logic Auto Progress từ data cũ
        if (monologueData.autoProgressLines.Length > dialogueIndex &&
            monologueData.autoProgressLines[dialogueIndex])
        {
            dialogueUI.continueIndicator.gameObject.SetActive(false);
            yield return new WaitForSeconds(monologueData.autoProgressDelay);
            NextLine();
        }
    }

    public void EndDialogue()
    {
        // Xử lý nhiệm vụ từ MonologueData (Logic cũ)
        if (monologueData.triggerQuestAtEnd && monologueData.questToGive != null)
        {
            // Kiểm tra null instance cho an toàn
            if (QuestController.Instance != null)
            {
                QuestController.Instance.AcceptQuest(monologueData.questToGive);
                Debug.Log($"Độc thoại đã kích hoạt nhiệm vụ: {monologueData.questToGive.questName}");
            }
        }

        // Dọn dẹp Coroutine & State
        StopAllCoroutines();
        GameStateManager.IsDialogueActive = false;
        GameStateManager.CanOpenMenu = true;

        dialogueUI.continueIndicator.gameObject.SetActive(false);
        dialogueUI.SetDialogueText("");
        dialogueUI.ShowDialogueUI(false);
        dialogueUI.ClearChoices();

        // Khôi phục trạng thái Game
        if (!maintainPauseAfterDialogue)
        {
            CommonUIController.Instance?.SetUIVisible(true);
            PauseController.SetPause(false);
        }

        maintainPauseAfterDialogue = false;

        // Gọi Event kết thúc
        OnDialogueEndEvent?.Invoke();

        // Save Game (Giống NPC.cs)
        saveController = Object.FindFirstObjectByType<SaveController>();
        if (saveController != null)
        {
            saveController.SaveGame();
        }
    }
}