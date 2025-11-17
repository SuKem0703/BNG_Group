using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Monologue : MonoBehaviour, IInteractable
{
    public event System.Action OnDialogueEndEvent;

    public MonologueData monologueData;
    private DialogueController dialogueUI;
    private SaveController saveController;

    private int dialogueIndex;
    private bool isTyping, isDialogueActive;
    
    private Sprite elricPortrait;

    public bool triggerOnEnter = false;

    private bool maintainPauseAfterDialogue = false;
    private void Start()
    {
        dialogueUI = DialogueController.instance;
        
        elricPortrait = Resources.Load<Sprite>("Elric_Portrait");
        if (elricPortrait == null)
        {
            Debug.LogError("Không tìm thấy 'Elric_Portrait' trong thư mục Resources!");
        }
    }
    private void Update()
    {
        if (!isDialogueActive)
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
        return !isDialogueActive;
    }

    public void Interact()
    {
        if (monologueData == null || (PauseController.IsGamePause && !isDialogueActive))
            return;

        if (isDialogueActive)
        {
            NextLine();
        }
        else
        {
            if (triggerOnEnter == true && isDialogueActive == false) return;

            Debug.Log("Độc thoại bắt đầu khi tương tác.");
            StartDialogue();
        }
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!triggerOnEnter || !collision.CompareTag("Player"))
            return;

        // If the player has a pickup handler, let that side handle interaction to avoid double-calls
        if (collision.GetComponent<PlayerItemCollector>() != null)
            return;

        OpenDialogOnTrigger();
    }
    public void OpenDialogOnTrigger()
    {
        if (monologueData == null || (PauseController.IsGamePause && !isDialogueActive))
            return;

        if (!isDialogueActive)
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
        isDialogueActive = true;

        MenuController.CanOpenMenu = false;

        // Ẩn toàn bộ UI chung
        CommonUIController.Instance?.SetUIVisible(false);
        dialogueUI.ClearChoices();
        
        // Luôn set thông tin là "Elric" cho độc thoại
        dialogueUI.SetNPCInfo("Elric", elricPortrait); 
        
        dialogueUI.ShowDialogueUI(true);
        PauseController.SetPause(true);

        DisplayCurrentLine();
    }

    void NextLine()
    {
        if (isTyping)
        {
            // Nếu đang gõ chữ, bỏ qua và hiển thị đầy đủ
            StopAllCoroutines();
            dialogueUI.SetDialogueText(monologueData.dialogueLines[dialogueIndex]);
            isTyping = false;
            dialogueUI.continueIndicator.gameObject.SetActive(true);
            return;
        }

        dialogueUI.continueIndicator.gameObject.SetActive(false);
        dialogueUI.ClearChoices(); // Đảm bảo không có lựa chọn nào

        // Chuyển sang dòng tiếp theo
        if (++dialogueIndex < monologueData.dialogueLines.Length)
        {
            DisplayCurrentLine();
        }
        else
        {
            // Hết dòng -> Kết thúc
            EndDialogue();
        }
    }

    IEnumerator TypeLine()
    {
        isTyping = true;
        dialogueUI.SetDialogueText("");
        dialogueUI.continueIndicator.gameObject.SetActive(false);

        // Dòng thoại hiện ra từng ký tự
        foreach (char letter in monologueData.dialogueLines[dialogueIndex])
        {
            dialogueUI.SetDialogueText(dialogueUI.dialogueText.text + letter);
            yield return new WaitForSeconds(monologueData.typingSpeed);
        }

        isTyping = false;
        dialogueUI.continueIndicator.gameObject.SetActive(true);

        // Auto progress nếu có thiết lập
        if (monologueData.autoProgressLines.Length > dialogueIndex &&
            monologueData.autoProgressLines[dialogueIndex])
        {
            dialogueUI.continueIndicator.gameObject.SetActive(false);
            yield return new WaitForSeconds(monologueData.autoProgressDelay);
            NextLine();
        }
    }


    void DisplayCurrentLine()
    {
        StopAllCoroutines();
        StartCoroutine(TypeLine());
    }

    public void EndDialogue()
    {
        if (monologueData.triggerQuestAtEnd && monologueData.questToGive != null)
        {
            QuestController.Instance.AcceptQuest(monologueData.questToGive);
            Debug.Log($"Độc thoại đã kích hoạt nhiệm vụ: {monologueData.questToGive.name}");
        }

        StopAllCoroutines();
        isDialogueActive = false;
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

        OnDialogueEndEvent?.Invoke();

        saveController = FindFirstObjectByType<SaveController>();
        saveController.SaveGame();
    }
}