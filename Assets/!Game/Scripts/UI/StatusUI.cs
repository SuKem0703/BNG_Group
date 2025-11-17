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
        if (playerStats == null)
        {
            Debug.LogError("PlayerStats không được gán vào StatusUI!");
            return;
        }

        // Real Time
        if (timeText != null)
        {
            if (ServerTimeFetcher.ServerTime != default && ServerTimeFetcher.LocalTimeAtFetch > 0f)
            {
                float secondsPassed = Time.time - ServerTimeFetcher.LocalTimeAtFetch;
                DateTime currentTime = ServerTimeFetcher.ServerTime.AddSeconds(secondsPassed);
                timeText.text = currentTime.ToString("HH:mm:ss");
            }
            else
            {
                timeText.text = "Đang tải...";
            }
        }

        // ========== HP ==========
        string currentClass = classController.GetCurrentClassName();
        bool hasLyria = GameFlags.HasRecruitedLyria();

        // Fill logic — chỉ class hiện tại mới có fill
        knightHealthBarFill.gameObject.SetActive(currentClass == "Knight");
        mageHealthBarFill.gameObject.SetActive(currentClass == "Mage" && hasLyria);

        if (currentClass == "Knight")
            knightHealthBarFill.fillAmount = (float)playerStats.knightHealth / playerStats.finalKnightMaxHP;
        else if (currentClass == "Mage" && hasLyria)
            mageHealthBarFill.fillAmount = (float)playerStats.mageHealth / playerStats.finalMageMaxHP;

        // Text logic — cho phép xem chỉ số cả hai khi ở menu, nhưng ẩn Mage nếu chưa có
        if (showBothHPInMenu)
        {
            knightHealthText.gameObject.SetActive(true);
            mageHealthText.gameObject.SetActive(hasLyria);
        }
        else
        {
            knightHealthText.gameObject.SetActive(currentClass == "Knight");
            mageHealthText.gameObject.SetActive(currentClass == "Mage" && hasLyria);
        }

        knightHealthText.text = $"{playerStats.knightHealth} / {playerStats.finalKnightMaxHP}";
        if (hasLyria)
            mageHealthText.text = $"{playerStats.mageHealth} / {playerStats.finalMageMaxHP}";

        // ========== MP ==========
        knightManaBarFill.gameObject.SetActive(currentClass == "Knight");
        mageManaBarFill.gameObject.SetActive(currentClass == "Mage" && hasLyria);

        if (currentClass == "Knight")
            knightManaBarFill.fillAmount = (float)playerStats.knightMP / playerStats.finalKnightMaxMP;
        else if (currentClass == "Mage" && hasLyria)
            mageManaBarFill.fillAmount = (float)playerStats.mageMP / playerStats.finalMageMaxMP;

        if (showBothHPInMenu)
        {
            knightManaText.gameObject.SetActive(true);
            mageManaText.gameObject.SetActive(hasLyria);
        }
        else
        {
            knightManaText.gameObject.SetActive(currentClass == "Knight");
            mageManaText.gameObject.SetActive(currentClass == "Mage" && hasLyria);
        }

        knightManaText.text = $"{playerStats.knightMP} / {playerStats.finalKnightMaxMP}";
        if (hasLyria)
            mageManaText.text = $"{playerStats.mageMP} / {playerStats.finalMageMaxMP}";

        // ========== Stamina ==========
        if (staminaBarFill != null)
        {
            float maxStamina = playerStats.finalStamina;
            float currentStamina = playerStats.currentStamina;
            staminaBarFill.fillAmount = (float)currentStamina / maxStamina;
            if (staminaText != null)
                staminaText.text = $"{currentStamina} / {maxStamina}";
        }

        // Level
        if (levelText != null)
            levelText.text = playerStats.level.ToString();

        // EXP
        if (expBarFill != null)
            expBarFill.fillAmount = (float)playerStats.exp / playerStats.expToNextLevel;

        // Stats
        if (physicDMGText)
            physicDMGText.text = playerStats.finalPhysicalAttack.ToString();
        if (magicDMGText)
            magicDMGText.text = playerStats.finalMagicAttack.ToString();
        if (defenseText)
            defenseText.text = playerStats.finalDefense.ToString();
        if (critChanceText)
            critChanceText.text = playerStats.finalCritRate.ToString("F2") + "%";
        if (moveSpeedText)
            moveSpeedText.text = playerStats.finalMoveSpeed.ToString("F2");

        // Currency
        if (coinText)
            coinText.text = playerStats.coin.ToString();
        if (gemText)
            gemText.text = playerStats.gem.ToString();

        // Portraits
        if (elricPortrait != null && lyriaPortrait != null)
        {
            bool isKnight = currentClass == "Knight";
            bool isMage = currentClass == "Mage";

            elricPortrait.gameObject.SetActive(isKnight);
            lyriaPortrait.gameObject.SetActive(isMage && hasLyria);
        }
    }
}
