using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public enum SaveReason
{
    Manual, AutoSave, Checkpoint, SceneTransition, QuitGame, QuestHandIn, Death,
    SpendCoin, SpendGem, BuyItem, AddItem, RemoveItem,
}

public class SaveController : MonoBehaviour
{
    public static SaveController Instance { get; private set; }

    private static HashSet<Chest> _activeChests = new HashSet<Chest>();
    private Dictionary<string, bool> _sessionChestStates = new Dictionary<string, bool>();
    private List<ChestSaveData> _cachedChestStates = new List<ChestSaveData>();

    public static void RegisterChest(Chest chest) => _activeChests.Add(chest);
    public static void UnregisterChest(Chest chest) => _activeChests.Remove(chest);

    public static event System.Action OnDataLoaded;
    public static event System.Action<string> OnUIDReady;
    public static bool IsDataLoaded { get; private set; } = false;
    public static bool IsSaving { get; private set; } = false;

    private GameObject mainLoadingCanvasInstance;
    private GameObject miniLoadingScreenInstance;

    private SaveAdapter uiAdapter;
    public void RegisterUIAdapter(SaveAdapter adapter) => uiAdapter = adapter;

    private LocalPlayerSaveAdapter localPlayerAdapter;

    private StorageChest[] storageChests;

    public static Vector3? nextSpawnPosition = null;
    public static string pendingSceneName = null;

    public static Vector3? currentCheckpointPos;
    public static string currentCheckpointScene;

    private Coroutine autoSaveCoroutine;
    private float autoSaveDebounceTime = 3.0f;
    private bool isAutoSavePending = false;

    private SaveData tempSaveData;

    void Awake()
    {
        Instance = this;
        IsDataLoaded = false;

        _activeChests.Clear();
        _sessionChestStates.Clear();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        ShowMainLoadingScreen();
        StartCoroutine(LoadAndFinalize());
        LocalizationManager.OnLanguageChanged += UpdateUIDText;
    }

    public void RegisterLocalPlayer(LocalPlayerSaveAdapter adapter)
    {
        localPlayerAdapter = adapter;
    }

    public void UnregisterLocalPlayer()
    {
        localPlayerAdapter = null;
    }

    private void UpdateUIDText()
    {
        if (IsDataLoaded)
        {
            OnUIDReady?.Invoke(GetPlayerUID());
        }
    }

