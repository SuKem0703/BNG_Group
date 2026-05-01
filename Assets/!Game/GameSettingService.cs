using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using System.IO;
using System.Collections;

public class GameSettingService : MonoBehaviour
{
    public static GameSettingService Instance { get; private set; }

    private string saveFilePath;
    public SaveSetting currentSettings;

    private UniversalAdditionalCameraData cameraData;
    private Light2D sunLight;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(transform.root.gameObject); return; }
        Instance = this;

        saveFilePath = Application.persistentDataPath + "/game_settings.json";
        LoadSettingsFromFile();
    }

    private void Start()
    {
        RefreshReferences();
        ApplyAllSettings();
    }

    public void RefreshReferences()
    {
        if (Camera.main != null)
            cameraData = Camera.main.GetComponent<UniversalAdditionalCameraData>();

        sunLight = GameObject.Find("Global Light 2D")?.GetComponent<Light2D>();
        if (sunLight != null)
            sunLight.intensity = currentSettings.lightIntensity * 2f;
    }

    public void SaveSettingsToFile()
    {
        string json = JsonUtility.ToJson(currentSettings, true);
        File.WriteAllText(saveFilePath, json);
        Debug.Log("[Settings] Đã lưu cài đặt xuống file JSON.");
    }

    public void Logout()
    {
        if (PlayerPrefs.HasKey("AuthToken"))
        {
            PlayerPrefs.DeleteKey("AuthToken");
            PlayerPrefs.Save();
        }
        MenuStateManager.Instance.ResetState();
        SceneManager.LoadScene("MainMenu");
    }

    public void QuitGame()
    {
        StartCoroutine(QuitAfterSaveRoutine());
    }

    private IEnumerator QuitAfterSaveRoutine()
    {
        if (SaveController.Instance != null)
        {
            yield return StartCoroutine(SaveController.Instance.SaveRoutine(SaveReason.Manual));
        }

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }


    public void SetAudioVolume(string type, float value)
    {
        if (type == "SFX")
        {
            currentSettings.sfxVolume = value;
            SoundEffectManager.SetSFXVolume(value);
        }
        else
        {
            currentSettings.bgmVolume = value;
            SoundEffectManager.SetBGMVolume(value);
        }
    }

    public void SetLightIntensity(float value)
    {
        currentSettings.lightIntensity = value;
        sunLight = GameObject.Find("Global Light 2D")?.GetComponent<Light2D>();
        if (sunLight != null) sunLight.intensity = value * 2f;
    }

    public void SetGraphics(int level, bool full, bool fxaa)
    {
        currentSettings.graphicsLevel = level;
        currentSettings.isFullScreen = full;
        currentSettings.fxaaEnabled = fxaa;
        ApplyAllSettings();
    }

    public void ApplyAllSettings()
    {
        int w = currentSettings.graphicsLevel == 1 ? 1280 : (currentSettings.graphicsLevel == 2 ? 1920 : 3840);
        int h = currentSettings.graphicsLevel == 1 ? 720 : (currentSettings.graphicsLevel == 2 ? 1080 : 2160);
        FullScreenMode mode = currentSettings.isFullScreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        Screen.SetResolution(w, h, mode);

        QualitySettings.SetQualityLevel(currentSettings.graphicsLevel == 1 ? 0 : (currentSettings.graphicsLevel == 2 ? 2 : 5), true);

        if (Camera.main != null)
        {
            cameraData = Camera.main.GetComponent<UniversalAdditionalCameraData>();
            if (cameraData != null)
                cameraData.antialiasing = currentSettings.fxaaEnabled ? AntialiasingMode.FastApproximateAntialiasing : AntialiasingMode.None;
        }

        SoundEffectManager.SetSFXVolume(currentSettings.sfxVolume);
        SoundEffectManager.SetBGMVolume(currentSettings.bgmVolume);
    }

    private void LoadSettingsFromFile()
    {
        if (File.Exists(saveFilePath))
        {
            currentSettings = JsonUtility.FromJson<SaveSetting>(File.ReadAllText(saveFilePath));
        }
        else
        {
            currentSettings = new SaveSetting { sfxVolume = 1f, bgmVolume = 1f, lightIntensity = 0.5f, graphicsLevel = 2, fxaaEnabled = true, isFullScreen = true, language = "vi" };
            SaveSettingsToFile();
        }
    }
}