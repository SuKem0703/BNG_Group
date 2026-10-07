using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum SaveReason
{
    Manual, AutoSave, Checkpoint, SceneTransition, QuitGame, QuestHandIn, Death,
    SpendCoin, SpendGem, BuyItem, AddItem, RemoveItem,
}

public class SaveController : MonoBehaviour
{
    public static SaveController Instance { get; private set; }

    public static uint MasterSeed { get; private set; } = 12345;

    private static HashSet<Chest> _activeChests = new HashSet<Chest>();
    private Dictionary<string, bool> _sessionChestStates = new Dictionary<string, bool>();
    private Dictionary<string, BestiaryEntry> _bestiaryCache = new Dictionary<string, BestiaryEntry>();
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

    private PlayerCore playerCore;
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

        PlayerCore.OnPlayerSpawned += HandlePlayerSpawned;
    }

    private void OnDestroy()
    {
        PlayerCore.OnPlayerSpawned -= HandlePlayerSpawned;
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        ShowMainLoadingScreen();
        StartCoroutine(LoadAndFinalize());
        LocalizationManager.OnLanguageChanged += UpdateUIDText;
    }

    private void HandlePlayerSpawned(PlayerCore core)
    {
        RegisterLocalPlayer(core);
    }

    public void RegisterLocalPlayer(PlayerCore adapter)
    {
        playerCore = adapter;
    }

    public void UnregisterLocalPlayer()
    {
        playerCore = null;
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

        while (playerCore == null || uiAdapter == null)
        {
            if (playerCore == null)
            {
                if (NetworkManager.Singleton.LocalClient != null && NetworkManager.Singleton.LocalClient.PlayerObject != null)
                {
                    playerCore = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerCore>();
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

        if (EconomyService.Instance != null) EconomyService.Instance.RefreshBalance();
        if (PlayerStatsService.Instance != null) PlayerStatsService.Instance.SyncProfile();
        if (FarmController.Instance != null) FarmController.Instance.FetchFarmDataFromServer();
        if (InventoryController.Instance != null)
        {
            InventoryController.Instance.RefreshInventory();

            var currentItems = InventoryController.Instance.GetInventoryItemsData();
            FindFirstObjectByType<KnightEquipmentPanel>(FindObjectsInactive.Include)?.SyncFromInventory(currentItems);
            FindFirstObjectByType<MageEquipmentPanel>(FindObjectsInactive.Include)?.SyncFromInventory(currentItems);
            FindFirstObjectByType<SharedEquipmentPanel>(FindObjectsInactive.Include)?.SyncFromInventory(currentItems);
        }

        if (playerCore != null && tempSaveData != null)
        {
            playerCore.playerVitals.netKnightHealth.Value = tempSaveData.currentKnightHP;
            playerCore.playerVitals.netMageHealth.Value = tempSaveData.currentmageHP;
            playerCore.playerVitals.knightMP = tempSaveData.currentKnightMP;
            playerCore.playerVitals.mageMP = tempSaveData.currentMageMP;
            playerCore.playerVitals.currentStamina = tempSaveData.currentStamina;

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
            if (playerCore != null)
            {
                playerCore.playerVitals.SetDeathStateServerRpc(false);
                var pMovement = playerCore.playerStats.GetComponentInChildren<PlayerMovement>();
                if (pMovement != null) pMovement.ResetDeathState();

                StartCoroutine(playerCore.playerVitals.FinalizeRespawnProtection(0.5f));
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
        if (!IsDataLoaded)
        {
            onSaveFinished?.Invoke(false);
            yield break;
        }

        IsSaving = true;

        if (FarmService.Instance != null) FarmService.Instance.ForceSendPendingHarvests();
        if (InventoryService.Instance != null) InventoryService.Instance.ForceSyncPendingQuantities();
        if (InventoryService.Instance != null) InventoryService.Instance.ForceSyncPendingMoves();
        if (playerCore != null && playerCore.playerStats != null) playerCore.playerStats.ForceSyncExpImmediate();

        if (!isSilent) ShowMiniLoadingScreen();

        if (uiAdapter == null || uiAdapter.inventoryController == null || playerCore == null)
        {
            if (!isSilent) HideMiniLoadingScreen();
            IsSaving = false;
            onSaveFinished?.Invoke(false);
            yield break;
        }

        string currentProfileId = PlayerPrefs.GetString("CurrentProfileId", "");
        ProfileRepository repo = new ProfileRepository();
        ProfileEntity profile = repo.GetProfile(currentProfileId);

        if (profile == null)
        {
            Debug.LogError("[SaveController] Lỗi: Không tìm thấy nhân vật trong database offline!");
            if (!isSilent) HideMiniLoadingScreen();
            IsSaving = false;
            onSaveFinished?.Invoke(false);
            yield break;
        }

        Vector3 savePos = playerCore.playerStats.transform.position;
        string saveScene = SceneManager.GetActiveScene().name;
        if (reason == SaveReason.SceneTransition && nextSpawnPosition != null) savePos = nextSpawnPosition.Value;
        if (reason == SaveReason.SceneTransition && !string.IsNullOrEmpty(pendingSceneName)) saveScene = pendingSceneName;

        string currentBoundary = FindFirstObjectByType<CinemachineConfiner2D>()?.BoundingShape2D?.gameObject.name ?? "";
        if (reason == SaveReason.SceneTransition || !string.IsNullOrEmpty(pendingSceneName)) currentBoundary = "";

        List<ChestSaveData> existingChestStates = JsonHelper.FromJson<ChestSaveData>(profile.ChestSaveDataJson);
        existingChestStates = MergeChestsState(existingChestStates);

        profile.CurrentSceneName = saveScene;
        profile.PlayerPosX = savePos.x;
        profile.PlayerPosY = savePos.y;
        profile.PlayerPosZ = savePos.z;

        profile.CheckpointSceneName = currentCheckpointScene ?? saveScene;
        profile.CheckpointPosX = currentCheckpointPos?.x ?? savePos.x;
        profile.CheckpointPosY = currentCheckpointPos?.y ?? savePos.y;
        profile.CheckpointPosZ = currentCheckpointPos?.z ?? savePos.z;

        profile.BackPackSlotCount = uiAdapter.inventoryController.slotCount;

        profile.CurrentKnightHP = (reason == SaveReason.Death) ? playerCore.playerStats.finalKnightMaxHP : playerCore.playerVitals.netKnightHealth.Value;
        profile.CurrentMageHP = (reason == SaveReason.Death) ? playerCore.playerStats.finalMageMaxHP : playerCore.playerVitals.netMageHealth.Value;
        profile.CurrentKnightMP = (reason == SaveReason.Death) ? playerCore.playerStats.finalKnightMaxMP : playerCore.playerVitals.knightMP;
        profile.CurrentMageMP = (reason == SaveReason.Death) ? playerCore.playerStats.finalMageMaxMP : playerCore.playerVitals.mageMP;
        profile.CurrentStamina = (reason == SaveReason.Death) ? playerCore.playerStats.finalStamina : playerCore.playerVitals.currentStamina;

        profile.ChestSaveDataJson = JsonHelper.ToJson(existingChestStates);

        if (QuestController.Instance != null)
        {
            profile.QuestProgressDataJson = JsonHelper.ToJson(QuestController.Instance.activeQuests);
            profile.HandInQuestIDsJson = JsonHelper.ToJson(QuestController.Instance.handInQuestIDs);
        }

        profile.CollectedBySceneJson = JsonHelper.ToJson(collectedByScene);

        repo.UpdateProfile(profile);
        bool saveSuccess = true;

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

        yield return null;

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
            chestStates.Add(new ChestSaveData { chestID = kvp.Key, isOpened = kvp.Value });
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
        string currentProfileId = PlayerPrefs.GetString("CurrentProfileId", "");

        ProfileRepository repo = new ProfileRepository();
        ProfileEntity offlineProfile = repo.GetProfile(currentProfileId);

        if (offlineProfile != null)
        {
            SaveData localData = new SaveData
            {
                currentSceneName = offlineProfile.CurrentSceneName,
                playerPosition = new Vector3(offlineProfile.PlayerPosX, offlineProfile.PlayerPosY, offlineProfile.PlayerPosZ),

                checkpointSceneName = offlineProfile.CheckpointSceneName,
                checkpointPosition = new Vector3(offlineProfile.CheckpointPosX, offlineProfile.CheckpointPosY, offlineProfile.CheckpointPosZ),

                backPackSlotCount = offlineProfile.BackPackSlotCount,

                currentKnightHP = offlineProfile.CurrentKnightHP,
                currentmageHP = offlineProfile.CurrentMageHP,
                currentKnightMP = offlineProfile.CurrentKnightMP,
                currentMageMP = offlineProfile.CurrentMageMP,
                currentStamina = offlineProfile.CurrentStamina,

                chestSaveData = JsonHelper.FromJson<ChestSaveData>(offlineProfile.ChestSaveDataJson),
                questProgressData = JsonHelper.FromJson<QuestProgress>(offlineProfile.QuestProgressDataJson),
                handInQuestIDs = JsonHelper.FromJson<string>(offlineProfile.HandInQuestIDsJson),
                collectedByScene = JsonHelper.FromJson<SaveController.SceneCollected>(offlineProfile.CollectedBySceneJson)
            };

            sceneLoadWasTriggered = ApplySaveData(localData);
        }
        else
        {
            Debug.LogError("[SaveController] Không tìm thấy dữ liệu nhân vật offline trong Database!");
        }

        onComplete(sceneLoadWasTriggered);
        yield return null;
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
            targetScene = "01_Home_Indoor";
        }

        bool sceneExists = Enumerable.Range(0, SceneManager.sceneCountInBuildSettings)
            .Select(SceneUtility.GetScenePathByBuildIndex)
            .Any(scenePath => scenePath.EndsWith($"{targetScene}.unity"));

        if (!sceneExists) targetScene = "01_Home_Indoor";

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

        if (playerCore != null)
        {
            playerCore.playerStats.transform.position = targetPos;
            nextSpawnPosition = null;
            pendingSceneName = null;
        }

        BoxCollider2D boundary = MapBoundary.GetBoundary(saveData?.mapBoundary);

        if (CameraController.Instance != null)
        {
            if (boundary != null) CameraController.Instance.UpdateMapBounds(boundary);
            else CameraController.Instance.AutoFindAndSetBoundary();
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

        if (TimeManager.Instance != null && saveData != null)
        {
            TimeManager.Instance.currentDay = saveData.currentDay > 0 ? saveData.currentDay : 1;
            TimeManager.Instance.currentTimeOfDay = saveData.currentDay > 0 ? saveData.currentTimeOfDay : 6f;
        }

        _cachedChestStates = saveData?.chestSaveData ?? new List<ChestSaveData>();
        LoadChestStates(_cachedChestStates);

        if (QuestController.Instance != null && saveData != null)
        {
            QuestController.Instance.LoadQuestProgress(saveData.questProgressData);
            QuestController.Instance.handInQuestIDs = saveData.handInQuestIDs;
        }

        collectedByScene = saveData?.collectedByScene ?? new List<SceneCollected>();

        _bestiaryCache.Clear();
        if (saveData != null && saveData.bestiaryData != null)
        {
            foreach (var entry in saveData.bestiaryData)
            {
                _bestiaryCache[entry.enemyID] = entry;
            }
        }

        if (SkillTreeService.Instance != null && saveData != null)
        {
            SkillTreeService.Instance.LoadSkillSaveData(saveData.skillTreeData);
        }

        var vcam = FindFirstObjectByType<CinemachineCamera>();
        if (vcam != null && playerCore != null)
        {
            vcam.ForceCameraPosition(playerCore.playerStats.transform.position, Quaternion.identity);
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
            if (state != null) chest.SetOpened(state.isOpened);
        }
    }

    public bool IsChestOpened(string chestID)
    {
        var state = _cachedChestStates.FirstOrDefault(c => c.chestID == chestID);
        return state != null && state.isOpened;
    }

    public Dictionary<string, BestiaryEntry> GetBestiaryCache() => _bestiaryCache;

    public void RecordEnemyDefeat(string enemyID)
    {
        if (!_bestiaryCache.ContainsKey(enemyID))
        {
            _bestiaryCache[enemyID] = new BestiaryEntry { enemyID = enemyID, status = 2, killCount = 1 };
        }
        else
        {
            _bestiaryCache[enemyID].status = 2;
            _bestiaryCache[enemyID].killCount++;
        }
        TriggerAutoSave();
    }

    public void UnlockBestiary(string enemyID, int newStatus)
    {
        if (!_bestiaryCache.ContainsKey(enemyID))
        {
            _bestiaryCache[enemyID] = new BestiaryEntry { enemyID = enemyID, status = newStatus, killCount = 0 };
            TriggerAutoSave();
        }
        else if (_bestiaryCache[enemyID].status < newStatus)
        {
            _bestiaryCache[enemyID].status = newStatus;
            TriggerAutoSave();
        }
    }

    public string GetPlayerUID() => PlayerPrefs.GetString("AccountId", "OfflineUser");

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

public static class JsonHelper
{
    public static List<T> FromJson<T>(string json)
    {
        if (string.IsNullOrEmpty(json) || json == "[]")
            return new List<T>();

        if (json.TrimStart().StartsWith("{"))
        {
            Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(json);
            return wrapper.Items ?? new List<T>();
        }

        string newJson = "{\"Items\":" + json + "}";
        Wrapper<T> wrapper2 = JsonUtility.FromJson<Wrapper<T>>(newJson);
        return wrapper2.Items ?? new List<T>();
    }

    public static string ToJson<T>(List<T> list)
    {
        Wrapper<T> wrapper = new Wrapper<T> { Items = list };
        return JsonUtility.ToJson(wrapper);
    }

    [System.Serializable]
    private class Wrapper<T>
    {
        public List<T> Items;
    }
}