    IEnumerator LoadAndFinalize()
    {
        while (NetworkManager.Singleton == null || (!NetworkManager.Singleton.IsServer && !NetworkManager.Singleton.IsConnectedClient))
        {
            yield return null;
        }

        while (localPlayerAdapter == null || uiAdapter == null)
        {
            if (localPlayerAdapter == null)
            {
                if (NetworkManager.Singleton.LocalClient != null && NetworkManager.Singleton.LocalClient.PlayerObject != null)
                {
                    localPlayerAdapter = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<LocalPlayerSaveAdapter>();
                }
            }
            yield return null;
        }

        bool sceneLoadTriggered = false;
        yield return StartCoroutine(LoadRoutine((wasTriggered) =>
        {
            sceneLoadTriggered = wasTriggered;
        }));

        if (sceneLoadTriggered)
        {
            HideMainLoadingScreen();
            yield break;
        }

        storageChests = FindObjectsByType<StorageChest>(FindObjectsSortMode.None);
        pendingSceneName = null;
        nextSpawnPosition = null;

        if (EconomyService.Instance != null)
            EconomyService.Instance.RefreshBalance();

        bool profileLoaded = false;
        if (PlayerStatsService.Instance != null)
        {
            PlayerStatsService.Instance.SyncProfile((success) => { profileLoaded = true; });
        }
        else profileLoaded = true;

        while (!profileLoaded) yield return null;

        if (FarmController.Instance != null)
            FarmController.Instance.FetchFarmDataFromServer();

        bool inventoryLoaded = false;
        if (InventoryService.Instance != null)
        {
            InventoryService.Instance.SyncInventory((serverItems) =>
            {
                if (serverItems != null)
                {
                    List<InventorySaveData> inventoryItems = new List<InventorySaveData>();
                    List<InventorySaveData> hotbarItems = new List<InventorySaveData>();

                    List<EquippedSaveData> knightEquips = new List<EquippedSaveData>();
                    List<EquippedSaveData> mageEquips = new List<EquippedSaveData>();
                    List<EquippedSaveData> sharedEquips = new List<EquippedSaveData>();

                    ItemDictionary itemDict = ItemDictionary.Instance;

                    foreach (var svItem in serverItems)
                    {
                        var itemData = new InventorySaveData
                        {
                            dbID = svItem.id,
                            itemID = svItem.itemId,
                            quantity = svItem.quantity,
                            slotIndex = svItem.slotIndex,
                            isEquipped = svItem.isEquipped,
                            rarity = (ItemRarity)svItem.rarity,
                            qualityFactor = svItem.qualityFactor
                        };

                        if (svItem.slotIndex >= 1000)
                        {
                            itemData.slotIndex -= 1000;
                            hotbarItems.Add(itemData);
                        }
                        else
                        {
                            inventoryItems.Add(itemData);
                        }

                        if (svItem.isEquipped && itemDict != null)
                        {
                            GameObject prefab = itemDict.GetItemPrefab(svItem.itemId);
                            if (prefab != null)
                            {
                                if (prefab.GetComponent<Item>() is EquipmentItem equipComp)
                                {
                                    EquippedSaveData equipData = new EquippedSaveData
                                    {
                                        itemID = svItem.itemId,
                                        quantity = svItem.quantity,
                                        isEquipped = true,
                                        rarity = (ItemRarity)svItem.rarity,
                                        qualityFactor = svItem.qualityFactor,
                                        sourceItemID = svItem.itemId
                                    };

                                    if (equipComp.classRestriction == ClassRestriction.Knight)
                                    {
                                        switch (equipComp.equipSlot)
                                        {
                                            case EquipSlot.Swords: equipData.slotIndex = 0; break;
                                            case EquipSlot.Shield: equipData.slotIndex = 1; break;
                                            case EquipSlot.Helmet: equipData.slotIndex = 2; break;
                                            case EquipSlot.Armor: equipData.slotIndex = 3; break;
                                            default: equipData.slotIndex = -1; break;
                                        }
                                        if (equipData.slotIndex != -1) knightEquips.Add(equipData);
                                    }
                                    else if (equipComp.classRestriction == ClassRestriction.Mage)
                                    {
                                        switch (equipComp.equipSlot)
                                        {
                                            case EquipSlot.Scepter: equipData.slotIndex = 0; break;
                                            case EquipSlot.Amulet: equipData.slotIndex = 1; break;
                                            case EquipSlot.Hat: equipData.slotIndex = 2; break;
                                            case EquipSlot.Robe: equipData.slotIndex = 3; break;
                                            default: equipData.slotIndex = -1; break;
                                        }
                                        if (equipData.slotIndex != -1) mageEquips.Add(equipData);
                                    }
                                    else
                                    {
                                        switch (equipComp.equipSlot)
                                        {
                                            case EquipSlot.Legs: equipData.slotIndex = 0; break;
                                            case EquipSlot.Boots: equipData.slotIndex = 1; break;
                                            case EquipSlot.Gloves: equipData.slotIndex = 2; break;
                                            case EquipSlot.Belt: equipData.slotIndex = 3; break;
                                            case EquipSlot.Ring: equipData.slotIndex = 4; break;
                                            case EquipSlot.Necklace: equipData.slotIndex = 5; break;
                                            default: equipData.slotIndex = -1; break;
                                        }
                                        if (equipData.slotIndex != -1) sharedEquips.Add(equipData);
                                    }
                                }
                            }
                        }
                    }

                    if (uiAdapter.inventoryController != null)
                        uiAdapter.inventoryController.SetInventoryItems(inventoryItems);

                    if (uiAdapter.hotbarController != null)
                        uiAdapter.hotbarController.SetHotbarItems(hotbarItems);

                    if (uiAdapter.knightEquipmentPanel != null) uiAdapter.knightEquipmentPanel.SetEquipmentItems(knightEquips);
                    if (uiAdapter.mageEquipmentPanel != null) uiAdapter.mageEquipmentPanel.SetEquipmentItems(mageEquips);
                    if (uiAdapter.sharedEquipmentPanel != null) uiAdapter.sharedEquipmentPanel.SetEquipmentItems(sharedEquips);

                    if (localPlayerAdapter != null && localPlayerAdapter.playerStats != null)
                    {
                        localPlayerAdapter.playerStats.ApplyEquippedItems();
                    }
                }
                inventoryLoaded = true;
            });
        }
        else inventoryLoaded = true;

        while (!inventoryLoaded) yield return null;

        if (localPlayerAdapter != null && tempSaveData != null)
        {
            localPlayerAdapter.playerStats.knightHealth = tempSaveData.currentKnightHP;
            localPlayerAdapter.playerStats.mageHealth = tempSaveData.currentmageHP;
            localPlayerAdapter.playerStats.knightMP = tempSaveData.currentKnightMP;
            localPlayerAdapter.playerStats.mageMP = tempSaveData.currentMageMP;
            localPlayerAdapter.playerStats.currentStamina = tempSaveData.currentStamina;

            tempSaveData = null;
        }

        IsDataLoaded = true;
        UpdateUIDText();

        yield return new WaitForSecondsRealtime(0.5f);
        HideMainLoadingScreen();
        OnDataLoaded?.Invoke();

        if (DeathService.IsRespawningFlag)
        {
            DeathService.IsRespawningFlag = false;
            if (localPlayerAdapter != null)
            {
                localPlayerAdapter.playerStats.SetDeathStateServerRpc(false);
                var pMovement = localPlayerAdapter.playerStats.GetComponentInChildren<PlayerMovement>();
                if (pMovement != null) pMovement.ResetDeathState();

                StartCoroutine(localPlayerAdapter.playerStats.FinalizeRespawnProtection(0.5f));
            }
        }
    }

