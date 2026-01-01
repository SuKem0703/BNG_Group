using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System.Collections;

public class GameSettingController : MonoBehaviour
{
    public static GameSettingController Instance;

    [Header("Âm thanh")]
    public Slider sfxSlider;
    private float lastTestPlayTime = 0f;
    public Slider bgmSlider;

    [Header("Ánh sáng")]
    public Slider lightSlider;
    public Light2D sunLight;

    [Header("Đồ họa")]
    [Tooltip("Level 1 = 16:9 HD | Level 2 = Full HD | Level 3 = 4K")]
    public int defaultGraphicsLevel = 2;
    public Slider graphicsSlider;
    public TextMeshProUGUI graphicsLabel;
    public int lastValidGraphicsLevel;

    [Header("Anti-Aliasing")]
    public Toggle fxaaToggle;

    [Header("Full Screen Mode")]
    public Toggle fullscreenToggle;

    [Header("Dành riêng cho Main Menu")]
    public GameObject settingUI;

    private UniversalAdditionalCameraData cameraData;
    private SaveController saveController;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Tìm SaveController
        saveController = FindFirstObjectByType<SaveController>();

        // Tìm cài đặt âm thanh
        if (sfxSlider == null)
        {
            sfxSlider = GameObject.Find("SFXSlider")?.GetComponent<Slider>();
        }
        if (bgmSlider == null)
        {
            bgmSlider = GameObject.Find("BGMSlider")?.GetComponent<Slider>();
        }

        // Cài đặt âm lượng SFX và BGM
        if (sfxSlider != null)
        {
            sfxSlider.SetValueWithoutNotify(1f);
            sfxSlider.onValueChanged.RemoveAllListeners();
            sfxSlider.onValueChanged.AddListener(value =>
            {
                SoundEffectManager.SetSFXVolume(value);

                // Chỉ phát thử nếu người chơi thật sự thay đổi giá trị
                if (Mathf.Abs(value - sfxSlider.value) > 0.001f && Time.unscaledTime - lastTestPlayTime > 0.25f)
                {
                    SoundEffectManager.Play("Dash");
                    lastTestPlayTime = Time.unscaledTime;
                }
            });
        }

        if (bgmSlider != null)
        {
            bgmSlider.SetValueWithoutNotify(1f);
            bgmSlider.onValueChanged.RemoveAllListeners();
            bgmSlider.onValueChanged.AddListener(value =>
            {
                SoundEffectManager.SetBGMVolume(value);
                PlayerPrefs.SetFloat("BGMVolume", value);
            });
        }

        // Tìm cài đặt camera
        Camera mainCam = Camera.main;
        if (mainCam != null)
            cameraData = mainCam.GetComponent<UniversalAdditionalCameraData>();

        if (cameraData == null)
            Debug.LogWarning("Không tìm thấy UniversalAdditionalCameraData trên Main Camera!");

        // Tìm Light2D
        lightSlider = GameObject.Find("LightSlider")?.GetComponent<Slider>();
        if (sunLight == null)
        {
            sunLight = GameObject.Find("Global Light 2D")?.GetComponent<Light2D>();
        }

        // Cài đặt slider ánh sáng
        if (lightSlider != null)
        {
            lightSlider.value = sunLight != null ? sunLight.intensity / 2f : 0.5f;
            lightSlider.onValueChanged.AddListener(UpdateLightIntensity);
        }

        // TÌm cài đặt đồ họa
        if (graphicsSlider == null)
        {
            graphicsSlider = GameObject.Find("GraphicSlider")?.GetComponent<Slider>();
        }
        if (graphicsLabel == null)
        {
            graphicsLabel = GameObject.Find("GraphicsLabel")?.GetComponent<TextMeshProUGUI>();
        }

        // Tìm cài đặt Fullscreen
        if (fullscreenToggle == null)
        {
            fullscreenToggle = GameObject.Find("FullscreenToggle")?.GetComponent<Toggle>();
        }
        // Gán sự kiện cho toggle
        fullscreenToggle.onValueChanged.AddListener(SetFullScreen);

        // Load trạng thái hiện tại
        fullscreenToggle.isOn = Screen.fullScreen;

        // Tìm cài đặt AA
        if (fxaaToggle == null)
        {
            fxaaToggle = GameObject.Find("FXAAToggle")?.GetComponent<Toggle>();
        }

        // Cài đặt slider đồ họa
        lastValidGraphicsLevel = defaultGraphicsLevel;

        if (graphicsSlider != null)
        {
            graphicsSlider.value = (defaultGraphicsLevel - 1) / 2f;
            graphicsSlider.onValueChanged.AddListener(UpdateGraphicsLabelPreview);
            SliderApplyOnRelease applyOnRelease = graphicsSlider.gameObject.AddComponent<SliderApplyOnRelease>();
            applyOnRelease.slider = graphicsSlider;
            applyOnRelease.gameSettingController = this;
        }

        // Cài đặt toggle FXAA
        if (fxaaToggle != null && cameraData != null)
        {
            fxaaToggle.isOn = cameraData.antialiasing == AntialiasingMode.FastApproximateAntialiasing;
            fxaaToggle.onValueChanged.AddListener(SetFXAA);
        }

