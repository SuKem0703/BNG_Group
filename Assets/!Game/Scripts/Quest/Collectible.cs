using UnityEngine;
using UnityEngine.SceneManagement;
using static NPC;

public class Collectible : AutoIDBehaviour
{
    [Header("Quest Condition (Để trống nếu là Item thường)")]
    public string requiredQuestID;
    public QuestState requiredState;

    private bool isCollected = false;

    private void OnEnable()
    {
        SaveController.OnDataLoaded += OnDataLoaded;

        if (SaveController.IsDataLoaded)
        {
            CheckSaveData();
        }
    }

    private void OnDisable()
    {
        SaveController.OnDataLoaded -= OnDataLoaded;
    }

    private void OnDataLoaded()
    {
        CheckSaveData();
        UpdateVisibility();
    }

    private void CheckSaveData()
    {
        var save = SaveController.Instance;
        if (save != null && save.IsCollected(SceneManager.GetActiveScene().name, UniqueID))
        {
            isCollected = true;
            Destroy(gameObject);
        }
    }

    public void UpdateVisibility()
    {
        if (isCollected)
        {
            Destroy(gameObject);
            return;
        }

        if (SaveController.Instance != null && !string.IsNullOrEmpty(UniqueID))
        {
            if (SaveController.Instance.IsCollected(SceneManager.GetActiveScene().name, UniqueID))
            {
                Destroy(gameObject);
                return;
            }
        }

        if (string.IsNullOrEmpty(requiredQuestID))
        {
            gameObject.SetActive(true);
            return;
        }

        if (QuestController.Instance == null)
        {
            gameObject.SetActive(false);
            return;
        }

        bool isActive = QuestController.Instance.IsQuestActive(requiredQuestID);
        bool isCompleted = QuestController.Instance.IsQuestCompleted(requiredQuestID);
        bool isHandedIn = QuestController.Instance.IsQuestHandedIn(requiredQuestID);

        bool shouldBeVisible = false;

        switch (requiredState)
        {
            case QuestState.NotStarted:
                shouldBeVisible = !isActive && !isHandedIn;
                break;
            case QuestState.InProgress:
                shouldBeVisible = isActive && !isCompleted;
                break;
            case QuestState.Completed:
                shouldBeVisible = isActive && isCompleted;
                break;
        }

        gameObject.SetActive(shouldBeVisible);
    }

    public void OnPickedUp()
    {
        SaveController.Instance?.MarkCollected(SceneManager.GetActiveScene().name, UniqueID);

        SaveController.Instance?.TriggerAutoSave();

        Destroy(gameObject);
    }
}