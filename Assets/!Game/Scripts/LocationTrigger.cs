using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class LocationTrigger : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("ID này PHẢI GIỐNG với ObjectID trong Quest Data")]
    public string locationID;

    [Tooltip("Tự động tắt sau khi kích hoạt để tránh gọi hàm liên tục")]
    public bool disableAfterTrigger = true;

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

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