        // Set đồ họa mặc định
        SetGraphicsLevel(defaultGraphicsLevel);
    }
    private void Start()
    {
        // Dành riêng cho Main Menu
        if (SceneManager.GetActiveScene().name == "MainMenu")
        {
            if (settingUI == null)
            {
                settingUI = GameObject.Find("SettingUI");
            }
            settingUI.SetActive(false);
            SoundEffectManager.PlayBGM("Lumina Castle", true);
        }
    }
    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
    public void SaveGame()
    {
        SaveController.Instance.SaveGame();
    }
    void UpdateLightIntensity(float value)
    {
        if (sunLight != null)
            sunLight.intensity = value * 2f;
    }
    public void UpdateGraphicsLabel(int level)
    {
        //Debug.Log($"Current resolution: {Screen.width} x {Screen.height}");

        if (graphicsLabel != null)
        {
            switch (level)
            {
                case 1: graphicsLabel.text = "Low"; break;
                case 2: graphicsLabel.text = "Medium"; break;
                case 3: graphicsLabel.text = "Ultra"; break;
            }
        }
    }
    public void SetFullScreen(bool isFullscreen)
    {
        if (isFullscreen)
        {
            Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
            Screen.fullScreen = true;
        }
        else
        {
            Screen.fullScreenMode = FullScreenMode.Windowed;
            Screen.fullScreen = false;

            Resolution currentRes = Screen.currentResolution;
            Screen.SetResolution(currentRes.width, currentRes.height, false);
        }
    }
    public bool SetGraphicsLevel(int level)
    {
        int targetWidth = 0;
        int targetHeight = 0;
        FullScreenMode mode = (fullscreenToggle != null && fullscreenToggle.isOn)
    ? FullScreenMode.FullScreenWindow
    : FullScreenMode.Windowed;

        switch (level)
        {
            case 1:
                targetWidth = 1280;
                targetHeight = 720;
                break;
            case 2:
                targetWidth = 1920;
                targetHeight = 1080;
                break;
            case 3:
                targetWidth = 3840;
                targetHeight = 2160;
                break;
            default:
                Debug.LogWarning("Graphics level không hợp lệ!");
                return false;
        }

        bool supported = false;
        foreach (var res in Screen.resolutions)
        {
            if (res.width == targetWidth && res.height == targetHeight)
            {
                supported = true;
                break;
            }
        }

        if (!supported)
        {
            Debug.LogWarning($"Độ phân giải {targetWidth}x{targetHeight} không được màn hình hỗ trợ!");
            return false;
        }

        Screen.SetResolution(targetWidth, targetHeight, mode);

        if (level == 1) QualitySettings.SetQualityLevel(0, true);
        else if (level == 2) QualitySettings.SetQualityLevel(2, true);
        else if (level == 3) QualitySettings.SetQualityLevel(5, true);

        UpdateGraphicsLabel(level);

        lastValidGraphicsLevel = level;

        return true;
    }
    void UpdateGraphicsLabelPreview(float value)
    {
        int level = Mathf.RoundToInt(value * 2f) + 1;
        UpdateGraphicsLabel(level);
    }
    public void SetFXAA(bool enable)
    {
        if (cameraData != null)
        {
            cameraData.antialiasing = enable ? AntialiasingMode.FastApproximateAntialiasing : AntialiasingMode.None;
        }
    }
    public void Logout()
    {
        // Xóa token
        if (PlayerPrefs.HasKey("AuthToken"))
        {
            PlayerPrefs.DeleteKey("AuthToken");
            PlayerPrefs.Save();
        }

        MenuStateManager.Instance.ResetState();

        // Trả về MainMenu
        SceneManager.LoadScene("MainMenu");
    }

    public void QuitGame()
    {
    if (saveController != null)
        {
            StartCoroutine(QuitAfterSave());
        }

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
                    Application.Quit();
#endif
    }
    private IEnumerator QuitAfterSave()
    {
        yield return StartCoroutine(saveController.SaveRoutine(SaveReason.Manual));

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
    Application.Quit();
#endif
    }

    // Hàm này sẽ được gọi khi người dùng muốn kiểm tra trạng thái FXAA
    private void CheckFXAAStatus()
    {
        if (cameraData != null)
        {
            bool fxaaEnabled = cameraData.antialiasing == AntialiasingMode.FastApproximateAntialiasing;
            Debug.Log($"[FXAA] Trạng thái hiện tại: {(fxaaEnabled ? "Bật" : "Tắt")}");
        }
        else
        {
            Debug.LogWarning("Không tìm thấy cameraData để kiểm tra FXAA!");
        }
    }

}
// Component nhỏ nhận sự kiện thả slider
public class SliderApplyOnRelease : MonoBehaviour, IPointerUpHandler
{
    public GameSettingController gameSettingController;
    public Slider slider;

    public void OnPointerUp(PointerEventData eventData)
    {
        if (gameSettingController != null && slider != null)
        {
            int level = Mathf.RoundToInt(slider.value * 2f) + 1;
            bool valid = gameSettingController.SetGraphicsLevel(level);
            if (!valid)
            {
                // Snap back về mức hợp lệ trước đó
                int last = gameSettingController.lastValidGraphicsLevel;
                slider.SetValueWithoutNotify((last - 1) / 2f);

                // Set lại chất lượng và label theo mức hợp lệ
                gameSettingController.SetGraphicsLevel(last);
            }
        }
    }

}