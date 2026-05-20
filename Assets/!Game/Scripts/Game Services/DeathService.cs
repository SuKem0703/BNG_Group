using UnityEngine;
using UnityEngine.SceneManagement;

public class DeathService : MonoBehaviour
{
    public static DeathService Instance { get; private set; }

    [Header("Death Penalty Settings")]
    public float expPenaltyPercentage = 0.1f;

    public static bool IsRespawningFlag = false;

    public static event System.Action OnPlayerDied;

    private PlayerVitals playerVitals;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    private void Start()
    {
        PlayerCore.OnPlayerSpawned += InitializeReference;
    }

    private void InitializeReference(PlayerCore core)
    {
        PlayerCore.OnPlayerSpawned -= InitializeReference;

        playerVitals = core.playerVitals;
    }

    private void OnDestroy()
    {
        PlayerCore.OnPlayerSpawned -= InitializeReference;
    }

    public void HandlePlayerDeath()
    {
        playerVitals.SetDeathStateServerRpc(true);

        Debug.Log("DeathService: Bắt đầu quy trình xử lý tử vong (Logic)...");

        playerVitals.SetInvincible(true);
        if (PlayerStats.Instance.playerCollider != null)
        {
            PlayerStats.Instance.playerCollider.enabled = false;
        }

        ApplyDeathPenalty();
        PlayerStats.Instance.ForceSyncExpImmediate();

        UpdateCheckpointInfo();

        PlayerMovement playerMovement = PlayerStats.Instance.GetComponent<PlayerMovement>();
        if (playerMovement != null)
        {
            playerMovement.TriggerDeath();
        }
        else
        {
            Debug.LogWarning("Không tìm thấy PlayerMovement.");
        }

        if (SaveController.Instance != null)
        {
            SaveController.Instance.SaveGame(SaveReason.Death, (isSuccess) =>
            {
                if (isSuccess) Debug.Log("DeathService: Đã đồng bộ trạng thái chết lên Server.");
                else Debug.LogError("DeathService: Đồng bộ tử vong thất bại!");
            }, true);
        }

        OnPlayerDied?.Invoke();
    }

    private void ApplyDeathPenalty()
    {
        int currentExp = PlayerStats.Instance.exp;
        int penalty = Mathf.FloorToInt(currentExp * expPenaltyPercentage);
        PlayerStats.Instance.AddEXP(-penalty);
        GameNotify.Show($"Bạn đã mất {penalty} EXP!");
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

    public void FinalizeRespawn()
    {
        if (PlayerStats.Instance != null)
        {
            playerVitals.SetInvincible(false);
            if (PlayerStats.Instance.playerCollider != null)
                PlayerStats.Instance.playerCollider.enabled = true;
        }
    }

    public void ExecuteRespawn()
    {
        IsRespawningFlag = true;

        string targetScene = SaveController.currentCheckpointScene;
        if (string.IsNullOrEmpty(targetScene)) targetScene = SceneManager.GetActiveScene().name;

        if (targetScene == SceneManager.GetActiveScene().name)
        {
            StartCoroutine(HandleInSceneRespawn());
        }
        else
        {
            CleanUpBeforeCrossSceneRespawn();

            if (Unity.Netcode.NetworkManager.Singleton != null && Unity.Netcode.NetworkManager.Singleton.IsListening)
            {
                if (Unity.Netcode.NetworkManager.Singleton.IsServer)
                {
                    Unity.Netcode.NetworkManager.Singleton.SceneManager.LoadScene(targetScene, LoadSceneMode.Single);
                }
            }
            else
            {
                SceneManager.LoadScene(targetScene, LoadSceneMode.Single);
            }
        }
    }

    private void CleanUpBeforeCrossSceneRespawn()
    {
        if (PlayerStats.Instance != null)
        {
            playerVitals.SetDeathStateServerRpc(false);
            playerVitals.ResetVitals();

            var pMovement = PlayerStats.Instance.GetComponent<PlayerMovement>();
            if (pMovement != null)
            {
                pMovement.ResetDeathState();
                pMovement.enabled = true;
            }

            playerVitals.SetInvincible(false);
            if (PlayerStats.Instance.playerCollider != null)
                PlayerStats.Instance.playerCollider.enabled = true;
        }

        if (CommonUIController.Instance != null)
            CommonUIController.Instance.SetUIVisible(true);

        IsRespawningFlag = false;
    }

    private System.Collections.IEnumerator HandleInSceneRespawn()
    {
        if (PlayerStats.Instance != null)
        {
            playerVitals.SetDeathStateServerRpc(false);

            if (PlayerStats.Instance.playerCollider != null)
                PlayerStats.Instance.playerCollider.enabled = false;

            Vector3 spawnPos = SaveController.currentCheckpointPos ?? Vector3.zero;
            PlayerStats.Instance.transform.position = spawnPos;

            playerVitals.ResetVitals();

            var vcam = FindFirstObjectByType<Unity.Cinemachine.CinemachineCamera>();
            if (vcam != null)
            {
                vcam.ForceCameraPosition(spawnPos, Quaternion.identity);
            }

            yield return new WaitForSeconds(0.2f);

            yield return playerVitals.FinalizeRespawnProtection(1.5f);

            var pMovement = PlayerStats.Instance.GetComponent<PlayerMovement>();
            if (pMovement != null)
            {
                pMovement.ResetDeathState();
                pMovement.enabled = true;
            }
        }

        if (CommonUIController.Instance != null) CommonUIController.Instance.SetUIVisible(true);

        IsRespawningFlag = false;
    }
}