//using UnityEngine;

//[RequireComponent(typeof(Collider2D))]
//public class LocationTrigger : MonoBehaviour
//{
//    [Tooltip("Unique ID for this location (use same ID as QuestObject.objectID)")]
//    public string locationID;

//    [Tooltip("Auto-disable trigger after player enters")]
//    public bool disableAfterTrigger = true;

//    private void Reset()
//    {
//        // make sure collider is trigger
//        var col = GetComponent<Collider2D>();
//        if (col != null) col.isTrigger = true;
//    }

//    private void OnTriggerEnter2D(Collider2D other)
//    {
//        if (!other.CompareTag("Player")) return;

//        var qc = QuestController.Instance;
//        if (qc == null || string.IsNullOrEmpty(locationID)) return;

//        qc.MarkLocationReached(locationID);

//        if (disableAfterTrigger)
//            gameObject.SetActive(false);
//    }
//}