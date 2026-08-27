using UnityEngine;
using UnityEngine.SceneManagement;

public class CheckPoint : MonoBehaviour, IInteractable
{
    public static CheckPoint Instance { get; private set; }

    [Header("Settings")]
    public string checkpointName = "";
    [Tooltip("Nếu true, chỉ cần đi qua là tự động lưu (Auto-save)")]
    public bool autoTrigger = false;

    [Header("Visual Effects")]
    public GameObject activeVisual;
    public GameObject inactiveVisual;

    private bool isActivated = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }
    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        if (checkpointName == "")
        {
            checkpointName = AreaController.currentArea != null ? AreaController.currentArea.mapName : "Unknown Area";
        }

        UpdateVisual();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!SaveController.IsDataLoaded) return;

        if (autoTrigger && other.CompareTag("Player"))
        {
            ActivateCheckpoint();
        }
    }

    public void Interact()
    {
        ActivateCheckpoint();
    }

    public bool CanInteract()
    {
        return !autoTrigger
               && GameStateManager.CanProcessInput()
               && SaveController.IsDataLoaded;
    }

    private void ActivateCheckpoint()
    {
        if (!SaveController.IsDataLoaded)
        {
            Debug.LogWarning($"[CheckPoint] Dữ liệu chưa tải xong. Hủy kích hoạt checkpoint '{checkpointName}' để tránh hỏng file save.");
            return;
        }

        if (SaveController.IsSaving) return;

        SaveController.nextSpawnPosition = transform.position;
        SaveController.pendingSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        isActivated = true;
        UpdateVisual();

        SaveController.Instance.SetCheckpoint(
            SceneManager.GetActiveScene().name,
            transform.position
        );

        if (!string.IsNullOrEmpty(checkpointName))
        {
            GameNotify.Show($"Đã lưu tại {checkpointName}");

        }

        Debug.Log($"[CheckPoint] Đã kích hoạt checkpoint: {transform.position}");

        if (SaveController.Instance != null)
        {
            SaveController.Instance.SaveGame(SaveReason.Checkpoint, (isSuccess) =>
            {
                Debug.Log("Lưu checkpoint hoàn tất!");
            });
        }
    }

    private void UpdateVisual()
    {
        if (activeVisual != null) activeVisual.SetActive(isActivated);
        if (inactiveVisual != null) inactiveVisual.SetActive(!isActivated);
    }
}