using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class TriggerZone : MonoBehaviour
{
    private NPC npc;
    private QuestHandler questHandler;

    private void Awake()
    {
        npc = GetComponent<NPC>();
        questHandler = GetComponent<QuestHandler>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (npc == null || !npc.triggerOnEnter || !collision.CompareTag("Player"))
            return;

        if (questHandler != null)
        {
            questHandler.UpdateActiveDialogue();
            questHandler.SyncQuestState();
        }

        if (npc.CurrentActiveDialogue == null) return;

        bool shouldTrigger = false;

        if (questHandler != null)
        {
            var state = questHandler.CurrentQuestState;
            if (npc.CurrentActiveDialogue.triggerOnEnter_NotStarted && state == QuestState.NotStarted) shouldTrigger = true;
            else if (npc.CurrentActiveDialogue.triggerOnEnter_InProgress && state == QuestState.InProgress) shouldTrigger = true;
            else if (npc.CurrentActiveDialogue.triggerOnEnter_Completed && state == QuestState.Completed) shouldTrigger = true;
            else if (npc.CurrentActiveDialogue.triggerOnEnter_NoMoreQuests && state == QuestState.NoMoreQuests) shouldTrigger = true;
        }
        else
        {
            shouldTrigger = true;
        }

        if (shouldTrigger && !GameStateManager.IsDialogueActive && !PauseController.IsGamePause)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                PlayerMovement playerMovement = player.GetComponent<PlayerMovement>();
                if (playerMovement != null) playerMovement.LookTowards(transform.position);
            }

            npc.StartDialogue();
        }
    }
}