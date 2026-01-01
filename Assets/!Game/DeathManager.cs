using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class DeathManager : MonoBehaviour
{
    public static DeathManager Instance { get; private set; }

    [Header("Death Penalty Settings")]
    [Tooltip("Phần trăm EXP bị mất khi chết (0.1 = 10%)")]
    public float expPenaltyPercentage = 0.1f;

    [Header("UI Animation Settings")]
    public float fadeDuration = 1.0f;
    public GameObject gameOverUI;

    // Biến để chặn spam nút khi đang load scene
    private bool isRespawning = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;

        if (gameOverUI == null) gameOverUI = GameObject.Find("GameOverUI");
        if (gameOverUI != null) gameOverUI.SetActive(false);
    }

    public void HandlePlayerDeath()
    {
        if (PlayerStats.Instance == null) return;

        Debug.Log("DeathManager: Xử lý tử vong...");
        isRespawning = false;

        // 1. Tính toán và Trừ EXP
        ApplyDeathPenalty();

        // 2. [QUAN TRỌNG] Đẩy EXP lên Server NGAY LẬP TỨC
        // Để đảm bảo Server ghi nhận việc trừ EXP trước khi Save
        PlayerStats.Instance.ForceSyncExpImmediate();

        // 3. Hồi phục HP/MP về Max (để chuẩn bị Save trạng thái "Sống")
        PlayerStats.Instance.RefreshStats();

        // 4. Update thông tin SaveData (Vị trí spawn...)
        if (SaveController.Instance != null)
        {
            if (SaveController.currentCheckpointPos != null && !string.IsNullOrEmpty(SaveController.currentCheckpointScene))
            {
                SaveController.nextSpawnPosition = SaveController.currentCheckpointPos.Value;
                SaveController.pendingSceneName = SaveController.currentCheckpointScene;
            }
            else
            {
                Debug.LogWarning("Chưa có checkpoint! Sẽ hồi sinh tại chỗ.");
                SaveController.nextSpawnPosition = PlayerStats.Instance.transform.position;
                SaveController.pendingSceneName = SceneManager.GetActiveScene().name;
            }
        }

        // 5. Lưu Game (Lúc này EXP đã trừ, HP đã đầy)
        if (SaveController.Instance != null)
        {
            StartCoroutine(SaveAndShowGameOver());
        }
        else
        {
            ShowGameOverUI();
        }
    }

    private void ApplyDeathPenalty()
    {
        int currentExp = PlayerStats.Instance.exp;
        int penalty = Mathf.FloorToInt(currentExp * expPenaltyPercentage);

        // Trừ local (để hiển thị) -> Hàm này sẽ queue vào pendingExpToAdd
        PlayerStats.Instance.AddEXP(-penalty);

        ShowNotification($"Bạn đã mất {penalty} EXP!");
    }

    private IEnumerator SaveAndShowGameOver()
    {
        bool saveCompleted = false;

        // Gọi save
        SaveController.Instance.SaveGame(SaveReason.Death, (isSuccess) =>
        {
            saveCompleted = true;
            if (isSuccess) Debug.Log("Đã lưu dữ liệu sau khi chết (bao gồm trừ EXP).");
            else Debug.LogError("Lưu dữ liệu thất bại!");
        });

        // Chờ save xong (hoặc timeout sau 3 giây để tránh treo game nếu mạng lag)
        float timeout = 3f;
        while (!saveCompleted && timeout > 0)
        {
            timeout -= Time.unscaledDeltaTime; // Dùng unscaled vì game có thể đang pause
            yield return null;
        }

        if (!saveCompleted) Debug.LogWarning("Save quá lâu, bỏ qua chờ đợi để hiện UI.");

        // Hiện UI sau khi save xong
        ShowGameOverUI();
    }

    private void ShowGameOverUI()
    {
        PauseController.SetPause(true); // Pause game
        if (CommonUIController.Instance != null) CommonUIController.Instance.SetUIVisible(false);

        // Ẩn nhân vật
        if (ClassController.Instance != null)
        {
            if (ClassController.Instance.knightObject) ClassController.Instance.knightObject.SetActive(false);
            if (ClassController.Instance.mageObject) ClassController.Instance.mageObject.SetActive(false);
        }

        if (gameOverUI != null)
        {
            CanvasGroup canvasGroup = gameOverUI.GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = gameOverUI.AddComponent<CanvasGroup>();

            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false; // Chặn bấm khi đang fade
            gameOverUI.SetActive(true);

            // Dùng SetUpdate(true) để chạy tween kể cả khi Time.timeScale = 0
            canvasGroup.DOFade(1f, fadeDuration).SetUpdate(true).OnComplete(() =>
            {
                canvasGroup.blocksRaycasts = true; // Cho phép bấm nút
            });
        }
        else
        {
            // Fallback nếu không có UI
            OnRespawnClicked();
        }
    }

    // Gắn hàm này vào nút "Hồi sinh" / "Tiếp tục"
    public void OnRespawnClicked()
    {
        // Chặn bấm liên tục
        if (isRespawning) return;
        isRespawning = true;

        // [FIX] Bỏ dòng check SaveController.IsSaving ở đây
        // Vì UI chỉ hiện sau khi Save xong. Nếu check ở đây có thể gây kẹt nút.

        Debug.Log("Nút Hồi sinh đã được bấm!");

        // 1. Unpause game trước khi load
        PauseController.SetPause(false);
        DOTween.KillAll();

        // 2. Xác định scene cần load
        string targetScene = SaveController.pendingSceneName;

        // Fallback nếu tên scene rỗng
        if (string.IsNullOrEmpty(targetScene))
        {
            targetScene = SceneManager.GetActiveScene().name;
        }

        Debug.Log($"[RESPAWN] Đang load về scene: {targetScene}");

        // 3. Load Scene
        SceneManager.LoadScene(targetScene, LoadSceneMode.Single);
    }

    private void ShowNotification(string message)
    {
        if (LoadResourceManager.Instance != null && LoadResourceManager.Instance.NotifyUIPrefab != null)
        {
            GameObject notifyUIObj = Instantiate(LoadResourceManager.Instance.NotifyUIPrefab);
            NotifyUIController notifyUI = notifyUIObj.GetComponent<NotifyUIController>();
            if (notifyUI != null) notifyUI.Show(message);
        }
    }
}