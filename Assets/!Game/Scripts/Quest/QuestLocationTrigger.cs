using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class QuestLocationTrigger : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("ID này PHẢI GIỐNG với ObjectID trong Quest Data")]
    public string locationID;

    public bool disableAfterTrigger = true;

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("PlayerInteract")) return;

        if (QuestController.Instance != null && !string.IsNullOrEmpty(locationID))
        {
            QuestController.Instance.MarkLocationReached(locationID);

            if (disableAfterTrigger)
            {
                gameObject.SetActive(false);
            }
        }
    }
}