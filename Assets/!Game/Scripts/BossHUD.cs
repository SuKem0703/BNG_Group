using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BossHUD : MonoBehaviour
{
    public static BossHUD Instance { get; private set; }

    [Header("UI Components")]
    public GameObject bossPanel;
    public Image healthFillImage;
    public TextMeshProUGUI bossNameText;

    public TextMeshProUGUI phaseDescriptionText;

    [Header("Phase Stack Indicator")]
    public GameObject healthStackIndicator;
    public TextMeshProUGUI healthStackText;

    [Header("Settings")]
    public float lerpSpeed = 5f;

    private Enemy _currentBoss;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;

        if (bossPanel != null) bossPanel.SetActive(false);
    }

    private void Update()
    {
        if (_currentBoss == null) return;

        float targetFill = Mathf.Clamp01((float)_currentBoss.netHealth.Value / _currentBoss.maxHealth);
        healthFillImage.fillAmount = Mathf.Lerp(healthFillImage.fillAmount, targetFill, Time.deltaTime * lerpSpeed);
    }

    public void ShowBossHealth(Enemy boss)
    {
        _currentBoss = boss;
        if (bossPanel != null) bossPanel.SetActive(true);

        UpdatePhaseInfo(boss);
    }

    public void UpdatePhaseInfo(Enemy boss)
    {
        if (bossNameText != null) bossNameText.text = boss.GetCurrentPhaseName();
        if (phaseDescriptionText != null) phaseDescriptionText.text = boss.GetCurrentPhaseDescription();

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

        if (healthFillImage != null) healthFillImage.fillAmount = 1f;
    }

    public void HideBossHealth()
    {
        if (bossPanel != null) bossPanel.SetActive(false);
        _currentBoss = null;
    }
}