    public void TriggerAutoSave()
    {
        if (IsSaving && !isAutoSavePending) return;

        if (autoSaveCoroutine != null) StopCoroutine(autoSaveCoroutine);

        isAutoSavePending = true;
        autoSaveCoroutine = StartCoroutine(DebounceAutoSave());
    }

    IEnumerator DebounceAutoSave()
    {
        yield return new WaitForSeconds(autoSaveDebounceTime);

        if (isAutoSavePending)
        {
            StartCoroutine(SaveRoutine(SaveReason.AutoSave, null, true));
            isAutoSavePending = false;
        }
    }

    public void SaveGame(SaveReason reason = SaveReason.Manual, System.Action<bool> onSaveFinished = null, bool isSilent = false)
    {
        if (IsSaving) return;

        if (autoSaveCoroutine != null) StopCoroutine(autoSaveCoroutine);
        isAutoSavePending = false;

        StartCoroutine(SaveRoutine(reason, onSaveFinished, isSilent));
    }

    public IEnumerator SaveRoutine(SaveReason reason, System.Action<bool> onSaveFinished = null, bool isSilent = false)
    {
        IsSaving = true;
        if (!isSilent) ShowMiniLoadingScreen();

        if (uiAdapter == null || uiAdapter.inventoryController == null || localPlayerAdapter == null)
        {
            if (!isSilent) HideMiniLoadingScreen();
            IsSaving = false;
            Debug.LogError("SaveController: Thiếu component quan trọng (Adapter/Inventory), hủy lưu.");
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

        Vector3 savePos = localPlayerAdapter.GetPosition();
        string saveScene = SceneManager.GetActiveScene().name;
        if (reason == SaveReason.SceneTransition && nextSpawnPosition != null) savePos = nextSpawnPosition.Value;
        if (reason == SaveReason.SceneTransition && !string.IsNullOrEmpty(pendingSceneName)) saveScene = pendingSceneName;

        SaveData saveData = new SaveData
        {
            playerPosition = nextSpawnPosition ?? localPlayerAdapter.GetPosition(),
            currentSceneName = pendingSceneName ?? SceneManager.GetActiveScene().name,
            checkpointPosition = currentCheckpointPos ?? localPlayerAdapter.GetPosition(),
            checkpointSceneName = currentCheckpointScene ?? SceneManager.GetActiveScene().name,

            mapBoundary = FindFirstObjectByType<CinemachineConfiner2D>()?.BoundingShape2D?.gameObject.name ?? "",
            backPackSlotCount = uiAdapter.inventoryController.slotCount,

            chestSaveData = MergeChestsState(existingChestStates),
            questProgressData = QuestController.Instance.activeQuests,
            handInQuestIDs = QuestController.Instance.handInQuestIDs,

            currentKnightHP = (reason == SaveReason.Death) ? localPlayerAdapter.playerStats.finalKnightMaxHP : localPlayerAdapter.playerStats.knightHealth,
            currentmageHP = (reason == SaveReason.Death) ? localPlayerAdapter.playerStats.finalMageMaxHP : localPlayerAdapter.playerStats.mageHealth,
            currentKnightMP = (reason == SaveReason.Death) ? localPlayerAdapter.playerStats.finalKnightMaxMP : localPlayerAdapter.playerStats.knightMP,
            currentMageMP = (reason == SaveReason.Death) ? localPlayerAdapter.playerStats.finalMageMaxMP : localPlayerAdapter.playerStats.mageMP,
            currentStamina = (reason == SaveReason.Death) ? localPlayerAdapter.playerStats.finalStamina : localPlayerAdapter.playerStats.currentStamina,

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

    public void SetCheckpoint(string scene, Vector3 pos)
    {
        currentCheckpointScene = scene;
        currentCheckpointPos = pos;

        if (IsSaving && !isAutoSavePending) return;
        if (autoSaveCoroutine != null) StopCoroutine(autoSaveCoroutine);

        StartCoroutine(SaveRoutine(SaveReason.Checkpoint, null, true));
    }

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

        PauseController.SetPause(true);
    }

    private void HideMainLoadingScreen()
    {
        if (mainLoadingCanvasInstance != null)
        {
            Destroy(mainLoadingCanvasInstance);
            mainLoadingCanvasInstance = null;
        }

        PauseController.SetPause(false);
    }

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
            PauseController.SetPause(true);
        }
    }

