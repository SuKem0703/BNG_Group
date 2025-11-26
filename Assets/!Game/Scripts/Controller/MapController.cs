using UnityEngine;

public enum MapType
{
    SafeZone,
    CombatZone,
    Dungeon
}

public class MapController : MonoBehaviour
{
    public static MapController Instance { get; private set; }

    [Header("Cấu hình Map")]
    [SerializeField] private string mapName = "Tên Bản Đồ";
    [SerializeField] private MapType mapType = MapType.CombatZone;
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

        Debug.Log($"Đã tải Map: {mapName} | Loại: {mapType}");
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