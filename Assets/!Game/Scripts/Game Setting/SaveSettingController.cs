using System.IO;
using UnityEngine;

public class SaveSettingController : MonoBehaviour
{
    private string saveFilePath;

    private SoundEffectManager soundEffectManager;
    private GameSettingController gameSettingController;

    void Start()
    {
        saveFilePath = Application.persistentDataPath + "/game_settings.json";

        soundEffectManager = FindFirstObjectByType<SoundEffectManager>();
        gameSettingController = FindFirstObjectByType<GameSettingController>();

        LoadSettings();
    }

    public void SaveSettings()
    {
        SaveSetting saveSetting = new SaveSetting
        {
            sfxVolume = gameSettingController.sfxSlider != null ? gameSettingController.sfxSlider.value : 1f,
            bgmVolume = gameSettingController.bgmSlider != null ? gameSettingController.bgmSlider.value : 1f,

            lightIntensity = gameSettingController.sunLight != null ? gameSettingController.sunLight.intensity : 1f,

            graphicsLevel = gameSettingController.lastValidGraphicsLevel,

            fxaaEnabled = gameSettingController.fxaaToggle != null ? gameSettingController.fxaaToggle.isOn : true,
            isFullScreen = gameSettingController.fullscreenToggle != null ? gameSettingController.fullscreenToggle.isOn : true
        };
        File.WriteAllText(saveFilePath, JsonUtility.ToJson(saveSetting, true));
        Debug.Log("Settings saved!");
    }

    public void LoadSettings()
    {
        if (File.Exists(saveFilePath))
        {
            SaveSetting saveSetting = JsonUtility.FromJson<SaveSetting>(File.ReadAllText(saveFilePath));

            if (gameSettingController == null) return;

            gameSettingController.sfxSlider.value = saveSetting.sfxVolume;
            gameSettingController.bgmSlider.value = saveSetting.bgmVolume;

            //gameSettingController.sunLight.intensity = saveSetting.lightIntensity;

            gameSettingController.lastValidGraphicsLevel = saveSetting.graphicsLevel;
            gameSettingController.graphicsSlider.value = (saveSetting.graphicsLevel - 1) / 2f;
            gameSettingController.SetGraphicsLevel(saveSetting.graphicsLevel);

            gameSettingController.fxaaToggle.isOn = saveSetting.fxaaEnabled;
            gameSettingController.fullscreenToggle.isOn = saveSetting.isFullScreen;
        }
        else
        {
            SaveSettings();
            Debug.LogWarning("No settings file found, using default settings.");
        }
    }
}
