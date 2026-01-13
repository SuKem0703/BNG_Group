using System.IO;
using UnityEngine;

public class SaveSettingController : MonoBehaviour
{
    private string saveFilePath;

    private SoundEffectManager soundEffectManager;
    private GameSettingController gameSettingController;
    private CameraZoomController cameraZoomController;

    void Start()
    {
        saveFilePath = Application.persistentDataPath + "/game_settings.json";

        soundEffectManager = FindFirstObjectByType<SoundEffectManager>();
        gameSettingController = FindFirstObjectByType<GameSettingController>();
        cameraZoomController = FindFirstObjectByType<CameraZoomController>();

        LoadSettings();
    }

    public void SaveSettings()
    {
        SaveSetting saveSetting = new SaveSetting();

        if (gameSettingController != null)
        {
            saveSetting.sfxVolume = gameSettingController.sfxSlider != null ? gameSettingController.sfxSlider.value : 1f;
            saveSetting.bgmVolume = gameSettingController.bgmSlider != null ? gameSettingController.bgmSlider.value : 1f;

            saveSetting.lightIntensity = gameSettingController.sunLight != null ? gameSettingController.sunLight.intensity : 1f;

            saveSetting.graphicsLevel = gameSettingController.lastValidGraphicsLevel;
            saveSetting.fxaaEnabled = gameSettingController.fxaaToggle != null ? gameSettingController.fxaaToggle.isOn : true;
            saveSetting.isFullScreen = gameSettingController.fullscreenToggle != null ? gameSettingController.fullscreenToggle.isOn : true;
        }

        saveSetting.language = (LocalizationManager.Instance != null) ? LocalizationManager.Instance.CurrentLang : "vi";

        if (cameraZoomController != null)
        {
            saveSetting.cameraZoom = cameraZoomController.GetCurrentZoom();
        }
        else
        {
            var cam = FindFirstObjectByType<Unity.Cinemachine.CinemachineCamera>();
            if (cam != null) saveSetting.cameraZoom = cam.Lens.OrthographicSize;
        }

        File.WriteAllText(saveFilePath, JsonUtility.ToJson(saveSetting, true));

        string msg = LocalizationManager.Instance != null
            ? LocalizationManager.Instance.GetText("MSG_SAVE_SETTING_SUCCESS")
            : "Settings Saved";
        GameNotify.Show(msg);
    }

    public void LoadSettings()
    {
        if (File.Exists(saveFilePath))
        {
            SaveSetting saveSetting = JsonUtility.FromJson<SaveSetting>(File.ReadAllText(saveFilePath));

            if (LocalizationManager.Instance != null && !string.IsNullOrEmpty(saveSetting.language))
            {
                LocalizationManager.Instance.LoadLanguage(saveSetting.language);

                if (gameSettingController != null)
                {
                    gameSettingController.UpdateLanguageUI();
                }
            }

            // Load UI Settings
            if (gameSettingController != null)
            {
                if (gameSettingController.sfxSlider) gameSettingController.sfxSlider.value = saveSetting.sfxVolume;
                if (gameSettingController.bgmSlider) gameSettingController.bgmSlider.value = saveSetting.bgmVolume;

                // gameSettingController.sunLight.intensity = saveSetting.lightIntensity; 

                gameSettingController.lastValidGraphicsLevel = saveSetting.graphicsLevel;
                if (gameSettingController.graphicsSlider)
                    gameSettingController.graphicsSlider.value = (saveSetting.graphicsLevel - 1) / 2f;

                gameSettingController.SetGraphicsLevel(saveSetting.graphicsLevel);

                if (gameSettingController.fxaaToggle) gameSettingController.fxaaToggle.isOn = saveSetting.fxaaEnabled;
                if (gameSettingController.fullscreenToggle) gameSettingController.fullscreenToggle.isOn = saveSetting.isFullScreen;
            }
            else
            {
                SoundEffectManager.SetSFXVolume(saveSetting.sfxVolume);
                SoundEffectManager.SetBGMVolume(saveSetting.bgmVolume);
            }

            if (cameraZoomController != null)
            {
                cameraZoomController.SetZoom(saveSetting.cameraZoom);
            }
        }
        else
        {
            SaveSettings();
            Debug.LogWarning("No settings file found, using default settings.");
        }
    }
}