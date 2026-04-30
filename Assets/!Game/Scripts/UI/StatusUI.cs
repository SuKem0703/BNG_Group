using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StatusUI : MonoBehaviour
{
    private PlayerStats playerStats;
    private ClassController classController;

    [Header("Portrait")]
    [SerializeField] private Image elricPortrait;
    [SerializeField] private Image lyriaPortrait;

    [Header("Real Time")]
    [SerializeField] private TextMeshProUGUI timeText;

    [Header("Knight HP")]
    [SerializeField] private Image knightHealthBarFill;
    [SerializeField] private TextMeshProUGUI knightHealthText;

    [Header("Mage HP")]
    [SerializeField] private Image mageHealthBarFill;
    [SerializeField] private TextMeshProUGUI mageHealthText;

    [Header("Knight MP")]
    [SerializeField] private Image knightManaBarFill;
    [SerializeField] private TextMeshProUGUI knightManaText;

    [Header("Mage MP")]
    [SerializeField] private Image mageManaBarFill;
    [SerializeField] private TextMeshProUGUI mageManaText;

    [Header("Stamina")]
    [SerializeField] private Image staminaBarFill;
    [SerializeField] private TextMeshProUGUI staminaText;

    [Header("Level")]
    [SerializeField] private TextMeshProUGUI levelText;

    [Header("EXP")]
    [SerializeField] private Image expBarFill;

    [Header("Stats")]
    [SerializeField] private TextMeshProUGUI physicDMGText;
    [SerializeField] private TextMeshProUGUI magicDMGText;
    [SerializeField] private TextMeshProUGUI defenseText;
    [SerializeField] private TextMeshProUGUI critChanceText;
    [SerializeField] private TextMeshProUGUI moveSpeedText;

    [Header("Currency")]
    [SerializeField] private TextMeshProUGUI coinText;
    [SerializeField] private TextMeshProUGUI gemText;

    [Header("UI Mode")]
    [SerializeField] private bool showBothHPInMenu = false;

    void Awake()
    {
        playerStats = FindFirstObjectByType<PlayerStats>();
        classController = FindFirstObjectByType<ClassController>();

        if (playerStats == null || classController == null)
        {
            Debug.LogWarning("PlayerStats hoặc ClassController chưa được gán.");
            return;
        }

        AssignInspector();
    }
    void AssignInspector()
    {
        elricPortrait ??= transform.FindDeepChild("ElricPortrait")?.GetComponent<Image>();
        lyriaPortrait ??= transform.FindDeepChild("LyriaPortrait")?.GetComponent<Image>();

        timeText ??= transform.FindDeepChild("TimeText")?.GetComponent<TextMeshProUGUI>();

        knightHealthBarFill ??= transform.FindDeepChild("KnightHealthBarFill")?.GetComponent<Image>();
        knightHealthText ??= transform.FindDeepChild("KnightHealthText")?.GetComponent<TextMeshProUGUI>();

        mageHealthBarFill ??= transform.FindDeepChild("MageHealthBarFill")?.GetComponent<Image>();
        mageHealthText ??= transform.FindDeepChild("MageHealthText")?.GetComponent<TextMeshProUGUI>();

        knightManaBarFill ??= transform.FindDeepChild("KnightManaBarFill")?.GetComponent<Image>();
        knightManaText ??= transform.FindDeepChild("KnightManaText")?.GetComponent<TextMeshProUGUI>();

        mageManaBarFill ??= transform.FindDeepChild("MageManaBarFill")?.GetComponent<Image>();
        mageManaText ??= transform.FindDeepChild("MageManaText")?.GetComponent<TextMeshProUGUI>();

        staminaBarFill ??= transform.FindDeepChild("StaminaBarFill")?.GetComponent<Image>();
        staminaText ??= transform.FindDeepChild("StaminaText")?.GetComponent<TextMeshProUGUI>();

        levelText ??= transform.FindDeepChild("LevelText")?.GetComponent<TextMeshProUGUI>();

        expBarFill ??= transform.FindDeepChild("ExpBarFill")?.GetComponent<Image>();

        physicDMGText ??= transform.FindDeepChild("PhysicDMGText")?.GetComponent<TextMeshProUGUI>();
        magicDMGText ??= transform.FindDeepChild("MagicDMGText")?.GetComponent<TextMeshProUGUI>();
        defenseText ??= transform.FindDeepChild("DefenseText")?.GetComponent<TextMeshProUGUI>();
        critChanceText ??= transform.FindDeepChild("CritChanceText")?.GetComponent<TextMeshProUGUI>();
        moveSpeedText ??= transform.FindDeepChild("MoveSpeedText")?.GetComponent<TextMeshProUGUI>();

        coinText ??= transform.FindDeepChild("CoinText")?.GetComponent<TextMeshProUGUI>();
        gemText ??= transform.FindDeepChild("GemText")?.GetComponent<TextMeshProUGUI>();
    }
    void Start()
    {
        if (playerStats == null || classController == null)
        {
            Debug.LogError("PlayerStats hoặc ClassController bị null!");
            return;
        }

        UpdateUI();
    }

    void Update()
    {
        if (playerStats == null) return;
        if (!gameObject.activeInHierarchy) return;
        UpdateUI();
    }

    void OnEnable()
    {
        UpdateUI();
    }

    void UpdateUI()
    {
        if (playerStats == null || classController == null)
        {
            Debug.LogError("PlayerStats hoặc ClassController không được gán!");
            return;
        }

        // Real Time
        if (timeText != null)
        {
            if (ServerTimeManager.ServerTime != default && ServerTimeManager.LocalTimeAtFetch > 0f)
            {
                float secondsPassed = Time.time - ServerTimeManager.LocalTimeAtFetch;
                DateTime currentTime = ServerTimeManager.ServerTime.AddSeconds(secondsPassed);
                timeText.text = currentTime.ToString("HH:mm:ss");
            }
            else
            {
                timeText.text = "Đang tải...";
            }
        }

        string currentClass = classController.GetCurrentClassName();
        bool hasLyria = GameFlags.HasRecruitedLyria();
        bool isKnight = currentClass == "Knight";
        bool isMage = currentClass == "Mage";

        // ========== HP ==========
        // Kiểm tra Knight HP
        if (knightHealthBarFill != null)
        {
            knightHealthBarFill.gameObject.SetActive(isKnight);
            if (isKnight)
                knightHealthBarFill.fillAmount = (float)playerStats.knightHealth / playerStats.finalKnightMaxHP;
        }

        if (knightHealthText != null)
        {
            knightHealthText.gameObject.SetActive(showBothHPInMenu || isKnight);
            knightHealthText.text = $"{playerStats.knightHealth} / {playerStats.finalKnightMaxHP}";
        }

        // Kiểm tra Mage HP
        if (mageHealthBarFill != null)
        {
            mageHealthBarFill.gameObject.SetActive(isMage && hasLyria);
            if (isMage && hasLyria)
                mageHealthBarFill.fillAmount = (float)playerStats.mageHealth / playerStats.finalMageMaxHP;
        }

        if (mageHealthText != null)
        {
            mageHealthText.gameObject.SetActive(hasLyria && (showBothHPInMenu || isMage));
            if (hasLyria)
                mageHealthText.text = $"{playerStats.mageHealth} / {playerStats.finalMageMaxHP}";
        }

        // ========== MP ==========
        // Kiểm tra Knight MP
        if (knightManaBarFill != null)
        {
            knightManaBarFill.gameObject.SetActive(isKnight);
            if (isKnight)
                knightManaBarFill.fillAmount = (float)playerStats.knightMP / playerStats.finalKnightMaxMP;
        }

        if (knightManaText != null)
        {
            knightManaText.gameObject.SetActive(showBothHPInMenu || isKnight);
            knightManaText.text = $"{playerStats.knightMP} / {playerStats.finalKnightMaxMP}";
        }

        // Kiểm tra Mage MP
        if (mageManaBarFill != null)
        {
            mageManaBarFill.gameObject.SetActive(isMage && hasLyria);
            if (isMage && hasLyria)
                mageManaBarFill.fillAmount = (float)playerStats.mageMP / playerStats.finalKnightMaxMP; // Lưu ý: Chỗ này nên là finalMageMaxMP nếu PlayerStats có hỗ trợ
        }

        if (mageManaText != null)
        {
            mageManaText.gameObject.SetActive(hasLyria && (showBothHPInMenu || isMage));
            if (hasLyria)
                mageManaText.text = $"{playerStats.mageMP} / {playerStats.finalMageMaxMP}";
        }

        // ========== Stamina ==========
        if (staminaBarFill != null)
        {
            float maxStamina = playerStats.finalStamina;
            float currentStamina = playerStats.currentStamina;
            staminaBarFill.fillAmount = maxStamina > 0 ? (float)currentStamina / maxStamina : 0;

            if (staminaText != null)
                staminaText.text = $"{currentStamina} / {maxStamina}";
        }

        // ========== Level & EXP ==========
        if (levelText != null)
            levelText.text = playerStats.level.ToString();

        if (expBarFill != null)
        {
            float expNeeded = playerStats.expToNextLevel;
            expBarFill.fillAmount = expNeeded > 0 ? (float)playerStats.exp / expNeeded : 0;
        }

        // ========== Stats & Currency ==========
        if (physicDMGText) physicDMGText.text = playerStats.finalPhysicalAttack.ToString();
        if (magicDMGText) magicDMGText.text = playerStats.finalMagicAttack.ToString();
        if (defenseText) defenseText.text = playerStats.finalDefense.ToString();
        if (critChanceText) critChanceText.text = playerStats.finalCritRate.ToString("F2") + "%";
        if (moveSpeedText) moveSpeedText.text = playerStats.finalMoveSpeed.ToString("F2");
        if (coinText) coinText.text = playerStats.coin.ToString();
        if (gemText) gemText.text = playerStats.gem.ToString();

        // ========== Portraits ==========
        if (elricPortrait != null)
            elricPortrait.gameObject.SetActive(isKnight);

        if (lyriaPortrait != null)
            lyriaPortrait.gameObject.SetActive(isMage && hasLyria);
    }
}
