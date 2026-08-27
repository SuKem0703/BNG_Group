using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerStats : NetworkBehaviour
{
    public static PlayerStats Instance { get; private set; }

    public event Action OnStatsUpdated;
    public static event Action<Slot[], string> OnEquipmentUIReady;

    [Header("Base Stats (Read-Only from Server)")]
    public int STR { get; private set; }
    public int DEX { get; private set; }
    public int CON { get; private set; }
    public int INT { get; private set; }

    [Header("Level & EXP (Read-Only)")]
    public int level { get; private set; } = 1;
    public int exp { get; private set; }
    public int potentialPoints { get; private set; }

    [Header("Player Identity")]
    public NetworkVariable<FixedString32Bytes> netUsername = new NetworkVariable<FixedString32Bytes>("", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    [Header("Combat States")]
    public NetworkVariable<bool> netIsOnBattle = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private int serverAggroCount = 0;

    private int effectSTR;
    private int effectDEX;
    private int effectINT;
    private int effectCON;

    private int bonusSTR, bonusDEX, bonusCON, bonusINT;
    public int bonusPhysicalAttack, bonusMagicAttack, bonusDefense;
    private int bonusKnightMaxHP, bonusMageMaxHP;
    private int bonusKnightMaxMP, bonusMageMaxMP;
    private int bonusHPRegen, bonusMPRegen, bonusStaminaRegen;
    private float bonusCritRate, bonusMoveSpeed;
    private int bonusStamina;
    private float _damageReduction;

    [Header("Cooldowns")]
    public float potionCooldownDuration = 2.0f;
    [SerializeField] private float potionCooldownTimer;

    public CapsuleCollider2D playerCollider;
    private ClassController classController => GetComponent<ClassController>();

    public int expToNextLevel
    {
        get
        {
            if (level < 100) return Mathf.FloorToInt(100 + level * 50 + Mathf.Pow(level, 2.2f));
            else if (100 <= level && level < 200) return Mathf.FloorToInt(100 + level * 80 + Mathf.Pow(level, 2.5f));
            else return Mathf.FloorToInt(100 + level * 100 + Mathf.Pow(level, 3f));
        }
    }

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

    public int finalKnightMaxHP => Mathf.FloorToInt(baseMaxHP + bonusKnightMaxHP);
    public int finalMageMaxHP => Mathf.FloorToInt(baseMaxHP + bonusMageMaxHP);
    public int finalKnightMaxMP => Mathf.FloorToInt(baseMaxMP + bonusKnightMaxMP);
    public int finalMageMaxMP => Mathf.FloorToInt(baseMaxMP + bonusMageMaxMP);
    public int finalDefense => baseDefense + bonusDefense;
    public float damageReduction => _damageReduction;

    private void Awake()
    {
        Application.runInBackground = true;
        if (playerCollider == null) playerCollider = GetComponent<CapsuleCollider2D>();

        NetworkObject netObj = GetComponent<NetworkObject>();
        if (netObj != null) netObj.DestroyWithScene = true;
    }

    void Start()
    {
        NetworkObject netObj = GetComponent<NetworkObject>();
        if (netObj != null && netObj.IsOwner)
        {
            Instance = this;
            netUsername.Value = PlayerPrefs.GetString("Username", "Unknown Player");

            if (InventoryController.Instance != null)
            {
                InventoryController.Instance.OnInventoryChanged += OnInventoryUpdated;
                ApplyEquippedItems();
            }
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

    void Update()
    {
        if (!IsOwner) return;

        if (potionCooldownTimer > 0)
        {
            potionCooldownTimer -= Time.unscaledDeltaTime;
        }
    }

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

    private void OnInventoryUpdated(List<InventorySaveData> inventoryData, int slotCount)
    {
        ApplyEquippedItems();
    }

    public void ApplyEquippedItems()
    {
        if (InventoryController.Instance == null || ItemDictionary.Instance == null) return;

        bool isKnightActive = classController != null && classController.IsKnightActive;

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

        OnStatsUpdated?.Invoke();
    }

    public void ModifyEffectStat(string statType, int amount)
    {
        switch (statType)
        {
            case "STR": effectSTR += amount; break;
            case "DEX": effectDEX += amount; break;
            case "INT": effectINT += amount; break;
            case "CON": effectCON += amount; break;
        }
        OnStatsUpdated?.Invoke();
    }

    public void ResetPotential()
    {
        int basePoints = 5;
        int pointsPerLevel = 5;
        int totalPoints = basePoints + (level - 1) * pointsPerLevel;
        STR = 0; DEX = 0; CON = 0; INT = 0;
        potentialPoints = totalPoints;
        OnStatsUpdated?.Invoke();
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

    public void PlayLevelUpEffect() => SoundEffectManager.Play("LevelUp");

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
            if (!netIsOnBattle.Value) return false;

            if (AreaController.currentArea != null && AreaController.currentArea.mapType == MapType.SafeZone) return false;
            if (InventoryController.Instance == null || classController == null) return false;

            int weaponSlotIndex = classController.IsKnightActive ? 2003 : 2103;

            return InventoryController.Instance.GetInventoryItemsData()
                .Any(item => item.isEquipped && item.slotIndex == weaponSlotIndex);
        }
    }

    public bool IsPotionOnCooldown() => potionCooldownTimer > 0;
    public void TriggerPotionCooldown() => potionCooldownTimer = potionCooldownDuration;
    public float GetPotionCooldownRemaining() => potionCooldownTimer;
}