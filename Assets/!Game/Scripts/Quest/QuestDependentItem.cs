using UnityEngine;

public class QuestDependentItem : MonoBehaviour
{
    [Tooltip("ID của Quest mà vật này phụ thuộc vào")]
    public string requiredQuestID;

    [Tooltip("Trạng thái Quest yêu cầu để vật này HIỂN THỊ")]
    public QuestStatusCondition requiredState;

    // Enum này có thể dùng chung
    public enum QuestStatusCondition
    {
        NotStarted,
        InProgress,
        Completed
    }

    /// <summary>
    /// Hàm này sẽ được Manager gọi để cập nhật trạng thái ẩn/hiện.
    /// </summary>
    public void UpdateVisibility()
    {
        if (QuestController.Instance == null || string.IsNullOrEmpty(requiredQuestID))
        {
            gameObject.SetActive(false);
            return;
        }

        // Lấy thông tin trạng thái quest
        bool isActive = QuestController.Instance.IsQuestActive(requiredQuestID);
        bool isCompleted = QuestController.Instance.IsQuestCompleted(requiredQuestID);
        bool isHandedIn = QuestController.Instance.IsQuestHandedIn(requiredQuestID);

        bool shouldBeVisible = false;

        // Quyết định xem có nên hiện không
        switch (requiredState)
        {
            case QuestStatusCondition.NotStarted:
                shouldBeVisible = !isActive && !isHandedIn;
                break;
            case QuestStatusCondition.InProgress:
                shouldBeVisible = isActive && !isCompleted;
                break;
            case QuestStatusCondition.Completed:
                shouldBeVisible = isActive && isCompleted;
                break;
        }

        // Tự set active cho chính mình
        gameObject.SetActive(shouldBeVisible);
    }
}