    private void HideMiniLoadingScreen()
    {
        if (miniLoadingScreenInstance != null)
        {
            miniLoadingScreenInstance.SetActive(false);
            if (GameStateManager.IsMenuOpen == true) return;
            PauseController.SetPause(false);
        }
    }

    public void MarkChestAsOpened(string chestID)
    {
        if (!string.IsNullOrEmpty(chestID))
        {
            _sessionChestStates[chestID] = true;
        }
    }

    private List<ChestSaveData> GetChestsState()
    {
        foreach (Chest chest in _activeChests)
        {
            if (string.IsNullOrEmpty(chest.UniqueID)) continue;
            _sessionChestStates[chest.UniqueID] = chest.IsOpened;
        }

        List<ChestSaveData> chestStates = new List<ChestSaveData>();
        foreach (var kvp in _sessionChestStates)
        {
            chestStates.Add(new ChestSaveData
            {
                chestID = kvp.Key,
                isOpened = kvp.Value,
            });
        }
        return chestStates;
    }

    private List<ChestSaveData> MergeChestsState(List<ChestSaveData> existingChestStates)
    {
        List<ChestSaveData> currentChests = GetChestsState();

        foreach (var chest in currentChests)
        {
            var existing = existingChestStates.FirstOrDefault(c => c.chestID == chest.chestID);
            if (existing != null) existing.isOpened = chest.isOpened;
            else existingChestStates.Add(chest);
        }

        return existingChestStates;
    }

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
        tempSaveData = saveData;

        string targetScene = saveData?.currentSceneName ?? "";
        Vector3 targetPos = saveData?.playerPosition ?? Vector3.zero;

        if (!string.IsNullOrEmpty(pendingSceneName))
        {
            targetScene = pendingSceneName;
            if (nextSpawnPosition != null) targetPos = nextSpawnPosition.Value;
        }
        else if (string.IsNullOrEmpty(targetScene))
        {
            targetScene = "MAP_CH1_01";
        }

        bool sceneExists = Enumerable.Range(0, SceneManager.sceneCountInBuildSettings)
            .Select(SceneUtility.GetScenePathByBuildIndex)
            .Any(scenePath => scenePath.EndsWith($"{targetScene}.unity"));

        if (!sceneExists) targetScene = "MAP_CH1_01";

        nextSpawnPosition = targetPos;

        if (SceneManager.GetActiveScene().name != targetScene)
        {
            pendingSceneName = targetScene;

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                if (NetworkManager.Singleton.IsServer)
                {
                    NetworkManager.Singleton.SceneManager.LoadScene(targetScene, LoadSceneMode.Single);
                }
            }
            else
            {
                SceneManager.LoadScene(targetScene);
            }

