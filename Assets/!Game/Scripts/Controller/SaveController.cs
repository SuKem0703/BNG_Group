using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SaveController : MonoBehaviour
{
    public static SaveController Instance { get; private set; }

    public static event System.Action OnDataLoaded;
    public static bool IsDataLoaded { get; private set; } = false;

    [SerializeField] private GameObject loadingCanvas;
    [SerializeField] private TextMeshProUGUI uidText;

    private string saveLocation;
    private InventoryController inventoryController;
    private HotbarController hotbarController;
    private Chest[] chests;
    private PlayerStats playerStats;
    private KnightEquipmentPanel knightEquipmentPanel;
    private MageEquipmentPanel mageEquipmentPanel;
    private SharedEquipmentPanel sharedEquipmentPanel;

    public static Vector3? nextSpawnPosition = null;
    public static string pendingSceneName = null;
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        IsDataLoaded = false;

        loadingCanvas = GameObject.Find("LoadingCanvas");
        uidText = GameObject.Find("UIDText").GetComponent<TextMeshProUGUI>();
    }
    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
    void Start()
    {
        PauseController.SetPause(true);
        if (loadingCanvas != null)
            loadingCanvas.SetActive(true);

        StartLoadProcess();
    }

    private void StartLoadProcess()
    {
        StartCoroutine(LoadAndFinalize());
    }

    IEnumerator LoadAndFinalize()
    {
        yield return InitializeComponents();

        bool sceneLoadTriggered = false;
        yield return StartCoroutine(LoadRoutine((wasTriggered) => {
            sceneLoadTriggered = wasTriggered;
        }));

        if (sceneLoadTriggered)
        {
            yield break;
        }

        IsDataLoaded = true;

        if (uidText != null)
            uidText.text = "UID: " + GetPlayerUID();
        else
            Debug.LogWarning("UIDText bị null, không thể set text.");

        if (loadingCanvas != null)
        {
            loadingCanvas.SetActive(false);
        }
        else
            Debug.LogWarning("LoadingCanvas bị null, không thể tắt.");

        PauseController.SetPause(false);
        OnDataLoaded?.Invoke();
    }
    IEnumerator InitializeComponents()
    {
        saveLocation = Path.Combine(Application.persistentDataPath, "saveData.json");

        inventoryController = FindFirstObjectByType<InventoryController>(FindObjectsInactive.Include);

        hotbarController = FindFirstObjectByType<HotbarController>();

        chests = FindObjectsByType<Chest>(FindObjectsSortMode.None);

        playerStats = GameObject.FindGameObjectWithTag("PlayerController").GetComponent<PlayerStats>();

        knightEquipmentPanel = FindFirstObjectByType<KnightEquipmentPanel>(FindObjectsInactive.Include);
        mageEquipmentPanel = FindFirstObjectByType<MageEquipmentPanel>(FindObjectsInactive.Include);
        sharedEquipmentPanel = FindFirstObjectByType<SharedEquipmentPanel>(FindObjectsInactive.Include);

        yield break;
    }
    public IEnumerator SaveRoutine()
    {
        if (inventoryController == null || hotbarController == null || knightEquipmentPanel == null || mageEquipmentPanel == null || sharedEquipmentPanel == null || playerStats == null)
        {
            Debug.LogError("SaveRoutine: Một trong các manager (Inventory, Hotbar, Equipment, PlayerStats) bị null. Hủy lưu.");
            yield break;
        }

        // Always prefer server copy for existing chest states to avoid overwriting other scenes' data
        List<ChestSaveData> existingChestStates = new List<ChestSaveData>();

        // Fetch current save from server (non-fatal). If server returns data, use its chestSaveData. Otherwise keep empty list.
        SaveData serverSave = null;
        yield return LoadFromServer((sd) => { serverSave = sd; });
        if (serverSave != null && serverSave.chestSaveData != null)
        {
            existingChestStates = serverSave.chestSaveData;
        }
        else
        {
            existingChestStates = new List<ChestSaveData>();
        }

        SaveData saveData = new SaveData
        {
            playerPosition = SaveController.nextSpawnPosition ?? GameObject.FindGameObjectWithTag("PlayerController").transform.position,
            currentSceneName = pendingSceneName ?? SceneManager.GetActiveScene().name,
            backPackSlotCount = inventoryController.slotCount,
            inventorySaveData = inventoryController.GetInventoryItems(),
            hotbarSaveData = hotbarController.GetHotbarItems(),
            chestSaveData = MergeChestsState(existingChestStates: existingChestStates),
            questProgressData = QuestController.Instance.activeQuests,
            handInQuestIDs = QuestController.Instance.handInQuestIDs,

            // Lưu vị trí Item đã được trang bị
            knightEquipSaveData = knightEquipmentPanel.GetEquipmentItems(),
            mageEquipSaveData = mageEquipmentPanel.GetEquipmentItems(),
            shareEquipSaveData = sharedEquipmentPanel.GetEquipmentItems(),

            // Lưu trạng thái người chơi
            lvl = playerStats.level,
            exp = playerStats.exp,
            currentKnightHP = playerStats.knightHealth,
            currentmageHP = playerStats.mageHealth,

            currentKnightMP = playerStats.knightMP,
            currentMageMP = playerStats.mageMP,

            currentStamina = playerStats.currentStamina,
            coin = playerStats.coin,
            gem = playerStats.gem,

            // Lưu các chỉ số và tiềm năng
            str = playerStats.STR,
            dex = playerStats.DEX,
            con = playerStats.CON,
            intStat = playerStats.INT,
            potentialPoints = playerStats.potentialPoints,

            // include collected items by scene so collectibles persist across visits
            collectedByScene = collectedByScene
        };

        yield return SaveToServer(saveData);
        pendingSceneName = null;
        //Debug.Log("Game đã được lưu thành công tại: " + saveLocation);
    }

    public void SaveGame()
    {
        StartCoroutine(SaveRoutine());
    }

    private List<ChestSaveData> GetChestsState()
    {
        List<ChestSaveData> chestStates = new List<ChestSaveData>();
        string scene = SceneManager.GetActiveScene().name;
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
    private List<ChestSaveData> MergeChestsState(List<ChestSaveData> existingChestStates)
    {
        // existingChestStates comes from server and may include chests from many scenes
        List<ChestSaveData> currentChests = GetChestsState();

        foreach (var chest in currentChests)
        {
            // Match by both chestID and sceneName to avoid cross-scene collisions
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
        string targetScene = saveData.currentSceneName;

        // Nếu null/rỗng -> về "1.1"
        if (string.IsNullOrEmpty(targetScene))
            targetScene = "1.1";

        // Kiểm tra xem scene có tồn tại trong Build Settings không
        bool sceneExists = Enumerable.Range(0, SceneManager.sceneCountInBuildSettings)
            .Select(SceneUtility.GetScenePathByBuildIndex)
            .Any(scenePath => scenePath.EndsWith($"{targetScene}.unity"));

        if (!sceneExists)
        {
            Debug.LogWarning($"[SaveController] Scene '{targetScene}' không tồn tại trong Build Settings. Chuyển về '1.1'.");
            targetScene = "1.1";
        }

        // Nếu khác scene hiện tại -> load scene
        if (SceneManager.GetActiveScene().name != targetScene)
        {
            SceneManager.LoadScene(targetScene);
            SaveController.pendingSceneName = targetScene;
            SaveController.nextSpawnPosition = saveData.playerPosition;
            return true;
        }

        //if (!string.IsNullOrEmpty(saveData.currentSceneName) && SceneManager.GetActiveScene().name != saveData.currentSceneName)
        //{
        //    SceneManager.LoadScene(saveData.currentSceneName);
        //    SaveController.pendingSceneName = saveData.currentSceneName;
        //    SaveController.nextSpawnPosition = saveData.playerPosition;
        //    return;
        //}

        GameObject player = GameObject.FindGameObjectWithTag("PlayerController");
        if (SaveController.nextSpawnPosition != null)
        {
            player.transform.position = SaveController.nextSpawnPosition.Value;
            SaveController.nextSpawnPosition = null;
        }
        else
        {
            player.transform.position = saveData.playerPosition;
        }

        inventoryController.slotCount = saveData.backPackSlotCount;
        inventoryController.SetInventoryItems(saveData.inventorySaveData);
        hotbarController.SetHotbarItems(saveData.hotbarSaveData);
        LoadChestStates(saveData.chestSaveData);

        knightEquipmentPanel.SetEquipmentItems(saveData.knightEquipSaveData);
        mageEquipmentPanel.SetEquipmentItems(saveData.mageEquipSaveData);

        QuestController.Instance.LoadQuestProgress(saveData.questProgressData);
        QuestController.Instance.handInQuestIDs = saveData.handInQuestIDs;

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
        playerStats.knightHealth = saveData.currentKnightHP;
        playerStats.mageHealth = saveData.currentmageHP;
        playerStats.knightMP = saveData.currentKnightMP;
        playerStats.mageMP = saveData.currentMageMP;
        playerStats.currentStamina = saveData.currentStamina;

        collectedByScene = saveData.collectedByScene;

        var vcam = FindFirstObjectByType<CinemachineCamera>();
        if (vcam != null)
        {
            // Force camera to player's position immediately to avoid cinematic damping sliding
            vcam.ForceCameraPosition(player.transform.position, Quaternion.identity);
        }

        // Also reset Cinemachine internal state and snap main camera transform as a fallback
        // This prevents long smooth-damping slides when entering a scene
        try
        {
            Unity.Cinemachine.CinemachineCore.ResetCameraState();
            if (Camera.main != null)
            {
                Vector3 camPos = Camera.main.transform.position;
                Camera.main.transform.position = new Vector3(player.transform.position.x, player.transform.position.y, camPos.z);
                Camera.main.transform.rotation = Quaternion.identity;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("Failed to force-snap Cinemachine camera: " + ex.Message);
        }

        //SaveGame();
        return false;
    }

    private void LoadChestStates(List<ChestSaveData> chestState)
    {
        string scene = SceneManager.GetActiveScene().name;
        foreach (Chest chest in chests)
        {
            // Prefer exact match (id + scene). For backward compatibility, also accept entries with empty sceneName that match id.
            ChestSaveData chestSaveData = chestState.FirstOrDefault(c => c.chestID == chest.ChestID);

            if (chestSaveData != null)
            {
                chest.SetOpened(chestSaveData.isOpened);
            }
        }
    }
    [System.Serializable]
    public class SaveDataRequest
    {
        public string DataSave;
    }
    IEnumerator SaveToServer(SaveData saveData)
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

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Đã lưu dữ liệu lên server thành công.");
        }
        else
        {
            Debug.Log("Token save: " + token);
            Debug.LogError("Lỗi khi lưu lên server: " + request.downloadHandler.text);
        }
    }

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
            Debug.Log("JSON nhận được từ server: " + request.downloadHandler.text);
            Debug.Log("Token load: " + token);
            onLoaded?.Invoke(data);
        }
        else
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            SceneManager.LoadScene("MainMenu");
            Debug.LogError("Lỗi khi tải dữ liệu từ server: " + request.downloadHandler.text);
        }
    }
    public string GetPlayerUID()
    {
        return PlayerPrefs.GetString("AccountId", "");
    }


    // Persist collected items per scene

    public List<SceneCollected> collectedByScene = new List<SceneCollected>();

    public bool IsCollected(string sceneName, string id)
    {
        if (collectedByScene == null)
            collectedByScene = new List<SceneCollected>();

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
