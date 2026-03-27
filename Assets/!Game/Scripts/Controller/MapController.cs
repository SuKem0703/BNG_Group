using UnityEngine;

public enum MapType
{
    SafeZone,
    CombatZone,
    Dungeon
}

public enum MapEnvironment
{
    City,
    Indoor,
    Outdoor,
    Dungeon,
    Village,
    Forest,
    Cave,
    Castle
}

public class MapController : MonoBehaviour
{
    public static MapController Instance { get; private set; }

    [Header("Cấu hình Map")]
    public string mapName = "";
    [SerializeField] private MapType mapType = MapType.SafeZone;
    [SerializeField] private MapEnvironment mapEnvironment = MapEnvironment.Outdoor;
    public AudioClip bgmClip;
    public bool IsCutsceneMode = false;

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    private void OnDestroy()
    {
        SaveController.OnDataLoaded -= PlayMapBGM;
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        if (SaveController.IsDataLoaded) PlayMapBGM();
        else SaveController.OnDataLoaded += PlayMapBGM;

        // Gọi hàm hiển thị UI
        ShowMapNameUI();

        //Debug.Log($"Đã tải Map: {mapName} | Loại: {mapType}");
    }

    public void ShowMapNameUI()
    {
        if (IsCutsceneMode) return;

        if (string.IsNullOrEmpty(mapName) || string.IsNullOrWhiteSpace(mapName))
        {
            return;
        }

        if (LoadResourceManager.Instance != null && LoadResourceManager.Instance.MapInfoUIPrefab != null)
        {
            GameObject uiObj = Instantiate(LoadResourceManager.Instance.MapInfoUIPrefab);

            MapInfoUIController controller = uiObj.GetComponent<MapInfoUIController>();
            if (controller != null)
            {
                controller.ShowMapName(mapName);
            }
        }
        else
        {
            Debug.LogWarning("Không tìm thấy LoadResourceManager hoặc MapInfoUIPrefab!");
        }
    }

    public void PlayMapBGM()
    {
        SaveController.OnDataLoaded -= PlayMapBGM;

        if (IsCutsceneMode) return;

        if (bgmClip != null)
        {
            SoundEffectManager.PlayBGM(bgmClip, true);
        }
    }

    public bool IsSafeZone()
    {
        return mapType == MapType.SafeZone;
    }

    public string GetMapName()
    {
        return mapName;
    }

    public void SetMapTypeOverride(MapType newType)
    {
        mapType = newType;
    }
}