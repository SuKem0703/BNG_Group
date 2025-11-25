using UnityEngine;
using UnityEngine.SceneManagement;
using static NPC;

public class Collectible : MonoBehaviour
{
    [Header("Quest Condition (Để trống nếu là Item thường)")]
    public string requiredQuestID;
    public QuestState requiredState;

    [Header("Save Data Settings")]
    [Tooltip("ID duy nhất để lưu game. Nếu để trống sẽ tự sinh theo toạ độ.")]
    public string uniqueID;

    private bool isCollected = false;

    private void Awake()
    {
        if (string.IsNullOrEmpty(uniqueID))
            uniqueID = GlobalHelper.GenerateUniqueID(gameObject);
    }

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
        if (save != null && save.IsCollected(SceneManager.GetActiveScene().name, uniqueID))
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

        if (SaveController.Instance != null && !string.IsNullOrEmpty(uniqueID))
        {
            if (SaveController.Instance.IsCollected(SceneManager.GetActiveScene().name, uniqueID))
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
        if (string.IsNullOrEmpty(uniqueID))
            uniqueID = GlobalHelper.GenerateUniqueID(gameObject);

        SaveController.Instance?.MarkCollected(SceneManager.GetActiveScene().name, uniqueID);

        SaveController.Instance?.TriggerAutoSave();

        Destroy(gameObject);
    }
}