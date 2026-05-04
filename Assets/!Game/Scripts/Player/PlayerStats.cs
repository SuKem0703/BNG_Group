using UnityEngine;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;

public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance { get; private set; }

    [Header("Base Stats (Read-Only from Server)")]
    public int STR { get; private set; }
    public int DEX { get; private set; }
    public int CON { get; private set; }
    public int INT { get; private set; }

    [Header("Level & EXP (Read-Only)")]
    public int level { get; private set; } = 1;
    public int exp { get; private set; }
    public int potentialPoints { get; private set; }
    public void SyncStatsFromServer(PlayerStatsService.ServerUserStat data)
    {
        this.level = data.level;
        this.exp = data.exp;
        this.potentialPoints = data.potentialPoints;

        this.STR = data.str;
        this.DEX = data.dex;
        this.INT = data.intStat;
        this.CON = data.con;

        ApplyEquippedItems();
    }

    private int effectSTR;
    private int effectDEX;
    private int effectINT;
    private int effectCON;

    public void ModifyEffectStat(string statType, int amount)
    {
        switch (statType)
        {
            case "STR": effectSTR += amount; break;
            case "DEX": effectDEX += amount; break;
            case "INT": effectINT += amount; break;
            case "CON": effectCON += amount; break;
        }
    }

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
    public int finalSTR => STR + bonusSTR + effectSTR;
    public int finalDEX => DEX + bonusDEX + effectDEX;
    public int finalCON => CON + bonusCON + effectCON;
    public int finalINT => INT + bonusINT + effectINT;
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
    public int coin { get; private set; }
    public int gem { get; private set; }
    public void SyncCurrency(int svCoin, int svGem)
    {
        coin = svCoin;
        gem = svGem;
    }

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

    public static event System.Action<PlayerStats> OnInitialized;

    public bool isInvincible = false;
    // Flag to prevent processing additional incoming damage while death handling is in progress
    private bool isProcessingDeath = false;
    public bool IsProcessingDeath => isProcessingDeath;

    public float potionCooldownDuration = 2.0f;
    [SerializeField] private float potionCooldownTimer;

    public CapsuleCollider2D playerCollider;

    public static event System.Action<Slot[], string> OnEquipmentUIReady;

    // Singleton Init
    private void Awake()
    {
        Application.runInBackground = true;

        if (playerCollider == null) playerCollider = GetComponent<CapsuleCollider2D>();

        // DontDestroyOnLoad(this.gameObject);

        NetworkObject netObj = GetComponent<NetworkObject>();
        if (netObj != null)
        {
            netObj.DestroyWithScene = true;
        }
    }

    // Singleton cleanup
    private void OnDestroy()
    {
        if (Instance == this) 
        {
            Instance = null;
            IsOnBattle = false;
            if (InventoryController.Instance != null)
                InventoryController.Instance.OnInventoryChanged -= OnInventoryUpdated;
        }
    }

    // Init Logic
    void Start()
    {
        NetworkObject netObj = GetComponent<NetworkObject>();
        IsOnBattle = false;

        if (netObj != null && netObj.IsOwner)
        {
            Instance = this;

            OnInitialized?.Invoke(this);

            if (InventoryController.Instance != null)
            {
                InventoryController.Instance.OnInventoryChanged += OnInventoryUpdated;
                ApplyEquippedItems();
            }
        }

        healthRegenCooldown = healthRegenDelay;
        mpRegenCooldown = mpRegenDelay;
        staminaRegenCooldown = staminaRegenDelay;
    }

    void Update()
    {
        NetworkObject netObj = GetComponent<NetworkObject>();

        if (netObj != null && !netObj.IsOwner) return;

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

    private void OnInventoryUpdated(List<InventorySaveData> inventoryData, int slotCount)
    {
        ApplyEquippedItems();
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
                currentStamina = (float)System.Math.Round(currentStamina, 2);
                currentStamina = Mathf.Min(currentStamina, finalStamina);
                staminaRegenTimer = 0f;
            }
        }
    }

    // Tính chỉ số từ trang bị
    public void ApplyEquippedItems()
    {
        if (InventoryController.Instance == null || ItemDictionary.Instance == null) return;

        var classController = GetComponent<ClassController>();
        if (classController == null) return;

        bool isKnightActive = true;

        if (classController.mageObject != null && classController.knightObject != null)
        {
            if (classController.mageObject.activeSelf && !classController.knightObject.activeSelf)
            {
                isKnightActive = false;
            }
        }

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

        var equippedData = InventoryController.Instance.GetInventoryItemsData()
            .Where(item => item.isEquipped)
            .GroupBy(item => item.itemID)
            .Select(group => group.First());

        foreach (var data in equippedData)
        {
            GameObject prefab = ItemDictionary.Instance.GetItemPrefab(data.itemID);
            if (prefab == null) continue;

            EquipmentItem equip = prefab.GetComponent<EquipmentItem>();
            if (equip == null) continue;

            float svQuality = data.qualityFactor;

            if (equip.classRestriction == ClassRestriction.Knight || equip.classRestriction == ClassRestriction.None)
            {
                bonusKnightMaxHP += Mathf.RoundToInt(equip.hpKnightBonus * svQuality);
                bonusKnightMaxMP += Mathf.RoundToInt(equip.mpKnightBonus * svQuality);
            }

            if (equip.classRestriction == ClassRestriction.Mage || equip.classRestriction == ClassRestriction.None)
            {
                bonusMageMaxHP += Mathf.RoundToInt(equip.hpMageBonus * svQuality);
                bonusMageMaxMP += Mathf.RoundToInt(equip.mpMageBonus * svQuality);
            }

            bool isMatchingActiveStatus = (equip.classRestriction == ClassRestriction.None) ||
                                          (isKnightActive && equip.classRestriction == ClassRestriction.Knight) ||
                                          (!isKnightActive && equip.classRestriction == ClassRestriction.Mage);

            if (isMatchingActiveStatus)
            {
                bonusSTR += Mathf.RoundToInt(equip.bonusSTR * svQuality);
                bonusDEX += Mathf.RoundToInt(equip.bonusDEX * svQuality);
                bonusCON += Mathf.RoundToInt(equip.bonusCON * svQuality);
                bonusINT += Mathf.RoundToInt(equip.bonusINT * svQuality);

                bonusPhysicalAttack += Mathf.RoundToInt(equip.physDamageBonus * svQuality);
                bonusMagicAttack += Mathf.RoundToInt(equip.magicDamageBonus * svQuality);
                bonusDefense += Mathf.RoundToInt(equip.defenseBonus * svQuality);
                bonusHPRegen += Mathf.RoundToInt(equip.hpRegenBonus * svQuality);
                bonusMPRegen += Mathf.RoundToInt(equip.mpRegenBonus * svQuality);
                bonusStaminaRegen += Mathf.RoundToInt(equip.staminaRegenBonus * svQuality);

                bonusCritRate += equip.critRateBonus * svQuality;
                bonusMoveSpeed += equip.moveSpeedBonus * svQuality;

                damageReduction += equip.damageReduction * svQuality;
            }
        }

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

    // --- LOGIC CỘNG EXP, TIỀN VÀ TIÊU TIỀN (RMI) ---
    [Header("Network Optimization")]
    private int pendingExpToAdd = 0;
    private Coroutine expBatchCoroutine;
    private float expDebounceTime = 1.0f;

    // Cộng EXP
    public void AddEXP(int amount)
    {
        exp += amount;

        pendingExpToAdd += amount;

        if (expBatchCoroutine != null) StopCoroutine(expBatchCoroutine);
        expBatchCoroutine = StartCoroutine(SendExpBatchRoutine());
    }

    public void ForceSyncExpImmediate()
    {
        if (expBatchCoroutine != null) StopCoroutine(expBatchCoroutine);

        if (pendingExpToAdd != 0)
        {
            Debug.Log($"[Network] Force Sync EXP: {pendingExpToAdd}");

            if (PlayerStatsService.Instance != null)
            {
                PlayerStatsService.Instance.AddExp(pendingExpToAdd);
            }

            pendingExpToAdd = 0;
        }
    }

    public void PlayLevelUpEffect()
    {
        SoundEffectManager.Play("LevelUp");

        // Hiệu ứng particle, text bay lên...
        GameObject popupPrefab = LoadResourceManager.Instance.DamagePopupPrefab;
        if (popupPrefab != null)
        {
            // Show text "LEVEL UP!"
        }

        Debug.Log($"Level Up! New stats synced from Server.");
    }

    private IEnumerator SendExpBatchRoutine()
    {
        yield return new WaitForSeconds(expDebounceTime);

        if (pendingExpToAdd > 0)
        {
            int amountToSend = pendingExpToAdd;
            pendingExpToAdd = 0;

            Debug.Log($"[Network] Sending BATCH EXP request: {amountToSend}");

            if (PlayerStatsService.Instance != null)
            {
                PlayerStatsService.Instance.AddExp(amountToSend);
            }
        }
    }

    // Nhận sát thương
    public int TakeDamage(int damage)
    {
        // Ignore damage if player is currently invincible or death flow is processing
        if (isInvincible || isProcessingDeath) return 0;

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
            if (popupScript != null) popupScript.Setup(dmgTaken, DamageSourceType.Enemy);
        }

        if (isKnight) knightHealth = currentHP;
        else mageHealth = currentHP;

        if (currentHP <= 0)
        {
            // Mark we are processing death to avoid further hits interrupting the flow
            isProcessingDeath = true;
            HandleDeath(isKnight ? "Knight" : "Mage");
        }

        return dmgTaken;
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

        // Ensure we don't accept damage while we decide next steps (switch class or game over)
        isProcessingDeath = true;

        bool knightAlive = knightHealth > 0;
        bool mageAlive = mageHealth > 0;
        bool hasLyria = GameFlags.HasRecruitedLyria();

        if (who == "Knight")
        {
            if (hasLyria && mageAlive)
            {
                classController.SwitchClass(classController.mageObject);
                // After switching class, allow damage processing again for the newly active class
                isProcessingDeath = false;
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
                // After switching class, allow damage processing again for the newly active class
                isProcessingDeath = false;
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
    public void UseStamina(float amount)
    {
        if (amount <= 0) return;

        currentStamina = Mathf.Max(currentStamina - amount, 0);
        currentStamina = (float)System.Math.Round(currentStamina, 2);
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
        currentStamina = (float)System.Math.Round(currentStamina, 2);
    }

    // Currency helpers
    public void AddCoin(int amount) => coin += amount;
    public void AddGem(int amount) => gem += amount;
    public void SyncCoinFromServer(int serverCoin)
    {
        coin = serverCoin;
        // Trigger event update UI ở đây nếu cần
        // OnCurrencyChanged?.Invoke();
    }

    public void SyncGemFromServer(int serverGem)
    {
        gem = serverGem;
    }

    // Called after scene load / respawn to finalize respawn protections
    public IEnumerator FinalizeRespawnProtection(float invincibilityDuration = 0.5f)
    {
        // Ensure player cannot take damage immediately after respawn
        SetInvincible(true);
        if (playerCollider != null) playerCollider.enabled = false;

        yield return new WaitForSeconds(invincibilityDuration);

        SetInvincible(false);
        if (playerCollider != null) playerCollider.enabled = true;
        // Allow damage processing again after respawn protection
        isProcessingDeath = false;
    }

    // --- LOGIC TIÊU TIỀN (RMI) ---
    public void RequestSpendCoin(int amount, string reason, System.Action onSuccess, System.Action onFail)
    {
        // Check sơ bộ ở client để đỡ tốn request nếu rõ ràng là không đủ
        if (coin < amount)
        {
            Debug.Log("Client check: Không đủ tiền!");
            onFail?.Invoke();
            return;
        }

        // Gọi RMI lên Server
        EconomyService.Instance.SpendCurrency("Coin", amount, reason, (isSuccess) =>
        {
            if (isSuccess) onSuccess?.Invoke();
            else onFail?.Invoke();
        });
    }

    public void RequestSpendGem(int amount, string reason, System.Action onSuccess, System.Action onFail)
    {
        EconomyService.Instance.SpendCurrency("Gem", amount, reason, (isSuccess) =>
        {
            if (isSuccess) onSuccess?.Invoke();
            else onFail?.Invoke();
        });
    }

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

        DeathService.Instance.HandlePlayerDeath();

        //Debug.Log("💀 GAME OVER");

        //PauseController.SetPause(true);

        //GameObject gameOverUI = GameObject.Find("GameOverUI");
        //if (gameOverUI != null)
        //{
        //    gameOverUI.SetActive(true);
        //}
        //else
        //{
        //    UnityEngine.SceneManagement.SceneManager.LoadScene("GameOverScene");
        //}
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

    private float RoundToTwoDecimal(float value)
    {
        return (float)System.Math.Round(value, 2);
    }
}