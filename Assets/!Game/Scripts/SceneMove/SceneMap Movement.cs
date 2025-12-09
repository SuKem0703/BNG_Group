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
            Debug.Log($"SceneMapMove trên '{gameObject.name}' không có Monologue (Optional).");
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (!SaveController.IsDataLoaded)
        {
            Debug.LogWarning($"[SceneMapMove] Dữ liệu chưa load xong. Chặn chuyển sang '{sceneName}'.");
            return;
        }

        if (!string.IsNullOrEmpty(SaveController.pendingSceneName))
        {
            return;
        }

        if (!canEnter)
        {
            HandleBlockedEntry($"Không thể vào map '{sceneName}' vì canEnter = false!");
            return;
        }

        // --- Logic Quest ---
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

        // --- Logic Save & Move ---
        SaveController saveController = FindFirstObjectByType<SaveController>();
        if (saveController != null)
        {
            if (SaveController.IsSaving)
            {
                Debug.LogWarning($"Không thể chuyển sang '{sceneName}' vì hệ thống đang bận lưu game.");
                return;
            }

            Debug.Log($"Bắt đầu quy trình Lưu và Chuyển sang map '{sceneName}'...");

            SaveController.nextSpawnPosition = playerPosition;
            SaveController.pendingSceneName = sceneName;

            saveController.SaveGame(() =>
            {
                Debug.Log($"Lưu hoàn tất. Đang chuyển sang scene: {sceneName}");
                SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
            });

            return;
        }

        Debug.Log("Switching Scene to " + sceneName + " (No SaveController found)");
        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }

    private void HandleBlockedEntry(string reason)
    {
        if (!SaveController.IsDataLoaded) return;

        if (!string.IsNullOrEmpty(SaveController.pendingSceneName)) return;

        if (monologueComponent != null)
        {
            if (monologueComponent.triggerOnEnter)
            {
                monologueComponent.OpenDialogOnTrigger();
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

        if (CinemachineShaker.Instance != null)
            CinemachineShaker.Instance.TriggerShake(3f, 10f, 0.2f);
    }
    private IEnumerator SaveAndLoad(SaveController saveController)
    {
        yield return StartCoroutine(saveController.SaveRoutine());
        yield return null;
        Debug.Log("Switching Scene to " + sceneName + " (after save)");
        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }
}