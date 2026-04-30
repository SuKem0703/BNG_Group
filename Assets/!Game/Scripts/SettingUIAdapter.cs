using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingUIAdapter : MonoBehaviour
{
    [Header("UI Controls")]
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider graphicsSlider;
    [SerializeField] private TextMeshProUGUI graphicsLabel;
    [SerializeField] private Toggle fxaaToggle;
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private Slider lightSlider;
    [SerializeField] private TextMeshProUGUI languageText;

    private void Start()
    {
        var ram = GameSettingService.Instance.currentSettings;

        sfxSlider.value = ram.sfxVolume;
        bgmSlider.value = ram.bgmVolume;
        lightSlider.value = ram.lightIntensity;
        graphicsSlider.value = (ram.graphicsLevel - 1) / 2f;
        fxaaToggle.isOn = ram.fxaaEnabled;
        fullscreenToggle.isOn = ram.isFullScreen;

        UpdateGraphicsLabel(ram.graphicsLevel);
        UpdateLanguageText(ram.language);

        sfxSlider.onValueChanged.AddListener(val => GameSettingService.Instance.SetAudioVolume("SFX", val));
        bgmSlider.onValueChanged.AddListener(val => GameSettingService.Instance.SetAudioVolume("BGM", val));
        lightSlider.onValueChanged.AddListener(val => GameSettingService.Instance.SetLightIntensity(val));
        graphicsSlider.onValueChanged.AddListener(val => UpdateGraphicsLabel(Mathf.RoundToInt(val * 2f) + 1));
    }

    public void OnSaveSettingsClick()
    {
        int level = Mathf.RoundToInt(graphicsSlider.value * 2f) + 1;
        GameSettingService.Instance.SetGraphics(level, fullscreenToggle.isOn, fxaaToggle.isOn);
        GameSettingService.Instance.SaveSettingsToFile();
        GameNotify.Show("Đã lưu cài đặt vào máy!");
    }

    public void OnLogoutClick() => GameSettingService.Instance.Logout();

    public void OnSaveGameClick()
    {
        if (SaveController.Instance != null)
            SaveController.Instance.SaveGame(SaveReason.Manual);
    }

    public void OnQuitGameClick() => GameSettingService.Instance.QuitGame();

    public void OnLanguageToggleClick()
    {
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.ToggleLanguage();
            string newLang = LocalizationManager.Instance.CurrentLang;
            GameSettingService.Instance.currentSettings.language = newLang; // Cập nhật RAM
            UpdateLanguageText(newLang);
        }
    }

    private void UpdateGraphicsLabel(int level)
    {
        if (graphicsLabel != null)
            graphicsLabel.text = level == 1 ? "Low" : (level == 2 ? "Medium" : "Ultra");
    }

    private void UpdateLanguageText(string lang)
    {
        if (languageText != null)
            languageText.text = (lang == "vi") ? "Tiếng Việt" : "English";
    }
}