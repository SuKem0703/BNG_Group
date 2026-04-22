using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BossHUD : MonoBehaviour
{
    public static BossHUD Instance { get; private set; }

    [Header("UI Components")]
    [Tooltip("Kéo Panel chứa toàn bộ UI Boss vào đây")]
    public GameObject bossPanel;
    public Image healthFillImage;
    public TextMeshProUGUI bossNameText;

    public TextMeshProUGUI phaseDescriptionText;

    [Header("Phase Stack Indicator")]
    [Tooltip("Object chứa icon hiển thị số mạng còn lại")]
    public GameObject healthStackIndicator;
    public TextMeshProUGUI healthStackText;

    [Header("Settings")]
    public float lerpSpeed = 5f;

    private Enemy _currentBoss;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;

        // Ẩn UI khi bắt đầu game
        if (bossPanel != null) bossPanel.SetActive(false);
    }

    private void Update()
    {
        if (_currentBoss == null) return;

        // Cập nhật thanh máu mượt mà
        float targetFill = Mathf.Clamp01((float)_currentBoss.currentHealth / _currentBoss.maxHealth);
        healthFillImage.fillAmount = Mathf.Lerp(healthFillImage.fillAmount, targetFill, Time.deltaTime * lerpSpeed);

        // if (_currentBoss.currentHealth <= 0 && _currentBoss.IsDefeated())
        // {
        //    HideBossHealth();
        // }
    }

    public void ShowBossHealth(Enemy boss)
    {
        _currentBoss = boss;
        if (bossPanel != null) bossPanel.SetActive(true);

        UpdatePhaseInfo(boss);
    }

    // Cập nhật thông tin khi chuyển Phase hoặc mới vào trận
    public void UpdatePhaseInfo(Enemy boss)
    {
        // Cập nhật Tên
        if (bossNameText != null) bossNameText.text = boss.GetCurrentPhaseName();

        // Cập nhật Mô tả
        if (phaseDescriptionText != null) phaseDescriptionText.text = boss.GetCurrentPhaseDescription();

        // Cập nhật số thanh máu còn lại (Health Stacks)
        if (healthStackText != null && healthStackIndicator != null)
        {
            int remainingPhases = boss.GetRemainingPhases();
            if (remainingPhases > 0)
            {
                healthStackIndicator.SetActive(true);
                healthStackText.text = $"x{remainingPhases + 1}";
            }
            else
            {
                healthStackIndicator.SetActive(false);
            }
        }

        // Hồi đầy thanh máu trên UI ngay lập tức để chuẩn bị cho Phase mới
        if (healthFillImage != null) healthFillImage.fillAmount = 1f;
    }

    public void HideBossHealth()
    {
        if (bossPanel != null) bossPanel.SetActive(false);
        _currentBoss = null;
    }
}