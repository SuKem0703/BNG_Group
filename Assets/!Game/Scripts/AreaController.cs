using UnityEngine;

public enum MapType
{
    SafeZone,
    CombatZone,
    Dungeon,
    BossArea
}

[RequireComponent(typeof(BoxCollider2D))]
public class AreaController : MonoBehaviour
{
    public static AreaController currentArea { get; set; }
    public static bool isGlobalCutsceneMode { get; set; } = false;

    [Header("Cấu hình Area")]
    public string mapName = "";
    public MapType mapType = MapType.SafeZone;
    public AudioClip bgmClip;
    public bool isCutsceneMode = false;

    [Header("Cấu hình Ánh sáng Khu vực")]
    public bool overrideGlobalLight = false;
    public float targetIntensity = 0.5f;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            currentArea = this;
            ActivateArea();
        }
    }

    public void ActivateArea()
    {
        if (isCutsceneMode || isGlobalCutsceneMode) return;

        if (TimeManager.Instance != null)
        {
            if (overrideGlobalLight)
            {
                TimeManager.Instance.SetEnvironmentOverride(true, targetIntensity);
            }
            else
            {
                TimeManager.Instance.SetEnvironmentOverride(false);
            }
        }

        if (bgmClip != null)
        {
            SoundEffectManager.PlayBGM(bgmClip, true);
        }

        ShowMapNameUI();
    }

    public void ShowMapNameUI()
    {
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
    }
}