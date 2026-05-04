using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Cinemachine;

public class SceneMapMove : MonoBehaviour
{
    public string sceneName;
    public Vector2 playerPosition;

    [Header("Internal Move Settings (Optional)")]
    public BoxCollider2D newMapBoundary;

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

    public bool IsEntryAllowed()
    {
        if (!canEnter)
            return false;

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

            return matches;
        }

        return true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("PlayerController"))
            return;

        if (!SaveController.IsDataLoaded)
        {
            string data_loading = GetText("NOTIFY_DATA_LOADING");
            GameNotify.Show(data_loading);
            return;
        }

        if (!string.IsNullOrEmpty(SaveController.pendingSceneName))
        {
            return;
        }

        if (SaveController.IsSaving)
        {
            Debug.LogWarning($"[SceneMapMove] Blocked '{sceneName}' due to saving.");
            string system_busy = GetText("NOTIFY_SYSTEM_BUSY");
            GameNotify.Show(system_busy);
            return;
        }

        if (!IsEntryAllowed())
        {
            HandleBlockedEntry($"Blocked: Entry conditions not met");
            return;
        }

        bool isInternalMove = string.IsNullOrEmpty(sceneName) || sceneName == "-1";

        if (isInternalMove)
        {
            StartCoroutine(InternalMoveRoutine(other.transform));
            return;
        }

        // --- XỬ LÝ CHUYỂN SANG SCENE KHÁC ---
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
                    if (Unity.Netcode.NetworkManager.Singleton != null && Unity.Netcode.NetworkManager.Singleton.IsListening)
                    {
                        Unity.Netcode.NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
                    }
                    else
                    {
                        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
                    }
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

        if (Unity.Netcode.NetworkManager.Singleton != null && Unity.Netcode.NetworkManager.Singleton.IsListening)
        {
            Unity.Netcode.NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
        }
        else
        {
            SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
        }
    }

    private IEnumerator InternalMoveRoutine(Transform playerTransform)
    {
        GameStateManager.StartLoading();

        GameObject faderObj = new GameObject("InternalMoveFader");
        Canvas canvas = faderObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;

        UnityEngine.UI.Image fadeImage = faderObj.AddComponent<UnityEngine.UI.Image>();
        fadeImage.color = new Color(0, 0, 0, 0);

        float fadeDuration = 0.5f;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            fadeImage.color = new Color(0, 0, 0, Mathf.Clamp01(elapsed / fadeDuration));
            yield return null;
        }
        fadeImage.color = Color.black;

        playerTransform.position = playerPosition;

        if (newMapBoundary != null)
        {
            var confiner = FindFirstObjectByType<CinemachineConfiner2D>();
            if (confiner != null)
            {
                confiner.BoundingShape2D = newMapBoundary;
                confiner.InvalidateBoundingShapeCache();
            }
        }

        var cam = FindFirstObjectByType<CinemachineCamera>();
        if (cam != null) cam.PreviousStateIsValid = false;

        Debug.Log($"[SceneMapMove] Đã dịch chuyển nội bộ tới {playerPosition}");

        elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            fadeImage.color = new Color(0, 0, 0, 1f - Mathf.Clamp01(elapsed / fadeDuration));
            yield return null;
        }

        Destroy(faderObj);

        GameStateManager.EndLoading();
    }

    private void HandleBlockedEntry(string debugReason)
    {
        if (CinemachineShaker.Instance != null)
            CinemachineShaker.Instance.TriggerShake(3f, 10f, 0.2f);

        if (monologueComponent != null)
        {
            monologueComponent.OpenDialogOnTrigger();
        }
    }

    private string GetText(string key)
    {
        if (LocalizationManager.Instance != null)
            return LocalizationManager.Instance.GetText(key);
        return key;
    }
}