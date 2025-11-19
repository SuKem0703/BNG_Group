using UnityEngine;
using UnityEngine.SceneManagement;

public class Collectible : MonoBehaviour
{
    [Tooltip("Unique ID for this item within the scene. If empty, a deterministic id based on position will be used.")]
    public string uniqueID;

    private void OnEnable()
    {
        // Always subscribe so we don't miss the event regardless of static flag timing
        SaveController.OnDataLoaded += OnDataLoaded;

        // If data already loaded, check immediately
        if (SaveController.IsDataLoaded)
        {
            CheckCollectedState();
        }
    }

    private void OnDisable()
    {
        SaveController.OnDataLoaded -= OnDataLoaded;
    }

    private void OnDataLoaded()
    {
        CheckCollectedState();
    }

    private void CheckCollectedState()
    {
        if (string.IsNullOrEmpty(uniqueID))
            uniqueID = GlobalHelper.GenerateUniqueID(gameObject);

        var save = FindFirstObjectByType<SaveController>();
        if (save != null && save.IsCollected(SceneManager.GetActiveScene().name, uniqueID))
        {
            // Destroy collectible if marked collected in save
            Destroy(gameObject);
        }
    }

    public void OnPickedUp()
    {
        if (string.IsNullOrEmpty(uniqueID))
            uniqueID = GlobalHelper.GenerateUniqueID(gameObject);

        var save = FindFirstObjectByType<SaveController>();
        save?.MarkCollected(SceneManager.GetActiveScene().name, uniqueID);
        save?.SaveGame();
        Destroy(gameObject);
    }
}
