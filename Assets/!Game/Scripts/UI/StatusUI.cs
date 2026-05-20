using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StatusUI : MonoBehaviour
{
    private PlayerVitals playerVitals;
    private ClassController classController;
    private PlayerStats playerStats;

    [Header("Portrait")]
    [SerializeField] private Image portraitImage;
    [SerializeField] private Sprite elricSprite;
    [SerializeField] private Sprite lyriaSprite;

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

    // CÁC BIẾN CACHE ĐỂ TỐI ƯU HÓA
    private float lastTimeUpdate = 0f;
    private string lastClass = "";
    private int _kHP = -1, _kMaxHP = -1, _mHP = -1, _mMaxHP = -1;
    private int _kMP = -1, _kMaxMP = -1, _mMP = -1, _mMaxMP = -1;
    private float _stamina = -1f, _maxStamina = -1f;
    private int _level = -1;
    private int _coin = -1, _gem = -1;

    void Awake()
    {
        AssignInspector();
    }

    private void Start()
    {
        PlayerCore.OnPlayerSpawned += InitializeReference;
    }

    private void InitializeReference(PlayerCore core)
    {
        PlayerCore.OnPlayerSpawned -= InitializeReference;

        playerStats = core.playerStats;
        playerVitals = core.playerVitals;
        classController = core.classController;
    }

    private void OnDestroy()
    {
        PlayerCore.OnPlayerSpawned -= InitializeReference;
    }

    void AssignInspector()
    {
        portraitImage ??= transform.FindDeepChild("PortraitImage")?.GetComponent<Image>();
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

    private void TryFindPlayer()
    {
        if (PlayerStats.Instance != null)
        {
            if (playerStats == null) playerStats = PlayerStats.Instance;
            if (playerVitals == null) playerVitals = playerStats.GetComponent<PlayerVitals>();
            if (classController == null) classController = playerStats.GetComponent<ClassController>();
        }
    }

    void OnEnable()
    {
        ForceResetCache();
    }

    void Update()
    {
        if (!gameObject.activeInHierarchy) return;

        if (playerVitals == null || classController == null || playerStats == null)
        {
            TryFindPlayer();
            return;
        }

        UpdateContinuousUI();
    }

    private void UpdateContinuousUI()
    {
        if (timeText != null && Time.time - lastTimeUpdate >= 1f)
        {
            lastTimeUpdate = Time.time;
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
        bool isKnight = currentClass == "Knight";
        bool isMage = currentClass == "Mage";

        if (lastClass != currentClass)
        {
            lastClass = currentClass;
            UpdateClassVisibility(isKnight, isMage);
        }

        if (knightHealthBarFill != null && (isKnight == true))
            knightHealthBarFill.fillAmount = (float)playerVitals.netKnightHealth.Value / playerStats.finalKnightMaxHP;

        if (mageHealthBarFill != null && (isMage == true))
            mageHealthBarFill.fillAmount = (float)playerVitals.netMageHealth.Value / playerStats.finalMageMaxHP;

        if (knightManaBarFill != null && (isKnight == true))
            knightManaBarFill.fillAmount = (float)playerVitals.knightMP / playerStats.finalKnightMaxMP;

        if (mageManaBarFill != null && (isMage == true))
            mageManaBarFill.fillAmount = (float)playerVitals.mageMP / playerStats.finalMageMaxMP;

        float maxSt = playerStats.finalStamina;
        float curSt = playerVitals.currentStamina;
        if (staminaBarFill != null) staminaBarFill.fillAmount = maxSt > 0 ? curSt / maxSt : 0;

        float expNeeded = playerStats.expToNextLevel;
        if (expBarFill != null) expBarFill.fillAmount = expNeeded > 0 ? (float)playerStats.exp / expNeeded : 0;

        if (_kHP != playerVitals.netKnightHealth.Value || _kMaxHP != playerStats.finalKnightMaxHP)
        {
            _kHP = playerVitals.netKnightHealth.Value; _kMaxHP = playerStats.finalKnightMaxHP;
            if (knightHealthText != null) knightHealthText.text = $"{_kHP} / {playerStats.finalKnightMaxHP}";
        }

        if (_mHP != playerVitals.netMageHealth.Value || _mMaxHP != playerStats.finalMageMaxHP)
        {
            _mHP = playerVitals.netMageHealth.Value; _mMaxHP = playerStats.finalMageMaxHP;
            if (mageHealthText != null) mageHealthText.text = $"{_mHP} / {playerStats.finalMageMaxHP}";
        }

        if (_kMP != playerVitals.knightMP || _kMaxMP != playerStats.finalKnightMaxMP)
        {
            _kMP = playerVitals.knightMP; _kMaxMP = playerStats.finalKnightMaxMP;
            if (knightManaText != null) knightManaText.text = $"{_kMP} / {playerStats.finalKnightMaxMP}";
        }

        if (_mMP != playerVitals.mageMP || _mMaxMP != playerStats.finalMageMaxMP)
        {
            _mMP = playerVitals.mageMP; _mMaxMP = playerStats.finalMageMaxMP;
            if (mageManaText != null) mageManaText.text = $"{_mMP} / {playerStats.finalMageMaxMP}";
        }

        if (_stamina != curSt || _maxStamina != maxSt)
        {
            _stamina = curSt; _maxStamina = maxSt;
            if (staminaText != null) staminaText.text = $"{(int)_stamina} / {(int)_maxStamina}";
        }

        if (_level != playerStats.level)
        {
            _level = playerStats.level;
            if (levelText != null) levelText.text = _level.ToString();

            UpdateStaticStats();
        }

        if (PlayerWallet.Instance != null)
        {
            if (_coin != PlayerWallet.Instance.coin)
            {
                _coin = PlayerWallet.Instance.coin;
                if (coinText != null) coinText.text = _coin.ToString();
            }

            if (_gem != PlayerWallet.Instance.gem)
            {
                _gem = PlayerWallet.Instance.gem;
                if (gemText != null) gemText.text = _gem.ToString();
            }
        }
    }

    private void UpdateClassVisibility(bool isKnight, bool isMage)
    {
        bool showKnight = isKnight;
        bool showMage = isMage;

        if (knightHealthBarFill != null) knightHealthBarFill.gameObject.SetActive(showKnight);
        if (knightHealthText != null) knightHealthText.gameObject.SetActive(showKnight);
        if (knightManaBarFill != null) knightManaBarFill.gameObject.SetActive(showKnight);
        if (knightManaText != null) knightManaText.gameObject.SetActive(showKnight);

        if (mageHealthBarFill != null) mageHealthBarFill.gameObject.SetActive(showMage);
        if (mageHealthText != null) mageHealthText.gameObject.SetActive(showMage);
        if (mageManaBarFill != null) mageManaBarFill.gameObject.SetActive(showMage);
        if (mageManaText != null) mageManaText.gameObject.SetActive(showMage);

        if (portraitImage != null)
        {
            portraitImage.sprite = isKnight ? elricSprite : lyriaSprite;
        }
    }

    private void UpdateStaticStats()
    {
        if (physicDMGText) physicDMGText.text = playerStats.finalPhysicalAttack.ToString();
        if (magicDMGText) magicDMGText.text = playerStats.finalMagicAttack.ToString();
        if (defenseText) defenseText.text = playerStats.finalDefense.ToString();
        if (critChanceText) critChanceText.text = playerStats.finalCritRate.ToString("F2") + "%";
        if (moveSpeedText) moveSpeedText.text = playerStats.finalMoveSpeed.ToString("F2");
    }

    private void ForceResetCache()
    {
        _kHP = -1; _mHP = -1; _kMP = -1; _mMP = -1;
        _stamina = -1; _level = -1; _coin = -1; _gem = -1;
        lastClass = "";
    }
}