using UnityEngine;

public enum SpecialActionType
{
    None,
    OpenShop,
    OpenUpgrade
}

[RequireComponent(typeof(CapsuleCollider2D), typeof(CircleCollider2D))]
public class NPC : AutoIDBehaviour, IInteractable, ITargetableInfo
{
    [Header("Danh sách hội thoại (theo thứ tự)")]
    public NPCDialogueData[] dialogueDataList;

    private string playerName = "Elric";
    private Sprite playerPortrait => LoadResourceManager.Instance?.ElricPortrait;

    [Header("Dialogue Settings")]
    public bool resetFacingOnDialogueEnd = true;

    public NPCDialogueData CurrentActiveDialogue { get; set; }
    public bool triggerOnEnter = false;

    public void InitChunkData(string customID, bool triggerEnter)
    {
        UniqueID = customID;
        triggerOnEnter = triggerEnter;
    }

    private void Awake()
    {
        if (dialogueDataList == null) return;

        foreach (NPCDialogueData dialogueData in dialogueDataList)
        {
            if (dialogueData == null || dialogueData.choices == null) continue;

            foreach (var choice in dialogueData.choices)
            {
                if (choice.specialActions == null || choice.specialTargetNames == null) continue;

                choice.specialTargets = new Object[choice.specialTargetNames.Length];
                for (int i = 0; i < choice.specialTargetNames.Length; i++)
                {
                    if (!string.IsNullOrEmpty(choice.specialTargetNames[i]))
                    {
                        GameObject go = GameObject.Find(choice.specialTargetNames[i]);
                        if (go != null) choice.specialTargets[i] = go;
                        else Debug.LogWarning($"Không tìm thấy GameObject tên: {choice.specialTargetNames[i]}");
                    }
                }
            }
        }
    }

    public bool CanInteract()
    {
        return GameStateManager.CanProcessInput();
    }

    public void Interact()
    {
        if (CurrentActiveDialogue == null || (PauseController.IsGamePause && !GameStateManager.IsDialogueActive))
            return;

        if (GameStateManager.IsDialogueActive)
        {
            DialogueController.instance.NextLine();
        }
        else
        {
            if (triggerOnEnter == true && GameStateManager.IsDialogueActive == false) return;
            StartDialogue();
        }
    }

    public void StartDialogue()
    {
        GameObject player = GameObject.FindGameObjectWithTag("PlayerController");
        if (player != null)
        {
            NPCAnimation nPCAnimation = GetComponent<NPCAnimation>();
            if (nPCAnimation != null) nPCAnimation.LookTowards(player.transform.position);
        }

        int startIndex = 0;
        QuestHandler questHandler = GetComponent<QuestHandler>();
        if (questHandler != null) startIndex = questHandler.GetStartingDialogueIndex();

        DialogueController.instance.StartDialogue(
            CurrentActiveDialogue,
            startIndex,
            playerName,
            playerPortrait,
            CurrentActiveDialogue.npcName,
            CurrentActiveDialogue.npcPortrait,
            OnDialogueFinished
        );
    }

    private void OnDialogueFinished()
    {
        QuestHandler questHandler = GetComponent<QuestHandler>();
        if (questHandler != null) questHandler.OnDialogueEnded();

        if (resetFacingOnDialogueEnd)
        {
            NPCAnimation nPCAnimation = GetComponent<NPCAnimation>();
            if (nPCAnimation != null) nPCAnimation.ResetFacing();
        }

        SaveController.Instance?.TriggerAutoSave();
    }

    public TargetInfoData GetInfo()
    {
        if (CurrentActiveDialogue != null)
            return new TargetInfoData(CurrentActiveDialogue.npcName, CurrentActiveDialogue.npcPortrait, "Nói chuyện", TargetType.NPC);
        return new TargetInfoData(gameObject.name, null, "Nói chuyện", TargetType.NPC);
    }
}