using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneMapMove : MonoBehaviour
{
    public string sceneName;
    public Vector2 playerPosition;

    [Header("Access Settings")]
    public bool canEnter = true;

    [Header("Quest Requirements")]
    public string requiredQuestID;
    public bool requireNotStarted = false;
    public bool requireInProgress = false;
    public bool requireCompleted = false;
    public bool requireNoMoreQuests = false;

    [Header("Monologue Settings")]
    private Monologue monologueComponent;
    private void Awake()
    {
        monologueComponent = GetComponent<Monologue>();
        if (monologueComponent == null)
        {
            Debug.LogError($"SceneMapMove trên '{gameObject.name}' yêu cầu một component Monologue.cs.");
        }
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (!canEnter)
        {
            HandleBlockedEntry($"Không thể vào map '{sceneName}' vì canEnter = false!");
            return;
        }

        if (!string.IsNullOrEmpty(requiredQuestID) && (requireNotStarted || requireInProgress || requireCompleted || requireNoMoreQuests))
        {
            bool noMoreQuests = false;
            bool completed = false;
            bool inProgress = false;
            bool notStarted = false;

            var qc = QuestController.Instance;
            if (qc == null)
            {
                notStarted = true;
            }
            else
            {
                if (qc.IsQuestHandedIn(requiredQuestID)) noMoreQuests = true;
                else if (qc.IsQuestCompleted(requiredQuestID)) completed = true;
                else if (qc.IsQuestActive(requiredQuestID)) inProgress = true;
                else notStarted = true;
            }

            bool matches = (requireNotStarted && notStarted)
                           || (requireInProgress && inProgress)
                           || (requireCompleted && completed)
                           || (requireNoMoreQuests && noMoreQuests);

            if (!matches)
            {
                HandleBlockedEntry($"Không thể vào map '{sceneName}': Yêu cầu nhiệm vụ không khớp.");
                return;
            }
        }

        SaveController saveController = FindFirstObjectByType<SaveController>();
        if (saveController != null)
        {
            // Kiểm tra nếu đang lưu thì không kích hoạt chuyển cảnh để tránh lỗi
            if (SaveController.IsSaving)
            {
                Debug.LogWarning($"Không thể chuyển sang '{sceneName}' vì đang trong quá trình lưu game khác.");
                return;
            }

            Debug.Log($"Bắt đầu quy trình Lưu và Chuyển sang map '{sceneName}'...");

            // Cài đặt thông tin cho scene tiếp theo
            SaveController.nextSpawnPosition = playerPosition;
            SaveController.pendingSceneName = sceneName;

            // Gọi hàm SaveGame và truyền vào một "Lambda Expression" (hàm vô danh).
            // Đoạn code bên trong dấu { } này CHỈ chạy khi SaveController báo đã lưu xong.
            saveController.SaveGame(() =>
            {
                Debug.Log($"Lưu hoàn tất. Đang chuyển sang scene: {sceneName}");
                SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
            });

            // Thoát hàm, mọi việc còn lại do SaveController lo.
            return;
        }

        Debug.Log("Switching Scene to " + sceneName);
        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }

    private void HandleBlockedEntry(string reason)
    {
        if (monologueComponent != null)
        {
            if (monologueComponent.triggerOnEnter)
            {
                var method = monologueComponent.GetType()
                    .GetMethod("StartDialogue", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                method?.Invoke(monologueComponent, null);

                Debug.Log(reason + " (Monologue tự động)");
            }
            else if (monologueComponent.CanInteract())
            {
                monologueComponent.Interact();
            }
        }
        else
        {
            Debug.LogWarning(reason);
        }

        // Rung camera
        if (CinemachineShaker.Instance != null)
            CinemachineShaker.Instance.TriggerShake(3f, 10f, 0.2f);
    }

    private IEnumerator SaveAndLoad(SaveController saveController)
    {
        // Wait for the save routine to finish so server has the latest save
        yield return StartCoroutine(saveController.SaveRoutine());

        // Small delay to ensure server consistency if needed
        yield return null;

        Debug.Log("Switching Scene to " + sceneName + " (after save)");
        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }
}