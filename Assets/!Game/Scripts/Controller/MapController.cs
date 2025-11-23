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
    [SerializeField] private AudioClip bgmClip;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }
    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        if (bgmClip != null)
        {
            SoundEffectManager.PlayBGM(bgmClip, true);
        }

        Debug.Log($"Đã tải Map: {mapName} | Loại: {mapType}");
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