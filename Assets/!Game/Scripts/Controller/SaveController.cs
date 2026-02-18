using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

// Định nghĩa Intent để Server biết lý do lưu
public enum SaveReason
{
    Manual,             // Người chơi bấm nút Save
    AutoSave,           // Lưu tự động theo thời gian
    Checkpoint,         // Lưu khi chạm checkpoint
    SceneTransition,    // Lưu khi chuyển cảnh
    QuitGame,           // Lưu khi thoát game
    QuestHandIn,         // Lưu khi trả nhiệm vụ
    Death,

    SpendCoin,
    SpendGem,

    BuyItem,
    AddItem,
    RemoveItem,
}

public class SaveController : MonoBehaviour
{
    public static SaveController Instance { get; private set; }

    public static event System.Action OnDataLoaded;
    public static bool IsDataLoaded { get; private set; } = false;
    public static bool IsSaving { get; private set; } = false;

    private GameObject mainLoadingCanvasInstance;
    private GameObject miniLoadingScreenInstance;

    [SerializeField] private TextMeshProUGUI uidText;

    private InventoryController inventoryController;
    private HotbarController hotbarController;
    private Chest[] chests;
    private PlayerStats playerStats;
    private KnightEquipmentPanel knightEquipmentPanel;
    private MageEquipmentPanel mageEquipmentPanel;
    private SharedEquipmentPanel sharedEquipmentPanel;
    private FarmController farmController;
    private StorageChest[] storageChests;

    // Vị trí spawn tiếp theo sau khi load
    public static Vector3? nextSpawnPosition = null;
    public static string pendingSceneName = null;

    // Vị trí checkpoint hiện tại
    public static Vector3? currentCheckpointPos;
    public static string currentCheckpointScene;

    private Coroutine autoSaveCoroutine;
    private float autoSaveDebounceTime = 3.0f;
    private bool isAutoSavePending = false;

    // Khởi tạo Singleton và các tham chiếu UI cơ bản
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        IsDataLoaded = false;

