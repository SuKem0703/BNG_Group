using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerStats : NetworkBehaviour
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
    private float _damageReduction;

    [Header("Player Identity")]
    public NetworkVariable<FixedString32Bytes> netUsername = new NetworkVariable<FixedString32Bytes>("", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public NetworkVariable<int> netKnightHealth = new NetworkVariable<int>(100, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<int> netMageHealth = new NetworkVariable<int>(100, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<int> netMaxKnightHP = new NetworkVariable<int>(100, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<int> netMaxMageHP = new NetworkVariable<int>(100, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    [Header("Derived Final Stats")]
    public int finalSTR => STR + bonusSTR + effectSTR;
    public int finalDEX => DEX + bonusDEX + effectDEX;
    public int finalCON => CON + bonusCON + effectCON;
    public int finalINT => INT + bonusINT + effectINT;
    public int finalPhysicalAttack => Mathf.FloorToInt(basePhysicalAttack + bonusPhysicalAttack);
    public int finalMagicAttack => Mathf.FloorToInt(baseMagicAttack + bonusMagicAttack);
    public float finalCritRate => Mathf.Min(baseCritRate + bonusCritRate, 100f);
    public float finalStamina => baseStamina + bonusStamina;
    public int finalHPRegen => baseHPRegen + bonusHPRegen;
    public int finalMPRegen => baseMPRegen + bonusMPRegen;
    public float finalStaminaRegen => baseStaminaRegen + bonusStaminaRegen;
    public float finalMoveSpeed => baseMoveSpeed + bonusMoveSpeed;

    public int finalKnightMaxHP => IsOwner ? Mathf.FloorToInt(baseMaxHP + bonusKnightMaxHP) : netMaxKnightHP.Value;
    public int finalMageMaxHP => IsOwner ? Mathf.FloorToInt(baseMaxHP + bonusMageMaxHP) : netMaxMageHP.Value;
    public int finalKnightMaxMP => Mathf.FloorToInt(baseMaxMP + bonusKnightMaxMP);
    public int finalMageMaxMP => Mathf.FloorToInt(baseMaxMP + bonusMageMaxMP);
    public int finalDefense => baseDefense + bonusDefense;
    public float damageReduction => _damageReduction;

    [Header("Current Stats (Local Except HP)")]
    public int knightHealth { get => netKnightHealth.Value; set { if (IsOwner) netKnightHealth.Value = value; } }
    public int mageHealth { get => netMageHealth.Value; set { if (IsOwner) netMageHealth.Value = value; } }

    public int knightMP { get; set; } = 50;
    public int mageMP { get; set; } = 50;
    public float currentStamina { get; set; } = 20f;

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

    public NetworkVariable<bool> netIsOnBattle = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private int serverAggroCount = 0;

    public void ChangeAggro(int amount)
    {
        if (!IsServer) return;

        serverAggroCount += amount;
        if (serverAggroCount < 0) serverAggroCount = 0;

        netIsOnBattle.Value = serverAggroCount > 0;
    }

    public bool CanAttack
    {
        get
        {
            if (MapController.Instance != null && MapController.Instance.IsSafeZone()) return false;

            ClassController classController = GetComponent<ClassController>();
            if (classController.knightObject.activeSelf) return KnightEquipmentPanel.HasWeaponEquipped;
            else return MageEquipmentPanel.HasWeaponEquipped;
        }
    }

    public static event System.Action<PlayerStats> OnInitialized;

    public bool isInvincible = false;
    private bool isProcessingDeath = false;
    public bool IsProcessingDeath => isProcessingDeath;

    public float potionCooldownDuration = 2.0f;
    [SerializeField] private float potionCooldownTimer;

    public CapsuleCollider2D playerCollider;

    public static event System.Action<Slot[], string> OnEquipmentUIReady;

    private void Awake()
    {
        Application.runInBackground = true;

        if (playerCollider == null) playerCollider = GetComponent<CapsuleCollider2D>();

        NetworkObject netObj = GetComponent<NetworkObject>();
        if (netObj != null)
        {
            netObj.DestroyWithScene = true;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;

            if (InventoryController.Instance != null)
                InventoryController.Instance.OnInventoryChanged -= OnInventoryUpdated;
        }
    }

    void Start()
    {
        NetworkObject netObj = GetComponent<NetworkObject>();

        if (netObj != null && netObj.IsOwner)
        {
            Instance = this;

            string savedName = PlayerPrefs.GetString("Username", "Unknown Player");
            netUsername.Value = savedName;

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
        if (!IsOwner) return;

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

    void HandleRegen()
    {
        ClassController classController = GetComponent<ClassController>();
        bool isKnight = classController.knightObject.activeSelf;

        if (isKnight)
        {
            if (knightHealth < finalKnightMaxHP)
            {
                healthRegenTimer += Time.unscaledDeltaTime;
                if (healthRegenTimer >= healthRegenInterval)
                {
                    knightHealth = Mathf.Min(knightHealth + finalHPRegen, finalKnightMaxHP);
                    healthRegenTimer = 0f;
                }
            }
        }
        else
        {
            if (mageHealth < finalMageMaxHP)
            {
                healthRegenTimer += Time.unscaledDeltaTime;
                if (healthRegenTimer >= healthRegenInterval)
                {
                    mageHealth = Mathf.Min(mageHealth + finalHPRegen, finalMageMaxHP);
                    healthRegenTimer = 0f;
                }
            }
        }

        if (isKnight)
        {
            if (knightMP < finalKnightMaxMP)
            {
                mpRegenTimer += Time.unscaledDeltaTime;
                if (mpRegenTimer >= mpRegenInterval)
                {
                    knightMP = Mathf.Min(knightMP + finalMPRegen, finalKnightMaxMP);
                    mpRegenTimer = 0f;
                }
            }
        }
        else
        {
            if (mageMP < finalMageMaxMP)
            {
                mpRegenTimer += Time.unscaledDeltaTime;
                if (mpRegenTimer >= mpRegenInterval)
                {
                    mageMP = Mathf.Min(mageMP + finalMPRegen, finalMageMaxMP);
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

    public void ApplyEquippedItems()
    {
        if (InventoryController.Instance == null || ItemDictionary.Instance == null) return;

        var classController = GetComponent<ClassController>();
        if (classController == null) return;

        bool isKnightActive = classController.knightObject.activeSelf;

        bonusSTR = bonusDEX = bonusCON = bonusINT = 0;
        bonusPhysicalAttack = bonusMagicAttack = bonusDefense = 0;
        bonusCritRate = bonusKnightMaxHP = bonusMageMaxHP = 0;
        bonusKnightMaxMP = bonusMageMaxMP = bonusHPRegen = bonusMPRegen = 0;
        bonusMoveSpeed = bonusStaminaRegen = 0;
        _damageReduction = 0;

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

                _damageReduction += equip.damageReduction * svQuality;
            }
        }

        knightHealth = Mathf.Min(knightHealth, finalKnightMaxHP);
        knightMP = Mathf.Min(knightMP, finalKnightMaxMP);
        mageHealth = Mathf.Min(mageHealth, finalMageMaxHP);
        mageMP = Mathf.Min(mageMP, finalMageMaxMP);

        if (IsOwner)
        {
            netMaxKnightHP.Value = finalKnightMaxHP;
            netMaxMageHP.Value = finalMageMaxHP;
        }
    }

    public void RefreshStats()
    {
        knightHealth = finalKnightMaxHP;
        mageHealth = finalMageMaxHP;
        knightMP = finalKnightMaxMP;
        mageMP = finalMageMaxMP;
        currentStamina = finalStamina;
    }

    [Header("Network Optimization")]
    private int pendingExpToAdd = 0;
    private Coroutine expBatchCoroutine;
    private float expDebounceTime = 1.0f;

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
            if (PlayerStatsService.Instance != null) PlayerStatsService.Instance.AddExp(pendingExpToAdd);
            pendingExpToAdd = 0;
        }
    }

    public void PlayLevelUpEffect()
    {
        SoundEffectManager.Play("LevelUp");
    }

    private IEnumerator SendExpBatchRoutine()
    {
        yield return new WaitForSeconds(expDebounceTime);
        if (pendingExpToAdd > 0)
        {
            int amountToSend = pendingExpToAdd;
            pendingExpToAdd = 0;
            if (PlayerStatsService.Instance != null) PlayerStatsService.Instance.AddExp(amountToSend);
        }
    }

    public int TakeDamage(int rawDamage)
    {
        if (isInvincible || isProcessingDeath) return 0;

        if (IsServer && !IsOwner)
        {
            TakeDamageClientRpc(rawDamage);
            return rawDamage;
        }

        return ProcessDamageLocally(rawDamage);
    }

    [ClientRpc]
    private void TakeDamageClientRpc(int rawDamage)
    {
        if (IsOwner)
        {
            ProcessDamageLocally(rawDamage);
        }
    }

    private int ProcessDamageLocally(int rawDamage)
    {
        if (isInvincible || isProcessingDeath) return 0;

        float def = finalDefense;
        float dmgRed = damageReduction;

        float mitigation = def / (def + 100f);
        float reductionFactor = (1f - mitigation) * (1f - dmgRed);
        int finalDamage = Mathf.Max(Mathf.CeilToInt(rawDamage * reductionFactor), 1);

        ClassController classController = GetComponent<ClassController>();
        bool isKnight = classController.knightObject.activeSelf;

        int currentHP = isKnight ? knightHealth : mageHealth;
        currentHP -= finalDamage;

        GameObject popupPrefab = LoadResourceManager.Instance.DamagePopupPrefab;
        if (popupPrefab != null)
        {
            Vector3 spawnPosition = transform.position + new Vector3(0, 1f, 0);
            GameObject popupGO = Instantiate(popupPrefab, spawnPosition, Quaternion.identity);
            DamagePopup popupScript = popupGO.GetComponent<DamagePopup>();
            if (popupScript != null) popupScript.Setup(finalDamage, DamageSourceType.Enemy);
        }

        if (isKnight) knightHealth = currentHP;
        else mageHealth = currentHP;

        if (currentHP <= 0)
        {
            isProcessingDeath = true;
            HandleDeath(isKnight ? "Knight" : "Mage");
        }

        return finalDamage;
    }

    public void SetInvincible(bool value) => isInvincible = value;

    private void HandleDeath(string who)
    {
        Debug.Log($"{who} has fallen!");

        ClassController classController = GetComponent<ClassController>();
        if (classController == null) return;

        isProcessingDeath = true;

        bool knightAlive = knightHealth > 0;
        bool mageAlive = mageHealth > 0;
        bool hasLyria = GameFlags.HasRecruitedLyria();

        if (who == "Knight")
        {
            if (hasLyria && mageAlive)
            {
                classController.SwitchClass(classController.mageObject);
                isProcessingDeath = false;
            }
            else GameOver();
        }
        else if (who == "Mage")
        {
            if (knightAlive)
            {
                classController.SwitchClass(classController.knightObject);
                isProcessingDeath = false;
            }
            else GameOver();
        }
    }

    public void UseMP(int amount, bool isKnight)
    {
        if (isKnight) knightMP = Mathf.Max(knightMP - amount, 0);
        else mageMP = Mathf.Max(mageMP - amount, 0);

        mpRegenCooldown = mpRegenDelay;
        mpRegenTimer = 0f;
    }

    public void RecoverMP(int amount, bool isKnight)
    {
        if (isKnight) knightMP = Mathf.Min(knightMP + amount, finalKnightMaxMP);
        else mageMP = Mathf.Min(mageMP + amount, finalMageMaxMP);
    }

    public void UseStamina(float amount)
    {
        if (amount <= 0) return;
        currentStamina = Mathf.Max(currentStamina - amount, 0);
        currentStamina = (float)System.Math.Round(currentStamina, 2);
        staminaRegenCooldown = staminaRegenDelay;
        staminaRegenTimer = 0f;
    }

    public void Heal(int amount, bool isKnight)
    {
        if (isKnight)
        {
            int current = knightHealth;
            knightHealth = Mathf.Min(knightHealth + amount, finalKnightMaxHP);
            int actualHeal = knightHealth - current;
            if (actualHeal > 0) ShowRecoveryPopup(actualHeal, DamageSourceType.Heal);
        }
        else
        {
            int current = mageHealth;
            mageHealth = Mathf.Min(mageHealth + amount, finalMageMaxHP);
            int actualHeal = mageHealth - current;
            if (actualHeal > 0) ShowRecoveryPopup(actualHeal, DamageSourceType.Heal);
        }
    }

    public void RecoverStamina(int amount)
    {
        currentStamina = Mathf.Min(currentStamina + amount, finalStamina);
        currentStamina = (float)System.Math.Round(currentStamina, 2);
    }

    public void AddCoin(int amount) => coin += amount;
    public void AddGem(int amount) => gem += amount;
    public void SyncCoinFromServer(int serverCoin) => coin = serverCoin;
    public void SyncGemFromServer(int serverGem) => gem = serverGem;

    public IEnumerator FinalizeRespawnProtection(float invincibilityDuration = 0.5f)
    {
        SetInvincible(true);
        if (playerCollider != null) playerCollider.enabled = false;
        yield return new WaitForSeconds(invincibilityDuration);
        SetInvincible(false);
        if (playerCollider != null) playerCollider.enabled = true;
        isProcessingDeath = false;
    }

    public void RequestSpendCoin(int amount, string reason, System.Action onSuccess, System.Action onFail)
    {
        if (coin < amount) { onFail?.Invoke(); return; }
        EconomyService.Instance.SpendCurrency("Coin", amount, reason, (isSuccess) => { if (isSuccess) onSuccess?.Invoke(); else onFail?.Invoke(); });
    }

    public void RequestSpendGem(int amount, string reason, System.Action onSuccess, System.Action onFail)
    {
        EconomyService.Instance.SpendCurrency("Gem", amount, reason, (isSuccess) => { if (isSuccess) onSuccess?.Invoke(); else onFail?.Invoke(); });
    }

    public void ResetPotential()
    {
        int basePoints = 5;
        int pointsPerLevel = 5;
        int totalPoints = basePoints + (level - 1) * pointsPerLevel;
        STR = 0; DEX = 0; CON = 0; INT = 0;
        potentialPoints = totalPoints;
    }

    private void GameOver()
    {
        if (PauseController.IsGamePause) return;
        DeathService.Instance.HandlePlayerDeath();
    }

    public void HealActiveCharacter(int amount)
    {
        ClassController classController = GetComponent<ClassController>();
        if (classController == null) return;
        if (classController.knightObject.activeSelf)
        {
            if (knightHealth >= finalKnightMaxHP) Heal(amount, false); else Heal(amount, true);
        }
        else
        {
            if (mageHealth >= finalMageMaxHP) Heal(amount, true); else Heal(amount, false);
        }
    }

    public void RecoverMPActiveCharacter(int amount)
    {
        ClassController classController = GetComponent<ClassController>();
        if (classController == null) return;
        if (classController.knightObject.activeSelf)
        {
            if (knightMP >= finalKnightMaxMP) RecoverMP(amount, false); else RecoverMP(amount, true);
        }
        else
        {
            if (mageMP >= finalMageMaxMP) RecoverMP(amount, true); else RecoverMP(amount, false);
        }
    }

    public bool IsPotionOnCooldown() => potionCooldownTimer > 0;
    public void TriggerPotionCooldown() => potionCooldownTimer = potionCooldownDuration;
    public float GetPotionCooldownRemaining() => potionCooldownTimer;

    public bool CanHeal() => (knightHealth < finalKnightMaxHP) || (mageHealth < finalMageMaxHP);
    public bool CanRecoverMP() => (knightMP < finalKnightMaxMP) || (mageMP < finalMageMaxMP);

    private void ShowRecoveryPopup(int amount, DamageSourceType type)
    {
        GameObject popupPrefab = LoadResourceManager.Instance.DamagePopupPrefab;
        if (popupPrefab == null || amount <= 0) return;

        var classController = GetComponent<ClassController>();
        Transform activeCharacterTransform = classController.knightObject.activeSelf
            ? classController.knightObject.transform : classController.mageObject.transform;

        Vector3 spawnPosition = activeCharacterTransform.position + new Vector3(0, 1.5f, 0);
        GameObject popupGO = Instantiate(popupPrefab, spawnPosition, Quaternion.identity);
        DamagePopup popupScript = popupGO.GetComponent<DamagePopup>();
        if (popupScript != null) popupScript.Setup(amount, type);
    }
}