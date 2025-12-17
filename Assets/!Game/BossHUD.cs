using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BossHUD : MonoBehaviour
{
    public static BossHUD Instance { get; private set; }

    [Header("UI Components")]
    [Tooltip("Kéo toàn bộ Panel chứa thanh máu vào đây để tắt/bật")]
    public GameObject bossPanel;
    public Image healthFillImage;
    public TextMeshProUGUI bossNameText;

    [Header("Settings")]
    public float lerpSpeed = 5f;

    private EnemyChase _currentBoss;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;

        // Ẩn lúc đầu game
        if (bossPanel != null) bossPanel.SetActive(false);
    }

    private void Update()
    {
        if (_currentBoss == null) return;

        // Cập nhật thanh máu mượt mà
        float targetFill = (float)_currentBoss.currentHealth / _currentBoss.maxHealth;
        healthFillImage.fillAmount = Mathf.Lerp(healthFillImage.fillAmount, targetFill, Time.deltaTime * lerpSpeed);

        // Tự động tắt nếu Boss chết hoặc bị hủy
        if (_currentBoss.currentHealth <= 0 || _currentBoss == null)
        {
            HideBossHealth();
        }
    }

    // Hàm này sẽ được Boss gọi khi xuất hiện
    public void ShowBossHealth(EnemyChase boss)
    {
        _currentBoss = boss;

        if (bossPanel != null) bossPanel.SetActive(true);
        if (bossNameText != null) bossNameText.text = boss.enemyName;

        // Reset thanh máu về 0 rồi chạy lên hoặc set full luôn tùy ý
        if (healthFillImage != null) healthFillImage.fillAmount = (float)boss.currentHealth / boss.maxHealth;
    }

    public void HideBossHealth()
    {
        if (bossPanel != null) bossPanel.SetActive(false);
        _currentBoss = null;
    }
}