        if (uidText == null)
        {
            uidText = GameObject.Find("UIDText")?.GetComponent<TextMeshProUGUI>();
        }
    }

    // Hủy Singleton khi object bị hủy
    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // Bắt đầu quy trình load game khi Scene khởi chạy
    void Start()
    {
        ShowMainLoadingScreen();
        StartCoroutine(LoadAndFinalize());

        LocalizationManager.OnLanguageChanged += UpdateUIDText;
    }

    private void UpdateUIDText()
    {
        if (uidText != null && IsDataLoaded)
        {
            string format = GetText("UI_UID");
            uidText.text = string.Format(format, GetPlayerUID());
        }
    }

    IEnumerator LoadAndFinalize()
    {
        yield return InitializeComponents();

        bool sceneLoadTriggered = false;
        yield return StartCoroutine(LoadRoutine((wasTriggered) =>
        {
            sceneLoadTriggered = wasTriggered;
        }));

        if (sceneLoadTriggered) yield break;

        pendingSceneName = null;
        nextSpawnPosition = null;

        if (EconomyService.Instance != null)
            EconomyService.Instance.RefreshBalance();

        if (PlayerStatsService.Instance != null)
        {
            PlayerStatsService.Instance.SyncProfile((success) => { });
        }

        bool inventoryLoaded = false;
        if (InventoryService.Instance != null)
        {
            InventoryService.Instance.SyncInventory((serverItems) =>
            {
                if (serverItems != null)
                {
                    // Tạo 2 danh sách riêng biệt
                    List<InventorySaveData> inventoryItems = new List<InventorySaveData>();
                    List<InventorySaveData> hotbarItems = new List<InventorySaveData>();

                    foreach (var svItem in serverItems)
                    {
                        var itemData = new InventorySaveData
                        {
                            dbID = svItem.id,
                            itemID = svItem.itemId,
                            quantity = svItem.quantity,
                            slotIndex = svItem.slotIndex, // Giữ nguyên index gốc
                            isEquipped = svItem.isEquipped,
                            rarity = (ItemRarity)svItem.rarity,
                            qualityFactor = svItem.qualityFactor
                        };

                        // QUY ƯỚC: Slot >= 1000 là Hotbar
                        if (svItem.slotIndex >= 1000)
                        {
                            itemData.slotIndex -= 1000; // Chuẩn hóa về 0, 1, 2...
                            hotbarItems.Add(itemData);
                        }
                        else
                        {
                            inventoryItems.Add(itemData);
                        }
                    }

                    // Đẩy vào InventoryController
                    if (InventoryController.Instance != null)
                        InventoryController.Instance.SetInventoryItems(inventoryItems);

                    // Đẩy vào HotbarController
                    if (HotbarController.Instance != null)
                        HotbarController.Instance.SetHotbarItems(hotbarItems);
                }
                inventoryLoaded = true;
            });
        }
        else inventoryLoaded = true;

        while (!inventoryLoaded) yield return null;

        IsDataLoaded = true;

        UpdateUIDText();

        yield return new WaitForSecondsRealtime(0.5f);
        HideMainLoadingScreen();
        OnDataLoaded?.Invoke();

        // If this load was triggered by a respawn, finalize respawn protections
        if (DeathManager.IsRespawningFlag)
        {
            DeathManager.IsRespawningFlag = false;
            var ps = GameObject.FindGameObjectWithTag("PlayerController")?.GetComponent<PlayerStats>();
            if (ps != null)
            {
                StartCoroutine(ps.FinalizeRespawnProtection(0.5f));
            }
        }
    }

    // Tìm kiếm và gán các reference component trong scene
    IEnumerator InitializeComponents()
    {
        inventoryController = FindFirstObjectByType<InventoryController>(FindObjectsInactive.Include);
        hotbarController = FindFirstObjectByType<HotbarController>();
        chests = FindObjectsByType<Chest>(FindObjectsSortMode.None);
        playerStats = GameObject.FindGameObjectWithTag("PlayerController").GetComponent<PlayerStats>();
        knightEquipmentPanel = FindFirstObjectByType<KnightEquipmentPanel>(FindObjectsInactive.Include);
        mageEquipmentPanel = FindFirstObjectByType<MageEquipmentPanel>(FindObjectsInactive.Include);
        sharedEquipmentPanel = FindFirstObjectByType<SharedEquipmentPanel>(FindObjectsInactive.Include);
        farmController = FindFirstObjectByType<FarmController>();
        storageChests = FindObjectsByType<StorageChest>(FindObjectsSortMode.None);

        yield break;
    }

    // Kích hoạt lưu tự động với cơ chế Debounce (chờ thao tác kết thúc)
    public void TriggerAutoSave()
    {
        if (IsSaving && !isAutoSavePending) return;

        if (autoSaveCoroutine != null)
        {
            StopCoroutine(autoSaveCoroutine);
        }

        isAutoSavePending = true;
        autoSaveCoroutine = StartCoroutine(DebounceAutoSave());
    }

    // Coroutine đếm ngược thời gian chờ trước khi thực hiện lưu ngầm
    IEnumerator DebounceAutoSave()
    {
        yield return new WaitForSeconds(autoSaveDebounceTime);

        if (isAutoSavePending)
        {
            // Gửi reason là AutoSave
            StartCoroutine(SaveRoutine(SaveReason.AutoSave, null, true));
            isAutoSavePending = false;
        }
    }

    // Hàm gọi lưu game công khai, sử dụng cho các thao tác quan trọng cần chặn màn hình
    // Thêm tham số reason, mặc định là Manual nếu không truyền
    public void SaveGame(SaveReason reason = SaveReason.Manual, System.Action<bool> onSaveFinished = null, bool isSilent = false)
    {
        if (IsSaving) return;

        if (autoSaveCoroutine != null) StopCoroutine(autoSaveCoroutine);
        isAutoSavePending = false;

        StartCoroutine(SaveRoutine(reason, onSaveFinished, isSilent));
    }

    // Logic cốt lõi của việc lưu dữ liệu, hỗ trợ chế độ im lặng hoặc chặn màn hình
    // Nhận reason để chuyển tiếp cho Server
    public IEnumerator SaveRoutine(SaveReason reason, System.Action<bool> onSaveFinished = null, bool isSilent = false)
    {
        IsSaving = true;
        if (!isSilent) ShowMiniLoadingScreen();

        if (inventoryController == null || playerStats == null)
        {
            if (!isSilent) HideMiniLoadingScreen();
            IsSaving = false;
            Debug.LogError("SaveController: Thiếu component quan trọng, hủy lưu.");
            onSaveFinished?.Invoke(false);
            yield break;
        }

        List<ChestSaveData> existingChestStates = new List<ChestSaveData>();
        FarmData existingFarmData = new FarmData();

        SaveData serverSave = null;
        yield return LoadFromServer((sd) => { serverSave = sd; });

        if (serverSave != null)
        {
            existingChestStates = serverSave.chestSaveData ?? new List<ChestSaveData>();
            existingFarmData = serverSave.farmData ?? new FarmData();
        }
        List<ChestStorageEntry> existingStorageData = new List<ChestStorageEntry>();
        if (serverSave != null && serverSave.allChestsData != null)
        {
            existingStorageData = serverSave.allChestsData;
        }
        //List<ChestStorageEntry> finalStorageData = MergeStorageChests(existingStorageData);

        Vector3 savePos = GameObject.FindGameObjectWithTag("PlayerController").transform.position;
        string saveScene = SceneManager.GetActiveScene().name;
        if (reason == SaveReason.SceneTransition && nextSpawnPosition != null) savePos = nextSpawnPosition.Value;
        if (reason == SaveReason.SceneTransition && !string.IsNullOrEmpty(pendingSceneName)) saveScene = pendingSceneName;

        SaveData saveData = new SaveData
        {
            playerPosition = nextSpawnPosition ?? GameObject.FindGameObjectWithTag("PlayerController").transform.position,
            currentSceneName = pendingSceneName ?? SceneManager.GetActiveScene().name,
            checkpointPosition = currentCheckpointPos ?? GameObject.FindGameObjectWithTag("PlayerController").transform.position,
            checkpointSceneName = currentCheckpointScene ?? SceneManager.GetActiveScene().name,

            // --- INVENTORY SECTION ---
            // Gửi list RỖNG vì ta dùng bảng riêng, nhưng vẫn gửi slotCount
            backPackSlotCount = inventoryController.slotCount,
            inventorySaveData = new List<InventorySaveData>(),
            hotbarSaveData = new List<InventorySaveData>(),
            // -------------------------

            chestSaveData = MergeChestsState(existingChestStates),
            questProgressData = QuestController.Instance.activeQuests,
            handInQuestIDs = QuestController.Instance.handInQuestIDs,

            knightEquipSaveData = knightEquipmentPanel.GetEquipmentItems(),
            mageEquipSaveData = mageEquipmentPanel.GetEquipmentItems(),
            shareEquipSaveData = sharedEquipmentPanel.GetEquipmentItems(),

            // If saving due to Death, persist full stats for respawn while leaving
            // the in-memory PlayerStats unchanged so the UI still shows 0 HP until respawn.
            currentKnightHP = (reason == SaveReason.Death) ? playerStats.finalKnightMaxHP : playerStats.knightHealth,
            currentmageHP = (reason == SaveReason.Death) ? playerStats.finalMageMaxHP : playerStats.mageHealth,
            currentKnightMP = (reason == SaveReason.Death) ? playerStats.finalKnightMaxMP : playerStats.knightMP,
            currentMageMP = (reason == SaveReason.Death) ? playerStats.finalMageMaxMP : playerStats.mageMP,
            currentStamina = (reason == SaveReason.Death) ? playerStats.finalStamina : playerStats.currentStamina,

            farmData = MergeFarmData(existingFarmData),
            //allChestsData = finalStorageData,
            collectedByScene = collectedByScene
        };

        bool saveSuccess = false;
        yield return SaveToServer(saveData, reason, (success) => saveSuccess = success);

        if (saveSuccess)
        {
            if (reason != SaveReason.SceneTransition)
            {
                pendingSceneName = null;
                nextSpawnPosition = null;
            }

            if (reason == SaveReason.Manual)
            {
                string msg = LocalizationManager.Instance.GetText("MSG_SAVE_SUCCESS");
                GameNotify.Show(msg);
            }
        }
        else
        {
            Debug.LogError("Lưu game thất bại! Hủy bỏ lệnh chuyển cảnh (nếu có).");
            pendingSceneName = null;
            nextSpawnPosition = null;

            if (!isSilent)
            {
                string msg = LocalizationManager.Instance.GetText("MSG_SAVE_FAIL");
                GameNotify.Show(msg);
            }
        }

        if (!isSilent) HideMiniLoadingScreen();
        IsSaving = false;
        onSaveFinished?.Invoke(saveSuccess);
    }

    // Cập nhật vị trí checkpoint
    public void SetCheckpoint(string scene, Vector3 pos)
    {
        currentCheckpointScene = scene;
        currentCheckpointPos = pos;

        if (IsSaving && !isAutoSavePending) return;
        if (autoSaveCoroutine != null) StopCoroutine(autoSaveCoroutine);

        // Gọi trực tiếp để gửi đúng reason Checkpoint
        StartCoroutine(SaveRoutine(SaveReason.Checkpoint, null, true));
    }

    // Hiển thị màn hình chờ chính (Loading Scene)
    private void ShowMainLoadingScreen()
    {
        if (mainLoadingCanvasInstance != null)
        {
            Destroy(mainLoadingCanvasInstance);
            mainLoadingCanvasInstance = null;
        }

        GameObject prefab = LoadResourceManager.Instance.MainLoadingCanvasPrefab;
        if (prefab != null)
        {
            mainLoadingCanvasInstance = Instantiate(prefab);
            mainLoadingCanvasInstance.SetActive(true);
        }

        //// Set global loading state so input is blocked during load
        //Debug.Log("SaveController: Showing main loading screen -> GameStateManager.StartLoading");
        //GameStateManager.StartLoading();

        PauseController.SetPause(true);
    }

    // Ẩn màn hình chờ chính
    private void HideMainLoadingScreen()
    {
        if (mainLoadingCanvasInstance != null)
        {
            Destroy(mainLoadingCanvasInstance);
            mainLoadingCanvasInstance = null;
        }

        //Debug.Log("SaveController: Hiding main loading screen -> GameStateManager.EndLoading");
        //GameStateManager.EndLoading();

        PauseController.SetPause(false);
    }

    // Hiển thị màn hình chờ phụ (Mini loading)
    private void ShowMiniLoadingScreen()
    {
        GameObject prefab = LoadResourceManager.Instance.MiniLoadingScreenPrefab;

        if (prefab != null)
        {
            if (miniLoadingScreenInstance == null)
            {
                miniLoadingScreenInstance = Instantiate(prefab);
            }

            miniLoadingScreenInstance.SetActive(true);
            //Debug.Log("SaveController: Showing mini loading screen -> GameStateManager.StartLoading");
            //GameStateManager.StartLoading();
            PauseController.SetPause(true);
        }
    }

    // Ẩn màn hình chờ phụ
    private void HideMiniLoadingScreen()
    {
        if (miniLoadingScreenInstance != null)
        {
            miniLoadingScreenInstance.SetActive(false);
            if (GameStateManager.IsMenuOpen == true) return;

            //Debug.Log("SaveController: Hiding mini loading screen -> GameStateManager.EndLoading");
            //GameStateManager.EndLoading();
            PauseController.SetPause(false);
        }
    }

    // Lấy trạng thái rương của scene hiện tại
    private List<ChestSaveData> GetChestsState()
    {
        List<ChestSaveData> chestStates = new List<ChestSaveData>();
        foreach (Chest chest in chests)
        {
            ChestSaveData chestSaveData = new ChestSaveData
            {
                chestID = chest.ChestID,
                isOpened = chest.IsOpened,
            };
            chestStates.Add(chestSaveData);
        }
        return chestStates;
    }

    // Gộp trạng thái rương hiện tại vào dữ liệu tổng từ server
    private List<ChestSaveData> MergeChestsState(List<ChestSaveData> existingChestStates)
    {
        List<ChestSaveData> currentChests = GetChestsState();

        foreach (var chest in currentChests)
        {
            var existing = existingChestStates.FirstOrDefault(c => c.chestID == chest.chestID);
            if (existing != null)
            {
                existing.isOpened = chest.isOpened;
            }
            else
            {
                existingChestStates.Add(chest);
            }
        }

        return existingChestStates;
    }

    // Gộp dữ liệu nông trại hiện tại vào dữ liệu tổng từ server
    private FarmData MergeFarmData(FarmData existingFarmData)
    {
        FarmData currentSceneData = farmController.GetFarmDataToSave();

        if (existingFarmData.plotDataList == null)
            existingFarmData.plotDataList = new List<FarmPlotSaveData>();

        foreach (var currentPlot in currentSceneData.plotDataList)
        {
            var existingPlot = existingFarmData.plotDataList
                .FirstOrDefault(p => p.plotID == currentPlot.plotID);

            if (existingPlot != null)
            {
                existingPlot.hasCrop = currentPlot.hasCrop;
                existingPlot.cropData = currentPlot.cropData;
            }
            else
            {
                existingFarmData.plotDataList.Add(currentPlot);
            }
        }

        return existingFarmData;
    }

    // Quy trình load dữ liệu từ server
    public IEnumerator LoadRoutine(System.Action<bool> onComplete)
    {
        bool sceneLoadWasTriggered = false;

        yield return LoadFromServer((saveData) =>
        {
            sceneLoadWasTriggered = ApplySaveData(saveData);
        });

        onComplete(sceneLoadWasTriggered);
    }

    private bool ApplySaveData(SaveData saveData)
    {
        // --- XỬ LÝ SCENE & VỊ TRÍ ---
        string targetScene = saveData.currentSceneName;
        Vector3 targetPos = saveData.playerPosition;

        if (!string.IsNullOrEmpty(pendingSceneName))
        {
            targetScene = pendingSceneName;
            if (nextSpawnPosition != null) targetPos = nextSpawnPosition.Value;
        }
        else if (string.IsNullOrEmpty(targetScene))
        {
            targetScene = "MAP_CH1_1_ROOM_START"; // Fallback map
        }

        // Kiểm tra Scene có tồn tại không
        bool sceneExists = Enumerable.Range(0, SceneManager.sceneCountInBuildSettings)
            .Select(SceneUtility.GetScenePathByBuildIndex)
            .Any(scenePath => scenePath.EndsWith($"{targetScene}.unity"));

        if (!sceneExists) targetScene = "MAP_CH1_1_ROOM_START";

        // Nếu khác Scene -> Chuyển Scene (Return true để báo hiệu cho LoadRoutine dừng lại)
        if (SceneManager.GetActiveScene().name != targetScene)
        {
            SceneManager.LoadScene(targetScene);
            pendingSceneName = targetScene;
            nextSpawnPosition = targetPos;
            return true;
        }

        // Nếu đúng Scene -> Đặt vị trí nhân vật
        GameObject player = GameObject.FindGameObjectWithTag("PlayerController");
        if (player != null)
        {
            player.transform.position = targetPos;
            Physics2D.SyncTransforms();
            nextSpawnPosition = null;
            pendingSceneName = null;
        }

        if (!string.IsNullOrEmpty(saveData.checkpointSceneName))
        {
            currentCheckpointScene = saveData.checkpointSceneName;
            currentCheckpointPos = saveData.checkpointPosition;
        }

        if (inventoryController != null)
        {
            inventoryController.slotCount = saveData.backPackSlotCount;
        }

        //LoadChestStates(saveData.chestSaveData);

        if (knightEquipmentPanel != null) knightEquipmentPanel.SetEquipmentItems(saveData.knightEquipSaveData);
        if (mageEquipmentPanel != null) mageEquipmentPanel.SetEquipmentItems(saveData.mageEquipSaveData);
        if (sharedEquipmentPanel != null) sharedEquipmentPanel.SetEquipmentItems(saveData.shareEquipSaveData);

        if (QuestController.Instance != null)
        {
            QuestController.Instance.LoadQuestProgress(saveData.questProgressData);
            QuestController.Instance.handInQuestIDs = saveData.handInQuestIDs;
        }

        if (playerStats != null)
        {
            playerStats.ApplyAllClassEquippedItems();

            // Load HP/MP/Stamina hiện tại
            playerStats.knightHealth = saveData.currentKnightHP;
            playerStats.mageHealth = saveData.currentmageHP;
            playerStats.knightMP = saveData.currentKnightMP;
            playerStats.mageMP = saveData.currentMageMP;
            playerStats.currentStamina = saveData.currentStamina;
        }

        if (farmController != null && saveData.farmData != null)
        {
            farmController.LoadFarmData(saveData.farmData);
        }

        //LoadStorageChestStates(saveData.allChestsData);

        collectedByScene = saveData.collectedByScene ?? new List<SceneCollected>();

        // 5. --- CAMERA SYNC ---
        var vcam = FindFirstObjectByType<CinemachineCamera>();
        if (vcam != null && player != null)
        {
            vcam.ForceCameraPosition(player.transform.position, Quaternion.identity);
        }
        try
        {
            Unity.Cinemachine.CinemachineCore.ResetCameraState();
        }
        catch { }

        return false;
    }


    [System.Serializable]
    public class SaveDataRequest
    {
        public string DataSave;
        public string Reason;
    }

    // Gửi dữ liệu lên Server
    IEnumerator SaveToServer(SaveData saveData, SaveReason reason, System.Action<bool> onComplete)
    {
        string json = JsonUtility.ToJson(new SaveDataRequest
        {
            DataSave = JsonUtility.ToJson(saveData),
            Reason = reason.ToString()
        });

        string url = NetworkConfig.GetUrl("api/GameData/save-data");
        string token = PlayerPrefs.GetString("AuthToken", "");

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", $"Bearer {token}");

        float startTime = Time.realtimeSinceStartup;
        yield return request.SendWebRequest();
        float duration = Time.realtimeSinceStartup - startTime;
        ServerTimeManager.ReportPing(duration);

        bool isSuccess = false;

        if (request.result == UnityWebRequest.Result.Success)
        {
            isSuccess = true;
        }
        else
        {
            isSuccess = false;
            
            Debug.LogError($"[Client] Save Failed! Code: {request.responseCode} | Error: {request.error}");
            Debug.LogError($"[Client] Server Reason: {request.downloadHandler.text}");
        }

        onComplete?.Invoke(isSuccess);
    }

    // Tải dữ liệu từ Server
    IEnumerator LoadFromServer(System.Action<SaveData> onLoaded)
    {
        string url = NetworkConfig.GetUrl("api/GameData/get-save");
        string token = PlayerPrefs.GetString("AuthToken", "");

        UnityWebRequest request = UnityWebRequest.Get(url);
        request.SetRequestHeader("Authorization", $"Bearer {token}");

        float startTime = Time.realtimeSinceStartup;
        yield return request.SendWebRequest();
        float duration = Time.realtimeSinceStartup - startTime;
        ServerTimeManager.ReportPing(duration);

        if (request.result == UnityWebRequest.Result.Success)
        {
            string json = request.downloadHandler.text;
            SaveData data = JsonUtility.FromJson<SaveData>(json);
            onLoaded?.Invoke(data);
        }
        else
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            SceneManager.LoadScene("MainMenu");
        }
    }

    // Lấy UID người chơi từ PlayerPrefs
    public string GetPlayerUID()
    {
        return PlayerPrefs.GetString("AccountId", "");
    }

    private string GetText(string key)
    {
        if (LocalizationManager.Instance != null)
            return LocalizationManager.Instance.GetText(key);
        return key;
    }

    public List<SceneCollected> collectedByScene = new List<SceneCollected>();

    // Kiểm tra item đã được thu thập ở scene chưa
    public bool IsCollected(string sceneName, string id)
    {
        if (collectedByScene == null)
            collectedByScene = new List<SceneCollected>();

        var s = collectedByScene.Find(x => x.sceneName == sceneName);
        return s != null && s.collectedIDs.Contains(id);
    }

    // Đánh dấu item đã được thu thập
    public void MarkCollected(string sceneName, string id)
    {
        var s = collectedByScene.Find(x => x.sceneName == sceneName);
        if (s == null)
        {
            s = new SceneCollected { sceneName = sceneName };
            collectedByScene.Add(s);
        }
        if (!s.collectedIDs.Contains(id))
            s.collectedIDs.Add(id);
    }

    [System.Serializable]
    public class SceneCollected
    {
        public string sceneName;
        public List<string> collectedIDs = new List<string>();
    }
}