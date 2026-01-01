using System.Collections;
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
            MapController mapController = MapController.Instance;
            string checkpointName = mapController.mapName;
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

        // Nếu checkpoint này đã kích hoạt rồi và bạn không muốn lưu lại liên tục thì mở comment dòng dưới
        // if (isActivated) return; 

        SaveController.nextSpawnPosition = transform.position;
        SaveController.pendingSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        isActivated = true;
        UpdateVisual();

        SaveController.Instance.SetCheckpoint(
            SceneManager.GetActiveScene().name,
            transform.position
        );

        Debug.Log($"[CheckPoint] Đã kích hoạt checkpoint: {checkpointName}");
        ShowNotification($"Đã lưu tại {checkpointName}");

        if (SaveController.Instance != null)
        {
            SaveController.Instance.SaveGame(SaveReason.Checkpoint, (isSuccess) =>
            {
                Debug.Log("Lưu checkpoint hoàn tất!");
                // SaveController.nextSpawnPosition = null; 
                // SaveController.pendingSceneName = null;
            });
        }
    }

    private void UpdateVisual()
    {
        if (activeVisual != null) activeVisual.SetActive(isActivated);
        if (inactiveVisual != null) inactiveVisual.SetActive(!isActivated);
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