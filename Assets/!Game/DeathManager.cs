using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class DeathManager : MonoBehaviour
{
    public static DeathManager Instance { get; private set; }

    [Header("Death Penalty Settings")]
    [Tooltip("Phần trăm vàng bị mất khi chết (0.1 = 10%)")]
    public float coinPenaltyPercentage = 0.1f;
    [Tooltip("Số vàng tối thiểu bị trừ")]
    public int minCoinPenalty = 10;

    [Header("UI Animation Settings")]
    [Tooltip("Thời gian hiện bảng Game Over")]
    public float fadeDuration = 1.0f;

    public GameObject gameOverUI;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;

        if (gameOverUI == null)
        {
            gameOverUI = GameObject.Find("GameOverUI");
        }

        gameOverUI.SetActive(false);
    }

    public void HandlePlayerDeath()
    {
        if (PlayerStats.Instance == null) return;

        Debug.Log("DeathManager: Bắt đầu quy trình xử lý tử vong...");

        ApplyDeathPenalty();

        PlayerStats.Instance.RefreshStats();

        if (SaveController.currentCheckpointPos != null && !string.IsNullOrEmpty(SaveController.currentCheckpointScene))
        {
            SaveController.nextSpawnPosition = SaveController.currentCheckpointPos.Value;
            SaveController.pendingSceneName = SaveController.currentCheckpointScene;
        }
        else
        {
            Debug.LogWarning("Chưa có checkpoint! Sẽ lưu vị trí hiện tại (chỗ chết).");
        }

        if (SaveController.Instance != null)
        {
            StartCoroutine(SaveAndShowGameOver());
        }
    }

    private void ApplyDeathPenalty()
    {
        int currentCoin = PlayerStats.Instance.coin;
        int penalty = Mathf.FloorToInt(currentCoin * coinPenaltyPercentage);

        if (penalty < minCoinPenalty && currentCoin > 0)
            penalty = Mathf.Min(minCoinPenalty, currentCoin);

        PlayerStats.Instance.SpendCoin(penalty);
    }

    private IEnumerator SaveAndShowGameOver()
    {
        bool saveCompleted = false;

        SaveController.Instance.SaveGame(() => { saveCompleted = true; });

        yield return new WaitUntil(() => saveCompleted);

        ShowGameOverUI();
    }

    private void ShowGameOverUI()
    {
        PauseController.SetPause(true);

        CommonUIController.Instance.SetUIVisible(false);

        ClassController.Instance.knightObject.SetActive(false);
        ClassController.Instance.mageObject.SetActive(false);

        if (gameOverUI != null)
        {
            CanvasGroup canvasGroup = gameOverUI.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameOverUI.AddComponent<CanvasGroup>();
            }

            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            gameOverUI.SetActive(true);

            canvasGroup.DOFade(1f, fadeDuration)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    canvasGroup.blocksRaycasts = true;
                });
        }
        else
        {
            Debug.LogError("Không tìm thấy GameOverUI! Load lại scene ngay lập tức.");
            OnRespawnClicked();
        }
    }

    // Hàm được gọi khi người chơi bấm nút "Hồi sinh" trên UI
    public void OnRespawnClicked()
    {
        PauseController.SetPause(false);
        DOTween.KillAll();

        if (SaveController.IsSaving)
        {
            Debug.LogWarning("[RESPAWN] IsSaving = true, abort");
            return;
        }

        string targetScene = SaveController.currentCheckpointScene;
        Vector3 targetPos = SaveController.currentCheckpointPos.Value;

        if (string.IsNullOrEmpty(targetScene))
        {
            Debug.LogError("[RESPAWN] CheckpointScene NULL or EMPTY");
            return;
        }

        // truyền vào TRƯỚC
        SaveController.nextSpawnPosition = targetPos;
        SaveController.pendingSceneName = targetScene;

        Debug.Log(
            $"[RESPAWN] Commit before save | Scene={targetScene} | Pos={targetPos}"
        );

        SceneManager.LoadScene(targetScene, LoadSceneMode.Single);

        //SaveController.Instance.SaveGame(() =>
        //{
        //    Debug.Log(
        //        $"[RESPAWN CALLBACK] Load scene='{targetScene}'"
        //    );

        //    SceneManager.LoadScene(targetScene, LoadSceneMode.Single);
        //});
    }
}