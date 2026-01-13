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
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        // ========================================================================
        // 1. KIỂM TRA TRẠNG THÁI KỸ THUẬT (Dùng Notify + Return ngay)
        // ========================================================================

        if (!SaveController.IsDataLoaded)
        {
            string data_loading = GetText("NOTIFY_DATA_LOADING");
            GameNotify.Show(data_loading);
            return;
        }

        if (!string.IsNullOrEmpty(SaveController.pendingSceneName))
        {
            // GameNotify.Show(LocalizationManager.GetText("NOTIFY_SCENE_SWITCHING")); 
            return;
        }

        // Check 3: Hệ thống đang bận lưu (tránh corrupt save file)
        if (SaveController.IsSaving)
        {
            Debug.LogWarning($"[SceneMapMove] Blocked '{sceneName}' due to saving.");
            string system_busy = GetText("NOTIFY_SYSTEM_BUSY");
            GameNotify.Show(system_busy);
            return;
        }

        // ========================================================================
        // 2. KIỂM TRA LOGIC GAME (Dùng Dialogue/Monologue)
        // ========================================================================

        if (!canEnter)
        {
            HandleBlockedEntry($"Blocked: canEnter = false");
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
                HandleBlockedEntry($"Blocked: Quest Requirements not met");
                return;
            }
        }

        // ========================================================================
        // 3. THỰC HIỆN CHUYỂN MAP (Hợp lệ)
        // ========================================================================

        SaveController saveController = FindFirstObjectByType<SaveController>();
        if (saveController != null)
        {
            string saving_game = GetText("NOTIFY_SAVING_GAME");
            GameNotify.Show(saving_game);

            SaveController.nextSpawnPosition = playerPosition;
            SaveController.pendingSceneName = sceneName;

            saveController.SaveGame(SaveReason.SceneTransition, (isSuccess) =>
            {
                if (isSuccess)
                {
                    SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
                }
                else
                {
                    SaveController.pendingSceneName = "";
                    Debug.LogError("Scene transition failed: Save error.");
                    string format = GetText("NOTIFY_SAVE_FAILED");
                    GameNotify.Show(format);
                }
            });
            return;
        }

        Debug.Log("Switching Scene (No SaveController)");
        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }

    /// <summary>
    /// Xử lý khi bị chặn bởi Logic Game (Hiện Dialogue + Rung màn hình)
    /// </summary>
    private void HandleBlockedEntry(string debugReason)
    {
        if (CinemachineShaker.Instance != null)
            CinemachineShaker.Instance.TriggerShake(3f, 10f, 0.2f);

        if (monologueComponent != null)
        {
            monologueComponent.OpenDialogOnTrigger();
        }
    }

    // Helper tĩnh để lấy text từ LocalizationManager
    private string GetText(string key)
    {
        if (LocalizationManager.Instance != null)
            return LocalizationManager.Instance.GetText(key);
        return key;
    }
}