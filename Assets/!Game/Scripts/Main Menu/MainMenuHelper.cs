using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuHelper : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject settingUI;

    private void Awake()
    {
        if (settingUI != null)
        {
            settingUI.SetActive(false);
        }
    }

    private void Start()
    {
        SoundEffectManager.PlayBGM("Lumina Castle", true);

        // Khởi tạo các hệ thống cài đặt từ bộ nhớ RAM
        if (GameSettingService.Instance != null)
        {
            GameSettingService.Instance.RefreshReferences();
            GameSettingService.Instance.ApplyAllSettings();
        }
    }
}