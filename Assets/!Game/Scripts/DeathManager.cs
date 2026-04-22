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

    private bool isRespawning = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;

        if (gameOverUI == null) gameOverUI = GameObject.Find("GameOverUI");
        if (gameOverUI != null) gameOverUI.SetActive(false);
    }

    // Flag set when a respawn is requested so newly loaded Player can apply protection
    public static bool IsRespawningFlag = false;

    public void HandlePlayerDeath()
    {
        if (PlayerStats.Instance == null) return;

        Debug.Log("DeathManager: Bắt đầu quy trình xử lý tử vong...");
        isRespawning = false;

        // Make player invincible immediately and disable collider to avoid further hits
        PlayerStats.Instance.SetInvincible(true);
        if (PlayerStats.Instance.playerCollider != null)
        {
            PlayerStats.Instance.playerCollider.enabled = false;
        }

        if (CommonUIController.Instance != null)
        {
            CommonUIController.Instance.SetUIVisible(false);
        }

        ApplyDeathPenalty();

        PlayerStats.Instance.ForceSyncExpImmediate();
        UpdateCheckpointInfo();

        // Tìm component PlayerMovement trên nhân vật hiện tại (Knight hoặc Mage)
        PlayerMovement playerMovement = PlayerStats.Instance.GetComponentInChildren<PlayerMovement>();
        if (playerMovement != null)
        {
            playerMovement.TriggerDeath();
        }
        else
        {
            Debug.LogWarning("Không tìm thấy PlayerMovement, sẽ hiện UI ngay lập tức.");
            ShowGameOverUI();
        }

        if (SaveController.Instance != null)
        {
            SaveController.Instance.SaveGame(SaveReason.Death, (isSuccess) =>
            {
                if (isSuccess) Debug.Log("DeathManager: Đã lưu dữ liệu ngầm thành công.");
                else Debug.LogError("DeathManager: Lưu dữ liệu thất bại!");
            }, true);
        }
    }

    private void UpdateCheckpointInfo()
    {
        if (SaveController.Instance != null)
        {
            if (SaveController.currentCheckpointPos != null && !string.IsNullOrEmpty(SaveController.currentCheckpointScene))
            {
                SaveController.nextSpawnPosition = SaveController.currentCheckpointPos.Value;
                SaveController.pendingSceneName = SaveController.currentCheckpointScene;
            }
            else
            {
                SaveController.nextSpawnPosition = PlayerStats.Instance.transform.position;
                SaveController.pendingSceneName = SceneManager.GetActiveScene().name;
            }
        }
    }

    private void ApplyDeathPenalty()
    {
        int currentExp = PlayerStats.Instance.exp;
        int penalty = Mathf.FloorToInt(currentExp * expPenaltyPercentage);
        PlayerStats.Instance.AddEXP(-penalty);
        GameNotify.Show($"Bạn đã mất {penalty} EXP!");
    }

    // [UPDATE] Hàm này giờ là Public để PlayerMovement gọi qua Animation Event
    public void ShowGameOverUI()
    {
        // Đảm bảo game pause khi UI hiện lên (sau khi animation xong)
        PauseController.SetPause(true);

        if (CommonUIController.Instance != null) CommonUIController.Instance.SetUIVisible(false);

        // [TÙY CHỌN] Có thể ẩn nhân vật ở đây nếu muốn, hoặc để xác xác nhân vật nằm đó
        // if (ClassController.Instance != null) ...

        if (gameOverUI != null)
        {
            CanvasGroup canvasGroup = gameOverUI.GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = gameOverUI.AddComponent<CanvasGroup>();

            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            gameOverUI.SetActive(true);

            canvasGroup.DOFade(1f, fadeDuration).SetUpdate(true).OnComplete(() =>
            {
                canvasGroup.blocksRaycasts = true;
            });
        }
        else
        {
            OnRespawnClicked();
        }
    }

    public void OnRespawnClicked()
    {
        if (isRespawning) return;
        isRespawning = true;

        Debug.Log("Nút Hồi sinh đã được bấm!");
        PauseController.SetPause(false);
        DOTween.KillAll();

        string targetScene = SaveController.pendingSceneName;
        if (string.IsNullOrEmpty(targetScene))
        {
            targetScene = SceneManager.GetActiveScene().name;
        }

        // Mark global flag so newly loaded scene can apply respawn protection
        IsRespawningFlag = true;
        SceneManager.LoadScene(targetScene, LoadSceneMode.Single);
    }

    // Called by animation event or after scene load to finalize respawn state
    public void FinalizeRespawn()
    {
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.SetInvincible(false);
            if (PlayerStats.Instance.playerCollider != null)
                PlayerStats.Instance.playerCollider.enabled = true;
        }
    }
}