            return true;
        }

        if (localPlayerAdapter != null)
        {
            localPlayerAdapter.SetPosition(targetPos);
            nextSpawnPosition = null;
            pendingSceneName = null;
        }

        var boundaryObj = GameObject.Find(saveData?.mapBoundary);
        var boundary = boundaryObj != null ? boundaryObj.GetComponent<BoxCollider2D>() : null;

        if (boundary != null)
        {
            var confiner = FindFirstObjectByType<CinemachineConfiner2D>();
            if (confiner != null)
            {
                confiner.BoundingShape2D = boundary;
                confiner.InvalidateBoundingShapeCache();
            }
        }

        if (!string.IsNullOrEmpty(saveData?.checkpointSceneName))
        {
            currentCheckpointScene = saveData.checkpointSceneName;
            currentCheckpointPos = saveData.checkpointPosition;
        }

        if (uiAdapter != null && uiAdapter.inventoryController != null && saveData != null)
        {
            uiAdapter.inventoryController.slotCount = saveData.backPackSlotCount;
            uiAdapter.inventoryController.ReBuildItemCounts();
        }

        _cachedChestStates = saveData?.chestSaveData ?? new List<ChestSaveData>();
        LoadChestStates(_cachedChestStates);

        if (QuestController.Instance != null && saveData != null)
        {
            QuestController.Instance.LoadQuestProgress(saveData.questProgressData);
            QuestController.Instance.handInQuestIDs = saveData.handInQuestIDs;
        }

        collectedByScene = saveData?.collectedByScene ?? new List<SceneCollected>();

        var vcam = FindFirstObjectByType<CinemachineCamera>();
        if (vcam != null && localPlayerAdapter != null)
        {
            vcam.ForceCameraPosition(localPlayerAdapter.GetPosition(), Quaternion.identity);
        }
        try { Unity.Cinemachine.CinemachineCore.ResetCameraState(); }
        catch { }

        return false;
    }

    public void LoadChestStates(List<ChestSaveData> chestState)
    {
        _sessionChestStates.Clear();
        if (chestState != null)
        {
            foreach (var state in chestState)
            {
                _sessionChestStates[state.chestID] = state.isOpened;
            }
        }

        foreach (Chest chest in _activeChests.ToList())
        {
            var state = chestState?.FirstOrDefault(c => c.chestID == chest.UniqueID);
            if (state != null)
            {
                chest.SetOpened(state.isOpened);
            }
        }
    }

    public bool IsChestOpened(string chestID)
    {
        var state = _cachedChestStates.FirstOrDefault(c => c.chestID == chestID);
        return state != null && state.isOpened;
    }

    [System.Serializable]
    public class SaveDataRequest
    {
        public string dataSave;
        public string reason;
    }

    IEnumerator SaveToServer(SaveData saveData, SaveReason reason, System.Action<bool> onComplete)
    {
        string json = JsonUtility.ToJson(new SaveDataRequest { dataSave = JsonUtility.ToJson(saveData), reason = reason.ToString() });

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

        if (request.result == UnityWebRequest.Result.Success) isSuccess = true;
        else
        {
            isSuccess = false;
            Debug.LogError($"[Client] Save Failed! Code: {request.responseCode} | Error: {request.error}");
            Debug.LogError($"[Client] Server Reason: {request.downloadHandler.text}");
        }

        onComplete?.Invoke(isSuccess);
    }

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
            Debug.LogError($"[SaveController] Lỗi tải dữ liệu từ Server: {request.error}");
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            SceneManager.LoadScene("MainMenu");
        }
    }

    public string GetPlayerUID() => PlayerPrefs.GetString("AccountId", "");

    private string GetText(string key)
    {
        if (LocalizationManager.Instance != null) return LocalizationManager.Instance.GetText(key);
        return key;
    }

    public List<SceneCollected> collectedByScene = new List<SceneCollected>();

    public bool IsCollected(string sceneName, string id)
    {
        if (collectedByScene == null) collectedByScene = new List<SceneCollected>();
        var s = collectedByScene.Find(x => x.sceneName == sceneName);
        return s != null && s.collectedIDs.Contains(id);
    }

    public void MarkCollected(string sceneName, string id)
    {
        var s = collectedByScene.Find(x => x.sceneName == sceneName);
        if (s == null)
        {
            s = new SceneCollected { sceneName = sceneName };
            collectedByScene.Add(s);
        }
        if (!s.collectedIDs.Contains(id)) s.collectedIDs.Add(id);
    }

    [System.Serializable]
    public class SceneCollected
    {
        public string sceneName;
        public List<string> collectedIDs = new List<string>();
    }
}