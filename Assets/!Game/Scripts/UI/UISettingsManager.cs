using UnityEngine;

public class UISettingsManager : MonoBehaviour
{
    public static UISettingsManager Instance { get; private set; }

    [Header("UI Panel Settings")]
    public GameObject settingsPanel;

    private bool isOpen = false;

    public bool IsSettingsOpen => isOpen;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleSettings();
        }
    }

    public void ToggleSettings()
    {
        if (settingsPanel == null) return;

        isOpen = !isOpen;
        settingsPanel.SetActive(isOpen);
        Time.timeScale = isOpen ? 0f : 1f;

        // Chỉ gọi Pause tương ứng với trạng thái mới
        PauseController.SetPause(isOpen);
        CommonUIController.Instance?.SetUIVisible(!isOpen);
    }

    public void CloseSettings()
    {
        if (settingsPanel == null) return;

        isOpen = false;
        settingsPanel.SetActive(false);
        Time.timeScale = 1f;

        PauseController.SetPause(false);
        CommonUIController.Instance?.SetUIVisible(true);
    }
}
