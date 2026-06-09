using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class QuestTriggerZone : MonoBehaviour
{
    public Quest questToGive;
    public bool triggerOnlyOnce = true;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player") && !collision.CompareTag("PlayerInteract")) 
            return;

        if (questToGive == null || QuestController.Instance == null) 
            return;

        string id = questToGive.questID;
        
        if (!QuestController.Instance.IsQuestActive(id) && 
            !QuestController.Instance.IsQuestCompleted(id) && 
            !QuestController.Instance.IsQuestHandedIn(id))
        {
            QuestController.Instance.AcceptQuest(questToGive);
            
            if (triggerOnlyOnce)
            {
                GetComponent<Collider2D>().enabled = false;
            }
        }
    }
}