using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

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
        StartLoadProcess();
    }

    // Wrapper để bắt đầu Coroutine load
    private void StartLoadProcess()
    {
        StartCoroutine(LoadAndFinalize());
    }

    // Quy trình load tuần tự: Khởi tạo component -> Load dữ liệu -> Xử lý UI
    IEnumerator LoadAndFinalize()
    {
        yield return InitializeComponents();

        bool sceneLoadTriggered = false;
        yield return StartCoroutine(LoadRoutine((wasTriggered) =>
        {
            sceneLoadTriggered = wasTriggered;
        }));

        if (sceneLoadTriggered)
        {
            yield break;
        }

        pendingSceneName = null;

        IsDataLoaded = true;

        if (uidText != null)
            uidText.text = "UID: " + GetPlayerUID();

        yield return new WaitForSecondsRealtime(0.5f);

        HideMainLoadingScreen();

        //MenuStateManager.Instance.ResetState();

        OnDataLoaded?.Invoke();
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
            StartCoroutine(SaveRoutine(null, true));
            isAutoSavePending = false;
        }
    }

    // Hàm gọi lưu game công khai, sử dụng cho các thao tác quan trọng cần chặn màn hình
    public void SaveGame(System.Action onSaveFinished = null)
    {
        if (IsSaving)
        {
            return;
        }

        if (autoSaveCoroutine != null) StopCoroutine(autoSaveCoroutine);
        isAutoSavePending = false;

        StartCoroutine(SaveRoutine(onSaveFinished, false));
    }

    // Logic cốt lõi của việc lưu dữ liệu, hỗ trợ chế độ im lặng hoặc chặn màn hình
    public IEnumerator SaveRoutine(System.Action onSaveFinished = null, bool isSilent = false)
    {
        IsSaving = true;

        if (!isSilent)
        {
            ShowMiniLoadingScreen();
        }

        if (inventoryController == null || hotbarController == null || knightEquipmentPanel == null || mageEquipmentPanel == null || sharedEquipmentPanel == null || playerStats == null)
        {
            if (!isSilent) HideMiniLoadingScreen();
            IsSaving = false;
            onSaveFinished?.Invoke();
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
        List<ChestStorageEntry> finalStorageData = MergeStorageChests(existingStorageData);

        SaveData saveData = new SaveData
        {
            playerPosition = nextSpawnPosition ?? GameObject.FindGameObjectWithTag("PlayerController").transform.position,
            currentSceneName = pendingSceneName ?? SceneManager.GetActiveScene().name,

            checkpointPosition = currentCheckpointPos ?? GameObject.FindGameObjectWithTag("PlayerController").transform.position,
            checkpointSceneName = currentCheckpointScene ?? SceneManager.GetActiveScene().name,

            backPackSlotCount = inventoryController.slotCount,
            inventorySaveData = inventoryController.GetInventoryItems(),
            hotbarSaveData = hotbarController.GetHotbarItems(),
            chestSaveData = MergeChestsState(existingChestStates),
            questProgressData = QuestController.Instance.activeQuests,
            handInQuestIDs = QuestController.Instance.handInQuestIDs,

            knightEquipSaveData = knightEquipmentPanel.GetEquipmentItems(),
            mageEquipSaveData = mageEquipmentPanel.GetEquipmentItems(),
            shareEquipSaveData = sharedEquipmentPanel.GetEquipmentItems(),

            lvl = playerStats.level,
            exp = playerStats.exp,
            currentKnightHP = playerStats.knightHealth,
            currentmageHP = playerStats.mageHealth,
            currentKnightMP = playerStats.knightMP,
            currentMageMP = playerStats.mageMP,
            currentStamina = playerStats.currentStamina,
            coin = playerStats.coin,
            gem = playerStats.gem,
            str = playerStats.STR,
            dex = playerStats.DEX,
            con = playerStats.CON,
            intStat = playerStats.INT,
            potentialPoints = playerStats.potentialPoints,

            farmData = MergeFarmData(existingFarmData),
            allChestsData = finalStorageData,

            collectedByScene = collectedByScene
        };

        bool saveSuccess = false;
        yield return SaveToServer(saveData, (success) => saveSuccess = success);

        pendingSceneName = null;

        if (!isSilent)
        {
            HideMiniLoadingScreen();
        }

        IsSaving = false;
        onSaveFinished?.Invoke();
    }

    // Cập nhật vị trí checkpoint
    public void SetCheckpoint(string scene, Vector3 pos)
    {
        currentCheckpointScene = scene;
        currentCheckpointPos = pos;

        TriggerAutoSave();
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

    // Gộp dữ liệu rương lưu trữ hiện tại vào dữ liệu tổng từ server
    private List<ChestStorageEntry> MergeStorageChests(List<ChestStorageEntry> existingChestsData)
    {
        List<ChestStorageEntry> currentChests = GetStorageChestsState();

        if (existingChestsData == null)
            existingChestsData = new List<ChestStorageEntry>();

        foreach (var newChest in currentChests)
        {
            var existingChest = existingChestsData.FirstOrDefault(c => c.chestID == newChest.chestID);

            if (existingChest != null)
            {
                existingChest.items = newChest.items;
            }
            else
            {
                existingChestsData.Add(newChest);
            }
        }

        return existingChestsData;
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

    // Áp dụng dữ liệu save vào game, xử lý chuyển scene nếu cần
    private bool ApplySaveData(SaveData saveData)
    {
        string targetScene = saveData.currentSceneName;
        Vector3 targetPos = saveData.playerPosition;

        if (!string.IsNullOrEmpty(pendingSceneName))
        {
            targetScene = pendingSceneName;

            // Nếu có vị trí chỉ định (từ Checkpoint hoặc Cổng dịch chuyển)
            if (nextSpawnPosition != null)
            {
                targetPos = nextSpawnPosition.Value;
            }
            else
            {
                Debug.LogWarning("[SaveController] Pending scene set but no spawn position! Using saved position.");
            }

            Debug.Log($"[SaveController] Override Load with Pending Scene: {targetScene} at {targetPos}");
        }
        else
        {
            // Chỉ fallback nếu dữ liệu scene bị lỗi
            if (string.IsNullOrEmpty(targetScene)) targetScene = "1.1";
        }

        // Kiểm tra Scene tồn tại trong Build Settings
        bool sceneExists = Enumerable.Range(0, SceneManager.sceneCountInBuildSettings)
            .Select(SceneUtility.GetScenePathByBuildIndex)
            .Any(scenePath => scenePath.EndsWith($"{targetScene}.unity"));

        if (!sceneExists)
        {
            Debug.LogError($"[SaveController] Target scene '{targetScene}' not found! Fallback to '1.1'");
            targetScene = "1.1";
        }

        // --- XỬ LÝ CHUYỂN SCENE ---
        if (SceneManager.GetActiveScene().name != targetScene)
        {
            Debug.Log($"[SaveController] Switching to target scene: {targetScene}");
            SceneManager.LoadScene(targetScene);

            pendingSceneName = targetScene;
            nextSpawnPosition = targetPos;

            return true;
        }

        // --- XỬ LÝ KHI ĐÃ Ở ĐÚNG SCENE ---

        // Đặt vị trí nhân vật
        GameObject player = GameObject.FindGameObjectWithTag("PlayerController");
        if (player != null)
        {
            player.transform.position = targetPos;

            nextSpawnPosition = null;
            pendingSceneName = null;
        }

        // --- CẬP NHẬT CHECKPOINT ---
        if (!string.IsNullOrEmpty(saveData.checkpointSceneName))
        {
            currentCheckpointScene = saveData.checkpointSceneName;
            currentCheckpointPos = saveData.checkpointPosition;

            // Debug.Log($"[SaveController] Synced Checkpoint: {currentCheckpointScene}");
        }
        else
        {
            // Debug.LogWarning("[SaveController] SaveData has no checkpoint info. Keeping current static values.");
        }

        // --- LOAD CÁC DỮ LIỆU KHÁC ---

        if (inventoryController != null)
        {
            inventoryController.slotCount = saveData.backPackSlotCount;
            inventoryController.SetInventoryItems(saveData.inventorySaveData);
        }
        if (hotbarController != null) hotbarController.SetHotbarItems(saveData.hotbarSaveData);

        LoadChestStates(saveData.chestSaveData);

        if (knightEquipmentPanel != null) knightEquipmentPanel.SetEquipmentItems(saveData.knightEquipSaveData);
        if (mageEquipmentPanel != null) mageEquipmentPanel.SetEquipmentItems(saveData.mageEquipSaveData);

        if (QuestController.Instance != null)
        {
            QuestController.Instance.LoadQuestProgress(saveData.questProgressData);
            QuestController.Instance.handInQuestIDs = saveData.handInQuestIDs;
        }

        if (playerStats != null)
        {
            playerStats.level = saveData.lvl;
            playerStats.exp = saveData.exp;
            playerStats.coin = saveData.coin;
            playerStats.gem = saveData.gem;
            playerStats.STR = saveData.str;
            playerStats.DEX = saveData.dex;
            playerStats.CON = saveData.con;
            playerStats.INT = saveData.intStat;
            playerStats.potentialPoints = saveData.potentialPoints;

            playerStats.ApplyAllClassEquippedItems();
            playerStats.ApplyEquippedItems();

            // Load HP/MP/Stamina
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

        LoadStorageChestStates(saveData.allChestsData);

        collectedByScene = saveData.collectedByScene ?? new List<SceneCollected>();

        // Cập nhật Camera
        var vcam = FindFirstObjectByType<CinemachineCamera>();
        if (vcam != null && player != null)
        {
            vcam.ForceCameraPosition(player.transform.position, Quaternion.identity);
        }

        try
        {
            Unity.Cinemachine.CinemachineCore.ResetCameraState();
            if (Camera.main != null && player != null)
            {
                Vector3 camPos = Camera.main.transform.position;
                Camera.main.transform.position = new Vector3(player.transform.position.x, player.transform.position.y, camPos.z);
            }
        }
        catch { }

        return false;
    }

    // Load trạng thái rương lưu trữ
    private void LoadStorageChestStates(List<ChestStorageEntry> allChestsData)
    {
        if (storageChests == null || storageChests.Length == 0) return;

        if (allChestsData == null) allChestsData = new List<ChestStorageEntry>();

        foreach (var chest in storageChests)
        {
            var data = allChestsData.FirstOrDefault(c => c.chestID == chest.chestID);

            if (data != null && data.items != null)
            {
                List<StorageChestSaveData> loadedItems = new List<StorageChestSaveData>();
                foreach (var itemData in data.items)
                {
                    loadedItems.Add(new StorageChestSaveData
                    {
                        itemID = itemData.itemID,
                        slotIndex = itemData.slotIndex,
                        quantity = itemData.quantity,
                        rarity = itemData.rarity,
                        qualityFactor = itemData.qualityFactor
                    });
                }
                chest.chestData = loadedItems;
            }
            else
            {
                chest.chestData = new List<StorageChestSaveData>();
            }
        }
    }
    // Load trạng thái mở rương
    private void LoadChestStates(List<ChestSaveData> chestState)
    {
        foreach (Chest chest in chests)
        {
            ChestSaveData chestSaveData = chestState.FirstOrDefault(c => c.chestID == chest.ChestID);

            if (chestSaveData != null)
            {
                chest.SetOpened(chestSaveData.isOpened);
            }
        }
    }

    // Lấy trạng thái rương lưu trữ của scene hiện tại
    private List<ChestStorageEntry> GetStorageChestsState()
    {
        List<ChestStorageEntry> currentSceneChests = new List<ChestStorageEntry>();

        // Nếu map không có rương nào thì trả về list rỗng ngay
        if (storageChests == null || storageChests.Length == 0)
        {
            return currentSceneChests;
        }

        foreach (var chest in storageChests)
        {
            // Bảo vệ null khi gọi Controller
            if (StorageChestController.Instance != null)
            {
                StorageChestController.Instance.SyncDataIfOpen(chest);
            }

            List<StorageChestSaveData> savedItems = new List<StorageChestSaveData>();

            // Bảo vệ null cho chestData
            if (chest.chestData != null)
            {
                foreach (var item in chest.chestData)
                {
                    savedItems.Add(new StorageChestSaveData
                    {
                        itemID = item.itemID,
                        slotIndex = item.slotIndex,
                        quantity = item.quantity,
                        rarity = item.rarity,
                        qualityFactor = item.qualityFactor
                    });
                }
            }

            currentSceneChests.Add(new ChestStorageEntry
            {
                chestID = chest.chestID,
                items = savedItems
            });
        }

        return currentSceneChests;
    }

    [System.Serializable]
    public class SaveDataRequest
    {
        public string DataSave;
    }

    // Gửi dữ liệu lên Server
    IEnumerator SaveToServer(SaveData saveData, System.Action<bool> onComplete)
    {
        string json = JsonUtility.ToJson(new SaveDataRequest
        {
            DataSave = JsonUtility.ToJson(saveData)
        });
        string url = "https://chronicles-of-knight-and-mage.onrender.com/api/GameData/save-data";

        string token = PlayerPrefs.GetString("AuthToken", "");

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", $"Bearer {token}");

        yield return request.SendWebRequest();

        bool isSuccess = false;
        if (request.result == UnityWebRequest.Result.Success)
        {
            isSuccess = true;
        }
        else
        {
            isSuccess = false;
        }

        onComplete?.Invoke(isSuccess);
    }

    // Tải dữ liệu từ Server
    IEnumerator LoadFromServer(System.Action<SaveData> onLoaded)
    {
        string url = "https://chronicles-of-knight-and-mage.onrender.com/api/GameData/get-save";

        string token = PlayerPrefs.GetString("AuthToken", "");

        UnityWebRequest request = UnityWebRequest.Get(url);
        request.SetRequestHeader("Authorization", $"Bearer {token}");

        yield return request.SendWebRequest();

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