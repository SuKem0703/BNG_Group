using UnityEngine;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance { get; private set; }

    [Header("Base Potential Stats")]
    public int STR, DEX, CON, INT;

    [Header("Level & EXP")]
    public int level = 1;
    public int exp;
    public int potentialPoints;
    public int expToNextLevel
    {
        get
        {
            if (level < 100)
                return Mathf.FloorToInt(100 + level * 50 + Mathf.Pow(level, 2.2f));
            else if (100 <= level && level < 200)
                return Mathf.FloorToInt(100 + level * 80 + Mathf.Pow(level, 2.5f));
            else
                return Mathf.FloorToInt(100 + level * 100 + Mathf.Pow(level, 3f));
        }
    }

    //Base stats
    public int basePhysicalAttack => finalSTR * 2;
    public int baseMagicAttack => finalINT * 2;
    public int baseDefense => finalDEX * 1;
    public int baseMaxHP => 100 + finalCON * 10;
    public int baseMaxMP => 50 + finalINT * 5;
    public float baseStamina => 20;
    public float baseCritRate => Mathf.Min(finalSTR * 0.01f, 100f);
    public int baseHPRegen => 1 + finalCON / 5;
    public int baseMPRegen => 1 + finalINT / 5;
    public float baseStaminaRegen => 1f * (1f + finalDEX * 0.02f);
    public float baseMoveSpeed => 4f + finalDEX * 0.01f;

    //Stats từ trang bị
    private int bonusSTR;
    private int bonusDEX;
    private int bonusCON;
    private int bonusINT;
    public int bonusPhysicalAttack;
    public int bonusMagicAttack;
    public int bonusDefense;
    private int bonusKnightMaxHP;
    private int bonusMageMaxHP;
    private int bonusKnightMaxMP;
    private int bonusMageMaxMP;
    private int bonusHPRegen;
    private int bonusMPRegen;
    private float bonusCritRate;
    private float bonusMoveSpeed;
    private int bonusStaminaRegen;
    private int bonusStamina;

    [Header("Derived Final Stats")]
    public int finalSTR => STR + bonusSTR;
    public int finalDEX => DEX + bonusDEX;
    public int finalCON => CON + bonusCON;
    public int finalINT => INT + bonusINT;
    public int finalPhysicalAttack => Mathf.FloorToInt(basePhysicalAttack + bonusPhysicalAttack);
    public int finalMagicAttack => Mathf.FloorToInt(baseMagicAttack + bonusMagicAttack);
    public int finalDefense => baseDefense + bonusDefense;
    public float finalCritRate => Mathf.Min(baseCritRate + bonusCritRate, 100f);
    public int finalKnightMaxHP => Mathf.FloorToInt(baseMaxHP + bonusKnightMaxHP);
    public int finalMageMaxHP => Mathf.FloorToInt(baseMaxHP + bonusMageMaxHP);
    public int finalKnightMaxMP => Mathf.FloorToInt(baseMaxMP + bonusKnightMaxMP);
    public int finalMageMaxMP => Mathf.FloorToInt(baseMaxMP + bonusMageMaxMP);
    public float finalStamina => baseStamina + bonusStamina;
    public int finalHPRegen => baseHPRegen + bonusHPRegen;
    public int finalMPRegen => baseMPRegen + bonusMPRegen;
    public float finalStaminaRegen => baseStaminaRegen + bonusStaminaRegen;
    public float finalMoveSpeed => baseMoveSpeed + bonusMoveSpeed;
    public float damageReduction;

    [Header("Current Stats")]
    public int knightHealth;
    public int mageHealth;
    public int knightMP;
    public int mageMP;
    public float currentStamina;

    [Header("Regen")]
    public float healthRegenDelay = 5f;
    public float mpRegenDelay = 2f;
    public float staminaRegenDelay = 1.5f;

    public float healthRegenInterval = 2f;
    public float mpRegenInterval = 1f;
    public float staminaRegenInterval = 0.5f;

    private float healthRegenCooldown;
    private float mpRegenCooldown;
    private float staminaRegenCooldown;

    private float healthRegenTimer;
    private float mpRegenTimer;
    private float staminaRegenTimer;

    [Header("Currency")]
    public int coin;
    public int gem;

    public static bool IsOnBattle = false;

    // Biến kiểm tra điều kiện tấn công tổng hợp
    public bool CanAttack
    {
        get
        {
            if (MapController.Instance != null && MapController.Instance.IsSafeZone()) return false;

            ClassController classController = GetComponent<ClassController>();
            if (classController.knightObject.activeSelf)
            {
                return KnightEquipmentPanel.HasWeaponEquipped;
            }
            else
            {
                return MageEquipmentPanel.HasWeaponEquipped;
            }
        }
    }

    public Slot[] equipmentKnightSlots;
    public Slot[] equipmentMageSlots;

    public Slot[] shareEquipmentSlots;

    public static event System.Action<PlayerStats> OnInitialized;

    public bool isInvincible = false;

    public float potionCooldownDuration = 2.0f;
    [SerializeField] private float potionCooldownTimer;

    public CapsuleCollider2D playerCollider;

    // Singleton Init
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        Application.runInBackground = true;

        if (playerCollider == null) playerCollider = GetComponent<CapsuleCollider2D>();
    }

    // Singleton cleanup
    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // Init Logic
    void Start()
    {
        PauseController.SetPause(false);

        IsOnBattle = false;

        equipmentKnightSlots = FindSlotsByName("KnightSlotContainer");
        equipmentMageSlots = FindSlotsByName("MageSlotContainer");

        shareEquipmentSlots = FindSlotsByName("ShareSlotContainer");

        if (equipmentKnightSlots == null || equipmentMageSlots == null || shareEquipmentSlots == null)
        {
            Debug.LogError("Không tìm thấy Equipment Slot!");
        }
        else
        {
            ApplyEquippedItems();
        }

        OnInitialized?.Invoke(this);

        healthRegenCooldown = healthRegenDelay;
        mpRegenCooldown = mpRegenDelay;
        staminaRegenCooldown = staminaRegenDelay;
    }

    // Loop
    void Update()
    {
        if (PauseController.IsGamePause)
        {
            healthRegenCooldown = healthRegenDelay;
            mpRegenCooldown = mpRegenDelay;
            staminaRegenCooldown = staminaRegenDelay;
            return;
        }
        HandleRegen();

        if (potionCooldownTimer > 0)
        {
            potionCooldownTimer -= Time.unscaledDeltaTime;
        }
    }

    // Logic hồi phục theo thời gian
    void HandleRegen()
    {
        ClassController classController = GetComponent<ClassController>();
        bool isKnight = classController.knightObject.activeSelf;

        // Health Regen
        if (isKnight)
        {
            if (knightHealth < finalKnightMaxHP)
            {
                healthRegenTimer += Time.unscaledDeltaTime;
                if (healthRegenTimer >= healthRegenInterval)
                {
                    knightHealth += finalHPRegen;
                    knightHealth = Mathf.Min(knightHealth, finalKnightMaxHP);
                    healthRegenTimer = 0f;
                }
            }
        }
        else // Mage active
        {
            if (mageHealth < finalMageMaxHP)
            {
                healthRegenTimer += Time.unscaledDeltaTime;
                if (healthRegenTimer >= healthRegenInterval)
                {
                    mageHealth += finalHPRegen;
                    mageHealth = Mathf.Min(mageHealth, finalMageMaxHP);
                    healthRegenTimer = 0f;
                }
            }
        }

        // MP Regen
        if (isKnight)
        {
            if (knightMP < finalKnightMaxMP)
            {
                mpRegenTimer += Time.unscaledDeltaTime;

                if (mpRegenTimer >= mpRegenInterval)
                {
                    knightMP += finalMPRegen;
                    knightMP = Mathf.Min(knightMP, finalKnightMaxMP);
                    mpRegenTimer = 0f;
                }
            }
        }
        else // Mage
        {
            if (mageMP < finalMageMaxMP)
            {
                mpRegenTimer += Time.unscaledDeltaTime;
                if (mpRegenTimer >= mpRegenInterval)
                {
                    mageMP += finalMPRegen;
                    mageMP = Mathf.Min(mageMP, finalMageMaxMP);
                    mpRegenTimer = 0f;
                }
            }
        }

        if (currentStamina < finalStamina)
        {
            staminaRegenTimer += Time.unscaledDeltaTime;
            if (staminaRegenTimer >= staminaRegenInterval)
            {
                currentStamina += finalStaminaRegen;
                currentStamina = Mathf.Min(currentStamina, finalStamina);
                staminaRegenTimer = 0f;
            }
        }
    }

    // Tính chỉ số từ trang bị
    public void ApplyEquippedItems()
    {
        var classController = GetComponent<ClassController>();

        int cachedKnightFinalMaxHP = finalKnightMaxHP;
        int cachedKnightFinalMaxMP = finalKnightMaxMP;
        int cachedMageFinalMaxHP = finalMageMaxHP;
        int cachedMageFinalMaxMP = finalMageMaxMP;

        bonusSTR = bonusDEX = bonusCON = bonusINT = 0;
        bonusPhysicalAttack = 0;
        bonusMagicAttack = 0;
        bonusDefense = 0;
        bonusCritRate = 0;
        bonusKnightMaxHP = 0;
        bonusMageMaxHP = 0;
        bonusKnightMaxMP = 0;
        bonusMageMaxMP = 0;
        bonusHPRegen = 0;
        bonusMPRegen = 0;
        bonusMoveSpeed = 0;
        bonusStaminaRegen = 0;
        damageReduction = 0;

        if (classController.knightObject.activeSelf)
        {
            foreach (Slot slot in equipmentKnightSlots)
            {
                if (!slot.isEquipmentSlot || slot.currentItem == null) continue;
                Item item = slot.currentItem.GetComponent<Item>();
                if (item == null) continue;

                bonusSTR += item.bonusSTR;
                bonusDEX += item.bonusDEX;
                bonusCON += item.bonusCON;
                bonusINT += item.bonusINT;

                bonusPhysicalAttack += item.physDamageBonus;
                bonusMagicAttack += item.magicDamageBonus;
                bonusDefense += item.defenseBonus;
                bonusCritRate += item.critRateBonus;
                bonusKnightMaxHP += item.hpKnightBonus;
                bonusKnightMaxMP += item.mpKnightBonus;
                bonusHPRegen += item.hpRegenBonus;
                bonusMPRegen += item.mpRegenBonus;
                bonusMoveSpeed += item.moveSpeedBonus;
                bonusStaminaRegen += item.staminaRegenBonus;
                damageReduction += item.damageReduction;
            }

            bonusMageMaxHP = cachedMageFinalMaxHP - baseMaxHP;
            bonusMageMaxMP = cachedMageFinalMaxMP - baseMaxMP;
        }
        else // Mage active
        {
            foreach (Slot slot in equipmentMageSlots)
            {
                if (!slot.isEquipmentSlot || slot.currentItem == null) continue;
                Item item = slot.currentItem.GetComponent<Item>();
                if (item == null) continue;

                bonusSTR += item.bonusSTR;
                bonusDEX += item.bonusDEX;
                bonusCON += item.bonusCON;
                bonusINT += item.bonusINT;

                bonusPhysicalAttack += item.physDamageBonus;
                bonusMagicAttack += item.magicDamageBonus;
                bonusDefense += item.defenseBonus;
                bonusCritRate += item.critRateBonus;
                bonusMageMaxHP += item.hpMageBonus;
                bonusMageMaxMP += item.mpMageBonus;
                bonusHPRegen += item.hpRegenBonus;
                bonusMPRegen += item.mpRegenBonus;
                bonusMoveSpeed += item.moveSpeedBonus;
                bonusStaminaRegen += item.staminaRegenBonus;
                damageReduction += item.damageReduction;
            }

            bonusKnightMaxHP = cachedKnightFinalMaxHP - baseMaxHP;
            bonusKnightMaxMP = cachedKnightFinalMaxMP - baseMaxMP;
        }

        knightHealth = Mathf.Min(knightHealth, finalKnightMaxHP);
        knightMP = Mathf.Min(knightMP, finalKnightMaxMP);
        mageHealth = Mathf.Min(mageHealth, finalMageMaxHP);
        mageMP = Mathf.Min(mageMP, finalMageMaxMP);
    }

    // Tính chỉ số cho cả 2 class (dùng khi load game)
    public void ApplyAllClassEquippedItems()
    {
        var classController = GetComponent<ClassController>();
        bool wasKnightActive = classController.knightObject.activeSelf;

        classController.knightObject.SetActive(true);
        classController.mageObject.SetActive(false);
        ApplyEquippedItems();

        int savedKnightHP = finalKnightMaxHP;
        int savedKnightMP = finalKnightMaxMP;

        classController.knightObject.SetActive(false);
        classController.mageObject.SetActive(true);
        ApplyEquippedItems();

        int savedMageHP = finalMageMaxHP;
        int savedMageMP = finalMageMaxMP;

        classController.knightObject.SetActive(wasKnightActive);
        classController.mageObject.SetActive(!wasKnightActive);

        bonusKnightMaxHP = savedKnightHP - baseMaxHP;
        bonusKnightMaxMP = savedKnightMP - baseMaxMP;
        bonusMageMaxHP = savedMageHP - baseMaxHP;
        bonusMageMaxMP = savedMageMP - baseMaxMP;

        knightHealth = Mathf.Min(knightHealth, finalKnightMaxHP);
        knightMP = Mathf.Min(knightMP, finalKnightMaxMP);
        mageHealth = Mathf.Min(mageHealth, finalMageMaxHP);
        mageMP = Mathf.Min(mageMP, finalMageMaxMP);
    }

    // Làm mới full máu/mana
    public void RefreshStats()
    {
        knightHealth = finalKnightMaxHP;
        mageHealth = finalMageMaxHP;

        knightMP = finalKnightMaxMP;
        mageMP = finalMageMaxMP;

        currentStamina = finalStamina;
    }

    // Cộng EXP
    public void AddEXP(int amount)
    {
        exp += amount;

        while (exp >= expToNextLevel)
        {
            exp -= expToNextLevel;
            LevelUp();
        }
    }

    // Lên cấp
    private void LevelUp()
    {
        level++;
        potentialPoints += 5;

        SoundEffectManager.Play("LevelUp");

        Debug.Log($"Level Up! New level: {level}, EXP to next: {expToNextLevel}, Potential Points: {potentialPoints}");
    }

    // Nhận sát thương
    public void TakeDamage(int damage)
    {
        if (isInvincible) return;

        float mitigation = finalDefense / (finalDefense + 100f);
        float reductionFactor = (1f - mitigation) * (1f - damageReduction);
        int dmgTaken = Mathf.Max(Mathf.CeilToInt(damage * reductionFactor), 1);

        ClassController classController = GetComponent<ClassController>();
        bool isKnight = classController.knightObject.activeSelf;

        int maxHP = isKnight ? finalKnightMaxHP : finalMageMaxHP;
        int currentHP = isKnight ? knightHealth : mageHealth;

        currentHP -= dmgTaken;

        GameObject popupPrefab = LoadResourceManager.Instance.DamagePopupPrefab;
        if (popupPrefab != null)
        {
            Vector3 spawnPosition = transform.position + new Vector3(0, 1f, 0);
            GameObject popupGO = Instantiate(popupPrefab, spawnPosition, Quaternion.identity);
            DamagePopup popupScript = popupGO.GetComponent<DamagePopup>();
            if (popupScript != null) popupScript.Setup(damage, DamageSourceType.Enemy);
        }

        if (isKnight) knightHealth = currentHP;
        else mageHealth = currentHP;

        if (currentHP <= 0)
            HandleDeath(isKnight ? "Knight" : "Mage");
    }

    // Set trạng thái bất tử
    public void SetInvincible(bool value)
    {
        isInvincible = value;
    }

    // Xử lý khi chết
    private void HandleDeath(string who)
    {
        Debug.Log($"{who} has fallen!");

        ClassController classController = GetComponent<ClassController>();
        if (classController == null) return;

        bool knightAlive = knightHealth > 0;
        bool mageAlive = mageHealth > 0;
        bool hasLyria = GameFlags.HasRecruitedLyria();

        if (who == "Knight")
        {
            if (hasLyria && mageAlive)
            {
                classController.SwitchClass(classController.mageObject);
            }
            else
            {
                GameOver();
            }
        }
        else if (who == "Mage")
        {
            if (knightAlive)
            {
                classController.SwitchClass(classController.knightObject);
            }
            else
            {
                GameOver();
            }
        }
    }

    // Sử dụng MP
    public void UseMP(int amount, bool isKnight)
    {
        if (isKnight)
            knightMP = Mathf.Max(knightMP - amount, 0);
        else
            mageMP = Mathf.Max(mageMP - amount, 0);

        mpRegenCooldown = mpRegenDelay;
        mpRegenTimer = 0f;
    }

    // Hồi MP
    public void RecoverMP(int amount, bool isKnight)
    {
        if (isKnight)
            knightMP = Mathf.Min(knightMP + amount, finalKnightMaxMP);
        else
            mageMP = Mathf.Min(mageMP + amount, finalMageMaxMP);
    }

    // Sử dụng Stamina
    public void UseStamina(int amount)
    {
        currentStamina = Mathf.Max(currentStamina - amount, 0);
        staminaRegenCooldown = staminaRegenDelay;
        staminaRegenTimer = 0f;
    }

    // Hồi HP
    public void Heal(int amount, bool isKnight)
    {
        if (isKnight)
        {
            int current = knightHealth;
            knightHealth = Mathf.Min(knightHealth + amount, finalKnightMaxHP);
            int actualHeal = knightHealth - current;

            if (actualHeal > 0)
                ShowRecoveryPopup(actualHeal, DamageSourceType.Heal);
        }
        else
        {
            int current = mageHealth;
            mageHealth = Mathf.Min(mageHealth + amount, finalMageMaxHP);
            int actualHeal = mageHealth - current;

            if (actualHeal > 0)
                ShowRecoveryPopup(actualHeal, DamageSourceType.Heal);
        }
    }

    // Hồi Stamina
    public void RecoverStamina(int amount)
    {
        currentStamina = Mathf.Min(currentStamina + amount, finalStamina);
    }

    // Currency helpers
    public void AddCoin(int amount) => coin += amount;
    public void SpendCoin(int amount) => coin = Mathf.Max(coin - amount, 0);
    public void AddGem(int amount) => gem += amount;
    public void SpendGem(int amount) => gem = Mathf.Max(gem - amount, 0);

    // Tìm Slot UI theo tên
    Slot[] FindSlotsByName(string parentName)
    {
        var allTransforms = Object.FindObjectsByType<Transform>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        Transform parent = allTransforms.FirstOrDefault(t =>
            t.name == parentName &&
            t.gameObject.scene.isLoaded
        );

        if (parent == null)
        {
            Debug.LogWarning($"Không tìm thấy object '{parentName}' trong scene.");
            return new Slot[0];
        }

        return parent.GetComponentsInChildren<Slot>(includeInactive: true);
    }

    // Reset điểm tiềm năng
    public void ResetPotential()
    {
        int basePoints = 5;
        int pointsPerLevel = 5;

        int totalPoints = basePoints + (level - 1) * pointsPerLevel;

        STR = 0;
        DEX = 0;
        CON = 0;
        INT = 0;

        potentialPoints = totalPoints;
    }

    // Game Over Logic
    private void GameOver()
    {
        if (PauseController.IsGamePause) return;

        Debug.Log("💀 GAME OVER");

        PauseController.SetPause(true);

        GameObject gameOverUI = GameObject.Find("GameOverUI");
        if (gameOverUI != null)
        {
            gameOverUI.SetActive(true);
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("GameOverScene");
        }
    }

    // Hồi máu cho nhân vật đang active
    public void HealActiveCharacter(int amount)
    {
        ClassController classController = GetComponent<ClassController>();
        if (classController == null) return;

        bool isKnightActive = classController.knightObject.activeSelf;

        if (isKnightActive)
        {
            if (knightHealth >= finalKnightMaxHP) Heal(amount, false);
            else Heal(amount, true);
        }
        else
        {
            if (mageHealth >= finalMageMaxHP) Heal(amount, true);
            else Heal(amount, false);
        }
    }

    // Hồi MP cho nhân vật đang active
    public void RecoverMPActiveCharacter(int amount)
    {
        ClassController classController = GetComponent<ClassController>();
        if (classController == null) return;

        bool isKnightActive = classController.knightObject.activeSelf;

        if (isKnightActive)
        {
            if (knightMP >= finalKnightMaxMP) RecoverMP(amount, false);
            else RecoverMP(amount, true);
        }
        else
        {
            if (mageMP >= finalMageMaxMP) RecoverMP(amount, true);
            else RecoverMP(amount, false);
        }
    }

    // Potion Cooldown helpers
    public bool IsPotionOnCooldown()
    {
        return potionCooldownTimer > 0;
    }
    public void TriggerPotionCooldown()
    {
        potionCooldownTimer = potionCooldownDuration;
    }
    public float GetPotionCooldownRemaining()
    {
        return potionCooldownTimer;
    }

    // Kiểm tra có cần hồi máu không
    public bool CanHeal()
    {
        bool knightCanHeal = knightHealth < finalKnightMaxHP;
        bool mageCanHeal = mageHealth < finalMageMaxHP;
        return knightCanHeal || mageCanHeal;
    }

    // Kiểm tra có cần hồi MP không
    public bool CanRecoverMP()
    {
        bool knightCanRecover = knightMP < finalKnightMaxMP;
        bool mageCanRecover = mageMP < finalMageMaxMP;
        return knightCanRecover || mageCanRecover;
    }

    // Hiển thị Popup số damage/heal
    private void ShowRecoveryPopup(int amount, DamageSourceType type)
    {
        GameObject popupPrefab = LoadResourceManager.Instance.DamagePopupPrefab;

        if (popupPrefab == null || amount <= 0)
        {
            Debug.LogWarning("Cannot show recovery popup: Prefab is null or amount is <= 0");
            return;
        }

        var classController = GetComponent<ClassController>();
        Transform activeCharacterTransform = classController.knightObject.activeSelf
            ? classController.knightObject.transform
            : classController.mageObject.transform;

        Vector3 spawnPosition = activeCharacterTransform.position + new Vector3(0, 1.5f, 0);

        GameObject popupGO = Instantiate(popupPrefab, spawnPosition, Quaternion.identity);

        DamagePopup popupScript = popupGO.GetComponent<DamagePopup>();
        if (popupScript != null)
        {
            popupScript.Setup(amount, type);
        }
    }
}